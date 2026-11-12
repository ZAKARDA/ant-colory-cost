using System;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

namespace Antchemy
{
    public sealed class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }
        public static bool MobilePlatform
        {
            get
            {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
        GoogleAdProvider google;
        AdFlow flow;
        AppOpenPolicy opening;
        GameObject loadingScreen;
        bool appOpenShowing;
        bool privacyBusy, pausedForAd, priorAudioPause;
        float priorTimeScale;
        public bool IsShowing => privacyBusy || (flow != null && flow.Busy) || (opening != null && opening.LaunchPending);
        public bool PrivacyRequired => google != null && google.PrivacyRequired;
        public bool RewardedReady => google != null && google.RewardedReady && !IsShowing;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if(Instance==null) new GameObject("AdMob manager").AddComponent<AdManager>();
        }

        void Awake()
        {
            if(Instance!=null && Instance!=this) { Destroy(gameObject); return; }
            Instance=this; DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            // Visual captures exercise layout only, with no consent or network requests.
            // Ads are requested behind the splash (ATT, consent, SDK, then every format) and the splash waits for them.
            if(!HomePreviewCapture.Requested)
                WelcomeSplash.Create(transform, OnSplashFinished, SplashWait);
            if(!MobilePlatform || HomePreviewCapture.Requested) return;
            var config=AdConfiguration.Load();
            google=new GoogleAdProvider(config,Application.platform==RuntimePlatform.Android,PauseForPrivacy,RefreshPrivacyEntry);
            flow=new AdFlow(google,config,()=>Time.realtimeSinceStartupAsDouble,SetPaused);
            opening=new AppOpenPolicy(config,()=>DateTime.UtcNow.Ticks/(double)TimeSpan.TicksPerSecond,false);
            AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
            StartCoroutine(StartAdsBehindSplash());
        }

        System.Collections.IEnumerator StartAdsBehindSplash()
        {
            // Give iOS a moment to become active so the tracking prompt can appear over the splash.
            yield return new WaitForSecondsRealtime(.8f);
            google?.Start();
        }

        void OnSplashFinished() { if(this!=null && google!=null && google.AppOpenReady) opening?.BeginLaunch(); }

        int SplashWait()
        {
            if(google==null) return 0;
            if(privacyBusy || google.AwaitingUser) return 2;
            return google.StartupSettled ? 0 : 1;
        }

        void Update()
        {
            bool busy=privacyBusy || (flow!=null && flow.Busy);
            if(!busy) google?.Tick();
            if(opening!=null && opening.LaunchPending && opening.TakeLaunch(google.AppOpenReady,busy)) ShowAppOpen();
            RefreshLoadingScreen();
        }

        void OnAppStateChanged(AppState state)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() => {
                if(this==null || opening==null) return;
                bool busy=privacyBusy || (flow!=null && flow.Busy);
                if(state==AppState.Background) opening.Background(busy);
                else if(state==AppState.Foreground && opening.TakeForeground(busy)) ShowAppOpen();
                RefreshLoadingScreen();
            });
        }

        void ShowAppOpen()
        {
            if(privacyBusy || flow==null) return;
            appOpenShowing=true;
            if(!flow.TryAppOpen(() => { appOpenShowing=false; RefreshLoadingScreen(); })) appOpenShowing=false;
        }

        void RefreshLoadingScreen()
        {
            bool show=appOpenShowing || (opening!=null && opening.LaunchPending);
            if(show && loadingScreen==null) loadingScreen=AppOpenLoadingScreen.Create(transform);
            if(loadingScreen!=null) loadingScreen.SetActive(show);
        }
        public void RecordWin() { flow?.RecordWin(); }
        public bool TryRewarded(Action grant) => !IsShowing && flow != null && flow.TryRewarded(grant);
        public void NextLevel(Action next)
        {
            if(IsShowing) return;
            if(flow!=null) flow.NextLevel(next); else next();
        }
        public void Break(Action next)
        {
            if(IsShowing) return;
            if(flow!=null) flow.Break(next); else next();
        }
        public void OpenPrivacyOptions() { if(!IsShowing && PrivacyRequired) google.ShowPrivacyOptions(); }
        void RefreshPrivacyEntry() { UIManager.Instance?.RefreshAdPrivacy(); }
        void PauseForPrivacy(bool paused) { privacyBusy=paused; SetPaused(paused); }
        internal void SetPaused(bool paused)
        {
            if(paused)
            {
                if(pausedForAd) return;
                pausedForAd=true; priorTimeScale=Time.timeScale; priorAudioPause=AudioListener.pause;
                Time.timeScale=0; AudioListener.pause=true;
            }
            else if(pausedForAd)
            {
                pausedForAd=false; Time.timeScale=priorTimeScale; AudioListener.pause=priorAudioPause;
            }
        }
        void OnDestroy()
        {
            if(Instance!=this) return;
            if(MobilePlatform) AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;
            google?.Dispose(); SetPaused(false); Instance=null;
        }
    }
}
