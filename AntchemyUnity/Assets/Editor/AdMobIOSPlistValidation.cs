#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Antchemy.Editor
{
    public static class AdMobIOSPlistValidation
    {
        public static void Run()
        {
            string folder=Path.Combine("Temp","AdMobPlistValidation"); Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,"Info.plist");
            const string id="ca-app-pub-3940256099942544~1458002511";
            string[] networks={"cstr6suwn9.skadnetwork","4fzdc2evr5.skadnetwork"};
            try
            {
                File.WriteAllText(path,"<?xml version=\"1.0\"?><!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\"><plist version=\"1.0\"><dict><key>CFBundleIdentifier</key><string>com.antchemy.game</string><key>SKAdNetworkItems</key><array><dict><key>SKAdNetworkIdentifier</key><string>existing.skadnetwork</string></dict></array></dict></plist>");
                bool rejected=false;
                try { AdMobIOSPlist.Validate(path,id,networks); } catch(BuildFailedException) { rejected=true; }
                if(!rejected) throw new Exception("A missing app ID must fail export validation.");
                AdMobIOSPlist.Patch(path,id,networks,""); AdMobIOSPlist.Patch(path,id,networks,"");
                AdMobIOSPlist.Validate(path,id,networks);
                var doc=XDocument.Load(path);
                if(doc.Descendants("key").Count(x=>x.Value=="GADApplicationIdentifier")!=1 || doc.Descendants("string").Count(x=>x.Value.EndsWith(".skadnetwork"))!=3 || !doc.Descendants("string").Any(x=>x.Value=="com.antchemy.game"))
                    throw new Exception("Patching must preserve app data and existing network IDs without duplicates.");
                // Read the actual SDK/settings and exercise the platform-independent export hook.
                AdMobIOSPlist.Configure(BuildTarget.iOS,folder);
                if(Application.platform==RuntimePlatform.OSXEditor)
                {
                    // The fixture path is a fixed relative path without spaces.
                    var start=new System.Diagnostics.ProcessStartInfo("/usr/bin/plutil","-lint "+path) { UseShellExecute=false, RedirectStandardOutput=true, RedirectStandardError=true };
                    using(var process=System.Diagnostics.Process.Start(start))
                    {
                        string output=process.StandardOutput.ReadToEnd()+process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        if(process.ExitCode!=0) throw new Exception("Apple plist parser rejected the generated file: "+output);
                    }
                }
                Debug.Log("COLONY_IOS_PLIST_REGRESSION_PASS: missing ID rejected; repair idempotent; existing values preserved; SDK settings applied.");
                string[] args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"-colony-repair-ios");
                if(index>=0 && index+1<args.Length) AdMobIOSPlist.Configure(BuildTarget.iOS,args[index+1]);
            }
            finally { Directory.Delete(folder,true); }
        }
    }
}
#endif
