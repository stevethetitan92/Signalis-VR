using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;
using UnityEngine;
using Valve.VR;
using Rewired;
namespace SignalisVrTracking {
 public sealed partial class Tracking {
  private readonly QuestHandInputPolicy questPolicy=new QuestHandInputPolicy();


  private QuestFrame questFrame;
  private bool questFailed;

  private bool questInputStopped;
  private float nextQuestConnect;
  private float nextQuestLog;
  private string lastQuestStatus;
  private float nextQuestRouteTrace;
  private string lastQuestRouteTrace;
  private void TraceQuestRouting() {
   if(!verboseDiagnostics||!questNativeInstalled||Time.realtimeSinceStartup<nextQuestRouteTrace)return;
   nextQuestRouteTrace=Time.realtimeSinceStartup+1f;
   try {
    string report="armed="+questPolicy.Armed+" sourceMove="+questFrame.moveX.ToString("F2")+","+questFrame.moveY.ToString("F2")+" sourceLook="+questFrame.lookX.ToString("F2")+","+questFrame.lookY.ToString("F2")+" buttons="+questFrame.buttons+QuestNativeReport();
    if(questFpPlayer!=null&&questFpMod!=null) {
     var gamePlayer=questFpPlayer.GetValue(questFpMod) as global::AlternatePlayerController;
     if(gamePlayer!=null)report+=" gameInput="+gamePlayer.input.x.ToString("F2")+","+gamePlayer.input.y.ToString("F2")+" playing="+gamePlayer.playing+" traversing="+gamePlayer.traversing;
    }
    var actions=Resources.FindObjectsOfTypeAll<global::CharacterAction>();
    if(actions.Length>0&&actions[0]!=null)report+=" actionMove="+actions[0].Move.x.ToString("F2")+","+actions[0].Move.y.ToString("F2");
    if(report!=lastQuestRouteTrace){lastQuestRouteTrace=report;MelonLogger.Msg("QUEST ROUTE: "+report);}
   }catch(Exception ex){nextQuestRouteTrace=Time.realtimeSinceStartup+5f;MelonLogger.Warning("Quest route trace: "+ex.Message);}
  }
  private object questFpMod;
  private FieldInfo questFpYaw;
  private FieldInfo questFpPlayer;
  private readonly uint questProcessId=(uint)Process.GetCurrentProcess().Id;
  [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window,out uint processId);
  private void EnsureQuestConnection() {
   if(system!=null||questFailed||questInputStopped||stopping||Time.realtimeSinceStartup<nextQuestConnect)return;
   nextQuestConnect=Time.realtimeSinceStartup+5f;
   try {
    NativeLibraries.Initialize();
    if(!OpenVR.IsHmdPresent())return;
    EVRInitError error=EVRInitError.None;
    // Connect as a scene; actual focus follows rendering, not initialization.
    // The live menu screen supplies those frames before immersive gameplay.
    var connected=OpenVR.Init(ref error,EVRApplicationType.VRApplication_Scene);
    if(error!=EVRInitError.None||connected==null) {MelonLogger.Warning("Quest menu connection waiting: "+error);return;}
    system=connected;startupDashboardDeadline=Time.realtimeSinceStartup+30f;
    MelonLogger.Msg("QUEST MENU: SIGNALIS scene connection acquired; Live menu rendering will establish scene focus; immersive gameplay starts automatically when eligible.");
   }catch(Exception ex){MelonLogger.Warning("Quest menu connection waiting: "+ex.Message);}
  }
  private bool startupDashboardFinished;
  private int startupDashboardAttempts;
  private float startupDashboardDeadline,nextStartupDashboardCheck,startupDashboardClearSince=-1f;
  private void DismissStartupDashboard() {
   if(startupDashboardFinished || system==null || !active || !stereo || stopping || questInputStopped)return;
   float now=Time.realtimeSinceStartup;
   if(now>startupDashboardDeadline){startupDashboardFinished=true;return;}
   if(NativeLibraries.VirtualDesktopMode)return;
   if(questMenuFrames<2 || !QuestHasSceneFocus() || OpenVR.Overlay==null)return;
   if(!OpenVR.Overlay.IsDashboardVisible()) {
    if(startupDashboardClearSince<0f)startupDashboardClearSince=now;
    if(now-startupDashboardClearSince>=2f)startupDashboardFinished=true;
    return;
   }
   startupDashboardClearSince=-1f;
   if(now<nextStartupDashboardCheck)return;
   if(startupDashboardAttempts>=1){startupDashboardFinished=true;MelonLogger.Warning("Startup dashboard remained visible after dismissal request.");return;}
   nextStartupDashboardCheck=now+5f;startupDashboardAttempts++;
   try {
    var path=System.IO.Path.Combine(OpenVR.RuntimePath(),"bin","win64","vrcmd.exe");
    if(!System.IO.File.Exists(path)){startupDashboardFinished=true;MelonLogger.Warning("SteamVR command helper unavailable for startup dashboard dismissal.");return;}
    var start=new ProcessStartInfo(path,"--compositorcmd system_dashboard_toggle");
    start.UseShellExecute=false;start.CreateNoWindow=true;start.WindowStyle=ProcessWindowStyle.Hidden;
    using(var process=Process.Start(start)){}
    MelonLogger.Msg("Startup dashboard dismissal requested; visibility will be checked, attempt="+startupDashboardAttempts);
   } catch(Exception ex){startupDashboardFinished=true;MelonLogger.Warning("Startup dashboard dismissal failed: "+ex.Message);}
  }
  private void PrepareQuestSceneConnection() { if(questInputStopped){questMenuAttempted=false;questFailed=false;} questInputStopped=false; }
  private void EnsureQuestControllers() {
   if(questFailed||system==null||!ReInput.isReady)return;
   if(questNativeInstalled){PollQuestControllers();return;}
   try {
    foreach(var mod in MelonHandler.Mods) {
     if(mod.GetType().FullName!="FPv2_reworked.FPv2_rework")continue;
     questFpMod=mod;questFpYaw=mod.GetType().GetField("x",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
     questFpPlayer=mod.GetType().GetField("player_control",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
    }
    InstallQuestFirstPerson();
    InstallQuestNativeReads();
    PollQuestControllers();
    MelonLogger.Msg("QUEST INPUT0472: no custom controller, no dynamic maps, no Rewired input event subscription. Native reads only; runtime unverified.");
   }catch(Exception ex){questFailed=true;ReleaseQuestControls();MelonLogger.Warning("Quest setup failed: "+ex.Message);}
  }
  private bool ReadQuestHand(ETrackedControllerRole role,out QuestSample sample) {
   sample=new QuestSample();uint id=system.GetTrackedDeviceIndexForControllerRole(role);
   if(id==OpenVR.k_unTrackedDeviceIndexInvalid)return false;
   var state=new VRControllerState_t();
   if(!system.GetControllerState(id,ref state,(uint)Marshal.SizeOf(typeof(VRControllerState_t))))return false;
   sample=new QuestSample{x=state.rAxis0.x,y=state.rAxis0.y,trigger=state.rAxis1.x,grip=state.rAxis2.x,buttons=state.ulButtonPressed,touches=state.ulButtonTouched};return true;
  }
  private readonly SprintTogglePolicy sprintToggle=new SprintTogglePolicy();
  private void PollQuestControllers() {
   try {
    uint foreground;GetWindowThreadProcessId(GetForegroundWindow(),out foreground);
    bool ready=QuestInputPolicy.Ready(system!=null,stopping,questFailed,questInputStopped);
    // SteamVR scene ownership and dashboard availability govern VR input.
    // Windows desktop focus must not require a mouse click in the headset.
    bool vrInput=ready&&QuestHasSceneFocus()&&system.IsInputAvailable()&&(OpenVR.Overlay==null||!OpenVR.Overlay.IsDashboardVisible());
    questAvailabilityValue=vrInput;questAvailabilityFrame=Time.frameCount;
    QuestSample left=new QuestSample(),right=new QuestSample();
    bool l=vrInput&&ReadQuestHand(ETrackedControllerRole.LeftHand,out left),r=vrInput&&ReadQuestHand(ETrackedControllerRole.RightHand,out right);
    questFrame=questPolicy.UpdateHands(ready&&vrInput,l,r,left,right);
    if(PollVrDeathInput(vrInput,questFrame.buttons))return;
    gunVisualTrigger=vrInput&&r&&questPolicy.RightArmed?right.trigger:0;
    manualLeftGrip=vrInput&&l&&questPolicy.LeftArmed&&(left.grip>.55f||left.Button(2));
    modMenuRightGrip=vrInput&&r&&questPolicy.RightArmed&&(right.grip>.55f||right.Button(2));
    hudPlacementLeftTrigger=vrInput&&l&&questPolicy.LeftArmed?left.trigger:0;hudPlacementRightTrigger=vrInput&&r&&questPolicy.RightArmed?right.trigger:0;hudPlacementBack=vrInput&&r&&questPolicy.RightArmed&&right.Button(1);
    PollPortableRadio(vrInput&&l&&r,left,right);
    PollReportShortcut(vrInput&&l&&questPolicy.LeftArmed&&!heldReceiver,left.grip>.55f||left.Button(2),left.Button(1));
    PollModMenu();PollPauseHold(vrInput&&l,left.Button(7));PollFlashlightShortcut(vrInput&&l,left);
    PollHandWheel(vrInput&&r,right.Button(32));
    PollQuickHealth(vrInput&&r,right.trigger>.55f||right.Button(33));
     PollHeldModules(vrInput&&l&&r,left,right);
    FilterManualReloadInput();
    PollSwingProd();PollProdAudio();PollThermite();PollManualFlares();
    
    bool sprintAllowed=vrInput&&l&&questPolicy.LeftArmed&&!modMenuOpen&&!quickSelect.Open&&WeaponGameplayReady()&&!global::PlayerState.paused&&!global::PlayerState.cutscene&&!global::PlayerState.eventScreen&&!global::PlayerState.suspendInput;
    if(sprintToggle.Update(sprintAllowed,left.Button(32),right.Button(32)))questFrame.buttons|=1<<4;
    PollQuestMenus();PollReceiverGrips(vrInput&&l&&r,left,right);RecordQuestActivity();UpdateQuestInputMode();SnapshotQuestNative();
    // Optional observers must never reach the input-failure latch.
    try{TraceQuestLook(right.x,right.y,r);}catch(Exception ex){if(Time.realtimeSinceStartup>=nextQuestLookLog){nextQuestLookLog=Time.realtimeSinceStartup+10;MelonLogger.Warning("VR LOOK observer: "+ex.Message);}}

    if(!ready)return;
    string status="focus="+(foreground==questProcessId)+" vrInput="+vrInput+" left="+l+" right="+r+" armed="+questPolicy.Armed+" leftArmed="+questPolicy.LeftArmed+" rightArmed="+questPolicy.RightArmed;
    if(status!=lastQuestStatus||Time.realtimeSinceStartup>=nextQuestLog) {
     lastQuestStatus=status;nextQuestLog=Time.realtimeSinceStartup+3f;
     MelonLogger.Msg("QUEST INPUT: "+status+" buttons="+questFrame.buttons+" move="+questFrame.moveX+","+questFrame.moveY+" look="+questFrame.lookX+","+questFrame.lookY);
    }
   }catch(Exception ex){questFailed=true;ReleaseQuestControls();MelonLogger.Warning("Quest input stopped: "+ex.Message);}
  }
  private void ReleaseQuestControls() {

   ClearComputerScreenIsolation();
   
   radioButtons.Update(false,false,false,0);radioPowerButton.Update(false,false);
   receiverGripRepeat.Update(false,false,false,0);
   quickSelect.Reset();quickGestureHeld=false;quickUse.Reset();HideQuickHealth();HideQuickSelect();sprintToggle.Reset();ResetProdSample();StopProdAudio();ClearProdFrozen();HideSwingProd();
   questPolicy.Update(false,new QuestSample(),new QuestSample());questFrame=new QuestFrame();gunVisualTrigger=0;questLastActivity=-100f;questNativeAvailable=false;questNativeEdges.Reset();PollQuestMenus();

  }
 }
}


















