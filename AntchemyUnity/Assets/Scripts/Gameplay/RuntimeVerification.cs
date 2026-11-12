using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Antchemy
{
    // Opt-in actual simulation verification. Completely dormant during normal play.
    public class RuntimeVerification : MonoBehaviour
    {
        private int _failures;
        private static int _runtimeErrors;
        private bool _finished;
        private float _watchdog;
        private void Awake() { _watchdog=Time.realtimeSinceStartup+190; }
        private void Update() { if(!_finished&&Time.realtimeSinceStartup>_watchdog) { Check(false,"simulation watchdog timeout"); Finish(); } }
        private static bool Requested()
        {
            bool requested=Array.IndexOf(Environment.GetCommandLineArgs(),"-colony-smoke-test")>=0;
#if UNITY_EDITOR
            requested|=UnityEditor.SessionState.GetBool("Antchemy.SmokeTest",false);
#endif
            return requested;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeginErrorTracking()
        {
            if(!Requested()) return;
            _runtimeErrors=0; Application.logMessageReceived-=CaptureLog; Application.logMessageReceived+=CaptureLog;
        }
        private static void CaptureLog(string message,string stackTrace,LogType type)
        {
            if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert) return;
            if(stackTrace.Contains("UnityEditor.Search")||message.StartsWith("COLONY TEST FAIL:")) return;
            _runtimeErrors++;
        }
        private int _savedLevel,_savedCoins;
        private bool _hadLevel,_hadCoins;
        private readonly Dictionary<string,int?> _savedAudio = new Dictionary<string,int?>();
        private readonly Dictionary<string,int?> _savedProgress = new Dictionary<string,int?>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(Requested()) new GameObject("RuntimeVerification").AddComponent<RuntimeVerification>();
        }
        private void Check(bool condition,string description)
        {
            if(condition) Debug.Log("COLONY TEST PASS: "+description);
            else { _failures++; Debug.LogError("COLONY TEST FAIL: "+description); }
        }
        private IEnumerator Start()
        {
            _hadLevel=PlayerPrefs.HasKey("Antchemy_Level"); _hadCoins=PlayerPrefs.HasKey("Antchemy_Coins");
            _savedLevel=PlayerPrefs.GetInt("Antchemy_Level",1); _savedCoins=PlayerPrefs.GetInt("Antchemy_Coins",250);
            foreach(string key in new[]{"Antchemy_UnlockedLevel","Antchemy_ResumeLevel"})
                _savedProgress[key]=PlayerPrefs.HasKey(key)?(int?)PlayerPrefs.GetInt(key):null;
            foreach(string key in new[]{"Antchemy_Muted","Antchemy_MusicEnabled","Antchemy_SoundEnabled"})
                _savedAudio[key]=PlayerPrefs.HasKey(key)?(int?)PlayerPrefs.GetInt(key):null;
            ProgressionVerification.Run(Check);
            yield return null;
            GoogleAdProvider.PrepareCallbacks();
            bool earlyAdCallback=false;
            float callbackTimeScale=Time.timeScale;Time.timeScale=0;
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(()=>earlyAdCallback=true);
            yield return null;yield return null;
            Check(earlyAdCallback && GoogleMobileAds.Common.MobileAdsEventExecutor.IsActive(),"consent callback queue drains on Unity's main thread before SDK initialization even while paused");
            Time.timeScale=callbackTimeScale;
            AdVerification.Run(Check);
            var manager=LevelManager.Instance;
            if(manager==null) { Check(false,"LevelManager exists"); Finish(); yield break; }
            var ui = UIManager.Instance;
            Check(ui != null && ui.IsHomeVisible && Time.timeScale == 0 && GameManager.Instance.CurrentState == GameState.MainMenu,"launch opens Home and pauses the board");
            int boardBeforeSettings=PictureGrid.Instance.TotalRemainingCubes;
            int levelBeforeSettings=GameManager.Instance.CurrentLevel, coinsBeforeSettings=GameManager.Instance.Coins;
            FindButton(ui,"Open Settings").onClick.Invoke();
            Check(ui.IsSettingsVisible && !ui.IsHomeVisible && ui.HasOpenModal && Time.timeScale==0,"Home Settings button opens a dedicated page and keeps the board paused");
            Check(FindButton(ui,"Share game")!=null && FindButton(ui,"Rate us")!=null && FindButton(ui,"Privacy Policy")!=null && FindButton(ui,"Contact us")!=null,"Settings exposes Share, Rate Us, Privacy Policy and Contact Us buttons");
            Check(AppLinks.TryWebUrl("https://example.com/game",out string webUrl) && webUrl=="https://example.com/game" && !AppLinks.TryWebUrl("javascript:alert(1)",out _) && !AppLinks.TryWebUrl("file:///tmp/test",out _),"release links accept web destinations and reject script or local-file URLs");
            Check(AppLinks.TryContactUrl("support@example.com",out string emailUrl) && emailUrl.StartsWith("mailto:support%40example.com?subject=") && emailUrl.Contains("&body=") && !AppLinks.TryContactUrl("support@example.com\nBcc:other@example.com",out _) && !AppLinks.TryContactUrl("not-an-email",out _),"Contact Us builds an encoded email draft and rejects invalid contact data");
            var links=AppLinks.Settings;
            string savedStoreUrl=links.storeUrl, savedPolicyUrl=links.privacyPolicyUrl, savedSupportEmail=links.supportEmail;
            links.storeUrl="https://example.com/game";
            Check(AppLinks.ShareText().EndsWith("https://example.com/game"),"Share message includes the configured app link");
            links.storeUrl=""; links.privacyPolicyUrl=""; links.supportEmail="";
            FindButton(ui,"Share game").onClick.Invoke();
            Check(ui.IsSettingsDialogVisible && ui.IsSettingsVisible && Time.timeScale==0,"Share opens a copy-message dialog without leaving Settings or resuming play");
            FindButton(ui,"Settings message Back").onClick.Invoke();
            Check(!ui.IsSettingsDialogVisible && ui.IsSettingsVisible,"Share dialog Back returns to Settings");
            foreach(string buttonName in new[]{"Rate us","Privacy Policy","Contact us"})
            {
                FindButton(ui,buttonName).onClick.Invoke();
                Check(ui.IsSettingsDialogVisible && Time.timeScale==0,buttonName+" explains unavailable release details without opening an invalid destination");
                FindButton(ui,"Settings message action").onClick.Invoke();
            }
            links.storeUrl=savedStoreUrl; links.privacyPolicyUrl=savedPolicyUrl; links.supportEmail=savedSupportEmail;
            var audio=AudioManager.Instance;
            var sources=audio.GetComponents<AudioSource>();
            PlayerPrefs.SetInt("Antchemy_Muted",1);
            PlayerPrefs.DeleteKey("Antchemy_MusicEnabled"); PlayerPrefs.DeleteKey("Antchemy_SoundEnabled");
            audio.ReloadPreferences();
            Check(!audio.MusicEnabled && !audio.SoundEnabled && sources[0].mute && sources[1].mute,"existing muted installations retain silence when loading separate audio settings");
            PlayerPrefs.SetInt("Antchemy_Muted",0); audio.ReloadPreferences(); ui.OpenSettings();
            FindButton(ui,"Music setting").onClick.Invoke();
            Check(!audio.MusicEnabled && audio.SoundEnabled && sources[0].mute && !sources[1].mute && PlayerPrefs.GetInt("Antchemy_MusicEnabled",-1)==0 && FindButton(ui,"Music setting").GetComponentInChildren<UnityEngine.UI.Text>().text=="OFF","Music switch mutes and saves music independently of sound effects");
            FindButton(ui,"Sound effects setting").onClick.Invoke();
            Check(!audio.MusicEnabled && !audio.SoundEnabled && sources[1].mute && PlayerPrefs.GetInt("Antchemy_SoundEnabled",-1)==0,"Sound Effects switch mutes and saves effects independently");
            FindButton(ui,"Music setting").onClick.Invoke(); audio.ReloadPreferences();
            Check(audio.MusicEnabled && !audio.SoundEnabled && !sources[0].mute && sources[1].mute,"mixed audio choices reload correctly from saved preferences");
            FindButton(ui,"Settings Back to Home").onClick.Invoke();
            Check(ui.IsHomeVisible && !ui.IsSettingsVisible && Time.timeScale==0 && PictureGrid.Instance.TotalRemainingCubes==boardBeforeSettings && GameManager.Instance.CurrentLevel==levelBeforeSettings && GameManager.Instance.Coins==coinsBeforeSettings,"Settings Back returns to paused Home with board, level and coins unchanged");
            FindButton(ui,"Open Settings").onClick.Invoke();
            Check(FindButton(ui,"Music setting").GetComponentInChildren<UnityEngine.UI.Text>().text=="ON" && FindButton(ui,"Sound effects setting").GetComponentInChildren<UnityEngine.UI.Text>().text=="OFF","reopening Settings displays the saved independent switch states");
            RestoreAudioPreferences(); ui.CloseSettings();
            string homeScreenshot=Argument("-colony-home-capture");
            if(!string.IsNullOrEmpty(homeScreenshot))
            {
                string directory=Path.GetDirectoryName(homeScreenshot);
                if(!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                ScreenCapture.CaptureScreenshot(homeScreenshot);
                yield return new WaitForSecondsRealtime(.4f);
            }
            ui.OpenPictures();
            Check(ui.IsPicturePickerVisible && Time.timeScale == 0,"Home opens the picture collection without starting play");
            ui.ShowPicturePage(1);
            bool cardLocksCorrect=true;
            foreach(var card in ui.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                if(!card.name.StartsWith("Picture ") || !int.TryParse(card.name.Substring(8),out int number)) continue;
                bool unlocked=GameManager.Instance.IsLevelUnlocked(number);
                var image=card.GetComponentInChildren<UnityEngine.UI.RawImage>();
                cardLocksCorrect &= card.interactable==unlocked && (card.transform.Find("Locked picture")!=null)==!unlocked && image!=null && (unlocked?image.color.a==1:image.color.a<.5f);
            }
            Check(cardLocksCorrect,"collection cards match unlock state with disabled buttons, dimmed previews and padlocks");
            int blockedLevel=GameManager.Instance.HighestUnlockedLevel+1;
            int beforeBlockedLevel=GameManager.Instance.CurrentLevel, beforeBlockedBoard=manager.LoadedLevelNumber;
            ui.PlayPicture(blockedLevel);
            bool directBlocked=!GameManager.Instance.SelectLevel(blockedLevel);
            Check(directBlocked && ui.IsPicturePickerVisible && Time.timeScale==0 && GameManager.Instance.CurrentLevel==beforeBlockedLevel && manager.LoadedLevelNumber==beforeBlockedBoard,"locked selection is rejected by both UI and game manager without dismissing the paused collection");
            var previews=ui.GetComponentsInChildren<UnityEngine.UI.RawImage>();
            int previewCount=0; bool validPreviews=true; Texture atlas=null; var regions=new HashSet<Rect>();
            foreach(var preview in previews)
            {
                if(preview.name!="Puzzle preview") continue;
                previewCount++;
                if(atlas==null) atlas=preview.texture;
                validPreviews &= preview.texture!=null && preview.texture==atlas && !preview.raycastTarget;
                regions.Add(preview.uvRect);
            }
            Check(previewCount==100 && validPreviews && atlas!=null && atlas.width==1600 && atlas.height==1600 && regions.Count==100,"all 100 cards display distinct previews from one loaded atlas without blocking taps");
            FindButton(ui,"NEXT >").onClick.Invoke();
            Check(ui.CurrentPicturePage==2 && FindButton(ui,"Picture 101")!=null && FindButton(ui,"Picture 200")!=null && FindButton(ui,"Picture 1")==null,"Next shows the next hundred pictures");
            FindButton(ui,"< PREVIOUS").onClick.Invoke();
            Check(ui.CurrentPicturePage==1 && FindButton(ui,"Picture 1")!=null,"Previous returns to the first picture page");
            ui.ShowPicturePage(27);
            var lastCard=FindButton(ui,"Picture 2700");
            var lastPreview=lastCard!=null?lastCard.GetComponentInChildren<UnityEngine.UI.RawImage>():null;
            Check(lastPreview!=null && lastPreview.texture!=null && lastPreview.texture!=atlas && lastCard.GetComponentInChildren<UnityEngine.UI.Text>().text=="2700","last page displays picture 2700 with its own atlas and level number");
            ui.ShowPicturePage(28);
            Check(ui.CurrentPicturePage==27 && FindButton(ui,"Picture 2700")!=null,"collection pagination stops at the last available picture");
            yield return UILayoutVerification.CheckCollection(ui,Check);
            ui.ClosePictures();
            Check(ui.IsHomeVisible && !ui.IsPicturePickerVisible && Time.timeScale == 0,"collection Back returns to Home");
            ui.OnClickPlay();
            Check(!ui.IsHomeVisible && Time.timeScale > 0 && GameManager.Instance.CurrentState == GameState.InGame,"Home Play starts the saved board");
            manager.LoadLevel(1);
            yield return new WaitForSeconds(.6f);
            UILayoutVerification.Run(ui,Check);
            Check(manager.LoadedLevelNumber==1&&manager.SlotCount==3,"level 1 loads with three empty nests");
            var grid=PictureGrid.Instance;
            int initial=grid.TotalRemainingCubes;
            Check(initial>0&&manager.OccupiedSlotCount==0,"board populated and no occupied nests");
            string screenshot=Argument("-colony-capture");
            if(!string.IsNullOrEmpty(screenshot))
            {
                string directory=Path.GetDirectoryName(screenshot);
                if(!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                ScreenCapture.CaptureScreenshot(screenshot);
                yield return new WaitForSeconds(.4f);
            }
            Colony selected=null;
            foreach(var lane in manager.Lanes)
                if(lane.FrontColony!=null&&grid.HasAvailableTarget(lane.FrontColony.ColorIndex)) { selected=lane.FrontColony; break; }
            Check(selected!=null,"native level 1 has a reachable queue front");
            if(selected!=null)
            {
                int queued=manager.QueuedColonyCount;
                manager.OnColonyTapped(selected); manager.OnColonyTapped(selected);
                Check(manager.OccupiedSlotCount==1&&manager.QueuedColonyCount==queued-1,"double tap assigns exactly one colony");
                Check(selected.transform.parent==selected.AssignedSlot.transform,"selected colony reparented to nest before queue advances");
                yield return new WaitForSeconds(.5f);
                Check(Projectile.InFlightCount>0,"worker ants actually dispatch");
                int beforeHome=grid.TotalRemainingCubes, occupiedBeforeHome=manager.OccupiedSlotCount;
                ui.OnClickHome();
                ui.OpenSettings();
                yield return new WaitForSecondsRealtime(.25f);
                Check(ui.IsSettingsVisible && grid.TotalRemainingCubes==beforeHome && manager.OccupiedSlotCount==occupiedBeforeHome,"Settings keeps an in-progress board and its occupied nests frozen");
                ui.CloseSettings();
                Check(ui.IsHomeVisible && grid.TotalRemainingCubes==beforeHome && manager.OccupiedSlotCount==occupiedBeforeHome,"Home freezes the live board and occupied nests");
                ui.OnClickPlay();
                Check(!ui.IsHomeVisible && grid.TotalRemainingCubes==beforeHome && manager.OccupiedSlotCount==occupiedBeforeHome,"Continue resumes the same board without restarting");
                yield return new WaitForSeconds(.8f);
                Check(grid.TotalRemainingCubes<initial,"worker ants collect native level cubes");
                manager.LoadLevel(1);
                Check(Projectile.InFlightCount==0&&grid.ReservedCubeCount==0,"restart cancels workers and reservations");
                Check(manager.OccupiedSlotCount==0&&grid.TotalRemainingCubes==initial,"restart rebuilds initial board and empty nests");
                yield return new WaitForSeconds(.9f);
                Check(grid.TotalRemainingCubes==initial,"cancelled workers cannot mutate restarted board");
            }
            ui.OnClickHome(); ui.OpenPictures(); ui.ShowPicturePage(1); FindButton(ui,"Picture 1").onClick.Invoke();
            Check(!ui.IsHomeVisible && !ui.IsPicturePickerVisible && manager.LoadedLevelNumber==1 && Time.timeScale>0,"choosing an unlocked Home picture starts that exact level");
            ui.OpenPictures(); ui.ClosePictures();
            Check(!ui.IsHomeVisible && manager.LoadedLevelNumber==1 && Time.timeScale>0,"in-game picture picker Back resumes gameplay");
            AdVerification.VerifyBoardReward(manager,ui,RingLevel(),Check);
            // Dense ring: center is inaccessible until one edge cell is removed.
            var ring=RingLevel(); manager.LoadLevelData(ring,901); grid=PictureGrid.Instance;
            Check(!grid.IsExposed(4)&&grid.GetTargetCube(0,Vector3.zero)==null,"enclosed center is not collectible");
            var route=grid.GetPathToCube(0,new Vector3(-2,-2,0));
            Check(route.Count>=3,"exposed cube has an exterior route");
            for(int i=0;i<route.Count-1;i++) Check(!grid.IsWorldPositionOccupied(route[i]),"outbound route avoids occupied cells #"+i);
            var reserved=new System.Collections.Generic.List<Tile>();
            for(int i=0;i<8;i++)
            {
                var target=grid.ReserveTargetCube(1,Vector3.zero);
                Check(target!=null&&!reserved.Contains(target),"frontier reservations are unique #"+i);
                if(target!=null) reserved.Add(target);
            }
            Check(grid.ReserveTargetCube(1,Vector3.zero)==null,"reserved frontier cannot be targeted twice");
            foreach(var cube in reserved) cube.ReleaseReservation();
            grid.Cubes[1].HitAndDestroy();
            Check(grid.IsExposed(4)&&grid.HasAvailableTarget(0),"removing exterior edge opens route to center");
            yield return null;
            // Five colonies whose only color is enclosed: they retain capacity and fail after settling.
            manager.LoadLevelData(RingLevel(),902);
            foreach(var lane in manager.Lanes) manager.OnColonyTapped(lane.FrontColony);
            manager.EvaluateGameState();
            Check(!manager.IsLevelEnded,"moving colonies prevent premature loss");
            yield return new WaitForSeconds(.6f);
            manager.EvaluateGameState();
            Check(manager.IsLevelEnded&&manager.OccupiedSlotCount==3,"all three blocked nests lose after settling");
            foreach(var slot in manager.Nests) Check(slot.OccupyingColony!=null&&slot.OccupyingColony.AntCount==1,"blocked colony retains its ant capacity");
            Check(manager.TryAddSlot()&&!manager.IsLevelEnded&&manager.SlotCount==4,"extra nest revives failed level");
            manager.LoadLevelData(RingLevel(),903);
            var front=manager.Lanes[0].FrontColony;
            manager.OnColonyTapped(front);
            Check(manager.CanUndoLastMove&&manager.TryUndoLastMove(),"latest undispached selection supports undo");
            Check(manager.OccupiedSlotCount==0&&manager.Lanes[0].FrontColony==front,"undo restores queue front and empty nest");
            // Reachable matching work means a fully occupied board must remain live.
            var reachable=RingLevel(); reachable.ColorData[0].ColorIndex=0;
            manager.LoadLevelData(reachable,904);
            foreach(var lane in manager.Lanes) manager.OnColonyTapped(lane.FrontColony);
            yield return new WaitForSeconds(.3f);
            manager.EvaluateGameState();
            Check(!manager.IsLevelEnded,"full nests with reachable matching work remain active");
            // Hidden food is revealed by exterior reachability; hidden queued colonies reveal at front.
            var hidden=RingLevel(); hidden.HiddenData=new HiddenData {HiddenCubeDataIds=new List<int>{4}};
            hidden.LaneData[0].ColonyDatas.Add(new ColonyData {ColorIndex=1,AntCount=1,IsHidden=true});
            manager.LoadLevelData(hidden,905); grid=PictureGrid.Instance;
            Check(grid.Cubes[4].IsHidden&&manager.Lanes[0].Colonies[1].IsHidden,"hidden food and rear colony conceal their information");
            grid.Cubes[1].HitAndDestroy();
            Check(!grid.Cubes[4].IsHidden,"newly selectable hidden food reveals");
            manager.OnColonyTapped(manager.Lanes[0].FrontColony);
            Check(!manager.Lanes[0].FrontColony.IsHidden,"queued hidden colony reveals when it reaches front");
            // A link may include two consecutive colonies in one column and another column's front.
            var linked=RingLevel(); linked.LaneData[0].ColonyDatas.Add(new ColonyData {ColorIndex=0,AntCount=1});
            linked.LinkedColonies=new List<LinkedColonyData>{new LinkedColonyData {Ids=new List<ColonyPositionData>{Position(0,0),Position(0,1),Position(1,0)}}};
            manager.LoadLevelData(linked,906);
            var linkedFront=manager.Lanes[0].FrontColony; int before=manager.QueuedColonyCount;
            manager.OnColonyTapped(linkedFront); manager.OnColonyTapped(linkedFront);
            Check(manager.OccupiedSlotCount==3&&manager.QueuedColonyCount==before-3,"linked contiguous prefixes deploy atomically into three nests");
            var blockedLink=RingLevel(); blockedLink.LaneData[1].ColonyDatas.Add(new ColonyData {ColorIndex=0,AntCount=1});
            blockedLink.LinkedColonies=new List<LinkedColonyData>{new LinkedColonyData {Ids=new List<ColonyPositionData>{Position(0,0),Position(1,1)}}};
            manager.LoadLevelData(blockedLink,907); before=manager.QueuedColonyCount;
            manager.OnColonyTapped(manager.Lanes[0].FrontColony);
            Check(manager.OccupiedSlotCount==0&&manager.QueuedColonyCount==before,"linked group cannot bypass unrelated colony in another column");
            // Linked nests also retire atomically; a drained member holds its nest while siblings work.
            var linkedWork=RingLevel(); linkedWork.LaneData[0].ColonyDatas[0].ColorIndex=1;
            linkedWork.LinkedColonies=new List<LinkedColonyData>{new LinkedColonyData {Ids=new List<ColonyPositionData>{Position(0,0),Position(1,0)}}};
            manager.LoadLevelData(linkedWork,916); grid=PictureGrid.Instance;
            var early=manager.Lanes[0].FrontColony; var late=manager.Lanes[1].FrontColony;
            manager.OnColonyTapped(early); float linkedDeadline=Time.realtimeSinceStartup+8;
            while(early!=null&&(early.AntCount>0||early.InFlightCount>0)&&Time.realtimeSinceStartup<linkedDeadline) yield return new WaitForSeconds(.1f);
            Check(early!=null&&early.CurrentState==ColonyState.ActiveShooting&&early.AssignedSlot!=null&&manager.OccupiedSlotCount==2,"drained linked member retains its occupied nest");
            grid.Cubes[1].HitAndDestroy(); linkedDeadline=Time.realtimeSinceStartup+8;
            while(manager.OccupiedSlotCount>0&&Time.realtimeSinceStartup<linkedDeadline) yield return new WaitForSeconds(.1f);
            Check(manager.OccupiedSlotCount==0,"linked nests release only once every member finishes and returns");
            // One free nest cannot fit a remaining linked group requiring two nests.
            var tooLarge=RingLevel();
            tooLarge.LaneData[0].ColonyDatas.Add(new ColonyData {ColorIndex=0,AntCount=1});
            tooLarge.LaneData[1].ColonyDatas.Add(new ColonyData {ColorIndex=0,AntCount=1});
            tooLarge.LaneData[4].ColonyDatas.Clear();
            tooLarge.LinkedColonies=new List<LinkedColonyData>{new LinkedColonyData {Ids=new List<ColonyPositionData>{Position(0,1),Position(1,1)}}};
            manager.LoadLevelData(tooLarge,917);
            for(int i=0;i<4;i++) manager.OnColonyTapped(manager.Lanes[i].FrontColony);
            yield return new WaitForSeconds(.6f); manager.EvaluateGameState();
            Check(manager.IsLevelEnded&&manager.OccupiedSlotCount==4&&manager.LastLoseReason==LoseReason.OutOfSlot,"blocked occupied nests plus linked group larger than remaining space cause loss");
            // A drained linked member must not conceal a deadlock when its sibling cannot work.
            manager.LoadLevelData(linkedWork,918);
            early=manager.Lanes[0].FrontColony; manager.OnColonyTapped(early);
            for(int i=2;i<5;i++) manager.OnColonyTapped(manager.Lanes[i].FrontColony);
            linkedDeadline=Time.realtimeSinceStartup+8;
            while(!manager.IsLevelEnded&&Time.realtimeSinceStartup<linkedDeadline) yield return new WaitForSeconds(.1f);
            Check(manager.IsLevelEnded&&manager.OccupiedSlotCount==3&&early!=null&&early.AntCount==0,"drained linked member does not mask a fully blocked board");
            var allFrozen=RingLevel(); foreach(var column in allFrozen.LaneData) column.ColonyDatas[0].IceHp=1;
            manager.LoadLevelData(allFrozen,919); yield return new WaitForSeconds(.6f);
            Check(manager.IsLevelEnded&&manager.LastLoseReason==LoseReason.LockStuck,"all gated queue fronts with no active work report LockStuck");
            // Keys are empty road footprints; a front lock opens once any footprint tile is reachable.
            var locked=RingLevel(); locked.ColorData.RemoveAt(1);
            locked.LaneData[0].ColonyDatas.Insert(0,new ColonyData {ColorIndex=4,AntCount=0});
            locked.LockKeys=new List<LockKeyData>{new LockKeyData {LockPos=Position(0,0),PixelIds=new List<int>{4}}};
            manager.LoadLevelData(locked,908); grid=PictureGrid.Instance;
            Check(manager.Lanes[0].FrontColony.IsLockPlaceholder&&!grid.IsEmptyReachable(4),"enclosed key keeps front placeholder locked");
            manager.OnColonyTapped(manager.Lanes[0].FrontColony);
            Check(manager.OccupiedSlotCount==0,"zero-capacity lock cannot be deployed as a colony");
            grid.Cubes[1].HitAndDestroy();
            Check(!manager.Lanes[0].FrontColony.IsLockPlaceholder&&grid.IsEmptyReachable(4),"reachable key removes front lock without requiring key cells collected");
            // Overlapping blockers release independently; cookies count globally, doors count matching color.
            var blocked=RingLevel(); blocked.ColorData.RemoveAt(1); blocked.ColorData[0].CubeDataIds.Remove(0);
            blocked.ColorData.Add(new ColorGroup {ColorIndex=0,CubeDataIds=new List<int>{0}});
            blocked.SealedTiles.Add(new SealedTileData {PixelIds=new List<int>{4},RequireCount=3});
            blocked.Gates.Add(new GateData {PixelIds=new List<int>{4},RequireColor=1,RequireCount=1});
            manager.LoadLevelData(blocked,909); grid=PictureGrid.Instance;
            Check(grid.IsBlocked(4)&&!grid.IsEmptyReachable(4),"empty blocker footprint obstructs exterior flood-fill");
            grid.Cubes[0].HitAndDestroy();
            Check(BlockerManager.ActiveCount==2,"wrong-color collection advances cookie but not hungry door");
            grid.Cubes[1].HitAndDestroy();
            Check(BlockerManager.ActiveCount==1&&grid.IsBlocked(4),"matching pickup resolves hungry door while overlapping cookie stays blocked");
            grid.Cubes[3].HitAndDestroy();
            Check(BlockerManager.ActiveCount==0&&!grid.IsBlocked(4)&&grid.IsEmptyReachable(4),"cookie global pickup count releases empty road and rebuilds frontier");
            // Spawners preserve authored identities and FIFO order; tunnel covers target queue tokens.
            var spawned=RingLevel(); spawned.LaneData[0].ColonyDatas.Add(new ColonyData {ColorIndex=2,AntCount=1});
            spawned.LaneData[0].ColonySpawnerDatas.Add(new ColonySpawnerData {Index=1,ColonyDatas=new List<ColonyData>{new ColonyData {ColorIndex=3,AntCount=1},new ColonyData {ColorIndex=4,AntCount=1}}});
            spawned.LaneData[0].Burrows.Add(new BurrowData {StartIndex=1,CoverCount=1,RequireColor=1,RequireCount=1});
            manager.LoadLevelData(spawned,910); grid=PictureGrid.Instance;
            var fifo=manager.Lanes[0].Colonies;
            Check(fifo.Count==4&&fifo[0].ColorIndex==0&&fifo[1].ColorIndex==3&&fifo[2].ColorIndex==4&&fifo[3].ColorIndex==2,"spawner expands its FIFO before next authored colony");
            Check(fifo[0].SourceQueueIndex==0&&fifo[1].SourceQueueIndex==-1&&fifo[3].SourceQueueIndex==1,"spawner preserves original authored indices for links and locks");
            Check(!fifo[0].IsCoveredByTunnel&&fifo[1].IsCoveredByTunnel&&fifo[2].IsCoveredByTunnel&&!fifo[3].IsCoveredByTunnel,"tunnel covers spawner token and delays all its queued contents");
            grid.Cubes[4].HitAndDestroy();
            Check(fifo[1].IsCoveredByTunnel,"wrong-color global pickup leaves tunnel covered");
            grid.Cubes[0].HitAndDestroy();
            Check(!fifo[1].IsCoveredByTunnel&&!fifo[2].IsCoveredByTunnel,"matching global pickup releases tunnel cover");
            // A pipe replaces just-picked matching food while preserving exact finite demand.
            var pipeLevel=RingLevel(); pipeLevel.ConveyorData.Add(new ConveyorData {BeltPositions=new List<int>{4},BeltColors=new List<int>{1,1}});
            manager.LoadLevelData(pipeLevel,911); grid=PictureGrid.Instance;
            Check(grid.TotalInitialCubes==10&&grid.TotalRemainingCubes==10&&!grid.Cubes.ContainsKey(4)&&grid.IsBlocked(4),"pipe footprint excluded from food and pending colors counted");
            grid.Cubes[0].HitAndDestroy();
            Check(grid.Cubes.ContainsKey(0)&&grid.Cubes[0].ColorIndex==1&&ConveyorManager.PendingFoodCount==1&&grid.TotalRemainingCubes==9,"pipe refills matching picked location and consumes one queued color");
            grid.Cubes[0].HitAndDestroy();
            Check(ConveyorManager.PendingFoodCount==0&&!grid.IsBlocked(4)&&grid.TotalRemainingCubes==8,"pipe drains finite queue and opens footprint road");
            var soloPipe=new LevelData {SizeX=3,SizeY=3};
            soloPipe.ColorData.Add(new ColorGroup {ColorIndex=1,CubeDataIds=new List<int>{0}});
            soloPipe.ConveyorData.Add(new ConveyorData {BeltPositions=new List<int>{4},BeltColors=new List<int>{1,1}});
            manager.LoadLevelData(soloPipe,914); grid=PictureGrid.Instance;
            Check(grid.TotalRemainingCubes==3&&!manager.IsLevelEnded,"solo pipe includes future food in victory demand");
            grid.Cubes[0].HitAndDestroy(); Check(grid.TotalRemainingCubes==2&&!manager.IsLevelEnded,"first solo pipe pickup cannot win early");
            grid.Cubes[0].HitAndDestroy(); Check(grid.TotalRemainingCubes==1&&!manager.IsLevelEnded&&!grid.IsBlocked(4),"drained pipe still requires its last refilled food");
            grid.Cubes[0].HitAndDestroy(); Check(grid.TotalRemainingCubes==0&&manager.IsLevelEnded,"final refilled food completes solo pipe puzzle");
            var optional=JsonUtility.FromJson<LevelData>("{\"SizeX\":1,\"SizeY\":1,\"ColorData\":null,\"LaneData\":null,\"HiddenData\":{\"HiddenCubeDataIds\":null},\"LinkedColonies\":null,\"LockKeys\":null,\"ConveyorData\":null,\"SealedTiles\":null,\"Gates\":null}");
            manager.LoadLevelData(optional,915);
            Check(optional.ColorData!=null&&optional.LaneData!=null&&optional.HiddenData.HiddenCubeDataIds!=null&&optional.ConveyorData!=null,"explicit null source collections normalize before build");
            // Dynamite and rock settle only when both matching placeholders reach their fronts.
            var obstacles=RingLevel(); obstacles.LaneData[0].ColonyDatas.Insert(0,new ColonyData {AntCount=0,ObstacleType=1,ObstaclePairId=1});
            obstacles.LaneData[1].ColonyDatas.Add(new ColonyData {AntCount=0,ObstacleType=2,ObstaclePairId=1});
            manager.LoadLevelData(obstacles,912); before=manager.QueuedColonyCount;
            Check(manager.Lanes[0].FrontColony.ObstacleType==1,"unpaired front dynamite waits for rock");
            manager.OnColonyTapped(manager.Lanes[1].FrontColony);
            Check(manager.QueuedColonyCount==before-3&&manager.Lanes[0].FrontColony.ObstacleType==0,"matched front dynamite and rock resolve together");
            // Ice counts completed colonies, rather than collected food.
            var icy=RingLevel(); icy.LaneData[0].ColonyDatas[0].IceHp=2;
            icy.LaneData[1].ColonyDatas[0].ColorIndex=1; icy.LaneData[2].ColonyDatas[0].ColorIndex=1;
            manager.LoadLevelData(icy,913); var frozen=manager.Lanes[0].FrontColony;
            manager.OnColonyTapped(frozen); Check(manager.OccupiedSlotCount==0&&frozen.IceHp==2,"ice prevents colony selection");
            manager.OnColonyTapped(manager.Lanes[1].FrontColony); manager.OnColonyTapped(manager.Lanes[2].FrontColony);
            float thawDeadline=Time.realtimeSinceStartup+8;
            while(frozen.IceHp>0&&Time.realtimeSinceStartup<thawDeadline) yield return new WaitForSeconds(.1f);
            Check(frozen.IceHp==0&&frozen.CanDeploy,"two completed colonies remove exactly two ice layers");
            // Representative real content across the full package parses and builds without data loss.
            foreach(int source in new[]{121,214,567,1532,2330,2700})
            {
                manager.LoadLevel(source);
                Check(manager.SourceLevelNumber==source&&PictureGrid.Instance.TotalInitialCubes>0,"native advanced source loads #"+source);
                yield return null;
            }
            manager.LoadLevelData(RingLevel(),908);
            foreach(var lane in manager.Lanes) manager.OnColonyTapped(lane.FrontColony);
            yield return new WaitForSeconds(.6f);manager.EvaluateGameState();
            ui.OnClickHome();ui.OnClickPlay();
            Check(!manager.IsLevelEnded && manager.LoadedLevelNumber==GameManager.Instance.CurrentLevel,"Play from a failed board restarts the current picture");
            // Exercise a complete native puzzle through the same public tap path used by input.
            var game=GameManager.Instance;
            if(game!=null) game.SelectLevel(1); else manager.LoadLevel(1);
            int coinsBefore=game!=null?game.Coins:0;
            int unlockedBefore=game!=null?game.HighestUnlockedLevel:1;
            float deadline=Time.realtimeSinceStartup+120;
            while(!manager.IsLevelEnded&&Time.realtimeSinceStartup<deadline)
            {
                if(manager.GetFirstFreeSlot()!=null)
                {
                    foreach(var lane in manager.Lanes)
                    {
                        var candidate=lane.FrontColony;
                        if(candidate!=null&&!candidate.IsLockPlaceholder&&PictureGrid.Instance.HasAvailableTarget(candidate.ColorIndex)) manager.OnColonyTapped(candidate);
                    }
                }
                yield return new WaitForSeconds(.25f);
            }
            Check(manager.IsLevelEnded&&PictureGrid.Instance.TotalRemainingCubes==0,"native level 1 plays all the way to victory");
            if(game!=null)
            {
                Check(game.CurrentState==GameState.Win&&game.Coins==coinsBefore+50,"victory rewards once and enters win state");
                Check(game.HighestUnlockedLevel==Mathf.Max(unlockedBefore,2) && PlayerPrefs.GetInt("Antchemy_UnlockedLevel",0)==game.HighestUnlockedLevel && PlayerPrefs.GetInt("Antchemy_ResumeLevel",0)==2,"victory saves the next puzzle without losing earlier unlock progress");
                ui.OnClickHome(); ui.OpenPictures(); ui.ShowPicturePage(1);
                Check(FindButton(ui,"Picture 2").interactable && FindButton(ui,"Picture 2").transform.Find("Locked picture")==null,"winning refreshes the collection with the next picture playable");
                ui.ClosePictures(); ui.OnClickPlay();
                Check(game.CurrentLevel==2&&manager.LoadedLevelNumber==2&&!manager.IsLevelEnded,"next puzzle loads level 2");
                int secondCount=PictureGrid.Instance.TotalRemainingCubes;
                game.RestartLevel();
                Check(manager.LoadedLevelNumber==2&&PictureGrid.Instance.TotalRemainingCubes==secondCount&&manager.OccupiedSlotCount==0,"retry retains selected puzzle and resets board");
                Check(game.HighestUnlockedLevel==Mathf.Max(unlockedBefore,2) && PlayerPrefs.GetInt("Antchemy_UnlockedLevel",0)==game.HighestUnlockedLevel,"retry preserves unlocked progress");
            }
            yield return new WaitForSeconds(.1f);
            Finish();
        }
        private static ColonyPositionData Position(int lane,int colony) => new ColonyPositionData {LaneId=lane,ColonyId=colony};
        private static UnityEngine.UI.Button FindButton(UIManager ui,string name)
        {
            foreach(var button in ui.GetComponentsInChildren<UnityEngine.UI.Button>()) if(button.name==name) return button;
            return null;
        }
        private static LevelData RingLevel()
        {
            var level=new LevelData { SizeX=3,SizeY=3 };
            level.ColorData.Add(new ColorGroup { ColorIndex=1,CubeDataIds=new System.Collections.Generic.List<int>{0,1,2,3,5,6,7,8} });
            level.ColorData.Add(new ColorGroup { ColorIndex=0,CubeDataIds=new System.Collections.Generic.List<int>{4} });
            for(int i=0;i<5;i++)
            {
                var column=new LaneColumn(); column.ColonyDatas.Add(new ColonyData {ColorIndex=0,AntCount=1}); level.LaneData.Add(column);
            }
            return level;
        }
        private static string Argument(string name)
        {
            string[] args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,name);
            return index>=0&&index+1<args.Length?args[index+1]:null;
        }
        private void Finish()
        {
            if(_finished) return; _finished=true;
            Application.logMessageReceived-=CaptureLog;
            Check(_runtimeErrors==0,"simulation emitted no unexpected errors or exceptions");
            if(_hadLevel) PlayerPrefs.SetInt("Antchemy_Level",_savedLevel); else PlayerPrefs.DeleteKey("Antchemy_Level");
            if(_hadCoins) PlayerPrefs.SetInt("Antchemy_Coins",_savedCoins); else PlayerPrefs.DeleteKey("Antchemy_Coins");
            foreach(var saved in _savedProgress) if(saved.Value.HasValue) PlayerPrefs.SetInt(saved.Key,saved.Value.Value); else PlayerPrefs.DeleteKey(saved.Key);
            RestoreAudioPreferences();
            PlayerPrefs.Save();
            Debug.Log($"COLONY SMOKE RESULT: {(_failures==0?"PASS":"FAIL")}, failures={_failures}, runtimeErrors={_runtimeErrors}");
#if UNITY_EDITOR
            UnityEditor.SessionState.SetBool("Antchemy.SmokeTest",false);
            UnityEditor.EditorApplication.Exit(_failures==0?0:1);
#else
            Application.Quit(_failures==0?0:1);
#endif
        }
        private void RestoreAudioPreferences()
        {
            foreach(var saved in _savedAudio)
                if(saved.Value.HasValue) PlayerPrefs.SetInt(saved.Key,saved.Value.Value); else PlayerPrefs.DeleteKey(saved.Key);
            PlayerPrefs.Save();
            AudioManager.Instance?.ReloadPreferences();
        }
    }
}
