using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Collections;
using MelonLoader;
using UnityEngine;
using Valve.VR;

[assembly: MelonInfo(typeof(SignalisVrTracking.Tracking), "SIGNALIS VR Object Highlights", "0.7.166", "Local development")]
[assembly: MelonGame("rose-engine", "SIGNALIS")]
namespace SignalisVrTracking
{
    public sealed partial class Tracking : MelonMod
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int key);
        private readonly HotkeyEdge diagnosticConnect = new HotkeyEdge();
        private readonly HotkeyEdge diagnosticRender = new HotkeyEdge();
        private readonly HotkeyEdge diagnosticSubmit = new HotkeyEdge();
        private readonly HotkeyEdge diagnosticStop = new HotkeyEdge();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int ConfigureBridgeDelegate(IntPtr submit, IntPtr wait, IntPtr left, IntPtr right);
        private static ConfigureBridgeDelegate ConfigureBridgeBound;
        private static int ConfigureBridge(IntPtr submit, IntPtr wait, IntPtr left, IntPtr right)
        {
            if (ConfigureBridgeBound == null) ConfigureBridgeBound = SignalisVrTracking.NativeLibraries.Bind<ConfigureBridgeDelegate>(true, "ConfigureBridge");
            return ConfigureBridgeBound(submit, wait, left, right);
        }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int QueueFrameDelegate(int eventId, int flip,int unityFrame);
        private static QueueFrameDelegate QueueFrameBound;
        private static int QueueFrame(int eventId, int flip)
        {
            if (QueueFrameBound == null) QueueFrameBound = SignalisVrTracking.NativeLibraries.Bind<QueueFrameDelegate>(true, "QueueFrameWithFrame");
            return QueueFrameBound(eventId, flip,Time.frameCount);
        }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int QueuePoseDelegate(int eventId,int unityFrame);
        private static QueuePoseDelegate QueuePoseBound;
        private static int QueuePose(int eventId)
        {
            if (QueuePoseBound == null) QueuePoseBound = SignalisVrTracking.NativeLibraries.Bind<QueuePoseDelegate>(true, "QueuePoseWithFrame");
            return QueuePoseBound(eventId,Time.frameCount);
        }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetPreparedPoseDelegate([Out] float[] matrix, out int valid,int unityFrame,out long frameId);
        private static GetPreparedPoseDelegate GetPreparedPoseBound;
        private static int GetPreparedPose([Out] float[] matrix, out int valid)
        {
            if (GetPreparedPoseBound == null) GetPreparedPoseBound = SignalisVrTracking.NativeLibraries.Bind<GetPreparedPoseDelegate>(true, "GetPreparedPoseWithFrame");
            long id; int result=GetPreparedPoseBound(matrix,out valid,Time.frameCount,out id);
            if(result==1&&valid!=0)stereoTraceId=id;
            return result;
        }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int CancelPreparedPoseDelegate();
        private static CancelPreparedPoseDelegate CancelPreparedPoseBound;
        private static int CancelPreparedPose()
        {
            if (CancelPreparedPoseBound == null) CancelPreparedPoseBound = SignalisVrTracking.NativeLibraries.Bind<CancelPreparedPoseDelegate>(true, "CancelPreparedPose");
            return CancelPreparedPoseBound();
        }
        private readonly float[] preparedMatrix = new float[12];
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetRenderEventFuncDelegate();
        private static GetRenderEventFuncDelegate GetRenderEventFuncBound;
        private static IntPtr GetRenderEventFunc()
        {
            if (GetRenderEventFuncBound == null) GetRenderEventFuncBound = SignalisVrTracking.NativeLibraries.Bind<GetRenderEventFuncDelegate>(true, "GetRenderEventFunc");
            return GetRenderEventFuncBound();
        }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int GetBridgeVersionDelegate();
        private static GetBridgeVersionDelegate GetBridgeVersionBound;
        private static int GetBridgeVersion()
        {
            if (GetBridgeVersionBound == null) GetBridgeVersionBound = SignalisVrTracking.NativeLibraries.Bind<GetBridgeVersionDelegate>(true, "GetBridgeVersion");
            return GetBridgeVersionBound();
        }
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void GetBridgeStatusDelegate(out int state, out int leftError, out int rightError, out int frames, out uint renderThread);
        private static GetBridgeStatusDelegate GetBridgeStatusBound;
        private static void GetBridgeStatus(out int state, out int leftError, out int rightError, out int frames, out uint renderThread)
        {
            if (GetBridgeStatusBound == null) GetBridgeStatusBound = SignalisVrTracking.NativeLibraries.Bind<GetBridgeStatusDelegate>(true, "GetBridgeStatus");
            GetBridgeStatusBound(out state, out leftError, out rightError, out frames, out renderThread);
        }
        private CVRSystem system;
        private readonly TrackedDevicePose_t[] poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private Camera camera;
        private Quaternion originalRotation, neutral;
        private Quaternion stereoRotation;
        private Vector3 trackedHeadPosition, neutralHeadPosition, stereoPosition;
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
        private readonly CameraRecovery recovery = new CameraRecovery();
        private bool menuCaptureReady;
        private bool pauseMenuVisible;
        private bool bookScreenVisible;
        private bool dialogueScreenVisible;
        private readonly HotkeyEdge menuFlip = new HotkeyEdge();
        private readonly HotkeyEdge recenterKey = new HotkeyEdge();
        private float nextFocusLog;
        private int diagnosticFrames;
        private bool traceFrame;
        public override void OnApplicationStart()
        {
            LoadModSettings();InstallDependencyGuards();InstallEnemyEyeLifecycle();InstallQuickNewGame();InstallHeldModules();InstallInventoryCapacity();InstallMacheteRemoval();InstallInventorySmg();InstallInventoryWheel();InstallGameplayCheats();
            if(desktopMouseOnly) {
                MarkerLifecycle.Install();InstallControllerWeapon();InstallMouseWeaponTest();MarkerSuppression.Install();
                MelonLogger.Msg("DESKTOP MENU TEST: No VR startup. F4 mod menu; F1 first person; F5 mouse aiming; RMB aim/LMB fire. Hand HUD uses a screen-side preview anchor.");
                return;
            }
            try
            {
                NativeLibraries.Initialize();
                if (GetBridgeVersion() != 403) throw new InvalidOperationException("Render bridge ABI mismatch: expected 403.");
                MelonLogger.Msg("VR dependencies verified: " + NativeLibraries.LibraryDirectory);
            }
            catch (Exception ex) { MelonLogger.Warning("VR startup check failed: " + ex.Message); }
            MarkerLifecycle.Install();
            InstallControllerWeapon();
            InstallMouseWeaponTest();InstallFlashlightRenderScope();
            MarkerSuppression.Install();
            MelonCoroutines.Start(PuzzleFrames());
            MelonLogger.Msg("HEADSET CRYO ANCHORS 0.7.10: Automatic first-person/stereo; F10 retry; F6/F9 diagnostics; F12 stop. These keys work without game keyboard focus. F1 still enables first person.");
        }
        public override void OnSceneWasLoaded(int index, string name)
        {
            ResetControllerPositionFilters();
            ClearComputerScreenIsolation();mapScanWork.Clear();mapScanLoaded=-1;mapScanPhase=-1;
            desktopMirrorFrame=-1;ResetPlayerScreenEffects();
            ResetDoorCamera();
            ClearPickupModels();ClearStarAxes();DestroySnowCursor();ClearWeaponMatte();ClearVrDeath();enemyHeadBones.Clear();shotCriticalTargets.Clear();ClearGunKnockback();DestroyVrMirrorState();DestroyEnemyEyeMaterials();LeaveCalibrationRoom(index<0);DestroyTestEnemy();CloseModMenu(false);ResetFlashlightPlacement();DestroyLaserPointTextures();DestroyControlsTab();DestroyCheatSheet();DestroyQuickSelect();DestroySwingProd();DestroyHdRounds();DestroyHdMeleeTextures();DestroyHdGunEffects();DestroyThermite();DestroyManualFlares();DestroyManualReload();DestroyModHud();DestroyEquippedWeaponVisual(true);DestroyControllerHands();handsFailed=false;nextHandsDiscovery=0f;
            RestoreMouseWeaponCamera();mouseWeaponCamera=null;nextMouseCameraSearch=0f;
            if(desktopMouseOnly){rightWeaponTime=-100f;rifleOwner=null;nextWeaponSearch=0f;HideRifleImpacts(true);MarkerSuppression.Reset();return;}
            rightWeaponTime=-100f;rifleOwner=null;nextWeaponSearch=0f;HideRifleImpacts(true);
            nativeEventContext = false; eventWalker=null;stereoEventControl=null;eventWalkReturnCamera=null;eventWalkStereo=false; snowWalkStereo = false; sceneRecoveryDraining = false; sceneRecoveryRequest = false; nextSceneRecovery = 0f; sceneCandidateId = 0; RestoreCinematicBars(); sceneStableSince = -1f; sceneRecoveryAttempts = 0;
            if (questMenuScene || (active && stereo && diagnostic.CanSubmit && !stopping)) {
                questMenuScene = true;
                recovery.Reset(); waitingForGameplay = false; menuCaptureReady = false;
                pauseMenuVisible = bookScreenVisible = dialogueScreenVisible = false;
                puzzleAnchorPending = true;
                // Persistent game-screen resources survive scene changes.
                // Clear scene-specific camera/input state without stopping submission.
                ReleaseQuestControls();
                camera = null;
                diagnostic.Stop();
                MelonLogger.Msg("QUEST MENU SCENE: preserving live screen across scene load: " + name);
            } else {
                bool requestedBeforeLoad = immersiveRequested;
                Stop();
                // Scene cleanup is not a user request to disable automatic startup.
                immersiveRequested = requestedBeforeLoad && !questInputStopped;
            }
            doorMarkerAnchors.Clear();
            MarkerSuppression.Reset();
            ClearBookBrackets();
            // Reuse imported gun assets; no bulk decoding/upload during scene loading.
        }
        public override void OnApplicationQuit()
        {
            if(desktopMouseOnly&&frameTraceWriter!=null)frameTraceWriter.Finish(1000);
            if(desktopMouseOnly){RestoreMouseWeaponCamera();HideRifleImpacts(true);return;}
            Stop();
            if(frameTraceWriter!=null && !frameTraceWriter.Finish(1000))MelonLogger.Warning("Frame trace writer still finishing at exit; final batch may be incomplete.");
            MelonLogger.Msg("Exit: VR resources are retained for process cleanup; no explicit compositor teardown.");
        }
        public override void OnUpdate()
        {
            ObserveFrameTrace();
            PollProblemReports();
            try{PollMapDiscovery();}catch(Exception ex){mapScanPhase=-1;nextMapScan=nextMapDiscovery=Time.realtimeSinceStartup+10;MelonLogger.Warning("MAP observer: "+ex.Message);}
            
            try{PollPickupModels();}catch(Exception ex){nextPickupModelPoll=Time.realtimeSinceStartup+10;MelonLogger.Warning("Pickup model: "+ex.Message);}
            ApplyInventoryCapacity();ApplyGameplayCheats();PollGrantWeapons();PollTestEnemy();
            if(desktopMouseOnly) {
                try {PollMouseWeaponTest();RestoreDesktopHud();PollDesktopMenuTest();}catch(Exception ex){WeaponAimFailure(ex);}
                return;
            }
            try
            {
                if (verboseDiagnostics && !snowWalkStereo) ObserveInteractionIndicators();
                if (verboseDiagnostics) ObserveDialogueUi();
                ObserveBookReaders();
                UpdateSnowTiming();
                PollControllerComparison();
                PollMouseWeaponTest();
                ObserveCamera("Update entry"); // Direct Windows key edges do not depend on Unity keyboard focus.
                bool connectPressed = diagnosticConnect.Poll((GetAsyncKeyState(0x75) & 0x8000) != 0);
                bool renderPressed = diagnosticRender.Poll((GetAsyncKeyState(0x78) & 0x8000) != 0);
                bool submitPressed = diagnosticSubmit.Poll((GetAsyncKeyState(0x79) & 0x8000) != 0);
                bool stopPressed = diagnosticStop.Poll((GetAsyncKeyState(0x7B) & 0x8000) != 0);
                if (connectPressed || renderPressed || submitPressed || stopPressed)
                    MelonLogger.Msg("DIAG physical hotkey: " + (stopPressed ? "stop" : connectPressed ? "connect" : renderPressed ? "render" : "submit"));
                if (menuFlip.Poll((GetAsyncKeyState(0x7A) & 0x8000) != 0)) { puzzleContentFlip = !puzzleContentFlip; ApplyPuzzleUV(); MelonLogger.Msg("Inventory image flip=" + puzzleContentFlip); }
                if (stopping) { FinishStop(); return; }
                UpdateStarAxes();LogVrFocus();
                EnsureQuestConnection();
                EnsureQuestControllers();
                EnsureQuestMenuScene();
                ObserveResolutionAndTiming();
                // Dashboard handling deferred at user request.
                UpdateQuestInputMode();
                TraceQuestRouting();
                RestoreRedPitForTransition();UpdateNativeCutsceneCamera();
                UpdateSceneRecovery();
            if(!UpdateEventWalkCamera())return;
                if (stopPressed) { questInputStopped = true; MelonLogger.Msg("DIAG: manual stop."); Stop(); return; }
                if (bridgeConfigured && stereo) CheckRenderStatus();
                PrepareNextPose();
                if (connectPressed)
                {
                    if (diagnostic.Stage != 0) { MelonLogger.Msg("DIAG: already connected. F9 is the next stage."); return; }
                    MelonLogger.Msg("DIAG A: prepare immersive mode; live menu screen may remain active.");
                    NativeLibraries.Initialize();
                    PrepareQuestSceneConnection();
                    EVRInitError error = EVRInitError.None;
                    if (system == null) system = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
                    if (error != EVRInitError.None || system == null) throw new InvalidOperationException("OpenVR connection: " + error);
                    diagnostic.Connected();
                    MelonLogger.Msg("DIAG A complete: connected. Observe dashboard, wait 20 seconds, then F9.");
                }
                if (renderPressed)
                {
                    if (diagnostic.Stage != 1) { MelonLogger.Msg("DIAG: F9 requires stage A first (F6). It does not toggle VR in this build."); return; }
                    MelonLogger.Msg("DIAG B: prepare gameplay camera; live menu screen remains until F10.");
                    Start(true);
                    if (!active || !stereo || camera == null || !camera.isActiveAndEnabled) return;
                    diagnostic.RenderReady();
                    diagnosticFrames = 0;
                    SetVrFramePacing();
                    MelonLogger.Msg("DIAG B ready. Live screen remains visible until F10 starts immersive gameplay.");
                }
                if (submitPressed)
                {
                    PrepareQuestSceneConnection();
                    immersiveRequested=true;sceneRecoveryAttempts=0;nextSceneRecovery=0f;
                    recenterPending=true;
                    // The existing scene recovery requests F1 in eligible gameplay
                    // and prepares submission once that camera is actually available.
                    if(questMenuScene)UpdateSceneRecovery();
                    else if(active && stereo && camera!=null && camera.isActiveAndEnabled && !recovery.Paused)diagnostic.BeginSubmit(Time.realtimeSinceStartup);
                    MelonLogger.Msg("F10: first-person and stereo requested; starts when an eligible gameplay camera is ready. F12 stops VR.");
                }
                if (recenterKey.Poll((GetAsyncKeyState(0x76) & 0x8000) != 0) && active)
                {
                    if (questMenuScene || recovery.Paused || waitingForGameplay) puzzleAnchorPending = true;
                    else recenterPending = true;
                    MelonLogger.Msg("Recenter requested for next valid tracked frame.");
                }
            }
            catch (Exception ex) { MelonLogger.Warning("Tracking stopped: " + ex.Message); Stop(); }
        }
        public override void OnLateUpdate() {
            if(desktopMouseOnly)return;

            
            ObserveCamera("LateUpdate entry passive");
            RestoreRedPitForTransition();UpdateNativeCutsceneCamera();

            UpdateSceneRecovery();
            UpdateEventWalkCamera();
            PrepareCinematicBars();
            if (questMenuScene || !active || diagnostic.Stage < 2) return;
            try
            {
                if (waitingForGameplay)
                {
                    TraceOperation(48,TraceClock());
                    if (Time.realtimeSinceStartup < nextCameraCheck) return;
                    nextCameraCheck = Time.realtimeSinceStartup;
                    var restored = FindGameplayCamera();
                    if (restored == null) return;
                    camera = restored;
                    originalRotation = camera.transform.localRotation;
                    recenterPending = true;
                    waitingForGameplay = false;
                    MelonLogger.Msg("Gameplay camera restored; stereo resumed automatically.");
                }
                ObserveCamera("LateUpdate guard"); if (camera == null || (perspectiveCutsceneReturn==null && (!camera.gameObject.activeInHierarchy || camera.orthographic || camera.transform.parent == null || (eventWalkStereo ? camera!=EventWalkCamera() : snowWalkStereo ? !IsSnowWalkCamera(camera) : camera.transform.parent.name != "Character Root"))))
                {
                    TraceOperation(49,TraceClock());
                    try
                    {
                        if (camera == null) MelonLogger.Msg("CAMERA GUARD: referenced camera is null or destroyed.");
                        else
                        {
                            var parent = camera.transform.parent;
                            MelonLogger.Msg("CAMERA GUARD: id=" + camera.GetInstanceID() + " name=" + camera.name
                                + " activeEnabled=" + camera.isActiveAndEnabled + " enabled=" + camera.enabled
                                + " activeHierarchy=" + camera.gameObject.activeInHierarchy + " orthographic=" + camera.orthographic
                                + " parent=" + (parent == null ? "<null>" : parent.name)
                                + " localPosition=" + camera.transform.localPosition);
                        }
                    }
                    catch (Exception ex) { MelonLogger.Warning("CAMERA GUARD snapshot failed: " + ex.Message); }
                    MelonLogger.Msg("DIAG: camera eligibility check failed; test stopped. See CAMERA GUARD above.");
                    if (stereo && diagnostic.CanSubmit && !stopping) {
                        OnSceneWasLoaded(-1, "gameplay camera unavailable");
                    } else Stop();
                    return;
                }
                ObserveDoorCamera();
                bool wasPaused = recovery.Paused;
                var pauseMenu = global::PauseMenu.instance;
                var pauseObject = pauseMenu != null ? pauseMenu.MenuObject : null;
                bool pauseVisible = pauseObject != null && pauseObject.activeInHierarchy;
                if (pauseVisible != pauseMenuVisible)
                    MelonLogger.Msg("PAUSE MENU: visible=" + pauseVisible + ", gameplayCameraEnabled=" + camera.enabled);
                pauseMenuVisible = pauseVisible;
                // Native manager covers newly spawned readers immediately between registry refreshes.
                var liveBookManager=global::BookManager.instance;
                var liveBook=liveBookManager==null?null:liveBookManager.screen;
                bool bookVisible = liveBook!=null && liveBook.enabled && liveBook.gameObject.activeInHierarchy && liveBook.open;
                for (int r = 0; r < cachedBookReaders.Length; r++)
                {
                    var reader = cachedBookReaders[r];
                    if (reader != null && reader.enabled && reader.gameObject.activeInHierarchy && reader.open)
                    {
                        bookVisible = true;
                        break;
                    }
                }
                if (bookVisible != bookScreenVisible)
                    MelonLogger.Msg("BOOK SCREEN: open=" + bookVisible + ", gameplayCameraEnabled=" + camera.enabled);
                bookScreenVisible = bookVisible;
                var dialogueUi = global::BlackSleekGuiSubs.instance;
                var dialogueHolder = dialogueUi != null ? dialogueUi.DialogueHolder : null;
                bool dialogueVisible = dialogueUi != null &&
                    ((dialogueHolder != null && dialogueHolder.activeInHierarchy
                        && (dialogueUi._showDialogueBox || dialogueUi._ending))
                    || (global::BlackSleekGuiSubs.itemView && dialogueUi._ItemViewBox != null
                        && dialogueUi._ItemViewBox.activeInHierarchy));
                if (dialogueVisible != dialogueScreenVisible)
                    MelonLogger.Msg("DIALOGUE SCREEN: visible=" + dialogueVisible + ", gameplayCameraEnabled=" + camera.enabled);
                dialogueScreenVisible = dialogueVisible;
                bool stereoEligible=ObserveStereoEligibility();
                int recoveryAction = recovery.Observe(stereoEligible,
                    Time.realtimeSinceStartup,FastStereoReturnEligible(stereoEligible));
                if (!wasPaused && recovery.Paused) { desktopMirrorFrame=-1;ResetPlayerScreenEffects(); menuCaptureReady = false; puzzleAnchorPending = true; puzzleLogged = false; }
                if (!wasPaused && recovery.Paused)
                    MelonLogger.Msg("CAMERA RECOVERY: menu/camera suspended; displaying capture until the same camera returns. Native resources retained.");
                if (recovery.Paused)
                {
                    TraceOperation(47,TraceClock());
                    // Drain without blocking, discarding old prepared poses.
                    bool drained = menuCaptureReady && recoveryAction != 2;
                    if (!drained) drained = !bridgeConfigured || CancelPreparedPose() != 0;
                    if (drained) menuCaptureReady = true;
                    if (recoveryAction == 2 && drained)
                    {
                        menuCaptureReady = false;
                        recovery.Reset();
                        MelonLogger.Msg("CAMERA RECOVERY: camera ready; requesting fresh pose without an extra recovery frame.");
                    }
                    if(recovery.Paused)return;
                }
                Quaternion pose;
                if (stereo && !CheckRenderStatus()) return;
                traceFrame = diagnosticFrames < 3;
                if (traceFrame) MelonLogger.Msg("DIAG stage " + diagnostic.Stage + " frame " + diagnosticFrames + ": pose call begin.");
                bool tracked = ReadPose(out pose, true);
                if (tracked) headComfortPose = pose;
                // A queued render-thread pose is asynchronous, not tracking loss.
                if (!tracked && diagnostic.CanSubmit) return;
                if (traceFrame) MelonLogger.Msg("DIAG: pose returned, valid=" + tracked);
                if (tracked != wasTracked) MelonLogger.Msg(tracked ? "Headset tracking acquired." : "Headset tracking lost; camera returned to neutral.");
                wasTracked = tracked;
                if (tracked && recenterPending)
                {
                    neutral = stereo ? Heading(pose) : pose;
                    neutralHeadPosition = trackedHeadPosition;
                    ResetControllerPositionFilters();
                    recenterPending = false;
                    MelonLogger.Msg("Head tracking centered from current render pose.");
                }
                var baseRotation = stereo ? Heading(originalRotation)*Quaternion.Euler(-questStereoPitch,0f,0f) : originalRotation;
                var trackedLocal = tracked ? baseRotation * Quaternion.Inverse(neutral) * pose : baseRotation;
                if (stereo)
                {
                    // Align once to the doorway; subsequent physical head turns stay live.
                    if(doorHeadingPending&&tracked){neutral=Heading(pose);doorHeadingPending=false;}
                    // Keep headset rotation out of the camera controlled by FPv2.
                    // Otherwise other camera/control code can feed it back into the rig.
                    var viewBasis = DoorCameraBasis((snowWalkStereo||eventWalkStereo) ? camera.transform.rotation : camera.transform.parent.rotation * baseRotation);
                    stereoRotation = viewBasis * (tracked ? Quaternion.Inverse(neutral) * pose : Quaternion.identity);
                    // Position and orientation share the same tracking basis, not current head rotation.
                    var trackingToWorld = viewBasis * Quaternion.Inverse(neutral);
                    controllerTrackingToWorld = trackingToWorld;
                    stereoPosition = HeadPosition.Map(DoorCameraOrigin(camera.transform.position), trackingToWorld, trackedHeadPosition, neutralHeadPosition, UnitsPerMetre);
                    RememberDoorCamera(viewBasis);
                }
                else camera.transform.localRotation = trackedLocal;
                if (verboseDiagnostics && tracked && Time.realtimeSinceStartup >= nextPoseLog)
                {
                    nextPoseLog = Time.realtimeSinceStartup + 2f;
                    var rig = camera.transform.parent.rotation.eulerAngles;
                    var local = originalRotation.eulerAngles;
                    var raw = pose.eulerAngles;
                    MelonLogger.Msg(string.Format(CultureInfo.InvariantCulture,
                        "Orientation inputs: rig=({0:F1},{1:F1},{2:F1}) initialLocal=({3:F1},{4:F1},{5:F1}) rawHead=({6:F1},{7:F1},{8:F1})",
                        rig.x, rig.y, rig.z, local.x, local.y, local.z, raw.x, raw.y, raw.z));
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
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="PEN_Hole") return;
            if (stopping) { MelonLogger.Warning("Previous render event is still finishing. Retry after it drains."); return; }
            var go = GameObject.Find("__Prerequisites__/Character Origin/Character Root/Main Camera");
            if (go == null) { MelonLogger.Warning("Load gameplay and press F1 before enabling head tracking."); return; }
            var candidate = go.GetComponent<Camera>();
            if (candidate == null || candidate.orthographic || !candidate.isActiveAndEnabled)
            { MelonLogger.Warning("First-person gameplay camera is not active."); return; }
                    NativeLibraries.Initialize();
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
            MelonLogger.Msg("DIAG: gameplay camera ready; F10 switches from live screen to immersive gameplay.");
        }
        private Camera FindGameplayCamera()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="PEN_Hole") return null;
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
                if (!stereo) EnableStereoSettings();
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
            // Restore the runtime recommendation, including installations with a saved legacy cap.
            int w = checked((int)width), h = checked((int)height);
            MelonLogger.Msg("VR EYE BUDGET: recommended="+width+"x"+height+" allocated="+w+"x"+h+"; runtime recommended resolution, legacy cap ignored.");
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
            NativeLibraries.Initialize();
            if (GetBridgeVersion() != 403) throw new InvalidOperationException("Render bridge ABI mismatch: expected 403. Restore the complete 0.7.10 diagnostic package and restart SIGNALIS.");
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
            SetVrFramePacing();
            submitted = false;
            int state, leftError, rightError; uint thread;
            GetBridgeStatus(out state, out leftError, out rightError, out resumeFrame, out thread);
        }
        private struct DoorMarkerAnchor
        {
            public SpriteRenderer renderer;
            public Vector3 position;
            public Quaternion rotation;
        }
        private readonly System.Collections.Generic.Dictionary<int, DoorMarkerAnchor> doorMarkerAnchors
            = new System.Collections.Generic.Dictionary<int, DoorMarkerAnchor>();
        private SpriteRenderer[] markerSprites = new SpriteRenderer[0];
        private Quaternion[] markerRotations = new Quaternion[0];
        private Vector3[] markerPositions = new Vector3[0];
        private Vector3[] markerScales = new Vector3[0];
        private Sprite[] savedMarkerArtwork = new Sprite[0];
        private bool[] markerEnabled = new bool[0];
        private bool[] markerPrepared = new bool[0];
        private Transform[] markerTransforms = new Transform[0];
        private int rotatedMarkerCount;
        private static bool IsLadderMarker(SpriteRenderer sr)
        {
            return sr.transform.parent != null && sr.transform.parent.name == "Ladder Icon";
        }
        private float nextMarkerSpriteSearch;
        private readonly System.Collections.Generic.List<Renderer> storageLabels=new System.Collections.Generic.List<Renderer>();
        private readonly System.Collections.Generic.Dictionary<Renderer,bool> storageLabelStates=new System.Collections.Generic.Dictionary<Renderer,bool>();
        private void FaceMarkersForVr()
        {
            // All world prompt artwork is now suppressed for desktop and both eyes.
            // Do not discover, project, resize, raycast or generate item holograms.
            MarkerSuppression.Apply();
            rotatedMarkerCount = 0;
        }
        private void RestoreMarkerRotations()
        {
            foreach(var pair in storageLabelStates)if(pair.Key!=null)pair.Key.enabled=pair.Value;storageLabelStates.Clear();
            HideBookBrackets();
            HideItemHolograms();
            for (int i = rotatedMarkerCount - 1; i >= 0; i--)
            {
                if (!markerPrepared[i]) continue;
                markerPrepared[i] = false;
                var sr = markerSprites[i];
                if (sr != null)
                {
                    sr.sprite = savedMarkerArtwork[i];
                    sr.enabled = markerEnabled[i];
                    sr.color = markerColors[i];
                    sr.forceRenderingOff = markerRenderVetoes[i];
                    var markerTransform = markerTransforms[i];
                    markerTransform.rotation = markerRotations[i];
                    markerTransform.position = markerPositions[i];
                    markerTransform.localScale = markerScales[i];
                }
            }
            rotatedMarkerCount = 0;
        }
        private void RenderStereo()
        {
            long started=System.Diagnostics.Stopwatch.GetTimestamp();
            try {
            bool sampleHeadComfort = BeginHeadComfortSample();
            long markerStarted=System.Diagnostics.Stopwatch.GetTimestamp();
            FaceMarkersForVr();
            RecordMarkerCost(markerStarted);

            long heldVisualStarted=TraceClock();
            PrepareControllerMotion();
            long heldStep=TraceClock();PrepareControllerHands();TraceOperation(60,heldStep);
            heldStep=TraceClock();PrepareControllerWeapon();TraceOperation(61,heldStep);
            heldStep=TraceClock();AlignControllerHandGrip();TraceOperation(62,heldStep);
            heldStep=TraceClock();PrepareEquippedWeaponVisual();TraceOperation(63,heldStep);
            
            heldStep=TraceClock();PrepareManualReload();PrepareSwingProd();PrepareThermite();TraceOperation(64,heldStep);
            heldStep=TraceClock();PrepareQuickSelect();PrepareQuickHealth();TraceOperation(65,heldStep);
            heldStep=TraceClock();PrepareHeldModules();PrepareHipAmmo();TraceOperation(66,heldStep);
            TraceOperation(34,heldVisualStarted);
                PrepareFlashlightPlacement();
            PrepareElsterPlayer();HideControllerMotion();PrepareHdGunEffects();foreach(var frozen in prodFrozen)AnimateStunVisual(frozen);
            PrepareModLasers();PrepareEnemyTargetBoxes();
            long hudStarted=TraceClock();PrepareModHud();TraceOperation(37,hudStarted);
            PrepareAimRings();
            ShowRifleImpacts();
            long mirrorsStarted=TraceClock();PrepareVrMirrors();TraceOperation(39,mirrorsStarted);
            long enemiesStarted=TraceClock();PrepareEnemyEyeMaterials();TraceOperation(38,enemiesStarted);ClearWheelOcclusion();
            using(var nativeBodyVisibility=HideNativeEyeBody())
            for (int i = 0; i < 2; i++)
            {
                var eye = eyes[i];
                ObserveCamera("before CopyFrom"); eye.CopyFrom(camera); ObserveCamera("after CopyFrom");
                // Observed interaction sprites use layer 23, excluded by the gameplay camera.
                eye.cullingMask |= 1 << 23;
                eye.cullingMask |= 1 << 30; // Controller proxies, enabled only for these two renders.
                eye.enabled = false;
                eye.stereoTargetEye = StereoTargetEyeMask.None;
                eye.targetTexture = textures[i];
                eye.rect = new Rect(0f, 0f, 1f, 1f);
                eye.clearFlags = CameraClearFlags.SolidColor;
                eye.backgroundColor = Color.black;
                var offset = system.GetEyeToHeadTransform((EVREye)i);
                eye.transform.position = stereoPosition + stereoRotation * (new Vector3(offset.m3, offset.m7, -offset.m11) * UnitsPerMetre);
                var forward = new Vector3(-offset.m2, -offset.m6, offset.m10);
                var up = new Vector3(offset.m1, offset.m5, -offset.m9);
                eye.transform.rotation = stereoRotation * Quaternion.LookRotation(forward, up);
                if(i==0&&snapshotQueued){eye.transform.position=stereoPosition;CaptureQueuedSnapshot(eye);eye.transform.position = stereoPosition + stereoRotation * (new Vector3(offset.m3, offset.m7, -offset.m11) * UnitsPerMetre); }
                float left = 0, right = 0, top = 0, bottom = 0;
                system.GetProjectionRaw((EVREye)i, ref left, ref right, ref top, ref bottom);
                if (traceFrame) MelonLogger.Msg(string.Format(CultureInfo.InvariantCulture,
                    "Eye geometry {0}: tangents=({1:F5},{2:F5},{3:F5},{4:F5}) offset=({5:F5},{6:F5},{7:F5})",
                    i, left, right, top, bottom, offset.m3, offset.m7, offset.m11));
                float near = Math.Max(0.01f, camera.nearClipPlane);
                // Preserve OpenVR's asymmetric projection center; do not negate its vertical offset.
                eye.projectionMatrix = EyeProjection.FromRaw(left, right, top, bottom, near, camera.farClipPlane);
                if (traceFrame) ObserveProjectionReference(eye, i, near, camera.farClipPlane);
                eye.ResetWorldToCameraMatrix();
                if (traceFrame) MelonLogger.Msg("DIAG: render eye " + i + " begin.");
                ObserveCamera("before eye Render");
                if (sampleHeadComfort) ObserveHeadComfort(eye,i,"before");
                ObserveHandVisibility(eye,i);
                TraceEyeRender(eye,i);
                ObserveHandTrackingView(eye,i);
                if (sampleHeadComfort) ObserveHeadComfort(eye,i,"after");
                ObserveCamera("after eye Render");
                if (traceFrame) MelonLogger.Msg("DIAG: render eye " + i + " returned.");
            }
            } finally { try { RestoreEnemyEyeMaterials();RestoreVrMirrors();RestoreFlashlightPlacement();RestoreControllerWeapon(); } finally { try { HideManualReload(); HideQuickSelect(); HideQuickHealth(); HideHeldModules(); HideSwingProd();HideThermite(); RestoreAimRings(); RestoreModLasers(); RestoreEnemyTargetBoxes(); HideModHud(); } finally { HideHdGunEffects();RestoreEquippedWeaponVisual(); RestoreControllerHands(); HideControllerMotion(); HideRifleImpacts(); RestoreMarkerRotations(); } } }
            // UI-only passes see restored native marker state, as before.
            for(int i=0;i<2;i++) {
                long overlayStarted=TraceClock();
                RenderDialogueOverlay(eyes[i]);
                RenderSnowCursor(eyes[i]);
                RenderPlayerScreenEffects(i);
                RenderNativeDoorFade(i);
                TraceOperation(14+i,overlayStarted);
            }
            SubmitEyes();
            desktopMirrorFrame=Time.frameCount;
            TraceOperation(35,started,controllerTrialMode);
            RecordRenderTiming(started);
        }
        private void SubmitEyes()
        {
            if (!diagnostic.CanSubmit && !questMenuScene) return;
            if (traceFrame) MelonLogger.Msg("DIAG: queue native submission begin.");
            long queuedAt=TraceClock();
            int nextEvent = ++eventId;
            if (QueueFrame(nextEvent, flipVertical ? 1 : 0) != 1) throw new InvalidOperationException("Previous frame has not drained.");
            // Ordered after Camera.Render on Unity's graphics command stream.
            // No compositor graphics calls run directly from this managed callback.
            GL.IssuePluginEvent(renderCallback, nextEvent);
            TraceOperation(12,queuedAt,nextEvent);
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
            if (state == 2 || state == 3 || state == 4) {TraceOperation(40+state,TraceClock());return false;}
            if (leftError != 0 || rightError != 0) throw new InvalidOperationException("Render-thread submission failed: left=" + leftError + ", right=" + rightError);
            if (!submitted && frames > resumeFrame)
            {
                submitted = true;
                MelonLogger.Msg("Both eye textures accepted on Unity render callback thread " + thread + ". Visual verification still required.");
            }
            bool ready=state==1||state==5;
            if(!ready)TraceOperation(50,TraceClock());
            return ready;
        }
        private void PrepareNextPose()
        {
            if (sceneRecoveryDraining || !active || !stereo || stopping || questInputStopped || !bridgeConfigured || system == null
                || (!diagnostic.CanSubmit && !questMenuScene)) return;
            try
            {
                // Nonblocking native queue accepts only idle state 1. Never overwrite
                // a queued submission, pending pose, or a pose already consumed for rendering.
                int next = ++eventId;
                if (QueuePose(next) == 1) GL.IssuePluginEvent(renderCallback, next);
            }
            catch (Exception ex) { MelonLogger.Warning("Pose preparation stopped: " + ex.Message); Stop(); }
        }
        private bool ReadPose(out Quaternion rotation, bool synchronize = false)
        {
            rotation = Quaternion.identity;
            if (system == null) return false;
            if (stereo && synchronize && (diagnostic.CanSubmit || questMenuScene))
            {
                int valid;
                int result = GetPreparedPose(preparedMatrix, out valid);
                if (result < 0) throw new InvalidOperationException("Render-thread SteamVR poses: " + result);
                if (result == 0)
                {
                    TraceOperation(45,TraceClock());
                    int poseEvent = ++eventId;
                    if (QueuePose(poseEvent) == 1) GL.IssuePluginEvent(renderCallback, poseEvent);
                    return false;
                }
                if (valid == 0) {TraceOperation(46,TraceClock());return false;}
                trackedHeadPosition = new Vector3(preparedMatrix[3], preparedMatrix[7], -preparedMatrix[11]);
                rotation = Quaternion.LookRotation(new Vector3(-preparedMatrix[2], -preparedMatrix[6], preparedMatrix[10]), new Vector3(preparedMatrix[1], preparedMatrix[5], -preparedMatrix[9]));
                return true;
            }
            else system.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0f, poses);
            var pose = poses[OpenVR.k_unTrackedDeviceIndex_Hmd];
            if (!pose.bDeviceIsConnected || !pose.bPoseIsValid) return false;
            var m = pose.mDeviceToAbsoluteTracking;
            trackedHeadPosition = new Vector3(m.m3, m.m7, -m.m11);
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
        private void Stop([System.Runtime.CompilerServices.CallerMemberName] string caller="unknown")
        {
            ResetControllerPositionFilters();
            ResetFlashlightPlacement();
            desktopMirrorFrame=-1;ResetPlayerScreenEffects();
            MelonLogger.Msg("VR STOP requested by "+caller);
            ResetDoorCamera();
            CloseModMenu(true);
            RestoreRedPitCamera();
            sceneStableSince=-1f; sceneCandidateId=0;
            centeredSnowCamera=0; snowCenterBeforeFirstEye=false;
            immersiveRequested=false; sceneRecoveryDraining=false; sceneRecoveryRequest=false; RestoreCinematicBars();
            nativeEventContext=false; questMenuScene=false;
            questStereoPitch=0f;
            ReleaseQuestControls();
            pauseMenuVisible = false; bookScreenVisible = false; dialogueScreenVisible = false;
            menuCaptureReady = false;
            recovery.Reset();
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
            stopping = true;ClearPickupModels();ClearStarAxes();DestroySnowCursor();ClearWeaponMatte();ClearVrDeath();enemyHeadBones.Clear();shotCriticalTargets.Clear();ClearGunKnockback();ResetPlayerScreenEffects();StopProdAudio();ClearProdFrozen();DestroyProdEffects();
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
            if (!verboseDiagnostics || system == null || Time.realtimeSinceStartup < nextFocusLog) return;
            nextFocusLog = Time.realtimeSinceStartup + 2f;
            Quaternion pose;
            bool valid = ReadPose(out pose, false);
            bool dashboard = OpenVR.Overlay != null && OpenVR.Overlay.IsDashboardVisible();
            MelonLogger.Msg("DIAG status: menuScene=" + questMenuScene + ", sceneFocusPid=" + RuntimeSceneFocusProcess() + ", gamePid=" + questProcessId + ", stage=" + diagnostic.Stage + ", tracked=" + valid + ", dashboard=" + dashboard + ", inputAvailable=" + system.IsInputAvailable() + ", localFrames=" + diagnosticFrames);
        }
    }
}










































































































































































































