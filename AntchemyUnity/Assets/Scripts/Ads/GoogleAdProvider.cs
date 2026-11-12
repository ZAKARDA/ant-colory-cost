using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace Antchemy
{
    public sealed class GoogleAdProvider : IFullscreenAdProvider, IDisposable
    {
        readonly AdConfiguration config;
        readonly bool android;
        readonly Action<bool> privacyPause;
        readonly Action privacyChanged;
        RewardedAd rewarded;
        InterstitialAd interstitial;
        AppOpenAd appOpen, showingAppOpen;
        bool loadingAppOpen;
        double appOpenRequestedAt, retryAppOpenAt;
        static double UtcNow => DateTime.UtcNow.Ticks/(double)TimeSpan.TicksPerSecond;
        bool initialized, sdkReady, disposed, loadingRewarded, loadingInterstitial;
        int consentGeneration;
        RewardedAd showingRewarded;
        InterstitialAd showingInterstitial;
        double rewardedAt, interstitialAt, retryRewardedAt, retryInterstitialAt;
        bool sampleTestSession, consentFinished, attPending;
        double attStartedAt;
        // The tracking prompt is on screen (bounded, so a missing callback can never hold the splash forever).
        public bool AwaitingUser => attPending && Now-attStartedAt<90;
        static bool Settled(bool enabled, bool ready, bool loading, double retryAt) => !enabled || ready || (!loading && retryAt>0);
        // True once consent is resolved and every enabled ad format has loaded (or definitively failed/ is unavailable).
        public bool StartupSettled => disposed || (consentFinished && (!CanRequest || (sdkReady
            && Settled(config.rewardedEnabled,RewardedReady,loadingRewarded,retryRewardedAt)
            && Settled(config.interstitialEnabled,InterstitialReady,loadingInterstitial,retryInterstitialAt)
            && Settled(config.appOpenEnabled,AppOpenReady,loadingAppOpen,retryAppOpenAt))));
        public string Status { get; private set; } = "Waiting to start";
        bool CanRequest => sampleTestSession || ConsentInformation.CanRequestAds();
        public bool PrivacyRequired => !sampleTestSession && ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
        public bool RewardedReady => !disposed && CanRequest && rewarded != null && rewarded.CanShowAd() && Now-rewardedAt<3300;
        public bool InterstitialReady => !disposed && CanRequest && interstitial != null && interstitial.CanShowAd() && Now-interstitialAt<3300;
        public bool AppOpenReady => !disposed && CanRequest && appOpen != null && appOpen.CanShowAd() && IsAppOpenFresh(UtcNow-appOpenRequestedAt);
        internal static bool IsAppOpenFresh(double ageSeconds) => ageSeconds>=0 && ageSeconds<4*60*60;
        static double Now => Time.realtimeSinceStartupAsDouble;
        static void Dispatch(Action action) => MobileAdsEventExecutor.ExecuteInUpdate(action);
        internal static void PrepareCallbacks() => MobileAdsEventExecutor.Initialize();

        public GoogleAdProvider(AdConfiguration config, bool android, Action<bool> privacyPause, Action privacyChanged)
        {
            this.config=config; this.android=android; this.privacyPause=privacyPause; this.privacyChanged=privacyChanged;
        }

        public void Start()
        {
            // Consent callbacks use Dispatch before MobileAds.Initialize creates its executor.
            PrepareCallbacks();
            // iOS: ask for tracking permission first so the SDK and consent form know the answer.
            Trace("Requesting tracking permission");
            attPending=true; attStartedAt=Now;
            AttPrompt.Request(() => { attPending=false; if(!disposed && !sampleTestSession) StartConsent(); });
        }

        void StartConsent()
        {
            Trace("Updating consent");
            ConsentInformation.Update(new ConsentRequestParameters(), error => Dispatch(() => {
                if(disposed || sampleTestSession) return;
                privacyChanged();
                if(error != null)
                {
                    consentFinished=true;
                    Trace("Consent update failed: " + error.Message);
                    InitializeIfAllowed(); return;
                }
                privacyPause(true);
                ConsentForm.LoadAndShowConsentFormIfRequired(formError => Dispatch(() => {
                    if(disposed || sampleTestSession) return;
                    privacyPause(false); privacyChanged(); consentFinished=true;
                    Trace(formError!=null?"Consent form failed: "+formError.Message:"Consent complete; ads allowed="+ConsentInformation.CanRequestAds());
                    InitializeIfAllowed();
                }));
            }));
            InitializeIfAllowed();
        }

        void Trace(string message) { Status=message; Debug.Log("[ColonyAds] "+message); }

        void InitializeIfAllowed()
        {
            if(initialized || !CanRequest) return;
            initialized=true;
            Trace("Initializing Google Mobile Ads SDK");
            SyncAudioPreference();
            MobileAds.Initialize(status => Dispatch(() => {
                if(disposed) return;
                sdkReady=true;
                Trace("SDK initialized; requesting sample="+config.UseTestUnits);
                LoadAppOpen(); LoadRewarded(); LoadInterstitial();
            }));
        }

        public void Tick()
        {
            if(!sdkReady || disposed || !CanRequest) return;
            if(!loadingRewarded && !RewardedReady && Now>=retryRewardedAt) LoadRewarded();
            if(!loadingInterstitial && !InterstitialReady && Now>=retryInterstitialAt) LoadInterstitial();
            if(!loadingAppOpen && !AppOpenReady && Now>=retryAppOpenAt) LoadAppOpen();
        }

        void LoadAppOpen()
        {
            if(disposed || loadingAppOpen || !config.appOpenEnabled || !CanRequest) return;
            string id=config.AppOpenId(android);
            if(string.IsNullOrWhiteSpace(id)) { retryAppOpenAt=Now+120; return; }
            appOpen?.Destroy(); appOpen=null; loadingAppOpen=true;
            int generation=consentGeneration;
            double requestedAt=UtcNow;
            SyncAudioPreference();
            Trace("Loading app-open: "+id);
            AppOpenAd.Load(id,new AdRequest(),(ad,error) => Dispatch(() => {
                loadingAppOpen=false;
                if(disposed || generation!=consentGeneration) { ad?.Destroy(); return; }
                if(error!=null || ad==null)
                {
                    ad?.Destroy(); retryAppOpenAt=Now+60;
                    Trace("App-open load failed: "+error); return;
                }
                appOpen=ad; appOpenRequestedAt=requestedAt;
                Trace("App-open loaded");
            }));
        }

        void LoadRewarded()
        {
            if(disposed || loadingRewarded || !config.rewardedEnabled || !CanRequest) return;
            string id=config.RewardedId(android);
            if(string.IsNullOrWhiteSpace(id)) { retryRewardedAt=Now+120; return; }
            rewarded?.Destroy(); rewarded=null; loadingRewarded=true;
            int generation=consentGeneration;
            SyncAudioPreference();
            Trace("Loading rewarded: "+id);
            RewardedAd.Load(id,new AdRequest(),(ad,error) => Dispatch(() => {
                loadingRewarded=false;
                if(disposed || generation!=consentGeneration) { ad?.Destroy(); return; }
                if(error!=null || ad==null)
                {
                    ad?.Destroy(); retryRewardedAt=Now+60;
                    Trace("Rewarded load failed: "+error); return;
                }
                rewarded=ad; rewardedAt=Now;
                Trace("Rewarded loaded");
            }));
        }

        void LoadInterstitial()
        {
            if(disposed || loadingInterstitial || !config.interstitialEnabled || !CanRequest) return;
            string id=config.InterstitialId(android);
            if(string.IsNullOrWhiteSpace(id)) { retryInterstitialAt=Now+120; return; }
            interstitial?.Destroy(); interstitial=null; loadingInterstitial=true;
            int generation=consentGeneration;
            SyncAudioPreference();
            Trace("Loading interstitial: "+id);
            InterstitialAd.Load(id,new AdRequest(),(ad,error) => Dispatch(() => {
                loadingInterstitial=false;
                if(disposed || generation!=consentGeneration) { ad?.Destroy(); return; }
                if(error!=null || ad==null)
                {
                    ad?.Destroy(); retryInterstitialAt=Now+60;
                    Trace("Interstitial load failed: "+error); return;
                }
                interstitial=ad; interstitialAt=Now;
                Trace("Interstitial loaded");
            }));
        }

        public void ShowRewarded(Action earned, Action<bool> closed)
        {
            if(!RewardedReady) { closed(false); return; }
            var ad=rewarded; rewarded=null; retryRewardedAt=double.PositiveInfinity;
            showingRewarded=ad;
            bool finished=false;
            Action<bool> finish = success => {
                if(finished || disposed) return; finished=true;
                showingRewarded=null; ad.Destroy(); retryRewardedAt=Now+1; Trace("Rewarded closed; success="+success); closed(success);
            };
            ad.OnAdFullScreenContentClosed += () => Dispatch(() => finish(true));
            ad.OnAdFullScreenContentFailed += error => Dispatch(() => { Trace("Rewarded show failed: "+error); finish(false); });
            ad.OnAdFullScreenContentOpened += () => Dispatch(() => Trace("Rewarded opened"));
            SyncAudioPreference();
            try { ad.Show(reward => Dispatch(() => { if(!disposed) { Trace("Rewarded earned: "+reward.Amount+" "+reward.Type); earned(); } })); }
            catch(Exception error) { Trace("Rewarded exception: "+error.Message); finish(false); }
        }

        public void ShowInterstitial(Action<bool> closed)
        {
            if(!InterstitialReady) { closed(false); return; }
            var ad=interstitial; interstitial=null; retryInterstitialAt=double.PositiveInfinity;
            showingInterstitial=ad;
            bool finished=false;
            Action<bool> finish = success => {
                if(finished || disposed) return; finished=true;
                showingInterstitial=null; ad.Destroy(); retryInterstitialAt=Now+1; Trace("Interstitial closed; success="+success); closed(success);
            };
            ad.OnAdFullScreenContentClosed += () => Dispatch(() => finish(true));
            ad.OnAdFullScreenContentFailed += error => Dispatch(() => { Trace("Interstitial show failed: "+error); finish(false); });
            ad.OnAdFullScreenContentOpened += () => Dispatch(() => Trace("Interstitial opened"));
            SyncAudioPreference();
            try { ad.Show(); }
            catch(Exception error) { Trace("Interstitial exception: "+error.Message); finish(false); }
        }

        public void ShowPrivacyOptions()
        {
            consentGeneration++;
            privacyPause(true);
            ConsentForm.ShowPrivacyOptionsForm(error => Dispatch(() => {
                if(disposed) return;
                privacyPause(false); privacyChanged();
                if(error!=null) Debug.LogWarning("Ad privacy options unavailable: "+error.Message);
                // Ads loaded under an earlier choice must not survive a changed choice.
                rewarded?.Destroy(); rewarded=null; interstitial?.Destroy(); interstitial=null;
                appOpen?.Destroy(); appOpen=null; retryAppOpenAt=Now+1;
                retryRewardedAt=Now+1; retryInterstitialAt=Now+1;
                InitializeIfAllowed();
            }));
        }

        public void ShowAppOpen(Action<bool> closed)
        {
            if(!AppOpenReady) { closed(false); return; }
            var ad=appOpen; appOpen=null; retryAppOpenAt=double.PositiveInfinity;
            showingAppOpen=ad;
            bool finished=false;
            Action<bool> finish = success => {
                if(finished || disposed) return; finished=true;
                showingAppOpen=null; ad.Destroy(); retryAppOpenAt=Now+1; Trace("App-open closed; success="+success); closed(success);
            };
            ad.OnAdFullScreenContentClosed += () => Dispatch(() => finish(true));
            ad.OnAdFullScreenContentFailed += error => Dispatch(() => { Trace("App-open show failed: "+error); finish(false); });
            ad.OnAdFullScreenContentOpened += () => Dispatch(() => Trace("App-open opened"));
            SyncAudioPreference();
            try { ad.Show(); }
            catch(Exception error) { Trace("App-open exception: "+error.Message); finish(false); }
        }

        static void SyncAudioPreference()
        {
            var audio=AudioManager.Instance;
            if(audio!=null) MobileAds.SetApplicationMuted(!audio.MusicEnabled && !audio.SoundEnabled);
        }

        public void Dispose()
        {
            disposed=true; rewarded?.Destroy(); rewarded=null; interstitial?.Destroy(); interstitial=null;
            showingRewarded?.Destroy(); showingRewarded=null; showingInterstitial?.Destroy(); showingInterstitial=null;
            appOpen?.Destroy(); appOpen=null; showingAppOpen?.Destroy(); showingAppOpen=null;
        }
    }
}
