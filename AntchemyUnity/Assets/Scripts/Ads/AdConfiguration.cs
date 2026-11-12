using System;
using UnityEngine;

namespace Antchemy
{
    [Serializable]
    public sealed class AdConfiguration
    {
        public bool useTestAds = true;
        public bool rewardedEnabled = true;
        public bool interstitialEnabled = true;
        public bool appOpenEnabled = true;
        public float appOpenLaunchWaitSeconds = 4;
        public float appOpenMinimumBackgroundSeconds = 30;
        public int winsBetweenInterstitials = 3;
        public float minimumSecondsBetweenAds = 120;
        public float firstInterstitialDelaySeconds = 60;
        public string androidRewardedUnitId = "";
        public string iosRewardedUnitId = "";
        public string androidInterstitialUnitId = "";
        public string iosInterstitialUnitId = "";
        public string androidAppOpenUnitId = "";
        public string iosAppOpenUnitId = "";

        public string RewardedId(bool android) => UseTestUnits
            ? (android ? "ca-app-pub-3940256099942544/5224354917" : "ca-app-pub-3940256099942544/1712485313")
            : (android ? androidRewardedUnitId : iosRewardedUnitId);
        public string InterstitialId(bool android) => UseTestUnits
            ? (android ? "ca-app-pub-3940256099942544/1033173712" : "ca-app-pub-3940256099942544/4411468910")
            : (android ? androidInterstitialUnitId : iosInterstitialUnitId);
        public string AppOpenId(bool android) => UseTestUnits
            ? (android ? "ca-app-pub-3940256099942544/9257395921" : "ca-app-pub-3940256099942544/5575463023")
            : (android ? androidAppOpenUnitId : iosAppOpenUnitId);

        // Development builds on a device always use Google's sample units so your own
        // AdMob account is never served test traffic. Release builds use your live units.
        public bool UseTestUnits => useTestAds || (Debug.isDebugBuild && !Application.isEditor);

        public static AdConfiguration Load()
        {
            var asset = Resources.Load<TextAsset>("AdMobConfiguration");
            return asset != null ? JsonUtility.FromJson<AdConfiguration>(asset.text) ?? new AdConfiguration() : new AdConfiguration();
        }
    }
}
