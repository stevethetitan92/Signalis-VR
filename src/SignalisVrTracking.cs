using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Collections;
using MelonLoader;
using UnityEngine;
using Valve.VR;

[assembly: MelonInfo(typeof(SignalisVrTracking.Tracking), "SIGNALIS VR Thread Fix", "0.4.2", "Local development")]
[assembly: MelonGame("rose-engine", "SIGNALIS")]
namespace SignalisVrTracking
{
    public sealed partial class Tracking : MelonMod
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryW(string path);
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int key);
        private readonly HotkeyEdge diagnosticConnect = new HotkeyEdge();
        private readonly HotkeyEdge diagnosticRender = new HotkeyEdge();
        private readonly HotkeyEdge diagnosticSubmit = new HotkeyEdge();
        private readonly HotkeyEdge diagnosticStop = new HotkeyEdge();
        [DllImport("SignalisVrRenderBridge", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ConfigureBridge(IntPtr submit, IntPtr wait, IntPtr left, IntPtr right);
        [DllImport("SignalisVrRenderBridge", CallingConvention = CallingConvention.Cdecl)]
        private static extern int QueueFrame(int eventId, int flip);
        [DllImport("SignalisVrRenderBridge", CallingConvention = CallingConvention.Cdecl)]
        private static extern int QueuePose(int eventId);
        [DllImport("SignalisVrRenderBridge", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetPreparedPose([Out] float[] matrix, out int valid);
        [DllImport("SignalisVrRenderBridge", CallingConvention = CallingConvention.Cdecl)]
        private static extern int CancelPreparedPose();
        private readonly float[] preparedMatrix = new float[12];
        [DllImport("SignalisVrRenderBridge", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetRenderEventFunc();
        [DllImport("SignalisVrRenderBridge", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetBridgeVersion();
        [DllImport("SignalisVrRenderBridge", CallingConvention = CallingConvention.Cdecl)]
        private static extern void GetBridgeStatus(out int state, out int leftError, out int rightError, out int frames, out uint renderThread);
        private CVRSystem system;
        private readonly TrackedDevicePose_t[] poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private Camera camera;
        private Quaternion originalRotation, neutral;
        private Quaternion stereoRotation;
        private bool recenterPending;
        private float nextPoseLog, nextFrameLog;
        private bool active;
        private bool waitingForGameplay;
        private float nextCameraCheck;
        private bool wasTracked;
        private bool stereo;
        private CVRCompositor compositor;
        private readonly Camera[] eyes = new Camera[2];
        private readonly RenderTexture[] textures = new RenderTexture[2];
        private readonly IntPtr[] textureHandles = new IntPtr[2];
        private readonly TrackedDevicePose_t[] gamePoses = new TrackedDevicePose_t[0];
        private bool submitted;
        private bool flipVertical = true;
        private bool originalBackground;
        private int oldVsync, oldFrameRate;
        private bool bridgeConfigured, stopping;
        private bool allocationAttempted;
        private int resumeFrame;
        private IntPtr renderCallback;
        private int eventId = 1000;
        // Initial estimate from the first-person camera height; needs headset calibration.
        private const float UnitsPerMetre = 5f;
        private readonly DiagnosticFlow diagnostic = new DiagnosticFlow();
        private float nextFocusLog;
        private int diagnosticFrames;
        private bool traceFrame;
        public override void OnApplicationStart()
        {
            MelonLogger.Msg("STABILITY 0.4.2: F6 connect; F9 render locally; F10 submit for 300 seconds; F12 stop. These keys work without game keyboard focus. F1 still enables first person.");
        }
        public override void OnSceneWasLoaded(int index, string name)
        {
            Stop();
        }
        public override void OnApplicationQuit()
        {
            Stop();
            MelonLogger.Msg("Exit: VR resources are retained for process cleanup; no explicit compositor teardown.");
        }
        public override void OnUpdate()
        {
            try
            {
                // Direct Windows key edges do not depend on Unity keyboard focus.
                bool connectPressed = diagnosticConnect.Poll((GetAsyncKeyState(0x75) & 0x8000) != 0);
                bool renderPressed = diagnosticRender.Poll((GetAsyncKeyState(0x78) & 0x8000) != 0);
                bool submitPressed = diagnosticSubmit.Poll((GetAsyncKeyState(0x79) & 0x8000) != 0);
                bool stopPressed = diagnosticStop.Poll((GetAsyncKeyState(0x7B) & 0x8000) != 0);
                if (connectPressed || renderPressed || submitPressed || stopPressed)
                    MelonLogger.Msg("DIAG physical hotkey: " + (stopPressed ? "stop" : connectPressed ? "connect" : renderPressed ? "render" : "submit"));
                if (stopping) { FinishStop(); return; }
                LogVrFocus();
                if (stopPressed) { MelonLogger.Msg("DIAG: manual stop."); Stop(); return; }
                if (diagnostic.Expired(Time.realtimeSinceStartup))
                { MelonLogger.Msg("DIAG: five-minute submission window finished; stopping without releasing resources."); Stop(); return; }
                if (bridgeConfigured && stereo) CheckRenderStatus();
                if (connectPressed)
                {
                    if (diagnostic.Stage != 0) { MelonLogger.Msg("DIAG: already connected. F9 is the next stage."); return; }
                    MelonLogger.Msg("DIAG A begin: OpenVR scene connection only; no eye allocation or submission.");
                    string library = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserLibs", "SignalisVrTracking", "openvr_api.dll");
                    if (LoadLibraryW(library) == IntPtr.Zero) throw new InvalidOperationException("Cannot load OpenVR runtime.");
                    EVRInitError error = EVRInitError.None;
                    if (system == null) system = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
                    if (error != EVRInitError.None || system == null) throw new InvalidOperationException("OpenVR connection: " + error);
                    diagnostic.Connected();
                    MelonLogger.Msg("DIAG A complete: connected. Observe dashboard, wait 20 seconds, then F9.");
                }
                if (renderPressed)
                {
                    if (diagnostic.Stage != 1) { MelonLogger.Msg("DIAG: F9 requires stage A first (F6). It does not toggle VR in this build."); return; }
                    MelonLogger.Msg("DIAG B begin: eye setup/render only; submission and WaitGetPoses disabled.");
                    Start(true);
                    if (!active || !stereo) return;
                    diagnostic.RenderReady();
                    diagnosticFrames = 0;
                    Application.targetFrameRate = 72;
                    MelonLogger.Msg("DIAG B active: images render locally only. Wait 20 seconds before F10.");
                }
                if (submitPressed)
                {
                    if (!diagnostic.BeginSubmit(Time.realtimeSinceStartup)) { MelonLogger.Msg("DIAG: F10 requires stage B (F9) first."); return; }
                    diagnosticFrames = 0;
                    MelonLogger.Msg("DIAG C begin: synchronized poses and native submission enabled for five minutes.");
                }
                if (active && Input.GetKeyDown(KeyCode.F7))
                {
                    if (waitingForGameplay) puzzleAnchorPending = true;
                    else recenterPending = true;
                    MelonLogger.Msg("Recenter requested for next valid tracked frame.");
                }
            }
            catch (Exception ex) { MelonLogger.Warning("Tracking stopped: " + ex.Message); Stop(); }
        }
        public override void OnLateUpdate()
        {
            if (!active || diagnostic.Stage < 2) return;
            try
            {
                if (waitingForGameplay)
                {
                    if (Time.realtimeSinceStartup < nextCameraCheck) return;
                    nextCameraCheck = Time.realtimeSinceStartup + 0.25f;
                    var restored = FindGameplayCamera();
                    if (restored == null) return;
                    camera = restored;
                    originalRotation = camera.transform.localRotation;
                    waitingForGameplay = false;
                    MelonLogger.Msg("Gameplay camera restored; stereo resumed automatically.");
                }
                if (camera == null || !camera.isActiveAndEnabled || camera.orthographic || camera.transform.parent == null || camera.transform.parent.name != "Character Root")
                { MelonLogger.Msg("DIAG: gameplay camera changed; test stopped. Stay out of puzzles for this test."); Stop(); return; }
                Quaternion pose;
                if (stereo && !CheckRenderStatus()) return;
                traceFrame = diagnosticFrames < 3;
                if (traceFrame) MelonLogger.Msg("DIAG stage " + diagnostic.Stage + " frame " + diagnosticFrames + ": pose call begin.");
                bool tracked = ReadPose(out pose, true);
                // A queued render-thread pose is asynchronous, not tracking loss.
                if (!tracked && diagnostic.CanSubmit) return;
                if (traceFrame) MelonLogger.Msg("DIAG: pose returned, valid=" + tracked);
                if (tracked != wasTracked) MelonLogger.Msg(tracked ? "Headset tracking acquired." : "Headset tracking lost; camera returned to neutral.");
                wasTracked = tracked;
                if (tracked && recenterPending)
                {
                    neutral = stereo ? Heading(pose) : pose;
                    recenterPending = false;
                    MelonLogger.Msg("Head tracking centered from current render pose.");
                }
                var baseRotation = stereo ? Heading(originalRotation) : originalRotation;
                var trackedLocal = tracked ? baseRotation * Quaternion.Inverse(neutral) * pose : baseRotation;
                if (stereo)
                {
                    // Keep headset rotation out of the camera controlled by FPv2.
                    // Otherwise other camera/control code can feed it back into the rig.
                    stereoRotation = camera.transform.parent.rotation * trackedLocal;
                }
                else camera.transform.localRotation = trackedLocal;
                if (tracked && Time.realtimeSinceStartup >= nextPoseLog)
                {
                    nextPoseLog = Time.realtimeSinceStartup + 2f;
                    var head = (Quaternion.Inverse(neutral) * pose).eulerAngles;
                    var view = stereo ? stereoRotation.eulerAngles : camera.transform.rotation.eulerAngles;
                    MelonLogger.Msg(string.Format(CultureInfo.InvariantCulture, "Pose diagnostic: relative head=({0:F1},{1:F1},{2:F1}), view=({3:F1},{4:F1},{5:F1})", head.x, head.y, head.z, view.x, view.y, view.z));
                }
                if (stereo && tracked) { RenderStereo(); diagnosticFrames++; }
            }
            catch (Exception ex) { MelonLogger.Warning("Tracking stopped: " + ex.Message); Stop(); }
        }
        private void Start(bool wantStereo)
        {
            if (stopping) { MelonLogger.Warning("Previous render event is still finishing. Retry after it drains."); return; }
            var go = GameObject.Find("__Prerequisites__/Character Origin/Character Root/Main Camera");
            if (go == null) { MelonLogger.Warning("Load gameplay and press F1 before enabling head tracking."); return; }
            var candidate = go.GetComponent<Camera>();
            if (candidate == null || candidate.orthographic || !candidate.isActiveAndEnabled)
            { MelonLogger.Warning("First-person gameplay camera is not active."); return; }
            string library = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserLibs", "SignalisVrTracking", "openvr_api.dll");
            if (!File.Exists(library)) throw new FileNotFoundException("Missing Valve runtime: " + library);
            if (LoadLibraryW(library) == IntPtr.Zero) throw new InvalidOperationException("Could not load OpenVR runtime, Windows error " + Marshal.GetLastWin32Error());
            EVRInitError error = EVRInitError.None;
            // Keep one scene connection for the process. F9 is pause/resume,
            // never an OpenVR shutdown followed by immediate reinitialization.
            if (system == null) system = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
            if (error != EVRInitError.None || system == null)
            { system = null; throw new InvalidOperationException("SteamVR connection failed: " + error + ". Connect Virtual Desktop and start SteamVR first."); }
            Quaternion pose;
            if (!ReadPose(out pose)) { Stop(); MelonLogger.Warning("No tracked headset. Put on the Quest, then press F6 to retry."); return; }
            camera = candidate;
            originalRotation = camera.transform.localRotation;
            neutral = wantStereo ? Heading(pose) : pose;
            recenterPending = true;
            nextPoseLog = nextFrameLog = 0f;
            active = true;
            wasTracked = true;
            if (wantStereo) SetupStereo();
            MelonLogger.Msg("DIAG: eye resources initialized; submission remains gated by stage C.");
        }
        private Camera FindGameplayCamera()
        {
            var go = GameObject.Find("__Prerequisites__/Character Origin/Character Root/Main Camera");
            if (go == null) return null;
            var found = go.GetComponent<Camera>();
            return found != null && found.isActiveAndEnabled && !found.orthographic ? found : null;
        }
        private void SuspendForGameplay()
        {
            if (!waitingForGameplay)
            {
                puzzleAnchorPending = true;
                puzzleLogged = false;
                MelonLogger.Msg("Gameplay camera unavailable; switching to live puzzle screen. F7 positions it ahead; F9 stops VR.");
            }
            waitingForGameplay = true;
            camera = null;
            nextCameraCheck = 0f;
        }
        private void SetupStereo()
        {
            if (bridgeConfigured)
            {
                if (eyes[0] == null || eyes[1] == null || textures[0] == null || textures[1] == null)
                    throw new InvalidOperationException("Retained VR resources were lost. Restart SIGNALIS before retrying.");
                EnableStereoSettings();
                MelonLogger.Msg("Stereo resumed using existing textures and SteamVR connection.");
                return;
            }
            if (allocationAttempted) throw new InvalidOperationException("A previous stereo setup failed. Restart SIGNALIS before retrying.");
            allocationAttempted = true;
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D11)
                throw new InvalidOperationException("This stereo prototype requires Direct3D 11.");
            compositor = OpenVR.Compositor;
            if (compositor == null) throw new InvalidOperationException("SteamVR compositor unavailable.");
            compositor.SetTrackingSpace(ETrackingUniverseOrigin.TrackingUniverseStanding);
            uint width = 0, height = 0;
            system.GetRecommendedRenderTargetSize(ref width, ref height);
            if (width == 0 || height == 0) throw new InvalidOperationException("SteamVR returned an invalid eye size.");
            float reduction = Math.Min(1f, 1600f / Math.Max(width, height));
            int w = Math.Max(64, (int)(width * reduction)), h = Math.Max(64, (int)(height * reduction));
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("SignalisVR_Eye_" + i);
                eyes[i] = go.AddComponent<Camera>();
                eyes[i].enabled = false;
                UnityEngine.Object.DontDestroyOnLoad(go);
                textures[i] = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                textures[i].name = "SignalisVR_Texture_" + i;
                textures[i].antiAliasing = 1;
                textures[i].useMipMap = false;
                if (!textures[i].Create()) throw new InvalidOperationException("Could not allocate eye texture.");
                textureHandles[i] = textures[i].GetNativeTexturePtr();
                if (textureHandles[i] == IntPtr.Zero) throw new InvalidOperationException("Eye texture has no native handle.");
            }
            string bridgePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserLibs", "SignalisVrTracking", "SignalisVrRenderBridge.dll");
            if (!File.Exists(bridgePath)) throw new FileNotFoundException("Missing render bridge: " + bridgePath);
            if (LoadLibraryW(bridgePath) == IntPtr.Zero) throw new InvalidOperationException("Cannot load render bridge: " + Marshal.GetLastWin32Error());
            if (GetBridgeVersion() != 400) throw new InvalidOperationException("Install both 0.4.0 DLLs together, then restart SIGNALIS.");
            EVRInitError tableError = EVRInitError.None;
            var table = OpenVR.GetGenericInterface("FnTable:" + OpenVR.IVRCompositor_Version, ref tableError);
            if (table == IntPtr.Zero || tableError != EVRInitError.None) throw new InvalidOperationException("Compositor function table: " + tableError);
            // Read actual native addresses, never managed delegate thunks on the render thread.
            var submitPointer = Marshal.ReadIntPtr(table, Marshal.OffsetOf(typeof(IVRCompositor), "Submit").ToInt32());
            var waitPointer = Marshal.ReadIntPtr(table, Marshal.OffsetOf(typeof(IVRCompositor), "WaitGetPoses").ToInt32());
            int configured = ConfigureBridge(submitPointer, waitPointer, textureHandles[0], textureHandles[1]);
            if (configured != 0) throw new InvalidOperationException("Render bridge rejected textures: " + configured);
            bridgeConfigured = true;
            renderCallback = GetRenderEventFunc();
            if (renderCallback == IntPtr.Zero) throw new InvalidOperationException("Render callback unavailable.");
            EnableStereoSettings();
            MelonLogger.Msg("Stereo eye textures: " + w + "x" + h + "; initial scale=" + UnitsPerMetre + " units/metre.");
        }
        private void EnableStereoSettings()
        {
            originalBackground = Application.runInBackground;
            oldVsync = QualitySettings.vSyncCount;
            oldFrameRate = Application.targetFrameRate;
            stereo = true;
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            submitted = false;
            int state, leftError, rightError; uint thread;
            GetBridgeStatus(out state, out leftError, out rightError, out resumeFrame, out thread);
        }
        private void RenderStereo()
        {
            for (int i = 0; i < 2; i++)
            {
                var eye = eyes[i];
                eye.CopyFrom(camera);
                eye.enabled = false;
                eye.stereoTargetEye = StereoTargetEyeMask.None;
                eye.targetTexture = textures[i];
                eye.rect = new Rect(0f, 0f, 1f, 1f);
                eye.clearFlags = CameraClearFlags.SolidColor;
                eye.backgroundColor = Color.black;
                var offset = system.GetEyeToHeadTransform((EVREye)i);
                eye.transform.position = camera.transform.position + stereoRotation * (new Vector3(offset.m3, offset.m7, -offset.m11) * UnitsPerMetre);
                var forward = new Vector3(-offset.m2, -offset.m6, offset.m10);
                var up = new Vector3(offset.m1, offset.m5, -offset.m9);
                eye.transform.rotation = stereoRotation * Quaternion.LookRotation(forward, up);
                float left = 0, right = 0, top = 0, bottom = 0;
                system.GetProjectionRaw((EVREye)i, ref left, ref right, ref top, ref bottom);
                float near = Math.Max(0.01f, camera.nearClipPlane);
                // OpenVR's raw vertical tangents point down; Unity's frustum points up.
                eye.projectionMatrix = Matrix4x4.Frustum(left * near, right * near, -bottom * near, -top * near, near, camera.farClipPlane);
                eye.ResetWorldToCameraMatrix();
                if (traceFrame) MelonLogger.Msg("DIAG: render eye " + i + " begin.");
                eye.Render();
                if (traceFrame) MelonLogger.Msg("DIAG: render eye " + i + " returned.");
            }
            SubmitEyes();
        }
        private void SubmitEyes()
        {
            if (!diagnostic.CanSubmit) return;
            if (traceFrame) MelonLogger.Msg("DIAG: queue native submission begin.");
            int nextEvent = ++eventId;
            if (QueueFrame(nextEvent, flipVertical ? 1 : 0) != 1) throw new InvalidOperationException("Previous frame has not drained.");
            // Ordered after Camera.Render on Unity's graphics command stream.
            // No compositor graphics calls run directly from this managed callback.
            GL.IssuePluginEvent(renderCallback, nextEvent);
            if (traceFrame) MelonLogger.Msg("DIAG: native event queued.");
        }
        private bool CheckRenderStatus()
        {
            int state, leftError, rightError, frames; uint thread;
            GetBridgeStatus(out state, out leftError, out rightError, out frames, out thread);
            if (Time.realtimeSinceStartup >= nextFrameLog)
            {
                nextFrameLog = Time.realtimeSinceStartup + 2f;
                MelonLogger.Msg("Frame diagnostic: completed=" + frames + ", state=" + state + ", left=" + leftError + ", right=" + rightError);
            }
            if (state == 2 || state == 3 || state == 4) return false;
            if (leftError != 0 || rightError != 0) throw new InvalidOperationException("Render-thread submission failed: left=" + leftError + ", right=" + rightError);
            if (!submitted && frames > resumeFrame)
            {
                submitted = true;
                MelonLogger.Msg("Both eye textures accepted on Unity render callback thread " + thread + ". Visual verification still required.");
            }
            return state == 1 || state == 5;
        }
        private bool ReadPose(out Quaternion rotation, bool synchronize = false)
        {
            rotation = Quaternion.identity;
            if (system == null) return false;
            if (stereo && synchronize && diagnostic.CanSubmit)
            {
                int valid;
                int result = GetPreparedPose(preparedMatrix, out valid);
                if (result < 0) throw new InvalidOperationException("Render-thread SteamVR poses: " + result);
                if (result == 0)
                {
                    int poseEvent = ++eventId;
                    if (QueuePose(poseEvent) == 1) GL.IssuePluginEvent(renderCallback, poseEvent);
                    return false;
                }
                if (valid == 0) return false;
                rotation = Quaternion.LookRotation(new Vector3(-preparedMatrix[2], -preparedMatrix[6], preparedMatrix[10]), new Vector3(preparedMatrix[1], preparedMatrix[5], -preparedMatrix[9]));
                return true;
            }
            else system.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0f, poses);
            var pose = poses[OpenVR.k_unTrackedDeviceIndex_Hmd];
            if (!pose.bDeviceIsConnected || !pose.bPoseIsValid) return false;
            var m = pose.mDeviceToAbsoluteTracking;
            // OpenVR uses right-handed coordinates; Unity uses left-handed coordinates.
            rotation = Quaternion.LookRotation(new Vector3(-m.m2, -m.m6, m.m10), new Vector3(m.m1, m.m5, -m.m9));
            return true;
        }
        // Recenter heading only: cancelling headset pitch/roll tilts the yaw axis.
        private static Quaternion Heading(Quaternion q)
        {
            double yaw = HeadingMath.Yaw(q.x, q.y, q.z, q.w);
            return new Quaternion(0f, (float)Math.Sin(yaw / 2), 0f, (float)Math.Cos(yaw / 2));
        }
        private void Stop()
        {
            diagnostic.Stop();
            waitingForGameplay = false;
            if (puzzleRenderer != null) puzzleRenderer.enabled = false;
            if (stereo)
            {
                stereo = false;
                Application.runInBackground = originalBackground;
                QualitySettings.vSyncCount = oldVsync;
                Application.targetFrameRate = oldFrameRate;
            }
            if (active && camera != null && !camera.orthographic && camera.transform.parent != null && camera.transform.parent.name == "Character Root")
                camera.transform.localRotation = originalRotation;
            active = false;
            camera = null;
            stopping = true;
            FinishStop();
        }
        private void FinishStop()
        {
            if (bridgeConfigured)
            {
                int state, leftError, rightError, frames; uint thread;
                GetBridgeStatus(out state, out leftError, out rightError, out frames, out thread);
                if (state == 2 || state == 3 || state == 4) return;
                if (CancelPreparedPose() == 0) return;
            }
            // A completed Submit callback does not prove external driver/encoder
            // consumers have finished with a texture. Reuse the same bounded pair
            // until process exit instead of releasing/recreating on each toggle.
            stopping = false;
            MelonLogger.Msg("DIAG stopped; resources retained. Restart game for an independent test.");
        }
        private void LogVrFocus()
        {
            if (system == null || Time.realtimeSinceStartup < nextFocusLog) return;
            nextFocusLog = Time.realtimeSinceStartup + 2f;
            Quaternion pose;
            bool valid = ReadPose(out pose, false);
            bool dashboard = OpenVR.Overlay != null && OpenVR.Overlay.IsDashboardVisible();
            MelonLogger.Msg("DIAG status: stage=" + diagnostic.Stage + ", tracked=" + valid + ", dashboard=" + dashboard + ", inputAvailable=" + system.IsInputAvailable() + ", localFrames=" + diagnosticFrames);
        }
    }
}
