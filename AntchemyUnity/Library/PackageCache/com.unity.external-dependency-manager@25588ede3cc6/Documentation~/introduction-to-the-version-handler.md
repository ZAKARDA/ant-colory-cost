# Introduction to the Version Handler (deprecated)

Manage shared Unity plug-in dependencies and upgrades with the Version Handler component.

> [!IMPORTANT]
> This feature is unsupported and will be removed in a future version of the package.

The Version Handler component of this package manages:

* Shared Unity plug-in dependencies.
* Upgrades to Unity plug-ins, including cleanup of files from previous versions.
* Restoration of plug-in assets to their original install locations if assets are tagged with the `exportpath` label.

> [!NOTE]
> The Version Handler doesn't work with Package Manager-installed packages because it must modify asset metadata. In addition, the Version Handler doesn't affect EDM libraries.

## Use Version Handler managed plug-ins

If you import multiple versions of a plug-in and the Version Handler is enabled, it checks managed assets automatically and determines which are outdated or should be removed. To disable automatic checks, disable the **Enable version management** option in **Edit** > **Project Settings** > **External Dependency Manager** > **Version Handler**.

If version management is disabled, check managed assets manually by navigating to **Assets** > **External Dependency Manager** > **Version Handler** > **Update**.

## List managed plug-ins

Plug-ins managed by the Version Handler, for example those that ship with manifest files, can be displayed using the **Assets** > **External Dependency Manager** > **Version Handler** > **Display Managed Packages** menu option. The list of plug-ins is written to the Console window with the files each plug-in uses.

## Restore install paths

Some developers move assets around in their project which can make it harder for plug-in maintainers to debug issues if this breaks Unity's [special folders](https://docs.unity3d.com/Manual/SpecialFolders.html) rules. If assets are labeled with their original install/export path, Version Handler can restore assets to their original locations when using the **Assets** > **External Dependency Manager** > **Version Handler** > **Move Files To Install Locations** menu option.

## Settings reference

Some behaviors of the **Version Handler** can be configured via **Edit** > **Project Settings** > **External Dependency Manager** > **Version Handler**.

| Property | Description |
| :---- | :---- |
| **Enable version management** | Control whether the plug-in automatically checks asset versions and applies changes. If this is disabled the process can be run manually when installing or upgrading managed plug-ins using **Assets** > **External Dependency Manager** > **Version Handler** > **Update**. |
| **Rename to canonical filenames** | Rename files to remove version numbers and other labels from filenames. |
| **Prompt for obsolete file deletion** | Enable the display of a window when obsolete files are deleted allowing the developer to select which files to delete and those to keep.  |
| **Allow disabling files via renaming** | Control whether obsolete or disabled files should be disabled by renaming them to `myfilename_DISABLED`. Renaming to disable files is required in some scenarios where Unity doesn't support removing files from the build when importing plug-ins. |
| **Verbose logging** | Enable a detailed log output that's useful for debugging while filing a bug report or building a new managed plug-in. |

## Additional resources

* [Troubleshooting common issues with EDM](troubleshooting-common-issues-with-edm.md)