# Get started with Android Resolver

Configure and use the Android Resolver to manage Android library dependencies in your project.

The Android Resolver manages Android library dependencies declared in your Unity project. It injects dependency declarations into your Gradle build templates, along with any Maven repository URLs specified by those dependencies. Gradle then resolves and downloads the dependencies at build time.

Resolution runs automatically when the Android build target is active and your project's dependencies change. You can also trigger resolution manually.

For example, to add the Google Play Games library (`com.google.android.gms:play-services-games` package) at version `24.0.0` to a plug-in's Android dependencies:

```xml
<dependencies>
  <androidPackages>
    <androidPackage spec="com.google.android.gms:play-services-games:24.0.0">
      <androidSdkPackageIds>
        <androidSdkPackageId>extra-google-m2repository</androidSdkPackageId>
      </androidSdkPackageIds>
    </androidPackage>
  </androidPackages>
</dependencies>
```

The version specification element supports:

- Specific versions, for example `24.0.0`.
- Partial matches, for example `24.0.+`  matches `24.0.0`, `24.0.1`, and later versions.
- Latest version using `LATEST` or `+`.  Avoid `LATEST` unless you're confident updates to the library will not break your Unity plug-in.

## Configure auto resolution

By default, the Android Resolver automatically monitors the dependencies you have specified and the `Assets/Plugins/Android` folder of your Unity project. The resolution process runs automatically if any dependencies are updated, or if the specified dependencies aren't present in your project.

To disable the auto resolution process, navigate to **Edit** > **Project Settings** > **External Dependency Manager** > **Android Resolver** and disable **Resolve Automatically**.

To resolve Android dependencies manually:

1. Select **Resolve** from the **Assets** > **External Dependency Manager** > **Android Resolver** menu.
2. If the **Resolve** operation fails, select **Assets** > **External Dependency Manager** > **Android Resolver** > **Force Resolve**.

## Remove libraries

The Android Resolver tracks resolved packages using asset labels. To remove resolved packages, navigate to **Assets** > **External Dependency Manager** > **Android Resolver** > **Delete Resolved Libraries**.

## Resolve dependencies with mainTemplate.gradle

The Android Resolver integrates dependencies by patching your project's `mainTemplate.gradle` file rather than downloading and processing AAR files directly. Android resolution results in the execution of the following operations:

1. Remove the result of previous Android resolutions. For example, delete all files and directories labeled with `gpsr` under `Assets/Plugins/Android` from the project and remove sections delimited with `// Android Resolver * Start` and `// Android Resolver * End` lines.
2. Collect the set of Android dependencies (libraries) specified by a project's `*Dependencies.xml` files.
3. Rename any `.srcaar` files in the build to `.aar` and exclude them from the Android build. `mainTemplate.gradle` is patched to include them instead of using local maven repositories.
4. Inject the required Gradle repositories into `mainTemplate.gradle` at the line matching the pattern `.*apply plugin: 'com\.android\.(application|library)'.*` or the section starting at the line `// Android Resolver Repos Start`. If you want to control the injection point in the file, place the section delimited by the lines `// Android Resolver Repos Start` and `// Android Resolver Repos End` in the global scope before the `dependencies` section.
5. Inject the required Android dependencies (libraries) into `mainTemplate.gradle` at the line matching the pattern `***DEPS***` or the section starting at the line `// Android Resolver Dependencies Start`. If you want to control the injection point in the file, place the section delimited by the lines `// Android Resolver Dependencies Start` and `// Android Resolver Dependencies End` in the `dependencies` section.
6. Inject the packaging options logic, which excludes architecture specific libraries based upon the selected build target, into `mainTemplate.gradle` at the line matching the pattern `android +{` or the section starting at the line `// Android Resolver Exclusions Start`. If you want to control the injection point in the file, place the section delimited by the lines `// Android Resolver Exclusions Start` and `// Android Resolver Exclusions End` in the global scope before the `android` section.

## Track dependencies

The Android Resolver creates the `ProjectSettings/AndroidResolverDependencies.xml` file to record the set of resolved dependencies in a project. **Auto-resolution** uses this file to decide when to run resolution again.

## View resolved dependencies

To view a list of the dependencies the Android Resolver has resolved in your project, navigate to **Assets** > **External Dependency Manager** > **Android Resolver** > **Display Libraries**.

## Additional resources

* [Android Resolver settings reference](android-resolver-settings-reference.md)