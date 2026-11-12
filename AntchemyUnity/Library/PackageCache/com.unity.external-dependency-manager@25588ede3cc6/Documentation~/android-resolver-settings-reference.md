# Android Resolver settings reference

Use the Android Resolver settings to control dependency resolution, Gradle integration, and build behavior.

Access the following Android Resolver settings from **Edit** > **Project Settings** > **External Dependency Manager** > **Android Resolver**.

| Property | Description |
| :---- | :---- |
| **Resolve Automatically** | Automatically resolve Android dependencies when changes are detected in the Editor. |
| **Resolve Automatically On Build** | Automatically resolve Android dependencies before each Android build. |
| **Auto-Resolution Disabled Warning** | Show a warning in the Editor when auto-resolution is disabled. <br/><br/> **Note**: This property is available only when both **Resolve Automatically** and **Resolve Automatically On Build** are disabled. |
| **Prompt Before Automatic Resolution** | Show a confirmation prompt each time auto-resolution is triggered in the Editor. The prompt isn't shown during automatic resolution on build. |
| **Patch AndroidManifest.xml** | Automatically replace references of the `applicationId` variable with the bundle ID in the `AndroidManifest.xml` file. |
| **Use Jetifier** | Enable Jetifier to convert legacy Android support packages to AndroidX equivalents. When enabled, the Android Resolver injects `android.useAndroidX=true` and `android.enableJetifier=true` into `gradleTemplate.properties`. Requires [**Custom Gradle Properties Template**](https://docs.unity3d.com/Manual/class-PlayerSettingsAndroid.html#Publishing) to be enabled in the Android Player settings. Disabling this setting removes the injected flags from `gradleTemplate.properties`. For more information, refer to [Jetifier](https://developer.android.com/tools/jetifier). |
| **Use Full Maven Repo Path When Building** | Use the full absolute filesystem path to the **Local Maven Repo Directory**, instead of a relative path, when building the Android app through Unity. Enable this if your project's location on disk doesn't change between builds. Disable it if you build the project from different locations or machines. |
| **Use Full Maven Repo Path When Exporting** | Use the full absolute filesystem path to the **Local Maven Repo Directory**, instead of a relative path, when exporting the Android project. Enable this if your project's location on disk doesn't change between exports. Disable it if you export the project to different locations or machines. |
| **Local Maven Repo Directory** | Specify where your local Maven repository is installed. It must be under a folder inside the project's `Assets` directory, and can't be under `Assets/Plugins/Android`. For example `Assets/GeneratedLocalRepo`. If you enter or browse to a path that doesn't meet these requirements, EDM displays a warning. |
| **Copy and Patch settingsTemplate.gradle** | For Unity 2022.2 and above, specify additional Maven repositories in `settingsTemplate.gradle`. Enable this setting so the External Dependency Manager also copies and patches `settingsTemplate.gradle` from the Unity engine folder. |
| **Verbose logging** | Enable a detailed log output that's useful for debugging while filing a bug report or building a new managed plug-in. |

## Additional resources

* [Get started with Android Resolver](get-started-with-android-resolver.md)
* [Introduction to the Version Handler (deprecated)](introduction-to-the-version-handler.md)