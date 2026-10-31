using System;
using System.Globalization;
using MelonLoader;
using UnityEngine;
namespace SignalisVrTracking { public sealed partial class Tracking {
        private static readonly bool verboseDiagnostics=Environment.GetEnvironmentVariable("SIGNALIS_VR_VERBOSE")=="1";
        private float nextResolutionReport;
        private float vrDisplayHz=72f;
        private void ObserveResolutionAndTiming() {
            if(system==null||compositor==null||Time.realtimeSinceStartup<nextResolutionReport)return;
            long observationStarted=TraceClock();
            nextResolutionReport=Time.realtimeSinceStartup+1f;
            try {
                uint w=0,h=0;system.GetRecommendedRenderTargetSize(ref w,ref h);
                var timing=new Valve.VR.Compositor_FrameTiming();
                timing.m_nSize=(uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Valve.VR.Compositor_FrameTiming));
                // OpenComposite supplies estimated timing fields, not GPU measurements.
                bool valid=!NativeLibraries.VirtualDesktopMode&&compositor.GetFrameTiming(ref timing,0);
                MelonLogger.Msg(string.Format(CultureInfo.InvariantCulture,
                    "VR RESOLUTION: eyes={0}x{1} recommended={2}x{3} scenePid={4} dashboard={5} cap={6} vsync={7} frame={8} timingValid={9} intervalMs={10:F2} gpuMs={11:F2} presents={12} reprojection={13}",
                    textures[0]==null?0:textures[0].width,textures[0]==null?0:textures[0].height,w,h,
                    RuntimeSceneFocusProcess(),Valve.VR.OpenVR.Overlay!=null&&Valve.VR.OpenVR.Overlay.IsDashboardVisible(),
                    Application.targetFrameRate,QualitySettings.vSyncCount,timing.m_nFrameIndex,valid,timing.m_flClientFrameIntervalMs,
                    timing.m_flTotalRenderGpuMs,timing.m_nNumFramePresents,timing.m_nReprojectionFlags));
            } catch(Exception ex) {nextResolutionReport=Time.realtimeSinceStartup+10f;MelonLogger.Warning("VR resolution observation unavailable: "+ex.Message);}
            finally {TraceOperation(24,observationStarted);}
        }
        private double perfRenderSum,perfRenderMax,perfFrameSum,perfFrameMax;
        private int perfCount,perfSlowFrames;
        private float nextPerfReport;
        private double markerMsSum,markerMsMax;
        private int markerSamples;
        private void RecordMarkerCost(long start) {
            TraceOperation(27,start);
            double ms=(System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            markerMsSum+=ms;markerMsMax=Math.Max(markerMsMax,ms);markerSamples++;
        }
        private void SetVrFramePacing() {
            float hz=72f;
            if(NativeLibraries.VirtualDesktopMode) {
                // This OpenComposite build reports a constant 90 Hz. Do not treat it
                // as a device measurement or apply high-refresh filtering by guess.
                float configured=NativeLibraries.ConfiguredDisplayHz(Environment.GetEnvironmentVariable("SIGNALIS_VR_REFRESH_HZ"));
                if(configured>0)hz=configured;
                MelonLogger.Msg("VDXR refresh: "+(configured>0?"launcher-configured "+hz:"unknown; conservative 72 Hz pacing fallback")+"; not a measured headset refresh. Set launcher -RefreshRate to your VD setting for matching high-refresh filtering.");
            } else if(system!=null) {
                var error=Valve.VR.ETrackedPropertyError.TrackedProp_Success;
                float reported=system.GetFloatTrackedDeviceProperty(Valve.VR.OpenVR.k_unTrackedDeviceIndex_Hmd,Valve.VR.ETrackedDeviceProperty.Prop_DisplayFrequency_Float,ref error);
                if(error==Valve.VR.ETrackedPropertyError.TrackedProp_Success && !float.IsNaN(reported) && !float.IsInfinity(reported) && reported>=60f && reported<=144f)hz=reported;
            }
            // The asynchronous pose request and eye submission span Unity updates.
            // A one-refresh-rate Unity cap can halve the submitted headset cadence.
            Application.targetFrameRate=(int)Math.Ceiling(hz*2f);
            vrDisplayHz=hz;
            ResetControllerPositionFilters();
            MelonLogger.Msg("VR AIM STABILITY: tracking-space positional filter="+(hz>=85f?"ON":"OFF")+" at "+hz+" Hz; 3mm maximum positional offset; shared controller rotation filter ON, 1.5 degree maximum rotational offset, adaptive fast-motion bypass. Headset result unverified.");
            MelonLogger.Msg("VR PACING: headsetHz="+hz+" gameUpdateCap="+Application.targetFrameRate+"; runtime verification required.");
        }
        private void RecordRenderTiming(long start) {
            double ms=(System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            double frame=Time.unscaledDeltaTime*1000.0;
            perfRenderSum+=ms;perfRenderMax=Math.Max(perfRenderMax,ms);
            perfFrameSum+=frame;perfFrameMax=Math.Max(perfFrameMax,frame);perfCount++;
            if(frame>33.333)perfSlowFrames++;
            if(Time.realtimeSinceStartup<nextPerfReport)return;
            nextPerfReport=Time.realtimeSinceStartup+10f;
            MelonLogger.Msg(string.Format(CultureInfo.InvariantCulture,"VR PERF: samples={0} managedRenderMs avg={1:F2} max={2:F2} gameFrameMs avg={3:F2} max={4:F2} over33ms={5}; CPU-side observations, not GPU/compositor timing",perfCount,perfRenderSum/perfCount,perfRenderMax,perfFrameSum/perfCount,perfFrameMax,perfSlowFrames));
            if(markerSamples>0)MelonLogger.Msg("VR PERF MARKERS: avgMs="+(markerMsSum/markerSamples).ToString("F2")+" maxMs="+markerMsMax.ToString("F2")+" samples="+markerSamples);
            markerMsSum=markerMsMax=0;markerSamples=0;
            perfCount=perfSlowFrames=0;perfRenderSum=perfRenderMax=perfFrameSum=perfFrameMax=0;
        }
        private Camera passiveCamera; private float nextPassiveSearch; private bool? lastCameraEnabled;
        private int observedCameraId;
        private string lastCameraCheckpoint = "none";
        private void ObserveCamera(string checkpoint)
        {
            if(!verboseDiagnostics)return;
            try
            {
                if (passiveCamera == null && Time.realtimeSinceStartup >= nextPassiveSearch)
                {
                    nextPassiveSearch = Time.realtimeSinceStartup + 0.5f;
                    var go = GameObject.Find("__Prerequisites__/Character Origin/Character Root/Main Camera");
                    if (go != null) passiveCamera = go.GetComponent<Camera>();
                }
                var observed = camera != null ? camera : passiveCamera;
                if (observed == null) return;
                int id = observed.GetInstanceID();
                bool enabled = observed.enabled;
                if (id != observedCameraId || !lastCameraEnabled.HasValue || lastCameraEnabled.Value != enabled)
                    MelonLogger.Msg("CAMERA TRANSITION: checkpoint=" + checkpoint + " previousCheckpoint=" + lastCameraCheckpoint
                        + " frame=" + Time.frameCount + " id=" + id + " enabled=" + enabled + " stage=" + diagnostic.Stage);
                observedCameraId = id;
                lastCameraEnabled = enabled;
                lastCameraCheckpoint = checkpoint;
            }
            catch (Exception ex) { MelonLogger.Warning("Camera observation failed: " + ex.Message); }
        }
        private global::BookScreen[] cachedBookReaders = new global::BookScreen[0];
        private float nextBookObservation;
        private int bookObservationScene=-1;
        private string lastBookObservation;
        private void ObserveBookReaders()
        {
            int scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
            if (scene==bookObservationScene && Time.realtimeSinceStartup < nextBookObservation) return;
            bookObservationScene=scene;
            long bookStarted=TraceClock();
            nextBookObservation = Time.realtimeSinceStartup + 30f;
            try
            {
                var readers = Resources.FindObjectsOfTypeAll<global::BookScreen>();
                cachedBookReaders = new global::BookScreen[readers.Length];
                for (int r = 0; r < readers.Length; r++) cachedBookReaders[r] = readers[r];
                if(!verboseDiagnostics)return;
                var manager = global::BookManager.instance;
                var selectedScreen = manager != null ? manager.screen : null;
                string report = "manager=" + (manager == null ? "null" : manager.GetInstanceID().ToString())
                    + " managerScreen=" + (selectedScreen == null ? "null" : selectedScreen.GetInstanceID().ToString());
                report += " readers=" + readers.Length;
                for (int i = 0; i < readers.Length && i < 32; i++)
                {
                    var reader = readers[i];
                    if (reader == null) continue;
                    var go = reader.gameObject;
                    report += " | id=" + reader.GetInstanceID() + " name=" + go.name
                        + " active=" + go.activeInHierarchy + " enabled=" + reader.enabled
                        + " open=" + reader.open + " opening=" + reader.opening
                        + " page=" + reader.pageToDisplay;
                }
                if (report != lastBookObservation)
                {
                    MelonLogger.Msg("BOOK DIAGNOSTIC: " + report);
                    lastBookObservation = report;
                }
            }
            catch (Exception ex)
            {
                string error = "error=" + ex.GetType().Name + ": " + ex.Message;
                if (error != lastBookObservation) MelonLogger.Warning("BOOK DIAGNOSTIC: " + error);
                lastBookObservation = error;
            } finally {TraceOperation(21,bookStarted);}
        }
        private float nextDialogueObservation;
        private string lastDialogueObservation;
        private static string DescribeUiObject(UnityEngine.Object value)
        {
            if (value == null) return "null";
            var go = value.TryCast<GameObject>();
            if (go == null)
            {
                var component = value.TryCast<Component>();
                if (component != null) go = component.gameObject;
            }
            return value.name + "#" + value.GetInstanceID()
                + (go == null ? "" : ":active=" + go.activeInHierarchy);
        }
        private void ObserveDialogueUi()
        {
            if (Time.realtimeSinceStartup < nextDialogueObservation) return;
            nextDialogueObservation = Time.realtimeSinceStartup + 0.5f;
            try
            {
                var ui = global::BlackSleekGuiSubs.instance;
                string report = ui == null ? "controller=null" :
                    "id=" + ui.GetInstanceID() + " show=" + ui._showDialogueBox
                    + " ending=" + ui._ending + " talking=" + global::BlackSleekGuiSubs.talking
                    + " itemView=" + global::BlackSleekGuiSubs.itemView + " ready=" + ui._windowReady
                    + " textBox=" + DescribeUiObject(ui.TextBox)
                    + " canvas=" + DescribeUiObject(ui.DialogueCanvas)
                    + " holder=" + DescribeUiObject(ui.DialogueHolder)
                    + " itemBox=" + DescribeUiObject(ui._ItemViewBox)
                    + " choices=" + DescribeUiObject(ui.choiceHolder);
                if (report != lastDialogueObservation)
                {
                    MelonLogger.Msg("DIALOGUE DIAGNOSTIC: " + report);
                    lastDialogueObservation = report;
                }
            }
            catch (Exception ex)
            {
                string error = "error=" + ex.GetType().Name + ": " + ex.Message;
                if (error != lastDialogueObservation) MelonLogger.Warning("DIALOGUE DIAGNOSTIC: " + error);
                lastDialogueObservation = error;
            }
        }
        private float nextMarkerCameraScan;
        private float nextIndicatorObservation;
        private string lastIndicatorError;
        private void ObserveInteractionIndicators()
        {
            if (!active || Time.realtimeSinceStartup < nextIndicatorObservation) return;
            nextIndicatorObservation = Time.realtimeSinceStartup + 2f;
            try
            {
                var markers = Resources.FindObjectsOfTypeAll<global::interactionPopup>();
                MelonLogger.Msg("INDICATOR SNAPSHOT: count=" + markers.Length + " stage=" + diagnostic.Stage
                    + " source=" + DescribeIndicatorCamera(camera)
                    + " left=" + DescribeIndicatorCamera(eyes[0]) + " right=" + DescribeIndicatorCamera(eyes[1]));
                int reported = 0;
                for (int i = 0; i < markers.Length && reported < 24; i++)
                {
                    var marker = markers[i];
                    if (marker == null || !marker.gameObject.activeInHierarchy) continue;
                    var sr = marker.SR;
                    if (camera != null && Vector3.Distance(camera.transform.position, marker.transform.position) > 40f) continue;
                    reported++;
                    string report = "id=" + marker.GetInstanceID() + " name=" + marker.gameObject.name
                        + " flickerTime=" + marker.flickerTime + " flickerTimer=" + marker.flickerTimer + " enabled=" + marker.enabled + " triggered=" + marker.wasTriggered + " forceOn=" + global::interactionPopup.forceOn
                        + " position=" + FormatMarkerVector(marker.transform.position);
                    if (sr == null) report += " SR=null";
                    else
                    {
                        int layer = sr.gameObject.layer;
                        report += " sprite=" + (sr.sprite == null ? "null" : sr.sprite.name)
                            + " active=" + sr.gameObject.activeInHierarchy + " rendererEnabled=" + sr.enabled
                            + " alpha=" + sr.color.a + " layer=" + layer + " position=" + FormatMarkerVector(sr.transform.position)
                            + " forward=" + FormatMarkerVector(sr.transform.forward) + " scale=" + FormatMarkerVector(sr.transform.lossyScale)
                            + " sourceIncludes=" + (camera != null && (camera.cullingMask & (1 << layer)) != 0)
                            + " leftIncludes=" + (eyes[0] != null && (eyes[0].cullingMask & (1 << layer)) != 0)
                            + " rightIncludes=" + (eyes[1] != null && (eyes[1].cullingMask & (1 << layer)) != 0)
                            + " shader=" + (sr.sharedMaterial == null || sr.sharedMaterial.shader == null ? "null" : sr.sharedMaterial.shader.name);
                    }
                    MelonLogger.Msg("INDICATOR: " + report);
                }
                var doors = Resources.FindObjectsOfTypeAll<global::DoorInteractionPopup>();
                int doorCount = 0;
                for (int i = 0; i < doors.Length && doorCount < 24; i++)
                {
                    var door = doors[i];
                    if (door == null || !door.gameObject.activeInHierarchy) continue;
                    if (camera != null && Vector3.Distance(camera.transform.position, door.transform.position) > 40f) continue;
                    doorCount++;
                    MelonLogger.Msg("DOOR MARKER: name=" + door.gameObject.name + " enabled=" + door.enabled
                        + " triggered=" + door.wasTriggered + " flickerTime=" + door.flickerTime
                        + " flickerTimer=" + door.flickerTimer + " position=" + FormatMarkerVector(door.transform.position));
                }
                if (Time.realtimeSinceStartup >= nextMarkerCameraScan)
                {
                    nextMarkerCameraScan = Time.realtimeSinceStartup + 10f;
                    var sprites = Resources.FindObjectsOfTypeAll<SpriteRenderer>();
                    int logged = 0;
                    for (int si = 0; si < sprites.Length && logged < 80; si++)
                    {
                        var sr = sprites[si];
                        if (sr == null || sr.gameObject.layer != 23 || !sr.gameObject.activeInHierarchy) continue;
                        if (camera != null && Vector3.Distance(camera.transform.position, sr.transform.position) > 45f) continue;
                        logged++;
                        string path = sr.gameObject.name;
                        var ancestor = sr.transform.parent;
                        for (int depth = 0; ancestor != null && depth < 5; depth++, ancestor = ancestor.parent)
                            path = ancestor.name + "/" + path;
                        var popup = sr.GetComponentInParent<global::DoorInteractionPopup>();
                        if (popup != null && popup.door != null && sr.gameObject.name.StartsWith("Door Icon", StringComparison.Ordinal)
                            && (popup.gameObject.name == "Door Icon E" || popup.gameObject.name == "Door Icon W"))
                        {
                            var root = popup.door.transform.parent != null ? popup.door.transform.parent : popup.door.transform;
                            var geometry = root.GetComponentsInChildren<Renderer>(true);
                            MelonLogger.Msg("SIDE GEOMETRY ROOT: name=" + root.name + " camera=" + (camera == null ? "null" : FormatMarkerVector(camera.transform.position)));
                            for (int gi = 0; gi < geometry.Length && gi < 32; gi++)
                            {
                                var r = geometry[gi];
                                if (r == null) continue;
                                MelonLogger.Msg("SIDE GEOMETRY: door=" + popup.gameObject.name + " name=" + r.gameObject.name
                                    + " layer=" + r.gameObject.layer + " enabled=" + r.enabled + " active=" + r.gameObject.activeInHierarchy
                                    + " center=" + FormatMarkerVector(r.bounds.center) + " size=" + FormatMarkerVector(r.bounds.size)
                                    + " position=" + FormatMarkerVector(r.transform.position));
                            }
                            DoorMarkerAnchor captured;
                            if (doorMarkerAnchors.TryGetValue(sr.GetInstanceID(), out captured))
                                MelonLogger.Msg("SIDE ANCHOR: door=" + popup.gameObject.name + " position=" + FormatMarkerVector(captured.position)
                                    + " rotation=" + FormatMarkerVector(captured.rotation.eulerAngles));
                        }
                        var obj = sr.GetComponentInParent<global::interactionPopup>();
                        MelonLogger.Msg("MARKER ART: path=" + path + " sprite=" + (sr.sprite == null ? "null" : sr.sprite.name)
                            + " enabled=" + sr.enabled + " flipX=" + sr.flipX + " flipY=" + sr.flipY
                            + " localRotation=" + FormatMarkerVector(sr.transform.localEulerAngles)
                            + " worldRotation=" + FormatMarkerVector(sr.transform.eulerAngles)
                            + " localScale=" + FormatMarkerVector(sr.transform.localScale)
                            + " worldScale=" + FormatMarkerVector(sr.transform.lossyScale)
                            + " pos=" + FormatMarkerVector(sr.transform.position)
                            + " doorPopup=" + (popup == null ? "null" : popup.gameObject.name)
                            + " objectPopup=" + (obj == null ? "null" : obj.gameObject.name)
                            + " doorPos=" + (popup == null || popup.door == null ? "null" : FormatMarkerVector(popup.door.transform.position)));
                    }
                    var cams = Resources.FindObjectsOfTypeAll<Camera>();
                    for (int i = 0; i < cams.Length; i++)
                    {
                        var c = cams[i];
                        if (c == null || !c.gameObject.activeInHierarchy || (c.cullingMask & (1 << 23)) == 0) continue;
                        string components = "";
                        var all = c.gameObject.GetComponents<Component>();
                        for (int j = 0; j < all.Length; j++) if (all[j] != null) components += all[j].GetIl2CppType().Name + ",";
                        MelonLogger.Msg("MARKER CAMERA: " + DescribeIndicatorCamera(c) + " depth=" + c.depth
                            + " ortho=" + c.orthographic + " target=" + (c.targetTexture == null ? "screen" : c.targetTexture.name)
                            + " components=" + components);
                    }
                }
                lastIndicatorError = null;
            }
            catch (Exception ex)
            {
                string error = ex.GetType().Name + ": " + ex.Message;
                if (lastIndicatorError != error) MelonLogger.Warning("INDICATOR observation failed: " + error);
                lastIndicatorError = error;
            }
        }
        private static string FormatMarkerVector(Vector3 v) { return string.Format(CultureInfo.InvariantCulture, "({0:F3},{1:F3},{2:F3})", v.x, v.y, v.z); }
        private string DescribeIndicatorCamera(Camera c)
        {
            return c == null ? "null" : c.name + ":enabled=" + c.enabled + ":mask=" + c.cullingMask
                + ":position=" + FormatMarkerVector(c.transform.position);
        }

        // Diagnostic-only: two samples/sec, max 120 seconds of stereo samples.
        // Keep genuine head roll and existing rendering/control behavior intact.
        private Quaternion headComfortPose;
        private float nextHeadComfortSample;
        private int headComfortSamples;
        private bool BeginHeadComfortSample() {
            if(!verboseDiagnostics)return false;
            if(headComfortSamples>=240 || Time.realtimeSinceStartup<nextHeadComfortSample)return false;
            nextHeadComfortSample=Time.realtimeSinceStartup+.5f;
            headComfortSamples++;
            return true;
        }
        private static float ComfortRoll(Vector3 up) {
            return (float)(Math.Atan2(up.x,up.y)*180.0/Math.PI);
        }
        private void ObserveHeadComfort(Camera eye,int index,string phase) {
            try {
                var roomUp=new Vector3(0f,0f,-1f);
                var eyeUp=eye.worldToCameraMatrix.MultiplyVector(roomUp);
                var wantedUp=Quaternion.Inverse(stereoRotation)*roomUp;
                var rawUp=Quaternion.Inverse(headComfortPose)*Vector3.up;
                var rigUp=camera.transform.parent.up;
                var projection=eye.projectionMatrix;
                MelonLogger.Msg(string.Format(CultureInfo.InvariantCulture,
                    "HEAD COMFORT: frame={0} stereoId={1} sample={2} eye={3} phase={4} snow={5} rawRoll={6:F3} wantedRoll={7:F3} renderedRoll={8:F3} rigUp={9} headForward={10} eyeUp={11} eyePosition={12} cameraPosition={13} projectionCross=({14:F6},{15:F6}) leanMetres={16} stereoPosition={17}",
                    Time.frameCount,stereoTraceId,headComfortSamples,index,phase,snowWalkStereo,
                    ComfortRoll(rawUp),ComfortRoll(wantedUp),ComfortRoll(eyeUp),FormatMarkerVector(rigUp),
                    FormatMarkerVector(headComfortPose*Vector3.forward),FormatMarkerVector(eyeUp),
                    FormatMarkerVector(eye.transform.position),FormatMarkerVector(camera.transform.position),projection.m01,projection.m10,FormatMarkerVector(trackedHeadPosition-neutralHeadPosition),FormatMarkerVector(stereoPosition)));
            } catch(Exception ex) {
                headComfortSamples=240;
                MelonLogger.Warning("Head comfort observation stopped: "+ex.Message);
            }
        }
        private void ObserveProjectionReference(Camera eye,int index,float near,float far) {
            try {
                var reference=system.GetProjectionMatrix((Valve.VR.EVREye)index,near,far);
                var p=eye.projectionMatrix;
                MelonLogger.Msg(string.Format(CultureInfo.InvariantCulture,
                    "EYE PROJECTION: eye={0} actual=({1:F6},{2:F6},{3:F6},{4:F6}) runtime=({5:F6},{6:F6},{7:F6},{8:F6})",
                    index,p.m00,p.m02,p.m11,p.m12,reference.m0,reference.m2,reference.m5,reference.m6));
            } catch(Exception ex) { MelonLogger.Warning("Projection reference unavailable: "+ex.Message); }
        }
}}


