using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if UNITY_2023_2_OR_NEWER
using UnityEngine.Analytics;
#endif
#if UNITY_6000_7_OR_NEWER
using Unity.Scripting.LifecycleManagement;
#endif

#if UNITY_6000_7_OR_NEWER
[NoAutoStaticsCleanup]
#endif
internal static class EDMAnalytics
{
    private const string VendorKey = "unity.external-dependency-manager";

    // Events
    private const string initEvent = "edmInit";
    private const string showSwitchPopupEvent = "edmShowSwitchPopup";
    private const string switchDependencyManagerEvent = "edmSwitchDependencyManager";
    private const string buildTriggeredEvent = "edmBuildTriggered";
    private const string apiUsageEvent = "edmApiUsage";
    private const string cocoapodsSetupEvent = "edmCocoapodsSetup";

    // Tracks which function names have already had an edmApiUsage event sent this domain load.
    private static readonly HashSet<string> sentApiUsage = new HashSet<string>();

    // Two separate payload structs for edmBuildTriggered — one per platform.
    // This ensures iOS fields are absent (null in DB) on Android events and vice versa.
    // IAnalytic.IData is only available on Unity 2023.2+, so it is applied conditionally.
    [Serializable]
    struct EdmAndroidBuildData
#if UNITY_2023_2_OR_NEWER
        : IAnalytic.IData
#endif
    {
        public int edm_state;
        public string build_guid;
        public string build_target;
        public bool auto_resolve;
        public bool auto_resolve_on_build;
        public bool auto_resolution_disabled_warning;
        public bool auto_resolution_prompt;
        public bool patch_android_manifest;
        public bool use_jetifier;
        public bool use_maven_repo_when_building;
        public bool use_maven_repo_when_exporting;
        public bool has_local_maven_repo;
        public bool patch_gradle_settings_template;
    }

    [Serializable]
    struct EdmIosBuildData
#if UNITY_2023_2_OR_NEWER
        : IAnalytic.IData
#endif
    {
        public int edm_state;
        public string build_guid;
        public string build_target;
        public bool spm_enabled;
        public bool allow_empty_podfile;
        public bool podfile_generation_enabled;
        public int cocoapods_integration_method;
        public bool use_shell_cocoapod;
        public bool set_lang_when_using_shell;
        public bool auto_pod_install;
        public bool use_frameworks;
        public bool static_link_frameworks;
        public bool swift_workaround;
        public string swift_workaround_version;
        public bool always_add_main_target;
        public bool allow_multiple_targets;
        public bool using_custom_cocoapods_path;
    }

    [Serializable]
    struct EdmCocoapodsSetupData
#if UNITY_2023_2_OR_NEWER
        : IAnalytic.IData
#endif
    {
        public bool using_custom_cocoapods_path;
        public bool pod_found;
        public int setup_status;
    }

    // Enumerators
    internal enum OtherResolverStatus
    {
        OnlyEdmPresent = 0,
        OtherResolversPresent = 1
    }

    internal enum SwitchType
    {
        MenuItemEdmToEdm4u = 0,
        MenuItemEdm4uToEdm = 1,
        PopupEdmToEdm4u = 2,
        PopupEdm4uToEdm = 3
    }

    internal enum EdmState
    {
        EdmAloneAndActive = 0,
        EdmAndEdm4uPresentEdmActive = 1,
        EdmAndEdm4uPresentEdm4uActive = 2
    }

#if UNITY_2023_2_OR_NEWER
    // Analytics classes
    [AnalyticInfo(eventName: initEvent, vendorKey: VendorKey, maxEventsPerHour: 1000, maxNumberOfElements: 1000)]
    class EdmInitAnalytic : IAnalytic
    {
        readonly EdmInitData _data;

        public EdmInitAnalytic(OtherResolverStatus status, EdmState edmState) =>
            _data = new EdmInitData { other_resolver_status = (int)status, edm_state = (int)edmState };

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            data = _data;
            error = null;
            return true;
        }

        [Serializable]
        struct EdmInitData : IAnalytic.IData { public int other_resolver_status; public int edm_state; }
    }

    [AnalyticInfo(eventName: showSwitchPopupEvent, vendorKey: VendorKey, maxEventsPerHour: 1000, maxNumberOfElements: 1000)]
    class EdmShowSwitchPopupAnalytic : IAnalytic
    {
        readonly EdmShowSwitchPopupData _data;

        public EdmShowSwitchPopupAnalytic(bool popupType) =>
            _data = new EdmShowSwitchPopupData { switch_popup_type = popupType };

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            data = _data;
            error = null;
            return true;
        }

        [Serializable]
        struct EdmShowSwitchPopupData : IAnalytic.IData { public bool switch_popup_type; }
    }

    [AnalyticInfo(eventName: switchDependencyManagerEvent, vendorKey: VendorKey, maxEventsPerHour: 1000, maxNumberOfElements: 1000)]
    class EdmSwitchDependencyManagerAnalytic : IAnalytic
    {
        readonly EdmSwitchDependencyManagerData _data;

        public EdmSwitchDependencyManagerAnalytic(SwitchType switchType) =>
            _data = new EdmSwitchDependencyManagerData { switch_type = (int)switchType };

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            data = _data;
            error = null;
            return true;
        }

        [Serializable]
        struct EdmSwitchDependencyManagerData : IAnalytic.IData { public int switch_type; }
    }

    [AnalyticInfo(eventName: buildTriggeredEvent, vendorKey: VendorKey, maxEventsPerHour: 1000, maxNumberOfElements: 1000)]
    class EdmAndroidBuildAnalytic : IAnalytic
    {
        readonly EdmAndroidBuildData _data;
        public EdmAndroidBuildAnalytic(EdmAndroidBuildData data) => _data = data;
        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            data = _data;
            error = null;
            return true;
        }
    }

    [AnalyticInfo(eventName: buildTriggeredEvent, vendorKey: VendorKey, maxEventsPerHour: 1000, maxNumberOfElements: 1000)]
    class EdmIosBuildAnalytic : IAnalytic
    {
        readonly EdmIosBuildData _data;
        public EdmIosBuildAnalytic(EdmIosBuildData data) => _data = data;
        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            data = _data;
            error = null;
            return true;
        }
    }

    [AnalyticInfo(eventName: apiUsageEvent, vendorKey: VendorKey, maxEventsPerHour: 1000, maxNumberOfElements: 1000)]
    class EdmApiUsageAnalytic : IAnalytic
    {
        readonly EdmApiUsageData _data;

        public EdmApiUsageAnalytic(string functionName) =>
            _data = new EdmApiUsageData { function_name = functionName };

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            data = _data;
            error = null;
            return true;
        }

        [Serializable]
        struct EdmApiUsageData : IAnalytic.IData { public string function_name; }
    }

    [AnalyticInfo(eventName: cocoapodsSetupEvent, vendorKey: VendorKey, maxEventsPerHour: 1000, maxNumberOfElements: 1000)]
    class EdmCocoapodsSetupAnalytic : IAnalytic
    {
        readonly EdmCocoapodsSetupData _data;

        public EdmCocoapodsSetupAnalytic(EdmCocoapodsSetupData data) => _data = data;

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            data = _data;
            error = null;
            return true;
        }
    }
#else
    // Analytics data structs (legacy API — Unity < 2023.2)
    // EdmAndroidBuildData, EdmIosBuildData and EdmCocoapodsSetupData are defined at class level above.
    [Serializable]
    struct EdmInitData { public int other_resolver_status; public int edm_state; }
    [Serializable]
    struct EdmShowSwitchPopupData { public bool switch_popup_type; }
    [Serializable]
    struct EdmSwitchDependencyManagerData { public int switch_type; }
    [Serializable]
    struct EdmApiUsageData { public string function_name; }
#endif

    static EdmAndroidBuildData BuildAndroidData(EdmState state, string buildGuid)
    {
        var a = EDMResolverSettings.instance.android;
        return new EdmAndroidBuildData
        {
            edm_state                     = (int)state,
            build_guid                    = buildGuid,
            build_target                  = "Android",
            auto_resolve                  = a.enableAutoResolution.value,
            auto_resolve_on_build         = a.autoResolveOnBuild.value,
            auto_resolution_disabled_warning = a.autoResolutionDisabledWarning.value,
            auto_resolution_prompt        = a.promptBeforeAutoResolution.value,
            patch_android_manifest        = a.patchAndroidManifest.value,
            use_jetifier                  = a.useJetifier.value,
            use_maven_repo_when_building  = a.useFullMavenWhenNotExport.value,
            use_maven_repo_when_exporting = a.useFullMavenWhenExport.value,
            has_local_maven_repo          = !string.IsNullOrEmpty(a.localMavenRepoDir.value),
            patch_gradle_settings_template = a.patchSettingsTemplateGradle.value,
        };
    }

    static EdmIosBuildData BuildIosData(EdmState state, string buildGuid)
    {
        var i = EDMResolverSettings.instance.ios;
        return new EdmIosBuildData
        {
            edm_state                     = (int)state,
            build_guid                    = buildGuid,
            build_target                  = "iOS",
            spm_enabled                   = i.swiftPackageManagerEnabled.value,
            allow_empty_podfile           = i.allowEmptyPodfileGeneration.value,
            podfile_generation_enabled    = i.podfileGenerationEnabled.value,
            cocoapods_integration_method  = (int)(CocoapodsIntegrationMethod)i.cocoapodsIntegrationMethod,
            use_shell_cocoapod            = i.podToolExecutionViaShellEnabled.value,
            set_lang_when_using_shell     = i.podToolShellExecutionSetLang.value,
            auto_pod_install              = i.autoPodToolInstallInEditor.value,
            use_frameworks                = i.podfileAddUseFrameworks.value,
            static_link_frameworks        = i.podfileStaticLinkFrameworks.value,
            swift_workaround              = i.swiftFrameworkSupportWorkaroundEnabled.value,
            swift_workaround_version      = i.swiftLanguageVersion.value,
            always_add_main_target        = i.podfileAlwaysAddMainTarget.value,
            allow_multiple_targets        = i.podfileAllowPodsInMultipleTargets.value,
            using_custom_cocoapods_path   = i.podToolPathEnabled.value,
        };
    }

    // Helper functions
    internal static EdmState GetEdmState()
    {
        if (!EDMInstancesHandling.Edm4UExists() && !EDMInstancesHandling.WasEdm4UInstalled())
            return EdmState.EdmAloneAndActive;

        return EDMInstancesHandling.IsEdmActive()
            ? EdmState.EdmAndEdm4uPresentEdmActive
            : EdmState.EdmAndEdm4uPresentEdm4uActive;
    }

    // Analytics event sending
#if UNITY_2023_2_OR_NEWER
    internal static void SendInitEvent(OtherResolverStatus status, EdmState edmState)
    {
        if (!EditorAnalytics.enabled)
            return;
        EditorAnalytics.SendAnalytic(new EdmInitAnalytic(status, edmState));
    }

    internal static void SendShowSwitchPopupEvent(bool switchPopupType)
    {
        if (!EditorAnalytics.enabled)
            return;
        EditorAnalytics.SendAnalytic(new EdmShowSwitchPopupAnalytic(switchPopupType));
    }

    internal static void SendSwitchDependencyManagerEvent(SwitchType switchType)
    {
        if (!EditorAnalytics.enabled)
            return;
        EditorAnalytics.SendAnalytic(new EdmSwitchDependencyManagerAnalytic(switchType));
    }

    internal static void SendBuildTriggeredEvent(EdmState state, string buildGuid, BuildTarget platform)
    {
        if (!EditorAnalytics.enabled)
            return;
        if (platform == BuildTarget.Android)
            EditorAnalytics.SendAnalytic(new EdmAndroidBuildAnalytic(BuildAndroidData(state, buildGuid)));
        else
            EditorAnalytics.SendAnalytic(new EdmIosBuildAnalytic(BuildIosData(state, buildGuid)));
    }

    internal static void SendApiUsageEvent(string functionName)
    {
        if (!EditorAnalytics.enabled || !sentApiUsage.Add(functionName))
            return;
        EditorAnalytics.SendAnalytic(new EdmApiUsageAnalytic(functionName));
    }

    internal static void SendCocoapodsSetupEvent(bool usingCustomPath, bool podFound, int setupStatus)
    {
        if (!EditorAnalytics.enabled)
            return;
        EditorAnalytics.SendAnalytic(new EdmCocoapodsSetupAnalytic(new EdmCocoapodsSetupData
        {
            using_custom_cocoapods_path = usingCustomPath,
            pod_found                   = podFound,
            setup_status                = setupStatus
        }));
    }
#else //Analytics events sending (legacy API — Unity < 2023.2)
    internal static void SendInitEvent(OtherResolverStatus status, EdmState edmState)
    {
        if (!EditorAnalytics.enabled)
            return;
        EditorAnalytics.RegisterEventWithLimit(initEvent, 1000, 1000, VendorKey);
        EditorAnalytics.SendEventWithLimit(initEvent, new EdmInitData { other_resolver_status = (int)status, edm_state = (int)edmState });
    }

    internal static void SendShowSwitchPopupEvent(bool switchPopupType)
    {
        if (!EditorAnalytics.enabled)
            return;
        EditorAnalytics.RegisterEventWithLimit(showSwitchPopupEvent, 1000, 1000, VendorKey);
        EditorAnalytics.SendEventWithLimit(showSwitchPopupEvent, new EdmShowSwitchPopupData { switch_popup_type = switchPopupType });
    }

    internal static void SendSwitchDependencyManagerEvent(SwitchType switchType)
    {
        if (!EditorAnalytics.enabled)
            return;
        EditorAnalytics.RegisterEventWithLimit(switchDependencyManagerEvent, 1000, 1000, VendorKey);
        EditorAnalytics.SendEventWithLimit(switchDependencyManagerEvent, new EdmSwitchDependencyManagerData { switch_type = (int)switchType });
    }

    internal static void SendBuildTriggeredEvent(EdmState state, string buildGuid, BuildTarget platform)
    {
        if (!EditorAnalytics.enabled)
            return;
        EditorAnalytics.RegisterEventWithLimit(buildTriggeredEvent, 1000, 1000, VendorKey);
        if (platform == BuildTarget.Android)
            EditorAnalytics.SendEventWithLimit(buildTriggeredEvent, BuildAndroidData(state, buildGuid));
        else
            EditorAnalytics.SendEventWithLimit(buildTriggeredEvent, BuildIosData(state, buildGuid));
    }

    internal static void SendApiUsageEvent(string functionName)
    {
        if (!EditorAnalytics.enabled || !sentApiUsage.Add(functionName))
            return;
        EditorAnalytics.RegisterEventWithLimit(apiUsageEvent, 1000, 1000, VendorKey);
        EditorAnalytics.SendEventWithLimit(apiUsageEvent, new EdmApiUsageData { function_name = functionName });
    }

    internal static void SendCocoapodsSetupEvent(bool usingCustomPath, bool podFound, int setupStatus)
    {
        if (!EditorAnalytics.enabled)
            return;
        EditorAnalytics.RegisterEventWithLimit(cocoapodsSetupEvent, 1000, 1000, VendorKey);
        EditorAnalytics.SendEventWithLimit(cocoapodsSetupEvent, new EdmCocoapodsSetupData
        {
            using_custom_cocoapods_path = usingCustomPath,
            pod_found                   = podFound,
            setup_status                = setupStatus
        });
    }
#endif
}
