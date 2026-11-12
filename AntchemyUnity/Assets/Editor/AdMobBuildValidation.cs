#if UNITY_EDITOR
using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Antchemy.Editor
{
    public sealed class AdMobBuildValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;
        public void OnPreprocessBuild(BuildReport report)
        {
            bool android=report.summary.platform==BuildTarget.Android;
            if(!android && report.summary.platform!=BuildTarget.iOS) return;
            var config=AdConfiguration.Load();
            var asset=AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset");
            if(asset==null) throw new BuildFailedException("Configure the AdMob app IDs in Assets > Google Mobile Ads > Settings.");
            var settings=new SerializedObject(asset);
            string appId=settings.FindProperty(android?"adMobAndroidAppId":"adMobIOSAppId").stringValue;
            if(!Regex.IsMatch(appId??"",@"^ca-app-pub-\d{16}~\d+$")) throw new BuildFailedException("A valid AdMob app ID is required for this mobile platform.");
            bool development=(report.summary.options & BuildOptions.Development)!=0;
            if(config.useTestAds && !development)
                throw new BuildFailedException("Release builds must not use test ads: set useTestAds to false in Resources/AdMobConfiguration.json.");
            if(config.useTestAds) { Debug.Log("ADMOB BUILD: Google test ads enabled (development build only)."); return; }
            if(appId.StartsWith("ca-app-pub-3940256099942544")) throw new BuildFailedException("Live ads require your own AdMob app ID.");
            if(config.rewardedEnabled) ValidateLiveUnit(config.RewardedId(android),"rewarded");
            if(config.interstitialEnabled) ValidateLiveUnit(config.InterstitialId(android),"interstitial");
            if(config.appOpenEnabled) ValidateLiveUnit(config.AppOpenId(android),"app-open");
        }
        static void ValidateLiveUnit(string id,string format)
        {
            if(!Regex.IsMatch(id??"",@"^ca-app-pub-\d{16}/\d+$") || id.StartsWith("ca-app-pub-3940256099942544"))
                throw new BuildFailedException("Configure your own "+format+" ad unit before enabling live ads.");
        }
    }
}
#endif
