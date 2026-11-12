using System;
using UnityEngine;

namespace Antchemy
{
    static class AdVerification
    {
        sealed class Provider : IFullscreenAdProvider
        {
            public bool RewardedReady { get; set; } = true;
            public bool InterstitialReady { get; set; } = true;
            public bool AppOpenReady { get; set; } = true;
            public Action Earn;
            public Action<bool> Close;
            public int RewardedShows, InterstitialShows, AppOpenShows;
            public bool ThrowOnShow;
            public void ShowRewarded(Action earned,Action<bool> closed)
            {
                if(ThrowOnShow) throw new InvalidOperationException("Simulated unavailable ad");
                RewardedShows++; Earn=earned; Close=closed;
            }
            public void ShowInterstitial(Action<bool> closed) { InterstitialShows++; Close=closed; }
            public void ShowAppOpen(Action<bool> closed)
            {
                if(ThrowOnShow) throw new InvalidOperationException("Simulated unavailable app-open ad");
                AppOpenShows++; Close=closed;
            }
        }

        public static void Run(Action<bool,string> check)
        {
            var config=AdConfiguration.Load();
            check(config.useTestAds && config.RewardedId(true)=="ca-app-pub-3940256099942544/5224354917" && config.RewardedId(false)=="ca-app-pub-3940256099942544/1712485313","AdMob development configuration uses Google's platform-specific rewarded test IDs");
            check(config.InterstitialId(true)=="ca-app-pub-3940256099942544/1033173712" && config.InterstitialId(false)=="ca-app-pub-3940256099942544/4411468910","between-level ads use the platform-specific interstitial test IDs");
            var provider=new Provider(); double clock=0; bool paused=false; int continued=0, rewards=0;
            var flow=new AdFlow(provider,config,()=>clock,p=>paused=p);
            flow.RecordWin(); flow.NextLevel(()=>continued++);
            check(continued==1 && provider.InterstitialShows==0,"first completed level continues without an interstitial");
            flow.RecordWin(); flow.RecordWin(); clock=59; flow.NextLevel(()=>continued++);
            check(continued==2 && provider.InterstitialShows==0,"startup grace period prevents early interstitials");
            clock=60; flow.NextLevel(()=>continued++);
            check(flow.Busy && paused && continued==2 && provider.InterstitialShows==1,"eligible between-level ad pauses and defers the next puzzle");
            flow.NextLevel(()=>continued++);
            check(provider.InterstitialShows==1 && continued==2,"repeated Next taps cannot stack fullscreen ads or transitions");
            var closeFirst=provider.Close; closeFirst(true); closeFirst(true);
            check(!flow.Busy && !paused && continued==3,"closing an interstitial resumes the next level exactly once");
            flow.RecordWin(); flow.RecordWin(); flow.RecordWin(); clock=61; flow.NextLevel(()=>continued++);
            check(provider.InterstitialShows==1 && continued==4,"fullscreen cooldown prevents back-to-back interstitials");
            clock=180; flow.NextLevel(()=>continued++); provider.Close(false);
            check(!flow.Busy && !paused && continued==5,"an ad presentation failure never blocks the next puzzle");
            provider.InterstitialReady=false; flow.NextLevel(()=>continued++);
            check(continued==6 && !paused,"missing or offline interstitial inventory never makes players wait");

            flow.TryRewarded(()=>rewards++); provider.Close(true);
            check(rewards==0 && !flow.Busy && !paused,"closing a rewarded ad without earning it grants no reward");
            flow.TryRewarded(()=>rewards++); var earn=provider.Earn; var close=provider.Close;
            earn(); earn();
            check(rewards==0 && flow.Busy && paused,"earned nest waits until the rewarded ad is closed");
            close(true); close(true); earn();
            check(rewards==1 && !paused && !flow.Busy,"duplicate SDK callbacks grant exactly one reward");
            flow.TryRewarded(()=>rewards++); provider.Close(true); provider.Earn(); provider.Earn();
            check(rewards==2,"reward callbacks arriving after ad closure still pay exactly once");
            flow.TryRewarded(()=>rewards++); provider.Earn(); provider.Close(false);
            check(rewards==2 && !flow.Busy && !paused,"failed rewarded presentation cannot grant a nest or leave play paused");
            provider.ThrowOnShow=true;
            check(!flow.TryRewarded(()=>rewards++) && !flow.Busy && !paused,"rewarded SDK exceptions release the pause without granting a reward");
            provider.ThrowOnShow=false; provider.RewardedReady=false;
            check(!flow.TryRewarded(()=>rewards++) && rewards==2 && !paused,"unloaded rewarded ads never grant a free reward");
            provider.RewardedReady=true; provider.InterstitialReady=true;
            flow.TryRewarded(()=>rewards++); provider.Earn(); provider.Close(true);
            int shown=provider.InterstitialShows; flow.NextLevel(()=>continued++);
            check(provider.InterstitialShows==shown,"watching a rewarded ad also postpones the next interstitial");

            float scale=Time.timeScale; bool audioPause=AudioListener.pause;
            AdManager.Instance.SetPaused(true);
            check(Time.timeScale==0 && AudioListener.pause,"fullscreen presentation pauses gameplay and Unity audio");
            AdManager.Instance.SetPaused(false);
            check(Time.timeScale==scale && AudioListener.pause==audioPause,"fullscreen closure restores the exact previous pause and audio state");
            VerifyAppOpen(check);
        }

        static void VerifyAppOpen(Action<bool,string> check)
        {
            var config=AdConfiguration.Load();
            check(config.appOpenEnabled && config.AppOpenId(true)=="ca-app-pub-3940256099942544/9257395921" && config.AppOpenId(false)=="ca-app-pub-3940256099942544/5575463023","app-open ads use Google's Android and iOS app-open test units");
            double clock=0; var policy=new AppOpenPolicy(config,()=>clock);
            check(!policy.TakeLaunch(false,false) && policy.LaunchPending,"launch waits briefly behind its loading screen while an ad loads");
            check(!policy.TakeLaunch(true,true) && policy.LaunchPending,"launch ad cannot overlap consent or another fullscreen ad");
            clock=1;
            check(policy.TakeLaunch(true,false) && !policy.LaunchPending && !policy.TakeLaunch(true,false),"ready launch opportunity is consumed exactly once");
            policy=new AppOpenPolicy(config,()=>clock); clock+=4;
            check(!policy.TakeLaunch(true,false) && !policy.LaunchPending,"launch timeout releases the loading screen even if an ad arrives at the deadline");
            clock+=1;
            check(!policy.TakeLaunch(true,false),"late ad responses cannot interrupt the home page after launch finishes");
            check(!policy.TakeForeground(false),"initial foreground notification without a background does not show a second launch ad");
            policy.Background(false); clock+=29;
            check(!policy.TakeForeground(false),"short app switches skip app-open ads");
            policy.Background(false); clock+=30;
            check(policy.TakeForeground(false) && !policy.TakeForeground(false),"a qualifying return creates exactly one app-open opportunity");
            policy.Background(true); clock+=200;
            check(!policy.TakeForeground(false),"returning from an ad or consent screen cannot chain an app-open ad");
            policy.Background(false); clock+=30;
            check(!policy.TakeForeground(true),"foreground presentation is suppressed while another fullscreen experience is active");
            policy=new AppOpenPolicy(config,()=>clock); policy.Background(false);
            check(!policy.LaunchPending && !policy.TakeLaunch(true,false),"backgrounding during initial loading cancels the cold-start opportunity");
            var provider=new Provider(); bool paused=false; int completed=0, advanced=0;
            var flow=new AdFlow(provider,config,()=>clock,p=>paused=p);
            check(flow.TryAppOpen(()=>completed++) && paused && flow.Busy,"app-open presentation pauses the board and acquires the shared fullscreen guard");
            flow.NextLevel(()=>advanced++);
            check(!flow.TryAppOpen(()=>completed++) && !flow.TryRewarded(()=>{}) && advanced==0,"app-open ads cannot overlap rewarded ads or level transitions");
            var closed=provider.Close; closed(true); closed(true);
            check(completed==1 && !paused && !flow.Busy,"app-open dismissal restores play and releases its loading screen exactly once");
            check(!flow.TryAppOpen(()=>completed++),"closing app-open ads starts the shared fullscreen cooldown");
            flow.RecordWin(); flow.RecordWin(); flow.RecordWin(); clock+=60; flow.NextLevel(()=>advanced++);
            check(advanced==1 && provider.InterstitialShows==0,"app-open ads postpone between-level interstitials");
            clock+=60; provider.AppOpenReady=false;
            check(!flow.TryAppOpen(()=>completed++) && !paused,"missing app-open inventory never blocks a returning player");
            provider.AppOpenReady=true; flow.TryRewarded(()=>{}); provider.Close(true);
            check(!flow.TryAppOpen(()=>completed++),"recent rewarded ads also suppress app-open presentation");
            clock+=120; flow.TryAppOpen(()=>completed++); provider.Close(false);
            check(completed==2 && !paused && !flow.Busy,"app-open presentation failure releases the loading screen and pause");
            clock+=120; provider.ThrowOnShow=true; flow.TryAppOpen(()=>completed++);
            check(completed==3 && !paused && !flow.Busy && !flow.TryAppOpen(()=>completed++),"app-open SDK exceptions recover once without an immediate retry loop");
            check(GoogleAdProvider.IsAppOpenFresh(14399) && !GoogleAdProvider.IsAppOpenFresh(14400) && !GoogleAdProvider.IsAppOpenFresh(-1),"app-open cache expires at four hours and rejects invalid clock ages");
            config.appOpenEnabled=false; policy=new AppOpenPolicy(config,()=>clock);
            clock+=120; provider.ThrowOnShow=false;
            check(!policy.LaunchPending && !flow.TryAppOpen(()=>completed++),"disabling app-open ads removes both startup waiting and ad presentation");
            var loading=AppOpenLoadingScreen.Create(AdManager.Instance.transform);
            var canvas=loading.GetComponent<Canvas>();
            var image=loading.GetComponentInChildren<UnityEngine.UI.Image>();
            check(canvas.renderMode==RenderMode.ScreenSpaceOverlay && canvas.sortingOrder==32760 && image.raycastTarget && loading.GetComponent<UnityEngine.UI.GraphicRaycaster>()!=null,"opening screen covers the game and blocks underlying UI taps");
            UnityEngine.Object.Destroy(loading);
        }

        public static void VerifyBoardReward(LevelManager level, UIManager ui, LevelData data, Action<bool,string> check)
        {
            level.LoadLevelData(data,9290);
            int generation=level.LoadGeneration;
            var provider=new Provider(); var flow=new AdFlow(provider,new AdConfiguration(),()=>0,_=>{});
            flow.TryRewarded(()=>ui.ApplyExtraNest(level,generation));
            provider.Earn(); check(level.SlotCount==5,"earned ad does not change nests before ad dismissal");
            provider.Close(true); provider.Earn();
            check(level.SlotCount==6,"confirmed rewarded dismissal adds exactly one nest to the current board");
            generation=level.LoadGeneration;
            flow.TryRewarded(()=>ui.ApplyExtraNest(level,generation));
            level.LoadLevelData(data,9291);
            provider.Earn(); provider.Close(true);
            check(level.SlotCount==5,"late ad reward cannot add a nest to a restarted or different board");
        }
    }
}
