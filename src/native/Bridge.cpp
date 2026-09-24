#include "Bridge.h"
#include <mutex>
#include <cstring>

namespace {
std::mutex gate;
int state = 0, token = 0, flip = 0, leftError = 0, rightError = 0, frames = 0;
DWORD renderThread = 0;
SubmitFn submit = nullptr;
WaitPosesFn waitPoses = nullptr;
VrTrackedPose renderPoses[64] {};
int poseError = 0;
ID3D11Texture2D* eyes[2] = {};
ID3D11Texture2D* copies[3][2] = {};
ID3D11Query* completed = nullptr;
ID3D11DeviceContext* context = nullptr;
void releaseResources() {
    for (auto& pair : copies) for (auto& texture : pair) { if (texture) texture->Release(); texture = nullptr; }
    if (completed) completed->Release();
    completed = nullptr;
    for (auto& eye : eyes) { if (eye) eye->Release(); eye = nullptr; }
    if (context) context->Release();
    context = nullptr;
    submit = nullptr; waitPoses = nullptr; state = 0;
}
void __stdcall renderEvent(int eventId) {
    std::lock_guard<std::mutex> lock(gate);
    if ((state != 2 && state != 4) || eventId != token) return;
    if (renderThread != 0 && renderThread != GetCurrentThreadId())
    { leftError = rightError = -1008; state = 1; return; }
    renderThread = GetCurrentThreadId();
    if (state == 4)
    {
        state = 3;
        poseError = waitPoses(renderPoses, 64, nullptr, 0);
        state = 5;
        return;
    }
    state = 3;
    renderThread = GetCurrentThreadId();
    // Unity dispatches this callback after both Camera.Render command streams.
    // All immediate-context work and SteamVR submissions now share that thread.
    const int slot = frames % 3;
    for (int i = 0; i < 2; ++i) context->CopyResource(copies[slot][i], eyes[i]);
    context->End(completed);
    context->Flush();
    // Flush alone is asynchronous. Never submit a copy whose GPU work is pending.
    const ULONGLONG deadline = GetTickCount64() + 250;
    HRESULT result;
    while ((result = context->GetData(completed, nullptr, 0, D3D11_ASYNC_GETDATA_DONOTFLUSH)) == S_FALSE && GetTickCount64() < deadline) Sleep(0);
    if (result != S_OK) { leftError = rightError = -1006; state = 1; return; }
    VrBounds bounds { 0.f, flip ? 1.f : 0.f, 1.f, flip ? 0.f : 1.f };
    VrTexture left { copies[slot][0], 0, 0 }, right { copies[slot][1], 0, 0 };
    leftError = submit(0, &left, &bounds, 0);
    rightError = leftError == 0 ? submit(1, &right, &bounds, 0) : -1005;
    // Unity has not presented its companion window here. Do not call the
    // optional PostPresentHandoff before Present; the next WaitGetPoses paces us.
    ++frames;
    state = 1;
}
}
API int __cdecl ConfigureBridge(void* submitPtr, void* waitPtr, void* left, void* right) {
    std::lock_guard<std::mutex> lock(gate);
    if (state != 0) return -1001;
    if (!submitPtr || !waitPtr || !left || !right || left == right) return -1002;
    void* inputs[2] { left, right };
    ID3D11Device* devices[2] {};
    for (int i = 0; i < 2; ++i) {
        // QueryInterface owns a reference until TryDetach drains queued work.
        HRESULT hr = static_cast<IUnknown*>(inputs[i])->QueryInterface(__uuidof(ID3D11Texture2D), reinterpret_cast<void**>(&eyes[i]));
        if (FAILED(hr)) { releaseResources(); if (devices[0]) devices[0]->Release(); return -1003; }
        eyes[i]->GetDevice(&devices[i]);
    }
    bool sameDevice = devices[0] == devices[1];
    D3D11_TEXTURE2D_DESC desc[2] {};
    eyes[0]->GetDesc(&desc[0]); eyes[1]->GetDesc(&desc[1]);
    devices[0]->GetImmediateContext(&context);
    if (!sameDevice || desc[0].SampleDesc.Count != 1 || desc[1].SampleDesc.Count != 1 ||
        desc[0].Width != desc[1].Width || desc[0].Height != desc[1].Height || desc[0].Format != desc[1].Format) {
        devices[0]->Release(); devices[1]->Release(); releaseResources(); return -1004;
    }
    HRESULT allocated = S_OK;
    D3D11_TEXTURE2D_DESC copyDesc = desc[0];
    copyDesc.Usage = D3D11_USAGE_DEFAULT;
    copyDesc.CPUAccessFlags = copyDesc.MiscFlags = 0;
    copyDesc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
    for (auto& pair : copies) for (auto& texture : pair)
        if (SUCCEEDED(allocated)) allocated = devices[0]->CreateTexture2D(&copyDesc, nullptr, &texture);
    D3D11_QUERY_DESC queryDesc { D3D11_QUERY_EVENT, 0 };
    if (SUCCEEDED(allocated)) allocated = devices[0]->CreateQuery(&queryDesc, &completed);
    devices[0]->Release(); devices[1]->Release();
    if (FAILED(allocated)) { releaseResources(); return -1007; }
    submit = reinterpret_cast<SubmitFn>(submitPtr);
    waitPoses = reinterpret_cast<WaitPosesFn>(waitPtr);
    poseError = 0;
    frames = leftError = rightError = 0; renderThread = 0;
    state = 1;
    return 0;
}
API int __cdecl QueueFrame(int eventId, int verticalFlip) {
    std::unique_lock<std::mutex> lock(gate, std::try_to_lock);
    if (!lock.owns_lock()) return 0;
    if (state != 6) return 0;
    token = eventId; flip = verticalFlip != 0; state = 2;
    return 1;
}
API RenderFn __cdecl GetRenderEventFunc() { return renderEvent; }
API int __cdecl GetBridgeVersion() { return 400; }
API int __cdecl QueuePose(int eventId) {
    std::unique_lock<std::mutex> lock(gate, std::try_to_lock);
    if (!lock.owns_lock() || state != 1) return 0;
    token = eventId; state = 4; return 1;
}
API int __cdecl GetPreparedPose(float* matrix, int* valid) {
    std::unique_lock<std::mutex> lock(gate, std::try_to_lock);
    if (!lock.owns_lock() || state != 5) return 0;
    if (poseError != 0) { state = 1; return -poseError; }
    std::memcpy(matrix, renderPoses[0].matrix, sizeof(renderPoses[0].matrix));
    *valid = renderPoses[0].connected && renderPoses[0].valid ? 1 : 0;
    state = *valid ? 6 : 1;
    return 1;
}
API int __cdecl CancelPreparedPose() {
    std::unique_lock<std::mutex> lock(gate, std::try_to_lock);
    if (!lock.owns_lock() || state == 2 || state == 3 || state == 4) return 0;
    if (state == 5 || state == 6) state = 1;
    return 1;
}
API int __cdecl TryDetach() {
    std::unique_lock<std::mutex> lock(gate, std::try_to_lock);
    if (!lock.owns_lock()) return 0;
    if (state == 2 || state == 4) return 0; // Queued Unity callbacks still own resources.
    releaseResources(); return 1;
}
API void __cdecl GetBridgeStatus(int* s, int* l, int* r, int* count, unsigned long* tid) {
    std::unique_lock<std::mutex> lock(gate, std::try_to_lock);
    // Never wait on compositor/driver work from Unity's main thread.
    // Do not read shared non-atomic fields without owning the lock.
    if (!lock.owns_lock()) { *s = 3; *l = *r = *count = 0; *tid = 0; return; }
    *s = state; *l = leftError; *r = rightError; *count = frames; *tid = renderThread;
}
