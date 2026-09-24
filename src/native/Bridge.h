#pragma once
#include <windows.h>
#include <d3d11.h>

struct VrTexture { void* handle; int type; int colorSpace; };
struct VrBounds { float uMin, vMin, uMax, vMax; };
using SubmitFn = int (__stdcall *)(int, const VrTexture*, const VrBounds*, int);
struct VrTrackedPose { float matrix[12]; float velocity[3]; float angularVelocity[3]; int trackingResult; bool valid; bool connected; };
static_assert(sizeof(VrTrackedPose) == 80, "OpenVR x64 pose ABI");
using WaitPosesFn = int (__stdcall *)(VrTrackedPose*, unsigned int, VrTrackedPose*, unsigned int);
using RenderFn = void (__stdcall *)(int);
static_assert(sizeof(VrTexture) == 16, "OpenVR x64 texture ABI");
static_assert(sizeof(VrBounds) == 16, "OpenVR bounds ABI");

#define API extern "C" __declspec(dllexport)
// State: 0 detached, 1 idle, 2 submit queued, 3 executing,
// 4 pose queued, 5 pose ready, 6 pose consumed / ready for submission.
API int __cdecl ConfigureBridge(void* submit, void* waitPoses, void* left, void* right);
API int __cdecl QueueFrame(int eventId, int flip);
API int __cdecl QueuePose(int eventId);
API int __cdecl GetPreparedPose(float* matrix, int* valid);
API int __cdecl CancelPreparedPose();
API RenderFn __cdecl GetRenderEventFunc();
API int __cdecl GetBridgeVersion();
API int __cdecl TryDetach();
API void __cdecl GetBridgeStatus(int* state, int* leftError, int* rightError, int* frames, unsigned long* renderThread);
