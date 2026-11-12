using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_6000_7_OR_NEWER
using Unity.Scripting.LifecycleManagement;
#endif

#if UNITY_6000_7_OR_NEWER
[NoAutoStaticsCleanup]
#endif
internal class EDMProjectSettings : SettingsProvider
{
    const string SettingsPath = "Project/External Dependency Manager";
    const string DocsUrl = "https://docs.unity3d.com/Packages/com.unity.external-dependency-manager@latest";
    const float LabelWidth = 280f;
    const float Indent = 16f;

    static readonly List<string> CocoapodsIntegrationOptions = new List<string> {
        "None - Do not integrate Cocoapods",
        "Xcode Project - Add Cocoapods to the Xcode project",
        "Xcode Workspace - Add Cocoapods to the Xcode workspace",
    };

    VisualElement _root;
    static int _activeTabIndex = 0;
    static EDMProjectSettings _instance;

    EDMProjectSettings() : base(SettingsPath, SettingsScope.Project) { }

    [SettingsProvider]
    static SettingsProvider Create() => new EDMProjectSettings();

    static void OpenSettingsAtTab(int tabIndex) {
        _activeTabIndex = tabIndex;
        // If the window is already open, OnActivate won't fire again, so rebuild directly.
        if (_instance != null) {
            _instance.BuildUI();
        }
        SettingsService.OpenProjectSettings(SettingsPath);
    }

#if !UNITY_EDM_DISABLED
    [MenuItem("Assets/External Dependency Manager/Android Resolver/Settings")]
    static void OpenAndroidSettings() => OpenSettingsAtTab(0);

    [MenuItem("Assets/External Dependency Manager/iOS Resolver/Settings")]
    static void OpenIOSSettings() => OpenSettingsAtTab(1);

    [MenuItem("Assets/External Dependency Manager/Version Handler/Settings")]
    static void OpenVersionHandlerSettings() =>
        SettingsService.OpenProjectSettings(SettingsPath);
#endif

    public override void OnActivate(string searchContext, VisualElement rootElement)
    {
        _root = rootElement;
        _instance = this;

        var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            $"Packages/{EDMInstancesHandling.UedmPackageName}/Editor/UI/EDMProjectSettings.uss");
        if (uss != null)
            _root.styleSheets.Add(uss);

        SubscribeExternalSettingsEvent(true);
        EDMInstancesHandling.ReadSettingsFromLegacySettings();
        BuildUI();
    }

    public override void OnDeactivate()
    {
        _instance = null;
        SubscribeExternalSettingsEvent(false);
    }

    void SubscribeExternalSettingsEvent(bool subscribe)
    {
        foreach (var assemblyQualifiedName in new[] {
            "GooglePlayServices.SettingsDialog, Google.JarResolver",
            "Google.IOSResolver, Google.IOSResolver" })
        {
            var type = System.Type.GetType(assemblyQualifiedName);
            if (type == null) continue;
            var evt = type.GetEvent("OnSettingsChanged");
            if (evt == null) continue;
            var handler = System.Delegate.CreateDelegate(evt.EventHandlerType, this,
                nameof(OnExternalSettingsChanged));
            if (subscribe) evt.AddEventHandler(null, handler);
            else evt.RemoveEventHandler(null, handler);
        }
    }

    void OnExternalSettingsChanged()
    {
        EDMInstancesHandling.ReadSettingsFromLegacySettings();
        BuildUI();
    }

    void BuildUI()
    {
        _root.Clear();

        var edmEnabled = EDMInstancesHandling.Settings.EdmEnabled;

        var titleRow = new VisualElement();
        titleRow.style.flexDirection = FlexDirection.Row;
        titleRow.style.alignItems = Align.Center;
        titleRow.style.paddingLeft = 14;
        titleRow.style.paddingRight = 2;
        titleRow.style.paddingTop = 4;
        titleRow.style.paddingBottom = 6;
        titleRow.style.marginBottom = 6;
        titleRow.style.flexShrink = 0;
        _root.Add(titleRow);

        var title = new Label("External Dependency Manager");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.fontSize = 19;
        title.style.flexGrow = 1;
        titleRow.Add(title);

        var helpButton = new Button(() => Application.OpenURL(DocsUrl));
        helpButton.tooltip = "Open documentation";
        helpButton.Add(new Image { image = (Texture2D)EditorGUIUtility.IconContent("_Help").image });
        helpButton.AddToClassList("edm-header-button");
        titleRow.Add(helpButton);

        if (!edmEnabled)
        {
            BuildDisabledUI();
            return;
        }

        var menuButton = new Button();
        menuButton.Add(new Image { image = (Texture2D)EditorGUIUtility.IconContent("_Menu").image });
        menuButton.AddToClassList("edm-header-button");
        menuButton.clicked += () =>
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Reset Android Resolver Settings"), false, ResetAndroidSettings);
            menu.AddItem(new GUIContent("Reset iOS Resolver Settings"), false, ResetIOSSettings);
            menu.AddItem(new GUIContent("Reset Version Handler Settings"), false, ResetVersionHandlerSettings);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Reset All Settings"), false, ResetSettings);
            menu.ShowAsContext();
        };
        titleRow.Add(menuButton);

        var settings = EDMResolverSettings.instance;
        var scroll = new ScrollView(ScrollViewMode.Vertical);
        scroll.style.flexGrow = 1;
        scroll.contentContainer.style.paddingLeft = 14;
        scroll.contentContainer.style.paddingRight = 14;
        _root.Add(scroll);

#if UNITY_6000_0_OR_NEWER
        var tabView = new TabView();
        tabView.style.borderTopLeftRadius = 6;
        tabView.style.borderTopRightRadius = 6;
        tabView.style.borderBottomLeftRadius = 6;
        tabView.style.borderBottomRightRadius = 6;
        tabView.style.overflow = Overflow.Hidden;
        tabView.Add(BuildAndroidTab(settings));
        tabView.Add(BuildIOSTab(settings));
        tabView.selectedTabIndex = _activeTabIndex;
        tabView.activeTabChanged += (_, tab) => _activeTabIndex = tabView.selectedTabIndex;
        scroll.Add(tabView);
#else // legacy option
        scroll.Add(BuildTabView(_activeTabIndex, idx => _activeTabIndex = idx,
            ("Android Resolver", BuildAndroidTab(settings)),
            ("iOS Resolver", BuildIOSTab(settings))
        ));
#endif // end of legacy option

        var versionHandlerSection = new VisualElement();
        BuildVersionHandlerSection(versionHandlerSection, settings);
        scroll.Add(versionHandlerSection);
    }

    void BuildDisabledUI()
    {
        var container = new VisualElement();
        container.style.paddingLeft = 14;
        container.style.paddingRight = 14;
        container.style.paddingTop = 4;
        container.style.flexGrow = 1;
        _root.Add(container);

        var headline = new Label("Unity's External Dependency Manager is not active.");
        headline.style.unityFontStyleAndWeight = FontStyle.Bold;
        headline.style.fontSize = 13;
        headline.style.whiteSpace = WhiteSpace.Normal;
        headline.style.marginBottom = 6;
        container.Add(headline);

        var subtext = new Label("Your project is currently using an alternative External Dependency Manager. Settings are only available when Unity's EDM is active.");
        subtext.style.whiteSpace = WhiteSpace.Normal;
        subtext.style.marginBottom = 12;
        container.Add(subtext);

        var switchButton = new Button(EDMManager.InstallEDMSelected);
        switchButton.text = "Change to Unity's External Dependency Manager";
        switchButton.style.whiteSpace = WhiteSpace.Normal;
        container.Add(switchButton);
    }

    void ResetAndroidSettings()
    {
        var settings = EDMResolverSettings.instance;
        settings.android.Dispose();
        settings.android = new EDMAndroidSettings();
        settings.Save();
        BuildUI();
    }

    void ResetIOSSettings()
    {
        var settings = EDMResolverSettings.instance;
        settings.ios.Dispose();
        settings.ios = new EDMIOSSettings();
        settings.Save();
        BuildUI();
    }

    void ResetVersionHandlerSettings()
    {
        var settings = EDMResolverSettings.instance;
        settings.versionHandler.Dispose();
        settings.versionHandler = new EDMVersionHandlerSettings();
        settings.Save();
        BuildUI();
    }

    void ResetSettings()
    {
        var settings = EDMResolverSettings.instance;
        settings.android.Dispose();
        settings.ios.Dispose();
        settings.versionHandler.Dispose();
        settings.android = new EDMAndroidSettings();
        settings.ios = new EDMIOSSettings();
        settings.versionHandler = new EDMVersionHandlerSettings();
        settings.Save();
        BuildUI();
    }

#if UNITY_6000_0_OR_NEWER
    static Tab CreateTab(string label)
    {
        var tab = new Tab(label);
        tab.style.paddingTop = 4;
        tab.style.paddingBottom = 4;
        tab.style.backgroundColor = new StyleColor(EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.05f)
            : new Color(0f, 0f, 0f, 0.05f));
        return tab;
    }

    static Tab BuildAndroidTab(EDMResolverSettings settings)
    {
        var tab = CreateTab("Android Resolver");
        var content = new VisualElement();
        tab.Add(content);
        BuildAndroidContent(content, settings.android);
        return tab;
    }

    static Tab BuildIOSTab(EDMResolverSettings settings)
    {
        var tab = CreateTab("iOS Resolver");
        var content = new VisualElement();
        tab.Add(content);
        BuildIOSContent(content, settings.ios);
        return tab;
    }
#else // legacy option
    static VisualElement CreateTabContent()
    {
        var content = new VisualElement();
        content.style.paddingTop = 4;
        content.style.paddingBottom = 4;
        content.style.paddingLeft = 8;
        content.style.paddingRight = 8;
        content.style.backgroundColor = new StyleColor(EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.05f)
            : new Color(0f, 0f, 0f, 0.05f));
        return content;
    }

    static VisualElement BuildTabView(int activeIndex, Action<int> onTabChanged, params (string label, VisualElement content)[] tabs)
    {
        var container = new VisualElement();
        container.style.borderTopLeftRadius = 6;
        container.style.borderTopRightRadius = 6;
        container.style.borderBottomLeftRadius = 6;
        container.style.borderBottomRightRadius = 6;
        container.style.overflow = Overflow.Hidden;

        var tabBar = new VisualElement();
        tabBar.style.flexDirection = FlexDirection.Row;
        tabBar.AddToClassList("edm-tab-bar");
        container.Add(tabBar);

        activeIndex = Mathf.Clamp(activeIndex, 0, tabs.Length - 1);
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].content.style.display = i == activeIndex ? DisplayStyle.Flex : DisplayStyle.None;
            container.Add(tabs[i].content);
        }

        var selectedUnderlineColor = new StyleColor(EditorGUIUtility.isProSkin
            ? new Color(0.85f, 0.85f, 0.85f)
            : new Color(0.2f, 0.2f, 0.2f));
        var clearColor = new StyleColor(Color.clear);

        var buttons = new Button[tabs.Length];
        for (int i = 0; i < tabs.Length; i++)
        {
            var button = new Button { text = tabs[i].label };
            button.AddToClassList("edm-tab-button");
            if (i == activeIndex)
            {
                button.AddToClassList("edm-tab-button--selected");
                button.style.borderBottomColor = selectedUnderlineColor;
            }
            buttons[i] = button;
            tabBar.Add(button);

            var capturedIdx = i;
            button.clicked += () =>
            {
                onTabChanged(capturedIdx);
                for (int j = 0; j < tabs.Length; j++)
                {
                    var isSelected = j == capturedIdx;
                    tabs[j].content.style.display = isSelected ? DisplayStyle.Flex : DisplayStyle.None;
                    buttons[j].EnableInClassList("edm-tab-button--selected", isSelected);
                    buttons[j].style.borderBottomColor = isSelected ? selectedUnderlineColor : clearColor;
                }
            };
        }

        return container;
    }

    static VisualElement BuildAndroidTab(EDMResolverSettings settings)
    {
        var tab = CreateTabContent();
        BuildAndroidContent(tab, settings.android);
        return tab;
    }

    static VisualElement BuildIOSTab(EDMResolverSettings settings)
    {
        var tab = CreateTabContent();
        BuildIOSContent(tab, settings.ios);
        return tab;
    }
#endif // end of legacy option tabs

    static void BuildAndroidContent(VisualElement content, EDMAndroidSettings android)
    {
        var autoResolution = AddToggle(content, "Resolve Automatically",
            () => android.enableAutoResolution, v => android.enableAutoResolution.value = v, tooltip:
            "Automatically resolve Android dependencies when changes are detected.");
        var autoResolveOnBuild = AddToggle(content, "Resolve Automatically On Build",
            () => android.autoResolveOnBuild, v => android.autoResolveOnBuild.value = v, tooltip:
            "Automatically resolve Android dependencies before each Android build.");

        var warningToggle = AddToggle(content, "Auto-Resolution Disabled Warning",
            () => android.autoResolutionDisabledWarning, v => android.autoResolutionDisabledWarning.value = v, tooltip:
            "Show a warning in the editor when auto-resolution is disabled. Only relevant when both auto-resolution options are off.");
        warningToggle.SetEnabled(!(android.enableAutoResolution || android.autoResolveOnBuild));
        autoResolution.RegisterValueChangedCallback(e =>
            warningToggle.SetEnabled(!(e.newValue || autoResolveOnBuild.value)));
        autoResolveOnBuild.RegisterValueChangedCallback(e =>
            warningToggle.SetEnabled(!(autoResolution.value || e.newValue)));

        var promptToggle = AddToggle(content, "Prompt Before Automatic Resolution",
            () => android.promptBeforeAutoResolution, v => android.promptBeforeAutoResolution.value = v, tooltip:
            "When enabled, shows a confirmation prompt each time auto-resolution is triggered in the editor. The prompt is not shown during automatic resolution on build.");
        promptToggle.SetEnabled(android.enableAutoResolution);
        autoResolution.RegisterValueChangedCallback(e =>
            promptToggle.SetEnabled(e.newValue));

        AddToggle(content, "Patch AndroidManifest.xml",
            () => android.patchAndroidManifest, v => android.patchAndroidManifest.value = v, tooltip:
            "Instances of \"applicationId\" variable references will be replaced in AndroidManifest.xml with the bundle ID. This works around a bug in Unity 2018.x where the \"applicationId\" variable is not replaced correctly.");
        var useJetifier = AddToggle(content, "Use Jetifier",
            () => android.useJetifier, v => android.useJetifier.value = v, tooltip:
            "Rewrites legacy Android support library references to use Jetpack via the Jetifier tool. " +
            "Allows an application to use Android Jetpack when other libraries in the project use the Android support libraries.");


        AddToggle(content, "Use Full Maven Repo Path When Building",
            () => android.useFullMavenWhenNotExport, v => android.useFullMavenWhenNotExport.value = v, tooltip:
            "If checked, the custom local Maven repo path will use the full absolute path when building through Unity. Best if the Unity project is always at the same path.");
        AddToggle(content, "Use Full Maven Repo Path When Exporting",
            () => android.useFullMavenWhenExport, v => android.useFullMavenWhenExport.value = v, tooltip:
            "If checked, the custom local Maven repo path will use the full absolute path when exporting the Android project. Best if the Unity project is always at the same path.");
        AddMavenRepoDirField(content, android);
        AddToggle(content, "Copy and Patch settingsTemplate.gradle",
            () => android.patchSettingsTemplateGradle, v => android.patchSettingsTemplateGradle.value = v, tooltip:
            "For Unity 2022.2 and above, any additional Maven repositories should be specified in settingsTemplate.gradle. If checked, EDM will also copy settingsTemplate.gradle from the Unity engine folder.");

        AddToggle(content, "Verbose Logging",
            () => android.verboseLogging, v => android.verboseLogging.value = v, tooltip:
            "Enable detailed logging output from the Android Resolver.");
    }

    static void BuildIOSContent(VisualElement content, EDMIOSSettings ios)
    {
        AddToggle(content, "Swift Package Manager Integration",
            () => ios.swiftPackageManagerEnabled, v => ios.swiftPackageManagerEnabled.value = v, tooltip:
            "Use Swift Package Manager to resolve dependencies that support it. If enabled, the resolver will prioritize SPM packages and fall back to Cocoapods for any dependencies that do not have an SPM equivalent specified.");
        AddToggle(content, "Allow Empty Podfile Generation",
            () => ios.allowEmptyPodfileGeneration, v => ios.allowEmptyPodfileGeneration.value = v, tooltip:
            "If enabled, a Podfile will be generated even if there are no Pods to install.");
        var podfileGeneration = AddToggle(content, "Podfile Generation",
            () => ios.podfileGenerationEnabled, v => ios.podfileGenerationEnabled.value = v, tooltip:
            "Podfile generation is required to install Cocoapods. It may be desirable to disable Podfile generation if frameworks are manually included in Unity's generated Xcode project.");
        AddDropdown(content, "Cocoapods Integration", CocoapodsIntegrationOptions,
            () => ios.cocoapodsIntegrationMethod.value, v => ios.cocoapodsIntegrationMethod.value = v);
        var podToolViaShell = AddToggle(content, "Use Shell to Execute Cocoapod Tool",
            () => ios.podToolExecutionViaShellEnabled, v => ios.podToolExecutionViaShellEnabled.value = v, tooltip:
            "Shell execution is useful when configuration in the shell environment (e.g ~/.profile) is required to execute Cocoapods tools.");

        var shellLangSection = new VisualElement();
        shellLangSection.style.display = ios.podToolExecutionViaShellEnabled ? DisplayStyle.Flex : DisplayStyle.None;
        AddToggle(shellLangSection, "Set LANG When Using Shell",
            () => ios.podToolShellExecutionSetLang, v => ios.podToolShellExecutionSetLang.value = v, Indent,
            "Useful for versions of cocoapods that depend on the value of LANG.");
        content.Add(shellLangSection);
        podToolViaShell.RegisterValueChangedCallback(e =>
            shellLangSection.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None);

        AddToggle(content, "Auto Install Cocoapod Tools in Editor",
            () => ios.autoPodToolInstallInEditor, v => ios.autoPodToolInstallInEditor.value = v, tooltip:
            "Automatically installs the Cocoapod tool if the editor isn't running in batch mode. If disabled, installation can be performed via Assets > External Dependency Manager > iOS Resolver > Install Cocoapods.");

        var podfileSection = new VisualElement();
        podfileSection.style.display = ios.podfileGenerationEnabled ? DisplayStyle.Flex : DisplayStyle.None;

        var useFrameworks = AddToggle(podfileSection, "Add use_frameworks! to Podfile",
            () => ios.podfileAddUseFrameworks, v => ios.podfileAddUseFrameworks.value = v, tooltip:
            "Add the use_frameworks! line to the Podfile. Required if any third-party Unity packages depend on Swift frameworks.");

        var frameworksSection = new VisualElement();
        frameworksSection.style.display = ios.podfileAddUseFrameworks ? DisplayStyle.Flex : DisplayStyle.None;
        AddToggle(frameworksSection, "Link Frameworks Statically",
            () => ios.podfileStaticLinkFrameworks, v => ios.podfileStaticLinkFrameworks.value = v, Indent,
            "Link frameworks statically is recommended just in case any pod framework includes static libraries.");
        var swiftWorkaround = AddToggle(frameworksSection, "Enable Swift Framework Support Workaround",
            () => ios.swiftFrameworkSupportWorkaroundEnabled, v => ios.swiftFrameworkSupportWorkaroundEnabled.value = v, Indent,
            "This workaround patches the Xcode project to properly link Swift Standard Library when some plugins depend on Swift Framework pods. Adds a dummy Swift file and enables CLANG_ENABLE_MODULES build settings.");

        var swiftVersionSection = new VisualElement();
        swiftVersionSection.style.display = ios.swiftFrameworkSupportWorkaroundEnabled ? DisplayStyle.Flex : DisplayStyle.None;
        AddTextField(swiftVersionSection, "Swift Language Version",
            () => ios.swiftLanguageVersion, v => ios.swiftLanguageVersion.value = v, Indent * 2,
            "Used to override 'SWIFT_VERSION' build setting in Xcode. Leave it blank to prevent override.");
        frameworksSection.Add(swiftVersionSection);
        swiftWorkaround.RegisterValueChangedCallback(e =>
            swiftVersionSection.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None);

        podfileSection.Add(frameworksSection);
        useFrameworks.RegisterValueChangedCallback(e =>
            frameworksSection.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None);

        var alwaysAddMainTarget = AddToggle(podfileSection, "Always Add Main Target to Podfile",
            () => ios.podfileAlwaysAddMainTarget, v => ios.podfileAlwaysAddMainTarget.value = v, tooltip:
            "Always add the main Unity target block to the Podfile.");

        var multipleTargetsSection = new VisualElement();
        multipleTargetsSection.style.display = ios.podfileAlwaysAddMainTarget ? DisplayStyle.Flex : DisplayStyle.None;
        AddToggle(multipleTargetsSection, "Allow Pods in Multiple Targets",
            () => ios.podfileAllowPodsInMultipleTargets, v => ios.podfileAllowPodsInMultipleTargets.value = v, Indent,
            "Allow adding the same pod to multiple targets, if specified in Dependencies.xml with the 'addToAllTargets' attribute.");
        podfileSection.Add(multipleTargetsSection);
        alwaysAddMainTarget.RegisterValueChangedCallback(e =>
            multipleTargetsSection.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None);

        content.Add(podfileSection);
        podfileGeneration.RegisterValueChangedCallback(e =>
            podfileSection.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None);

        AddToggle(content, "Verbose Logging",
            () => ios.verboseLogging, v => ios.verboseLogging.value = v, tooltip:
            "Enable detailed logging output from the iOS Resolver.");

        var pathToggle = AddToggle(content, "Use Custom CocoaPods Executable Path",
            () => ios.podToolPathEnabled, v => ios.podToolPathEnabled.value = v, tooltip:
            "When enabled, the path below is used directly as the CocoaPods executable, bypassing all automatic detection.");
        var pathField = AddTextField(content, "CocoaPods Executable Path",
            () => ios.podToolOverridePath, v => ios.podToolOverridePath.value = v, tooltip:
            "Absolute path to the CocoaPods executable, e.g. /opt/homebrew/bin/pod or /usr/local/bin/pod.");
        pathField.SetEnabled(ios.podToolPathEnabled);
        pathToggle.RegisterValueChangedCallback(e => pathField.SetEnabled(e.newValue));
    }

    static void BuildVersionHandlerSection(VisualElement parent, EDMResolverSettings settings)
    {
        var versionHandler = settings.versionHandler;

        var header = new Label("Version Handler");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 13;
        header.style.marginTop = 10;
        header.style.marginBottom = 4;
        parent.Add(header);

        AddToggle(parent, "Enable Version Management",
            () => versionHandler.enabled, val => versionHandler.enabled.value = val);
        AddToggle(parent, "Prompt for Obsolete File Deletion",
            () => versionHandler.cleanUpPromptEnabled, val => versionHandler.cleanUpPromptEnabled.value = val);
        AddToggle(parent, "Rename to Canonical Filenames",
            () => versionHandler.renameToCanonicalFilenames, val => versionHandler.renameToCanonicalFilenames.value = val, tooltip:
            "When enabled the Version Handler strips metadata from filenames. This can be a very slow operation as each renamed DLL causes the Unity editor to reload all DLLs.");
        AddToggle(parent, "Allow Disabling Files Via Renaming",
            () => versionHandler.renameToDisableFilesEnabled, val => versionHandler.renameToDisableFilesEnabled.value = val);
        AddToggle(parent, "Verbose Logging",
            () => versionHandler.verboseLogging, val => versionHandler.verboseLogging.value = val, tooltip:
            "Enable detailed logging output from the Version Handler.");
    }

    static Toggle AddToggle(VisualElement parent, string label, Func<bool> get, Action<bool> set, float indent = 0, string tooltip = null)
    {
        var toggle = new Toggle(label);
        if (indent > 0)
        {
            toggle.labelElement.style.marginLeft = indent;
            toggle.labelElement.style.width = LabelWidth - indent;
            toggle.labelElement.style.minWidth = LabelWidth - indent;
        }
        if (tooltip != null)
            toggle.tooltip = tooltip;
        toggle.value = get();
        toggle.RegisterValueChangedCallback(e =>
        {
            set(e.newValue);
            EDMResolverSettings.instance.Save();
        });
        parent.Add(toggle);
        return toggle;
    }

    static VisualElement AddTextField(VisualElement parent, string label, Func<string> get, Action<string> set,
        float indent = 0, string tooltip = null, Func<string, string> validate = null)
    {
        var row = new VisualElement();
        row.AddToClassList("edm-row");
        row.style.marginLeft = 2;
        row.style.paddingBottom = 2;

        var fieldLabel = new Label(label);
        fieldLabel.style.marginLeft = indent + 1;
        fieldLabel.style.width = LabelWidth - indent - 1;
        fieldLabel.style.minWidth = LabelWidth - indent - 1;
        if (tooltip != null)
            fieldLabel.tooltip = tooltip;
        row.Add(fieldLabel);

        var field = new TextField();
        field.labelElement.style.display = DisplayStyle.None;
        field.value = get();
        if (tooltip != null)
            field.tooltip = tooltip;
        field.RegisterValueChangedCallback(e =>
        {
            set(e.newValue);
            EDMResolverSettings.instance.Save();
        });
        row.Add(field);
        parent.Add(row);

        if (validate != null)
        {
            var warning = new HelpBox("", HelpBoxMessageType.Warning);
            warning.style.display = DisplayStyle.None;
            warning.style.marginLeft = indent + 2;
            parent.Add(warning);

            Action<string> refresh = v =>
            {
                var error = validate(v);
                warning.text = error ?? "";
                warning.style.display = error != null ? DisplayStyle.Flex : DisplayStyle.None;
            };
            refresh(get());
            field.RegisterValueChangedCallback(e => refresh(e.newValue));
        }
        return row;
    }

    static void AddMavenRepoDirField(VisualElement parent, EDMAndroidSettings android)
    {
        var row = new VisualElement();
        row.AddToClassList("edm-row");
        row.style.marginLeft = 2;
        row.style.paddingBottom = 2;

        var fieldLabel = new Label("Local Maven Repo Directory");
        fieldLabel.style.marginLeft = 1;
        fieldLabel.style.width = LabelWidth - 1;
        fieldLabel.style.minWidth = LabelWidth - 1;
        fieldLabel.tooltip = "Path to the local Maven repository directory. Must be under the Assets folder.";
        row.Add(fieldLabel);

        var field = new TextField();
        field.labelElement.style.display = DisplayStyle.None;
        field.value = android.localMavenRepoDir;
        field.style.flexGrow = 1;
        field.tooltip = fieldLabel.tooltip;
        row.Add(field);

        var browseButton = new Button(() =>
        {
            var pathValid = false;
            var path = string.Empty;
            while (!pathValid)
            {
                path = EditorUtility.OpenFolderPanel("Set Local Maven Repo Directory",
                    android.localMavenRepoDir, android.localMavenRepoDir);
                if (string.IsNullOrEmpty(path)) return;
                path = ToRelativeProjectPath(path);
                pathValid = ValidateMavenRepoDir(path, out var error);
                if (!pathValid)
                    EditorUtility.DisplayDialog("Invalid Folder", error, "OK");
            }

            field.value = path;
            android.localMavenRepoDir.value = path;
            EDMResolverSettings.instance.Save();
        });
        browseButton.text = "Browse";
        row.Add(browseButton);

        parent.Add(row);

        field.RegisterValueChangedCallback(e =>
        {
            android.localMavenRepoDir.value = e.newValue;
            EDMResolverSettings.instance.Save();
        });

        var warning = new HelpBox("", HelpBoxMessageType.Warning);
        warning.style.display = DisplayStyle.None;
        warning.style.marginLeft = 2;
        warning.style.marginBottom = 2;
        parent.Add(warning);

        Action<string> refresh = v =>
        {
            var pathValid = ValidateMavenRepoDir(v, out var error);
            warning.text = error;
            warning.style.display = !pathValid ? DisplayStyle.Flex : DisplayStyle.None;
        };
        refresh(android.localMavenRepoDir);
        field.RegisterValueChangedCallback(e => refresh(e.newValue));
    }

    static string ToRelativeProjectPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;
        path = path.Replace('\\', '/').TrimEnd('/');
        var projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
        if (path.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            path = path.Substring(projectRoot.Length);

        return path;
    }

    static bool ValidateMavenRepoDir(string path, out string error)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            error = "The selected folder must be under 'Assets' folder.";
            return false;
        }

        if (path.StartsWith("Assets/Plugins/Android/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("Assets/Plugins/Android", StringComparison.OrdinalIgnoreCase))
        {
            error = "The selected folder cannot be under 'Assets/Plugins/Android' folder.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    static void AddDropdown(VisualElement parent, string label, List<string> options,
        Func<int> get, Action<int> set, float indent = 0, string tooltip = null)
    {
        var dropdown = new DropdownField(label, options, get());
        dropdown.style.paddingBottom = 2;
        if (indent > 0)
        {
            dropdown.labelElement.style.marginLeft = indent;
            dropdown.labelElement.style.width = LabelWidth - indent;
            dropdown.labelElement.style.minWidth = LabelWidth - indent;
        }
        if (tooltip != null)
            dropdown.tooltip = tooltip;
        dropdown.RegisterValueChangedCallback(e =>
        {
            set(options.IndexOf(e.newValue));
            EDMResolverSettings.instance.Save();
        });
        parent.Add(dropdown);
    }
}
