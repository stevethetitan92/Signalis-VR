using System;
using MelonLoader;
using UnityEngine;
using Valve.VR;

namespace SignalisVrTracking {
 public sealed partial class Tracking {
  private bool questMenuScene,questMenuAttempted;
  private int questMenuWidth,questMenuHeight,questMenuFrames;
  private float questMenuStableSince;

  private bool QuestHasSceneFocus() {
   return QuestScenePolicy.OwnsScene(RuntimeSceneFocusProcess(),questProcessId);
  }
  private uint RuntimeSceneFocusProcess() {
   // OpenComposite has no SteamVR scene-process API; OpenXR input availability
   // is true only while this application's session is FOCUSED.
   if(NativeLibraries.VirtualDesktopMode)return system!=null&&system.IsInputAvailable()?questProcessId:0;
   return OpenVR.Compositor==null?0:OpenVR.Compositor.GetCurrentSceneFocusProcess();
  }
  private void EnsureQuestMenuScene() {
   if(questMenuAttempted||questInputStopped||questFailed||stopping||active||system==null||!questNativeInstalled)return;
   if(Screen.width<=0||Screen.height<=0)return;
   if(questMenuWidth!=Screen.width||questMenuHeight!=Screen.height) {
    questMenuWidth=Screen.width;questMenuHeight=Screen.height;questMenuStableSince=Time.realtimeSinceStartup;return;
   }
   if(Time.realtimeSinceStartup-questMenuStableSince<2f)return;
   questMenuAttempted=true;
   try {
    // Same bounded eye textures and native render-thread bridge used in gameplay.
    // No game camera is needed: the existing puzzle renderer draws a live screen.
    SetupStereo();
    active=true;questMenuScene=true;puzzleAnchorPending=true;
    SetVrFramePacing();
    MelonLogger.Msg("QUEST MENU SCENE: live game screen starting before stereo. First person/stereo starts automatically in eligible gameplay; F10 retries; F12 stops VR.");
   }catch(Exception ex){MelonLogger.Warning("Quest menu scene stopped: "+ex.Message);Stop();}
  }
  private void RenderQuestMenuScene() {
   if(sceneRecoveryDraining)return;
   if(!questMenuScene||!active||!stereo||stopping){RestoreCinematicBars();return;}
   try {
    if(!CheckRenderStatus())return;
    Quaternion pose;
    if(!ReadPose(out pose,true))return;
    EnsurePuzzleScreen();
    if(Screen.width!=puzzleCapture.width||Screen.height!=puzzleCapture.height)
     throw new InvalidOperationException("Game resolution changed after menu capture allocation. Restart SIGNALIS to recreate capture safely.");
    TraceCaptureScreen(puzzleCapture);
    RenderPuzzle(pose);
    questMenuFrames++;
    if(questMenuFrames==1)MelonLogger.Msg("QUEST MENU SCENE: first live frame queued. Scene ownership and headset appearance still require verification.");
   }catch(Exception ex){MelonLogger.Warning("Quest menu frame stopped: "+ex.Message);Stop();}

  }
 }
 public static class QuestScenePolicy {
  public static bool OwnsScene(uint scene,uint process){return process!=0&&scene==process;}
 }
}


