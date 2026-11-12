# Get started with iOS Resolver

Configure and use the iOS Resolver to add Swift Package and CocoaPod dependencies to your Xcode project.

Use the iOS Resolver to resolve iOS specific dependencies in your project. The iOS Resolver supports [Swift Packages](https://www.swift.org/packages/) and [CocoaPods](https://cocoapods.org/).

## Swift Package Manager support

Use Swift Packages to add iOS dependencies. External Dependency Manager (EDM) uses Unity's support for adding packages to the generated Xcode project, based on each library's Dependencies XML file.

For example, to add the Firebase Analytics package:

```xml
<dependencies>
  <remoteSwiftPackage url="https://github.com/firebase/firebase-ios-sdk.git" version="12.0.0">
    <swiftPackage name="FirebaseAnalytics"/>
  </remoteSwiftPackage>
</dependencies>
```

## Replace CocoaPods

In the `Dependencies.xml` files, a library can declare both Swift Packages and CocoaPods. This allows you to fall back to CocoaPods when needed. List which CocoaPods the Swift package replaces so resolution adds only one of them.

For example:

```xml
<dependencies>
  <remoteSwiftPackage url="https://github.com/googleads/swift-package-manager-google-mobile-ads.git" version="12.12.0" upToNextMajor="true">
    <swiftPackage name="GoogleMobileAds" replacesPod="Google-Mobiles-Ads-SDK"/>
  </remoteSwiftPackage>
</dependencies>
```

### Target Selection for Swift Packages

By default, packages are linked to the Unity framework target (`UnityFramework`). Use the `target` attribute on `<swiftPackage>` to specify which Xcode target the library or framework product should be linked to:

```xml
<dependencies>
  <iosPods>
    <remoteSwiftPackage url="https://github.com/firebase/firebase-ios-sdk.git" version="10.0.0">
      <!-- Linked only to UnityFramework (default) -->
      <swiftPackage name="FirebaseAnalytics" target="UnityFramework" />

      <!-- Linked only to the main app target (Unity-iPhone) -->
      <swiftPackage name="FirebaseMessaging" target="Main" />

      <!-- Linked only to the UnityAPI target (Unity 6.6+) -->
      <swiftPackage name="FirebaseMessaging" target="UnityAPI" />

      <!-- Linked to both UnityFramework and Unity-iPhone -->
      <swiftPackage name="SharedKit" target="All" />

      <!-- Linked to a custom target by exact target name -->
      <swiftPackage name="NotificationService" target="NotificationServiceExtension" />
    </remoteSwiftPackage>
  </iosPods>
</dependencies>
```

If a target doesn't exist in the exported Xcode project, for example `UnityAPI` when building with an Objective-C project type, an error is logged. To silently skip the package when the target isn't found, add `skipOnMissingTarget="true"`:

```xml
<swiftPackage name="FirebaseMessaging" target="UnityAPI" skipOnMissingTarget="true" />
```

## CocoaPods support

Unity generates a CocoaPods `Podfile` and runs the `pod` tool after the build to add dependencies to the exported Xcode project. Declare iOS pods in your dependency XML.

For example, to add the AdMob pod version 7.0 or greater with bitcode enabled:

```xml
<dependencies>
  <iosPods>
    <iosPod name="Google-Mobile-Ads-SDK" version="~> 7.0" bitcodeEnabled="true" minTargetSdk="6.0" addToAllTargets="false" />
  </iosPods>
</dependencies>
```

### Integrate CocoaPods

You can integrate CocoaPods in two ways:

* **Xcode Project integration**: Add CocoaPods to the `.xcodeproj` directly without creating a separate `xcworkspace`.
* **Xcode workspace integration**: If your Unity version supports opening an `xcworkspace`, the `pod` tool can generate a workspace that references CocoaPods.

Change the resolution strategy from **Edit** > **Project Settings** > **External Dependency Manager** > **iOS Resolver** > **Cocoapods Integration**.

### Append text to the generated Podfile

To modify the generated Podfile, create a script file such as the following example:

```c#
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class PostProcessIOS
{
    // Must be between 40 and 50 to ensure that it's not overridden by Podfile generation (40) and
    // that it's added before "pod install" (50).
    [PostProcessBuildAttribute(45)]
    private static void PostProcessBuild_iOS(BuildTarget target, string buildPath)
    {
        if (target == BuildTarget.iOS)
        {
            using (StreamWriter sw = File.AppendText(buildPath + "/Podfile"))
            {
                // For example, add an app extension
                sw.WriteLine("\ntarget 'NSExtension' do\n  pod 'Firebase/Messaging', '6.6.0'\nend");
            }
        }
    }
}
```

## Additional resources

* [iOS Resolver settings reference](ios-resolver-settings-reference.md)