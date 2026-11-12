# Get started with EDM

Add the External Dependency Manager (EDM) package to your project and configure dependencies.

## Add EDM to your project

To add EDM to your project, select the following link: <a class="kharma">com.unity.external-dependency-manager</a>. This opens the Package Manager window with the package name and version filled in. Select **Install** to add the package to your project.

Alternatively, open EDM via the **Package Manager** directly:

1. Go to **Window** > **Package Management** > **Package Manager**.
2. Select **Unity Registry**, then search for `External Dependency Manager`.
3. Select the package, then select **Install**.

The EDM package is added to your project.

## Select a dependency manager

If you install EDM into a new project, or an existing project where EDM is your only dependency manager, EDM is selected by default.

If you install the EDM package in a project that already uses a different external dependency manager, Unity prompts you to choose one. Examples of alternative managers include Google's EDM4U and the Levelplay Mobile Dependency Resolver.

To avoid conflicts, select which manager stays active. Select **Use Unity's External Dependency Manager** to use EDM in your project, or select **Close** to continue using the alternative.

You can also switch dependency managers manually. Go to **Assets** > **External Dependency Manager**, then select **Change To Unity's EDM** or **Change To Alternative EDM**.

It's recommended to have only one external dependency manager installed at a time.

Unity disables these menu items in the following cases:

* **Change To Unity's EDM** is disabled if EDM is already the active dependency manager.
* **Change To Alternative EDM** is disabled if EDM is already the active dependency manager and no alternative dependency manager package has been installed in the project.

### Remove an alternative dependency manager

How you remove an alternative dependency manager and retain only EDM depends on how you installed it:

* Legacy .unitypackage install: Delete the folder that contains the package.
* Package installed through the Unity Package Manager (UPM), without SDK dependencies: Go to **Assets** > **External Dependency Manager** and select **Change To Alternative EDM**, then uninstall the package.
* Package installed as a dependency of another UPM package: Keep the alternative dependency manager installed until the package migrates to EDM. If you uninstall the alternative dependency manager, Unity also uninstalls the dependent package.

## Add dependencies

To add dependencies to your project, create an XML file inside an `Editor` folder and name it `<name>Dependencies.xml`. For example, `MyPlugin/Editor/MyPluginDependencies.xml`.

For examples on how to structure the XML file, refer to [Get started with Android Resolver](get-started-with-android-resolver.md) and [Get started with iOS Resolver](get-started-with-ios-resolver.md).

## Plug-in distributor guide

If your plug-in requires EDM, the installation method varies depending on how your plug-in is distributed.

### Distribute as a UPM package

Add EDM as a [package dependency](https://docs.unity3d.com/Documentation/Manual/upm-dependencies.html) in your `package.json`:

```json
{
  "dependencies": {
    "com.unity.external-dependency-manager": "2.1.0"
  }
}
```

Unity's Package Manager resolves and installs EDM automatically when your package is installed. Install EDM in your development project so you can test the package integration locally.

### Distribute as a .unitypackage (legacy)

`.unitypackage` files have no dependency resolution mechanism. You must instruct users to install the EDM package separately from the Unity Package Manager before importing your package. Make this requirement visible in your `README.md` and package documentation.

Alternatively, you can include an editor script in your `.unitypackage` that automatically installs EDM via the Package Manager API on first import.

For example:

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;

[InitializeOnLoad]
public static class EDMInstaller
{
    static EDMInstaller()
    {
        // Replace VERSION with the specific package version you want to target
        Client.Add("com.unity.external-dependency-manager@VERSION");
    }
}
#endif
```


## Additional resources

* [Use EDM in your project](use-edm-in-your-project.md)
* [Troubleshooting EDM](troubleshooting-common-issues-with-edm.md)