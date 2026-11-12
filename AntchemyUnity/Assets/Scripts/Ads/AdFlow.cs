using System;

namespace Antchemy
{
    public interface IFullscreenAdProvider
    {
        bool RewardedReady { get; }
        bool InterstitialReady { get; }
        bool AppOpenReady { get; }
        void ShowRewarded(Action earned, Action<bool> closed);
        void ShowInterstitial(Action<bool> closed);
        void ShowAppOpen(Action<bool> closed);
    }

    // Coordinates one full-screen experience at a time, independently of SDK callback ordering.
    public sealed class AdFlow
    {
        readonly IFullscreenAdProvider provider;
        readonly AdConfiguration config;
        readonly Func<double> now;
        readonly Action<bool> setPaused;
        readonly double startedAt;
        double lastAdAt = double.NegativeInfinity;
        int wins;
        public bool Busy { get; private set; }

        public AdFlow(IFullscreenAdProvider provider, AdConfiguration config, Func<double> now, Action<bool> setPaused)
        {
            this.provider=provider; this.config=config; this.now=now; this.setPaused=setPaused;
            startedAt=now();
        }

        public void RecordWin() { wins++; }

        public bool TryAppOpen(Action completed)
        {
            if(Busy || !config.appOpenEnabled || !provider.AppOpenReady
                || now()-lastAdAt < Math.Max(0,config.minimumSecondsBetweenAds)) return false;
            bool closed=false;
            Busy=true; setPaused(true);
            Action<bool> finish = success => {
                if(closed) return;
                closed=true; Busy=false;
                // A failed presentation also consumes this opportunity to prevent retry loops.
                lastAdAt=now(); setPaused(false); completed();
            };
            try { provider.ShowAppOpen(finish); }
            catch(Exception) { finish(false); }
            return true;
        }

        public bool TryRewarded(Action grantReward)
        {
            if(Busy || !config.rewardedEnabled || !provider.RewardedReady) return false;
            bool closed=false, earned=false, granted=false, shown=false;
            Action grant = () => { if(closed && shown && earned && !granted) { granted=true; grantReward(); } };
            Busy=true; setPaused(true);
            try
            {
                provider.ShowRewarded(() => { earned=true; grant(); }, success => {
                    if(closed) return;
                    closed=true; shown=success; Busy=false;
                    if(success) lastAdAt=now();
                    setPaused(false); grant();
                });
            }
            catch(Exception)
            {
                if(!closed) { closed=true; Busy=false; setPaused(false); }
                return false;
            }
            return true;
        }

        // Restart / Next Picture: show an interstitial every time one is loaded, ignoring win counts and cooldowns.
        public void Break(Action continueToLevel)
        {
            if(Busy) return;
            if(!config.interstitialEnabled || !provider.InterstitialReady) { continueToLevel(); return; }
            bool closed=false;
            Busy=true; setPaused(true);
            Action<bool> finish = success => {
                if(closed) return;
                closed=true; Busy=false;
                if(success) { wins=0; lastAdAt=now(); }
                setPaused(false); continueToLevel();
            };
            try { provider.ShowInterstitial(finish); }
            catch(Exception) { finish(false); }
        }

        public void NextLevel(Action continueToLevel)
        {
            if(Busy) return;
            bool eligible = config.interstitialEnabled && wins >= Math.Max(1,config.winsBetweenInterstitials)
                && now()-startedAt >= Math.Max(0,config.firstInterstitialDelaySeconds)
                && now()-lastAdAt >= Math.Max(0,config.minimumSecondsBetweenAds);
            if(!eligible || !provider.InterstitialReady) { continueToLevel(); return; }
            bool closed=false;
            Busy=true; setPaused(true);
            Action<bool> finish = success => {
                if(closed) return;
                closed=true; Busy=false;
                if(success) { wins=0; lastAdAt=now(); }
                setPaused(false); continueToLevel();
            };
            try { provider.ShowInterstitial(finish); }
            catch(Exception) { finish(false); }
        }
    }
}
