# Troubleshooting common issues with EDM

Solve common issues when working with the External Dependency Manager (EDM) package.

## Enable verbose logging

When you need more resolver or build information, enable verbose logging and check the log after a build attempt. To enable verbose logging on Android and iOS:

1. For Android, enable **Verbose Logging** in **Edit** > **Project Settings** > **External Dependency Manager** > **Android Resolver**.
2. For iOS, enable **Verbose Logging** in **Edit** > **Project Settings** > **External Dependency Manager** > **iOS Resolver**.

## Disable auto resolution on Android

When Auto Resolution is enabled, the Android resolver can trigger when assets are changed or when the App Domain reloads, for example when selecting the Play button in the Unity Editor. Dependency resolution can be slow when the Unity project is large, or when the resolver needs to download and patch many Android libraries.

To disable auto resolution and speed up iteration:

1. Select **Assets** > **External Dependency Manager** > **Android Resolver** > **Resolve** or **Assets** > **External Dependency Manager** > **Android Resolver** > **Force Resolve** for manual resolution.
2. Disable **Resolve Automatically** in **Edit** > **Project Settings** > **External Dependency Manager** > **Android Resolver** to prevent resolution from running when assets change or when the App Domain reloads.
3. Disable **Resolve Automatically On Build** in the same settings page to speed up build time.

## Troubleshoot issues with EDM on Android

### Fix Resolution Failed errors

If resolution fails, follow these steps. After each step, confirm that resolution succeeded before you continue.

#### Use Force Resolve to fix resolution errors

To resolve dependencies:

1. Select **Assets** > **External Dependency Manager** > **Android Resolver** > **Resolve**.
2. If this fails, select **Force Resolve**. Force Resolve is slower but more dependable because it clears old intermediate data.

#### Fix JDK, SDK, NDK, and Gradle issues

If you receive error logs about Unity being unable to locate the JDK, Android SDK Tools, NDK, or Gradle files, use the following steps to force Unity to recognize them:

1. Navigate to **Unity > Preferences > External Tools**.
2. Toggle the **JDK**, **Android SDK**, **Android NDK** and **Gradle** checkboxes to their opposite value.
3. Toggle them back.
4. Run **Force Resolve** or build again.

#### Enable Jetifier

Android 9 introduced a new set of support libraries (AndroidX) which use the same class name but under a different package name. If your project has dependencies , including transitive dependencies, on both AndroidX and the older Android Support Libraries, duplicated class errors in `com.google.android.support.*` and `com.google.androidx.*` will occur during build time. [Jetifier](https://developer.android.com/tools/jetifier) is a tool to resolve such cases. It's recommended to enable Jetifier if your target Android version is 9+, or API level 28+.

To use Jetifier, enable [**Custom Gradle Properties Template**](https://docs.unity3d.com/Manual/class-PlayerSettingsAndroid.html#Publishing) in the Android Player settings, then enable **Use Jetifier** in **Edit** > **Project Settings** > **External Dependency Manager** > **Android Resolver**. The Android Resolver injects `android.useAndroidX=true` and `android.enableJetifier=true` into `gradleTemplate.properties` automatically during the next resolve. Disabling **Use Jetifier** removes the injected flags on the next resolve.

## Troubleshoot issues with EDM on iOS

If resolution fails for iOS, use the following steps. After each step, confirm that resolution succeeded before you continue.

### Verify CocoaPods is installed correctly

1. Ensure CocoaPods is installed correctly. For more information, refer to [Getting Started](https://guides.cocoapods.org/using/getting-started.html) (CocoaPods).
2. Verify that `pod install` and `pod update` run without errors in the folder where the Podfile is. This is usually the root folder of the Xcode project. For more information, refer to [pod install vs. pod update](https://guides.cocoapods.org/using/pod-install-vs-update.html) (CocoaPods).

### Fix CocoaPods detection issues

EDM automatically detects the `pod` executable, including Homebrew installs on Apple Silicon and Intel Macs, and Ruby version managers such as rbenv and RVM. If CocoaPods is installed but EDM still reports that it can't find `pod`:

1. Navigate to **Edit** > **Project Settings** > **External Dependency Manager** > **iOS Resolver**.
2. Enable **Use Custom CocoaPods Executable Path**.
3. Set **CocoaPods Executable Path** to the path of the `pod` executable. For example, `/opt/homebrew/bin/pod` or `/usr/local/bin/pod`.

### CocoaPods text encoding

When building from macOS, you might encounter issues related to text encoding. For example `WARNING: CocoaPods requires your terminal to be using UTF-8 encoding.` To resolve this issue, try the following steps:

1. From the terminal, run `pod install` directly, and open the resulting `xcworkspace` file.
2. Downgrade the version of CocoaPods to 1.10.2. The issue exists only in version 1.11 and newer.
3. In your `~/.bash_profile` or equivalent, add `export LANG=en_US.UTF-8`.

### Open Xcode Workspace files instead of Xcode Project files

Build iOS builds from [Xcode Workspaces](https://developer.apple.com/library/archive/featuredarticles/XcodeConcepts/Concept-Workspace.html) generated by CocoaPods, rather than using Xcode projects.

* Unity by default only generates `.xcodeproj` files. If the EDM package is in the project, it first generates Podfiles from all iOS dependencies specified in files named `Dependencies.xml` with a prefix, for example `AppDependencies.xml` , then runs CocoaPods, which generates an `.xcworkspace` file.
* In this case, open the generated project by double-clicking on `.xcworkspace` instead of `.xcodeproj` because the `.xcworkspace` file contains references to pods.
* If you build in an environment where you can't open Xcode workspaces, such as Unity Cloud Build, open **Edit** > **Project Settings** > **External Dependency Manager** > **iOS Resolver** and set **Cocoapods Integration** to **Xcode project**.

### Fix runtime Swift issues

If the game fails to run and the error logs mention Swift, the issue is likely related to Swift framework linking. To fix it, follow these steps:

1. Enable **Enable Swift Framework Support Workaround** in **Edit > Project Settings > External Dependency Manager > iOS Resolver**. This adds a placeholder Swift file to the project and enables the `CLANG_ENABLE_MODULES` build setting, which links the Swift standard library.
2. Open the generated Xcode project and confirm the placeholder Swift file and `CLANG_ENABLE_MODULES` build setting are present.

## Additional resources

* [Android Resolver settings reference](android-resolver-settings-reference.md)
* [iOS Resolver settings reference](ios-resolver-settings-reference.md)
* [Get started with Android Resolver](get-started-with-android-resolver.md)
* [Get started with iOS Resolver](get-started-with-ios-resolver.md)