# iOS Resolver settings reference

Use the iOS Resolver settings to control dependency resolution, CocoaPods and Swift Package Manager integration, and build behavior.

Access the following iOS Resolver settings from **Edit** > **Project Settings** > **External Dependency Manager** > **iOS Resolver**.

| Property | Description |
| :---- | :---- |
| **Swift Package Manager Integration** | Use [Swift Package Manager](https://docs.swift.org/swiftpm/documentation/packagemanagerdocs/) to resolve dependencies. When enabled, the resolver prioritizes Swift Package Manager packages and uses CocoaPods for any packages without a Swift equivalent. |
| **Allow empty Podfile generation** | Generate an empty Podfile when there are no pods to install. |
| **Podfile Generation** | Enable **Podfile Generation** to install CocoaPods in your project. <br/><br/>**Note**: Disable **Podfile Generation** if you're manually adding frameworks in the generated Xcode project. |
| **Cocoapods Integration** | Choose how to integrate CocoaPods: <ul><li> **Xcode Workspace - Add CocoaPods to the Xcode workspace** </li><li> **Xcode Project - Add CocoaPods to the Xcode project** </li><li> **None - Do not integrate CocoaPods** </li></ul>|
| **Use Shell to Execute Cocoapod Tool** | Use Shell execution to execute CocoaPods tools in the shell environment.  |
| **Set LANG When Using Shell** | Use LANG when executing CocoaPods tools in the shell environment. <br/><br/> **Note**: This property is available only when **Use Shell to Execute Cocoapod Tool** is enabled. |
| **Auto Install Cocoapod Tools in Editor** | Enable to automatically install the CocoaPods tools. If disabled, you can manually install CocoaPods tools from **Assets** > **External Dependency Manager** > **iOS Resolver** > **Install CocoaPods.** |
| **Verbose logging** | Enable a detailed log output that's useful for debugging while filing a bug report or building a new managed plug-in. |
| **Use Custom CocoaPods Executable Path** | Use the path specified in **CocoaPods Executable Path** directly as the CocoaPods executable, bypassing automatic detection. |
| **CocoaPods Executable Path** | Absolute path to the CocoaPods executable, for example `/opt/homebrew/bin/pod` or `/usr/local/bin/pod`. <br/><br/> **Note**: This property is available only when **Use Custom CocoaPods Executable Path** is enabled. |

## Podfile Configurations

The following properties are available only when **Podfile Generation** is enabled.

| Property | Description |
| :---- | :---- |
| **Add use_frameworks! to Podfile** | Adds `use_frameworks!` to the Podfile. This is required if you're using any third-party packages that depend on Swift frameworks. |
| **Link Frameworks Statically** | Add `use_frameworks! :linkage => :static`  to the Podfile. Enable this property if any pod framework in your project includes static libraries. <br/><br/> **Note**: This property is available only when **Add use_frameworks! to Podfile** is enabled. |
| **Enable Swift Framework Support Workaround** | Patch the Xcode project to link the Swift standard library when plug-ins depend on Swift framework pods. Adds a placeholder Swift file and enables the `CLANG_ENABLE_MODULES` build setting. <br/><br/> **Note**: This property is available only when **Add use_frameworks! to Podfile** is enabled. |
| **Swift Language Version** | Override the `SWIFT_VERSION` build setting in Xcode.  <br/><br/> **Note**: This property is available only when **Enable Swift Framework Support Workaround** is enabled. |
| **Always Add Main Target to Podfile** | Adds the following code to the Podfile to point to the main target: ``target `Unity-iPhone' do end``  |
| **Allow Pods in Multiple Targets** | Allow a Podfile to appear across multiple targets.<br/><br/> **Note**: The Podfile must be specified in the `Dependencies.xml` file with an `addToAllTargets="true"` attribute. This property is available only when **Always Add Main Target to Podfile** is enabled. |

## Additional resources

* [Get started with iOS Resolver](get-started-with-ios-resolver.md)
* [Introduction to the Version Handler (deprecated)](introduction-to-the-version-handler.md)