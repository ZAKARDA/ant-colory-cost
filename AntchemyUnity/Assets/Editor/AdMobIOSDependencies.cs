#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Antchemy.Editor
{
    public static class AdMobIOSDependencies
    {
        // EDM generates Podfile at order 40 and installs at 50. Use the equivalent
        // official CDN source instead of cloning the complete CocoaPods Specs repo.
        [PostProcessBuild(45)]
        public static void UseCocoaPodsCdn(BuildTarget target,string buildPath)
        {
            if(target!=BuildTarget.iOS) return;
            string path=Path.Combine(buildPath,"Podfile");
            if(!File.Exists(path)) return;
            string text=File.ReadAllText(path);
            text=text.Replace("source 'https://github.com/CocoaPods/Specs'", "source 'https://cdn.cocoapods.org/'");
            const string source="source 'https://cdn.cocoapods.org/'";
            int first=text.IndexOf(source,System.StringComparison.Ordinal);
            if(first>=0)
            {
                int next=text.IndexOf(source,first+source.Length,System.StringComparison.Ordinal);
                while(next>=0) { text=text.Remove(next,source.Length); next=text.IndexOf(source,first+source.Length,System.StringComparison.Ordinal); }
            }
            // Keep dependency targets compatible with the app and current Xcode SDKs.
            // Google podspecs can otherwise emit targets older than Xcode supports.
            if(!text.Contains("post_install do"))
            {
                var minimum=System.Version.TryParse(PlayerSettings.iOS.targetOSVersionString,out var version)?version:new System.Version(15,0);
                text += "\npost_install do |installer|\n  installer.pods_project.targets.each do |target|\n    target.build_configurations.each do |config|\n      current = config.build_settings['IPHONEOS_DEPLOYMENT_TARGET'] || '0'\n      if Gem::Version.new(current) < Gem::Version.new('"+minimum+"')\n        config.build_settings['IPHONEOS_DEPLOYMENT_TARGET'] = '"+minimum+"'\n      end\n    end\n  end\nend\n";
            }
            File.WriteAllText(path,text);
        }

        public static void ValidatePodfileHook()
        {
            string folder=Path.Combine("Temp","ColonyAdMobPodfileValidation");
            Directory.CreateDirectory(folder);
            string file=Path.Combine(folder,"Podfile");
            try
            {
                File.WriteAllText(file,"source 'https://github.com/CocoaPods/Specs'\nsource 'https://cdn.cocoapods.org/'\nplatform :ios, '15.0'\n");
                UseCocoaPodsCdn(BuildTarget.iOS,folder); UseCocoaPodsCdn(BuildTarget.iOS,folder);
                string result=File.ReadAllText(file);
                if(result.Contains("github.com/CocoaPods/Specs") || System.Text.RegularExpressions.Regex.Matches(result,"post_install do").Count!=1 || !result.Contains("IPHONEOS_DEPLOYMENT_TARGET"))
                    throw new System.Exception("AdMob iOS Podfile normalization failed.");
                UnityEngine.Debug.Log("COLONY_ADMOB_PODFILE_VALIDATION_PASS");
            }
            finally { Directory.Delete(folder,true); }
        }
    }
}
#endif
