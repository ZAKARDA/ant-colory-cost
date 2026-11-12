#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Antchemy.Editor
{
    // Deliberately independent of UNITY_IOS: an export can start from the Mac target,
    // where the SDK's own iOS-only plist callback has not been compiled yet.
    public static class AdMobIOSPlist
    {
        const string SettingsPath="Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        const string PackagePath="Packages/com.google.ads.mobile";

        [PostProcessBuild(1000)]
        public static void Configure(BuildTarget target,string output)
        {
            if(target!=BuildTarget.iOS) return;
            var asset=AssetDatabase.LoadAssetAtPath<ScriptableObject>(SettingsPath);
            if(asset==null) throw new BuildFailedException("AdMob settings are missing: "+SettingsPath);
            var settings=new SerializedObject(asset);
            string appId=settings.FindProperty("adMobIOSAppId").stringValue.Trim();
            if(!Regex.IsMatch(appId,@"^ca-app-pub-\d{16}~\d+$"))
                throw new BuildFailedException("Configure a valid iOS AdMob app ID before exporting.");
            var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(PackagePath);
            if(package==null) throw new BuildFailedException("Google Mobile Ads package is not resolved.");
            string networks=Path.Combine(package.resolvedPath,"GoogleMobileAds/Editor/GoogleMobileAdsSKAdNetworkItems.xml");
            if(!File.Exists(networks)) throw new BuildFailedException("AdMob SKAdNetwork list is missing from the SDK package.");
            string[] ids=XDocument.Load(networks).Descendants("SKAdNetworkIdentifier").Select(x=>x.Value.Trim()).Where(x=>x.Length>0).Distinct().ToArray();
            if(ids.Length==0) throw new BuildFailedException("The AdMob SDK's SKAdNetwork list is empty.");
            string path=Path.Combine(output,"Info.plist");
            Patch(path,appId,ids,settings.FindProperty("userTrackingUsageDescription").stringValue);
            Validate(path,appId,ids);
            Debug.Log("COLONY_IOS_ADMOB_PLIST_PASS: app ID and "+ids.Length+" SDK network IDs verified in "+path);
        }

        internal static void Patch(string path,string appId,string[] networks,string trackingDescription)
        {
            var doc=XDocument.Load(path);
            var dict=doc.Root?.Element("dict");
            if(dict==null) throw new BuildFailedException("Missing app dictionary in "+path);
            SetString(dict,"GADApplicationIdentifier",appId);
            SetString(dict,"GADUUnityVersion",Application.unityVersion);
            if(!string.IsNullOrWhiteSpace(trackingDescription)) SetString(dict,"NSUserTrackingUsageDescription",trackingDescription);
            var array=GetValue(dict,"SKAdNetworkItems");
            if(array==null) { array=new XElement("array"); dict.Add(new XElement("key","SKAdNetworkItems"),array); }
            if(array.Name!="array") throw new BuildFailedException("SKAdNetworkItems must be an array in "+path);
            var existing=array.Elements("dict").Select(x=>GetValue(x,"SKAdNetworkIdentifier")?.Value).ToHashSet();
            foreach(string id in networks)
                if(existing.Add(id)) array.Add(new XElement("dict",new XElement("key","SKAdNetworkIdentifier"),new XElement("string",id)));
            // Mono reads an absent DTD subset as an empty string and serializes it as
            // [], which Apple's plist parser rejects. Write a standard external DTD.
            doc.DocumentType?.Remove();
            doc.AddFirst(new XDocumentType("plist","-//Apple//DTD PLIST 1.0//EN","http://www.apple.com/DTDs/PropertyList-1.0.dtd",null));
            doc.Save(path);
        }

        internal static void Validate(string path,string appId,string[] networks)
        {
            var dict=XDocument.Load(path).Root?.Element("dict");
            if(dict==null || GetValue(dict,"GADApplicationIdentifier")?.Value!=appId)
                throw new BuildFailedException("Exported iOS app is missing its configured AdMob app ID.");
            var actual=GetValue(dict,"SKAdNetworkItems")?.Elements("dict").Select(x=>GetValue(x,"SKAdNetworkIdentifier")?.Value).ToHashSet();
            if(actual==null || networks.Any(id=>!actual.Contains(id)))
                throw new BuildFailedException("Exported iOS app is missing AdMob SKAdNetwork identifiers.");
        }

        static XElement GetValue(XElement dict,string name)
        {
            var key=dict.Elements("key").SingleOrDefault(x=>x.Value==name);
            return key?.ElementsAfterSelf().FirstOrDefault();
        }
        static void SetString(XElement dict,string key,string text)
        {
            var value=GetValue(dict,key);
            if(value==null) dict.Add(new XElement("key",key),new XElement("string",text));
            else value.ReplaceWith(new XElement("string",text));
        }
    }
}
#endif
