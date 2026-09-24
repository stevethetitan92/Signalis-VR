using System;
using System.Collections;
using MelonLoader;
using UnityEngine;
using Valve.VR;

namespace SignalisVrTracking
{
    public sealed partial class Tracking
    {
        private RenderTexture puzzleCapture;
        private GameObject puzzleScreen;
        private Mesh puzzleMesh;
        private MeshRenderer puzzleRenderer;
        private Material puzzleMaterial;
        private bool puzzleAnchorPending = true, puzzleContentFlip = true, puzzleLogged;
        private bool puzzleAllocationAttempted;
        // Keep this isolated from game geometry. Enabled only during our manual renders.
        private readonly Vector3 puzzleOrigin = new Vector3(10000f, 10000f, 10000f);

        private IEnumerator PuzzleFrames()
        {
            var endOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                yield return endOfFrame;
                if (!active || !stereo || !waitingForGameplay || stopping) continue;
                try
                {
                    if (!CheckRenderStatus()) continue;
                    Quaternion pose;
                    if (!ReadPose(out pose, true)) continue;
                    EnsurePuzzleScreen();
                    if (Screen.width != puzzleCapture.width || Screen.height != puzzleCapture.height)
                        throw new InvalidOperationException("Game resolution changed. Restart SIGNALIS to resize the puzzle screen safely.");
                    // Capture only the game view, after its cameras and UI have rendered.
                    // No desktop capture, CPU readback, or per-frame texture allocation.
                    ScreenCapture.CaptureScreenshotIntoRenderTexture(puzzleCapture);
                    RenderPuzzle(pose);
                    if (!puzzleLogged)
                    {
                        puzzleLogged = true;
                        MelonLogger.Msg("Live puzzle screen submitting: " + puzzleCapture.width + "x" + puzzleCapture.height + ". F7 centers; F11 flips content if needed.");
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning("Puzzle screen stopped: " + ex.Message);
                    Stop();
                }
            }
        }

        private void EnsurePuzzleScreen()
        {
            if (puzzleRenderer != null && puzzleCapture != null) return;
            if (puzzleAllocationAttempted) throw new InvalidOperationException("Puzzle screen setup previously failed. Restart SIGNALIS before retrying.");
            puzzleAllocationAttempted = true;
            if (Screen.width <= 0 || Screen.height <= 0) throw new InvalidOperationException("Game view has no dimensions.");
            Shader shader = Shader.Find("Unlit/Texture");
            if (shader == null || !shader.isSupported) shader = Shader.Find("UI/Default");
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("No supported unlit puzzle-screen shader found.");
            puzzleCapture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            puzzleCapture.name = "SignalisVR_PuzzleCapture";
            puzzleCapture.antiAliasing = 1;
            puzzleCapture.useMipMap = false;
            if (!puzzleCapture.Create()) throw new InvalidOperationException("Could not allocate puzzle capture.");
            puzzleMaterial = new Material(shader);
            puzzleMaterial.mainTexture = puzzleCapture;
            if (puzzleMaterial.HasProperty("_Color")) puzzleMaterial.SetColor("_Color", Color.white);
            puzzleMesh = new Mesh();
            puzzleMesh.name = "SignalisVR_PuzzleScreenMesh";
            puzzleMesh.vertices = new Vector3[] {
                new Vector3(-0.5f,-0.5f,0), new Vector3(0.5f,-0.5f,0),
                new Vector3(0.5f,0.5f,0), new Vector3(-0.5f,0.5f,0) };
            puzzleMesh.uv = new Vector2[] { new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) };
            puzzleMesh.colors = new Color[] { Color.white, Color.white, Color.white, Color.white };
            // Face the viewer at negative Z; no collider and no game input changes.
            puzzleMesh.triangles = new int[] { 0,2,1,0,3,2 };
            puzzleMesh.RecalculateBounds();
            puzzleScreen = new GameObject("SignalisVR_PuzzleScreen");
            puzzleScreen.layer = 31;
            UnityEngine.Object.DontDestroyOnLoad(puzzleScreen);
            puzzleScreen.AddComponent<MeshFilter>().sharedMesh = puzzleMesh;
            puzzleRenderer = puzzleScreen.AddComponent<MeshRenderer>();
            puzzleRenderer.enabled = false;
            puzzleRenderer.sharedMaterial = puzzleMaterial;
            puzzleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            puzzleRenderer.receiveShadows = false;
            puzzleScreen.transform.localScale = new Vector3(2.4f, 2.4f * Screen.height / Screen.width, 1f);
            ApplyPuzzleUV();
            MelonLogger.Msg("Puzzle screen ready with shader " + shader.name + ". Capture resources retained across transitions.");
        }

        private void ApplyPuzzleUV()
        {
            if (puzzleMaterial == null) return;
            puzzleMaterial.mainTextureScale = new Vector2(1f, puzzleContentFlip ? -1f : 1f);
            puzzleMaterial.mainTextureOffset = new Vector2(0f, puzzleContentFlip ? 1f : 0f);
        }

        private void RenderPuzzle(Quaternion pose)
        {
            var head = poses[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking;
            var headPosition = puzzleOrigin + new Vector3(head.m3, head.m7, -head.m11);
            if (puzzleAnchorPending)
            {
                var heading = Heading(pose);
                puzzleScreen.transform.position = headPosition + heading * new Vector3(0f, 0f, 2f);
                puzzleScreen.transform.rotation = heading;
                puzzleAnchorPending = false;
            }
            var previousTarget = RenderTexture.active;
            try
            {
                puzzleRenderer.enabled = true;
                for (int i = 0; i < 2; i++)
                {
                    var eye = eyes[i];
                    eye.enabled = false;
                    eye.orthographic = false;
                    eye.stereoTargetEye = StereoTargetEyeMask.None;
                    eye.targetTexture = textures[i];
                    eye.rect = new Rect(0, 0, 1, 1);
                    eye.cullingMask = 1 << 31;
                    eye.clearFlags = CameraClearFlags.SolidColor;
                    eye.backgroundColor = Color.black;
                    eye.allowHDR = false;
                    eye.allowMSAA = false;
                    eye.useOcclusionCulling = false;
                    eye.nearClipPlane = 0.05f;
                    eye.farClipPlane = 20f;
                    var offset = system.GetEyeToHeadTransform((EVREye)i);
                    eye.transform.position = headPosition + pose * new Vector3(offset.m3, offset.m7, -offset.m11);
                    eye.transform.rotation = pose * Quaternion.LookRotation(new Vector3(-offset.m2, -offset.m6, offset.m10), new Vector3(offset.m1, offset.m5, -offset.m9));
                    float left = 0, right = 0, top = 0, bottom = 0;
                    system.GetProjectionRaw((EVREye)i, ref left, ref right, ref top, ref bottom);
                    eye.projectionMatrix = Matrix4x4.Frustum(left * 0.05f, right * 0.05f, -bottom * 0.05f, -top * 0.05f, 0.05f, 20f);
                    eye.ResetWorldToCameraMatrix();
                    eye.Render();
                }
            }
            finally
            {
                puzzleRenderer.enabled = false;
                RenderTexture.active = previousTarget;
            }
            SubmitEyes();
        }
    }
}
