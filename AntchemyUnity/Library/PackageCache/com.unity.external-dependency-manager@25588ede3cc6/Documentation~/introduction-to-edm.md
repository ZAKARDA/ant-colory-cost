# Introduction to the External Dependency Manager

Understand the External Dependency Manager (EDM) and how it works with Android and iOS dependencies.

Many Unity plug-ins depend on Android-specific libraries, iOS CocoaPods, and Swift packages. Without a dependency manager, you might experience the following issues:

* Integrating platform-specific libraries for Android and iOS in a Unity project is complex.
* Resolving conflicting dependencies between plug-ins requires manual work.

The EDM package provides solutions for each of these issues.

## Android Resolver

The Android Resolver integrates Android library dependencies and resolves conflicts between plug-ins that share the same dependencies.

For example, a plug-in that requires the Google Play Games Android library redistributes the library and its transitive dependencies in the `ExamplePlugin/Android/` folder. If you import another plug-in that includes different versions of the same libraries, you might encounter Android build errors that are difficult to diagnose.

The Android Resolver provides the following capabilities:

* Solves Android library conflicts between plug-ins.
* Performs the processing steps required to use Android libraries (AARs and JARs) in Unity.
* (Experimental) Supports minification of included Java components without exporting a project.

For more information, refer to [Get started with Android Resolver](get-started-with-android-resolver.md).

## iOS Resolver

The iOS Resolver integrates with [CocoaPods](https://cocoapods.org/) and [Swift Package Manager](https://docs.swift.org/swiftpm/documentation/packagemanagerdocs/) to download and add iOS libraries and frameworks into your Xcode project. Multiple plug-ins can then share the same native components without manual work to fix duplicate or incompatible library versions.

For more information, refer to [Get started with iOS Resolver](get-started-with-ios-resolver.md).

## Additional resources

* [Get started with Android Resolver](get-started-with-android-resolver.md)
* [Get started with iOS Resolver](get-started-with-ios-resolver.md)
* [Get started with EDM](get-started-with-edm.md)
* [Use EDM in your project](use-edm-in-your-project.md)