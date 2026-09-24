#include "Bridge.h"
#include <thread>
#include <cstdio>
#include <cstdlib>
#include <atomic>
#include <future>
#include <queue>
#include <condition_variable>
#include <functional>

static void check(bool ok, const char* message) {
    if (!ok) { std::fprintf(stderr, "FAIL: %s\n", message); std::exit(1); }
}
static int calls = 0, failEye = -1, poseCalls = 0, poseFailure = 0;
static DWORD poseThread = 0;
static bool poseValid = true;
class RenderWorker {
    std::mutex mutex;
    std::condition_variable cv;
    std::queue<std::packaged_task<void()>> jobs;
    bool done = false;
    std::thread thread;
public:
    RenderWorker() : thread([this]() {
        for (;;) {
            std::packaged_task<void()> job;
            { std::unique_lock<std::mutex> lock(mutex); cv.wait(lock, [this] { return done || !jobs.empty(); });
              if (done && jobs.empty()) return; job = std::move(jobs.front()); jobs.pop(); }
            job();
        }
    }) {}
    std::future<void> Send(std::function<void()> action) {
        std::packaged_task<void()> job(action); auto result = job.get_future();
        { std::lock_guard<std::mutex> lock(mutex); jobs.push(std::move(job)); }
        cv.notify_one(); return result;
    }
    ~RenderWorker() { { std::lock_guard<std::mutex> lock(mutex); done = true; } cv.notify_one(); thread.join(); }
};
static bool expectedFlip = false;
static DWORD caller = 0;
static std::atomic<bool> simulateStall { false }, stallEntered { false };
static ID3D11Texture2D* sourceHandles[2] {};
static int __stdcall fakeSubmit(int eye, const VrTexture* texture, const VrBounds* bounds, int flags) {
    if (eye == 0 && simulateStall.load()) { stallEntered = true; Sleep(1000); }
    check(GetCurrentThreadId() != caller, "submission must use callback thread");
    check(GetCurrentThreadId() == poseThread, "WaitGetPoses and Submit must use the same render thread");
    check(texture && texture->handle && texture->type == 0 && flags == 0, "texture ABI");
    check(bounds->uMin == 0.f && bounds->uMax == 1.f && bounds->vMin == (expectedFlip ? 1.f : 0.f), "bounds ABI and flip");
    D3D11_TEXTURE2D_DESC desc {};
    static_cast<ID3D11Texture2D*>(texture->handle)->GetDesc(&desc);
    check(texture->handle != sourceHandles[eye], "submit dedicated copy, never Unity source");
    ID3D11Device* device = nullptr;
    ID3D11DeviceContext* context = nullptr;
    auto submitted = static_cast<ID3D11Texture2D*>(texture->handle);
    submitted->GetDevice(&device); device->GetImmediateContext(&context);
    auto stagingDesc = desc;
    stagingDesc.Usage = D3D11_USAGE_STAGING; stagingDesc.BindFlags = stagingDesc.MiscFlags = 0;
    stagingDesc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
    ID3D11Texture2D* staging = nullptr;
    check(SUCCEEDED(device->CreateTexture2D(&stagingDesc, nullptr, &staging)), "readback texture");
    context->CopyResource(staging, submitted);
    D3D11_MAPPED_SUBRESOURCE mapped {};
    check(SUCCEEDED(context->Map(staging, 0, D3D11_MAP_READ, 0, &mapped)), "readback map");
    check(*static_cast<unsigned int*>(mapped.pData) == 0xff336699u, "GPU copy preserves rendered pixels");
    context->Unmap(staging, 0); staging->Release(); context->Release(); device->Release();
    check(desc.Width == 64, "native retained texture after caller released reference");
    ++calls;
    return eye == failEye ? 102 : 0;
}
static int __stdcall fakeWait(VrTrackedPose* poses, unsigned int count, VrTrackedPose* game, unsigned int gameCount) {
    check(GetCurrentThreadId() != caller, "WaitGetPoses must not run on Unity main thread");
    check(count == 64 && game == nullptr && gameCount == 0, "pose ABI counts");
    poseThread = GetCurrentThreadId(); ++poseCalls;
    poses[0] = {}; poses[0].matrix[0] = poses[0].matrix[5] = poses[0].matrix[10] = 1;
    poses[0].matrix[3] = 0.125f; poses[0].valid = poseValid; poses[0].connected = true;
    return poseFailure;
}
int main(int argc, char**) {
    caller = GetCurrentThreadId();
    RenderWorker worker;
    int poseToken = -10000;
    auto prepare = [&]() {
        check(QueuePose(--poseToken) == 1, "queue render-thread pose");
        check(QueuePose(--poseToken) == 0, "reject duplicate pending pose");
        check(TryDetach() == 0, "retain resources while pose callback queued");
        const int queuedToken = poseToken + 1;
        worker.Send([=]() { GetRenderEventFunc()(queuedToken); }).get();
        float matrix[12] {}; int valid;
        check(GetPreparedPose(matrix, &valid) == 1 && valid == 1 && matrix[3] == 0.125f, "copy prepared headset pose to main thread");
    };
    ID3D11Device* device = nullptr;
    ID3D11DeviceContext* ctx = nullptr;
    HRESULT hr = D3D11CreateDevice(nullptr, argc > 1 ? D3D_DRIVER_TYPE_HARDWARE : D3D_DRIVER_TYPE_WARP, nullptr, 0, nullptr, 0, D3D11_SDK_VERSION, &device, nullptr, &ctx);
    check(SUCCEEDED(hr), "create software D3D11 device");
    auto makeEye = [&]() {
        D3D11_TEXTURE2D_DESC d {};
        d.Width = d.Height = 64; d.MipLevels = d.ArraySize = d.SampleDesc.Count = 1;
        d.Format = DXGI_FORMAT_R8G8B8A8_UNORM; d.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
        ID3D11Texture2D* eye = nullptr;
        unsigned int pixels[64 * 64];
        for (auto& pixel : pixels) pixel = 0xff336699u;
        D3D11_SUBRESOURCE_DATA data { pixels, 64 * 4, 0 };
        check(SUCCEEDED(device->CreateTexture2D(&d, &data, &eye)), "create texture");
        return eye;
    };
    check(ConfigureBridge(nullptr, nullptr, nullptr, nullptr) == -1002, "reject null configuration");
    for (int cycle = 0; cycle < 4; ++cycle) {
        auto left = makeEye(), right = makeEye();
        sourceHandles[0] = left; sourceHandles[1] = right;
        check(ConfigureBridge(reinterpret_cast<void*>(fakeSubmit), reinterpret_cast<void*>(fakeWait), left, right) == 0, "configure");
        check(ConfigureBridge(reinterpret_cast<void*>(fakeSubmit), nullptr, left, right) == -1001, "reject replacement while configured");
        left->Release(); right->Release();
        int before = calls;
        failEye = cycle >= 2 ? cycle - 2 : -1;
        expectedFlip = cycle == 1;
        check(QueueFrame(123, 0) == 0, "reject submission without prepared pose");
        prepare();
        check(QueueFrame(42 + cycle, expectedFlip ? 1 : 0) == 1, "queue frame");
        check(QueueFrame(99, 0) == 0, "reject second in-flight frame");
        check(TryDetach() == 0, "shutdown must wait for pending render event");
        auto callback = GetRenderEventFunc();
        worker.Send([&]() { callback(-1); callback(42 + cycle); callback(42 + cycle); }).get();
        int state, l, r, frames; unsigned long tid;
        GetBridgeStatus(&state, &l, &r, &frames, &tid);
        check(state == 1 && frames == 1 && tid != caller, "completion fence and callback thread");
        check(calls - before == (failEye == 0 ? 1 : 2), "ignore stale/duplicate events, propagate errors");
        check(failEye == 0 ? (l == 102 && r == -1005) : (failEye == 1 ? (l == 0 && r == 102) : (l == 0 && r == 0)), "submit status");
        if (cycle == 0) {
            // Pause does not detach. Resume uses exactly the same native textures.
            for (int repeat = 0; repeat < 2000; ++repeat) {
                prepare();
                check(QueueFrame(100 + repeat, 0) == 1, "resume retained resources");
                worker.Send([&]() { callback(100 + repeat); }).get();
            }
            GetBridgeStatus(&state, &l, &r, &frames, &tid);
            check(state == 1 && frames == 2001 && l == 0 && r == 0, "2000 repeated frames reuse retained textures");
        }
        check(TryDetach() == 1 && TryDetach() == 1, "detach after completion and idempotent cleanup");
    }
    check(poseCalls == 2004, "each submitted pair has one preceding pose wait");
    auto stallLeft = makeEye(), stallRight = makeEye();
    sourceHandles[0] = stallLeft; sourceHandles[1] = stallRight;
    failEye = -1; expectedFlip = false; simulateStall = true;
    check(ConfigureBridge(reinterpret_cast<void*>(fakeSubmit), reinterpret_cast<void*>(fakeWait), stallLeft, stallRight) == 0, "configure stall regression");
    stallLeft->Release(); stallRight->Release();
    prepare();
    check(QueueFrame(9999, 0) == 1, "queue stalled frame");
    auto stalled = worker.Send([]() { GetRenderEventFunc()(9999); });
    const auto deadline = GetTickCount64() + 5000;
    while (!stallEntered.load() && GetTickCount64() < deadline) Sleep(1);
    check(stallEntered.load(), "fake compositor reached blocked submit");
    const auto started = GetTickCount64();
    int busyState, busyLeft, busyRight, busyFrames; unsigned long busyThread;
    GetBridgeStatus(&busyState, &busyLeft, &busyRight, &busyFrames, &busyThread);
    check(busyState == 3, "report executing without waiting for submit");
    check(QueueFrame(10000, 0) == 0 && TryDetach() == 0, "do not queue or release during blocked submit");
    check(GetTickCount64() - started < 250, "main-thread bridge calls remain responsive during a one-second compositor stall");
    stalled.get();
    simulateStall = false;
    poseFailure = 101;
    check(QueuePose(11000) == 1, "queue failed pose");
    worker.Send([]() { GetRenderEventFunc()(11000); }).get();
    float invalidMatrix[12] {}; int validity;
    check(GetPreparedPose(invalidMatrix, &validity) == -101, "propagate focus error without rendering stale pose");
    check(QueueFrame(11001, 0) == 0, "failed pose cannot authorize submission");
    poseFailure = 0; poseValid = false;
    check(QueuePose(11002) == 1, "queue untracked pose");
    worker.Send([]() { GetRenderEventFunc()(11002); }).get();
    check(GetPreparedPose(invalidMatrix, &validity) == 1 && validity == 0, "report tracking loss");
    check(QueueFrame(11003, 0) == 0, "untracked pose cannot authorize submission");
    poseValid = true;
    check(QueuePose(11004) == 1 && CancelPreparedPose() == 0, "cancellation drains queued callback first");
    worker.Send([]() { GetRenderEventFunc()(11004); }).get();
    check(CancelPreparedPose() == 1, "cancel prepared pose without teardown");
    prepare();
    check(CancelPreparedPose() == 1 && QueueFrame(11005, 0) == 0, "cancel consumed pose revokes submission");
    check(QueuePose(11006) == 1, "queue wrong-thread check");
    GetRenderEventFunc()(11006);
    GetBridgeStatus(&busyState, &busyLeft, &busyRight, &busyFrames, &busyThread);
    check(busyLeft == -1008 && busyRight == -1008, "reject graphics call on a different thread");
    check(TryDetach() == 1, "cleanup after stalled submit finishes");
    std::puts("PASS: same-thread pose/submission ordering, pose ABI, tracking loss, focus errors, cancellation, wrong-thread rejection.");
    std::puts("PASS: stalled compositor does not block status, queue, or detach callers.");
    ctx->Release(); device->Release();
    std::puts("PASS: 2000 repeated frames, GPU completion, copy pixel readback, native references, render-thread dispatch, bounds, queue fencing, error handling, shutdown drain.");
}
