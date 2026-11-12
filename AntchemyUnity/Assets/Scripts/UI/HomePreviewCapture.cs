using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Antchemy
{
    // Optional visual QA capture; reads the saved board without changing progress or coins.
    public class HomePreviewCapture : MonoBehaviour
    {
        static string[] cachedArgs;
        // Command-line flags plus, for simulator captures on iOS (where flags cannot be passed), tokens from Documents/preview-args.txt.
        public static string[] PreviewArgs()
        {
            if(cachedArgs!=null) return cachedArgs;
            var list=new System.Collections.Generic.List<string>(Environment.GetCommandLineArgs());
            try
            {
                string file=Path.Combine(Application.persistentDataPath,"preview-args.txt");
                if(File.Exists(file)) list.AddRange(File.ReadAllText(file).Split(new[]{' ','\n','\r','\t'},StringSplitOptions.RemoveEmptyEntries));
            }
            catch(Exception) { }
            cachedArgs=list.ToArray();
            return cachedArgs;
        }
        string output;
        bool pictures;
        bool settings;
        bool opening;
        bool gameplay;
        public static bool Requested
        {
            get
            {
                foreach(string flag in new[]{"-colony-home-preview","-colony-picture-preview","-colony-settings-preview","-colony-opening-preview","-colony-gameplay-preview"})
                    if(Array.IndexOf(PreviewArgs(),flag)>=0) return true;
                return false;
            }
        }
        int page = 1;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            string[] arguments=PreviewArgs();
            int index=Array.IndexOf(arguments,"-colony-home-preview");
            bool pictures=false;
            if(index<0) { index=Array.IndexOf(arguments,"-colony-picture-preview"); pictures=true; }
            bool settings=false;
            if(index<0) { index=Array.IndexOf(arguments,"-colony-settings-preview"); pictures=false; settings=true; }
            bool opening=false;
            if(index<0) { index=Array.IndexOf(arguments,"-colony-opening-preview"); pictures=false; settings=false; opening=true; }
            bool gameplay=false;
            if(index<0) { index=Array.IndexOf(arguments,"-colony-gameplay-preview"); pictures=false; settings=false; opening=false; gameplay=true; }
            if(index<0 || index+1>=arguments.Length)return;
            var capture=new GameObject("Home visual verification").AddComponent<HomePreviewCapture>();
            capture.output=Path.IsPathRooted(arguments[index+1])?arguments[index+1]:Path.Combine(Application.persistentDataPath,arguments[index+1]);
            capture.pictures=pictures;
            capture.settings=settings;
            capture.opening=opening;
            capture.gameplay=gameplay;
            int pageIndex=Array.IndexOf(arguments,"-colony-picture-page");
            if(pageIndex>=0 && pageIndex+1<arguments.Length && int.TryParse(arguments[pageIndex+1],out int page)) capture.page=Mathf.Clamp(page,1,27);
        }
        IEnumerator Start()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(.4f);
            if(gameplay)
            {
                UIManager.Instance.OnClickPlay();
                int index=Array.IndexOf(PreviewArgs(),"-colony-preview-level");
                if(index>=0 && index+1<PreviewArgs().Length && int.TryParse(PreviewArgs()[index+1],out int level)) LevelManager.Instance.LoadLevel(level);
                yield return new WaitForSecondsRealtime(.2f);
                if(Array.IndexOf(PreviewArgs(),"-colony-preview-deploy-front")>=0)
                {
                    foreach(var lane in LevelManager.Instance.Lanes)
                        if(lane.FrontColony!=null&&lane.FrontColony.CanDeploy)
                        {
                            LevelManager.Instance.OnColonyTapped(lane.FrontColony);
                            break;
                        }
                    yield return new WaitForSecondsRealtime(.4f);
                }
                Canvas.ForceUpdateCanvases();
            }
            if(opening)
            {
                WelcomeSplash.Create(transform);
                Canvas.ForceUpdateCanvases();
                yield return null;
            }
            if(pictures)
            {
                UIManager.Instance.OpenPictures();
                UIManager.Instance.ShowPicturePage(page);
                yield return new WaitForSecondsRealtime(.2f);
                Canvas.ForceUpdateCanvases();
                int scrollIndex=Array.IndexOf(PreviewArgs(),"-colony-picture-scroll");
                if(scrollIndex>=0 && scrollIndex+1<PreviewArgs().Length && float.TryParse(PreviewArgs()[scrollIndex+1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float position))
                {
                    var scroll=UIManager.Instance.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
                    scroll.verticalNormalizedPosition=Mathf.Clamp01(position);
                    Canvas.ForceUpdateCanvases();
                    yield return null;
                }
                // Frame the existing player's last unlocked card and the first locked cards.
                if(Array.IndexOf(PreviewArgs(),"-colony-picture-focus-progress")>=0)
                {
                    int frontier=GameManager.Instance.HighestUnlockedLevel;
                    page=(frontier-1)/100+1;
                    UIManager.Instance.ShowPicturePage(page);
                    Canvas.ForceUpdateCanvases();
                    var scroll=UIManager.Instance.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
                    var grid=scroll.content.GetComponent<UnityEngine.UI.GridLayoutGroup>();
                    int row=((frontier-1)%100)/grid.constraintCount;
                    float offset=Mathf.Max(0,row-1)*(grid.cellSize.y+grid.spacing.y);
                    float extent=scroll.content.rect.height-scroll.viewport.rect.height;
                    scroll.verticalNormalizedPosition=extent>0?1-Mathf.Clamp01(offset/extent):1;
                    yield return new WaitForSecondsRealtime(.1f);
                }
            }
            if(settings)
            {
                UIManager.Instance.OpenSettings();
                if(Array.IndexOf(PreviewArgs(),"-colony-share-dialog")>=0) UIManager.Instance.OpenShare();
                yield return new WaitForSecondsRealtime(.2f);
                Canvas.ForceUpdateCanvases();
            }
            int delayIndex=Array.IndexOf(PreviewArgs(),"-colony-preview-extra-delay");
            if(delayIndex>=0 && delayIndex+1<PreviewArgs().Length && float.TryParse(PreviewArgs()[delayIndex+1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float extraDelay))
                yield return new WaitForSecondsRealtime(Mathf.Clamp(extraDelay,0,5));
            string directory=Path.GetDirectoryName(output);
            if(!string.IsNullOrEmpty(directory))Directory.CreateDirectory(directory);
            int superSize=1; int superIndex=Array.IndexOf(PreviewArgs(),"-colony-preview-supersize");
            if(superIndex>=0 && superIndex+1<PreviewArgs().Length) int.TryParse(PreviewArgs()[superIndex+1],out superSize);
            ScreenCapture.CaptureScreenshot(output,Mathf.Clamp(superSize,1,8));
            yield return new WaitForSecondsRealtime(.6f);
            bool visible=gameplay ? !UIManager.Instance.HasOpenModal : settings ? UIManager.Instance.IsSettingsVisible : pictures ? UIManager.Instance.IsPicturePickerVisible && UIManager.Instance.CurrentPicturePage==page : UIManager.Instance.IsHomeVisible;
            bool ready=File.Exists(output) && visible && (gameplay?Time.timeScale>0:Time.timeScale==0);
            Debug.Log("COLONY "+(gameplay?"GAMEPLAY":opening?"OPENING":settings?"SETTINGS":pictures?"PICTURE":"HOME")+" PREVIEW: "+(ready?"PASS":"FAIL"));
            Application.Quit(ready?0:1);
        }
    }
}
