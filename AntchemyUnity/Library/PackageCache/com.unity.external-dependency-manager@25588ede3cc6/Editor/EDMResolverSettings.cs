using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
#if UNITY_6000_7_OR_NEWER
using Unity.Scripting.LifecycleManagement;
#endif

[Serializable]
#if UNITY_6000_7_OR_NEWER
[NoAutoStaticsCleanup]
#endif
internal abstract class EDMSetting : IDisposable
{
    internal static event Action OnRead;
    internal static event Action OnWrite;

    internal static readonly Google.ProjectSettings Edm4uProjectSettings = new Google.ProjectSettings("");

    protected EDMSetting()
    {
        AssemblyReloadEvents.beforeAssemblyReload += Dispose;
        OnRead += Read;
        OnWrite += Write;
    }

    public abstract void Read();
    public abstract void Write();

    // Events can only be invoked from inside the class that declares them
    internal static void InvokeRead()  => OnRead?.Invoke();
    internal static void InvokeWrite() => OnWrite?.Invoke();

    public void Dispose()
    {
        AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
        OnRead -= Read;
        OnWrite -= Write;
    }
}

[Serializable]
internal class EDMSettingsBase : IDisposable
{
    public void Dispose()
    {
        foreach (var field in GetType().GetFields())
        {
            if (field.GetValue(this) is EDMSetting setting)
                setting.Dispose();
        }
    }
}

[Serializable]
internal class BoolEDMSetting : EDMSetting
{
    [NonSerialized] public string key;
    [NonSerialized] public bool defaultValue;
    public bool value;

    public BoolEDMSetting(string key, bool defaultValue = false)
    {
        this.key = key;
        this.defaultValue = defaultValue;
        value = defaultValue;
    }

    public override void Read()
    {
        if (!string.IsNullOrEmpty(key))
            value = Edm4uProjectSettings.GetBool(key, value);
    }

    public override void Write()
    {
        if (string.IsNullOrEmpty(key)) return;
        if (value != defaultValue || Edm4uProjectSettings.HasKey(key))
            Edm4uProjectSettings.SetBool(key, value);
    }

    public static implicit operator bool(BoolEDMSetting s) => s.value;
}

[Serializable]
internal class StringEDMSetting : EDMSetting
{
    [NonSerialized] public string key;
    [NonSerialized] public string defaultValue;
    public string value;

    public StringEDMSetting(string key, string defaultValue = "")
    {
        this.key = key;
        this.defaultValue = defaultValue;
        value = defaultValue;
    }

    public override void Read()
    {
        if (!string.IsNullOrEmpty(key))
            value = Edm4uProjectSettings.GetString(key, value);
    }

    public override void Write()
    {
        if (string.IsNullOrEmpty(key)) return;
        if (value != defaultValue || Edm4uProjectSettings.HasKey(key))
            Edm4uProjectSettings.SetString(key, value);
    }

    public static implicit operator string(StringEDMSetting s) => s.value;
}

[Serializable]
internal class EnumEDMSetting<T> : EDMSetting where T : struct, Enum
{
    [NonSerialized] public string key;
    [NonSerialized] public int defaultValue;
    public int value;

    public EnumEDMSetting(string key, T defaultValue)
    {
        this.key = key;
        this.defaultValue = Convert.ToInt32(defaultValue);
        value = this.defaultValue;
    }

    public override void Read()
    {
        if (string.IsNullOrEmpty(key)) return;
        int raw = Edm4uProjectSettings.GetInt(key, value);
        if (Enum.TryParse(raw.ToString(), out T parsed) && Enum.IsDefined(typeof(T), parsed))
            value = raw;
    }

    public override void Write()
    {
        if (string.IsNullOrEmpty(key)) return;
        if (value != defaultValue || Edm4uProjectSettings.HasKey(key))
            Edm4uProjectSettings.SetInt(key, value);
    }

    public static implicit operator T(EnumEDMSetting<T> s) => (T)(object)s.value;
}

[FilePath("ProjectSettings/ExternalDependencyManagerResolverSettings.asset",
    FilePathAttribute.Location.ProjectFolder)]
internal class EDMResolverSettings : ScriptableSingleton<EDMResolverSettings>
{
    public EDMAndroidSettings android = new EDMAndroidSettings();
    public EDMIOSSettings ios = new EDMIOSSettings();
    public EDMVersionHandlerSettings versionHandler = new EDMVersionHandlerSettings();

    internal void SaveWithoutBridging()
    {
        Save(true);
    }

    internal void Save()
    {
        Save(true);
        EDMInstancesHandling.BridgeSettingsToLegacySettings();
    }
}

[Serializable]
internal class EDMAndroidSettings : EDMSettingsBase
{
    internal const string Ns = "GooglePlayServices.";

    public BoolEDMSetting verboseLogging                = new BoolEDMSetting(Ns + "VerboseLogging", false);
    public BoolEDMSetting enableAutoResolution          = new BoolEDMSetting(Ns + "AutoResolverEnabled", true);
    public BoolEDMSetting autoResolveOnBuild            = new BoolEDMSetting(Ns + "AutoResolveOnBuild", true);
    public BoolEDMSetting autoResolutionDisabledWarning = new BoolEDMSetting(Ns + "AutoResolutionDisabledWarning", true);
    public BoolEDMSetting promptBeforeAutoResolution    = new BoolEDMSetting(Ns + "PromptBeforeAutoResolution", true);
    public BoolEDMSetting patchAndroidManifest          = new BoolEDMSetting(Ns + "PatchAndroidManifest", true);
    public BoolEDMSetting useJetifier                   = new BoolEDMSetting(Ns + "UseJetifier", true);
    public BoolEDMSetting useFullMavenWhenNotExport     = new BoolEDMSetting(Ns + "UseFullCustomMavenRepoPathWhenNotExport", false);
    public BoolEDMSetting useFullMavenWhenExport        = new BoolEDMSetting(Ns + "UseFullCustomMavenRepoPathWhenExport", true);
    public StringEDMSetting localMavenRepoDir           = new StringEDMSetting(Ns + "LocalMavenRepoDir", "Assets/GeneratedLocalRepo");
    public BoolEDMSetting patchSettingsTemplateGradle   = new BoolEDMSetting(Ns + "PatchSettingsTemplateGradle", true);
}

[Serializable] 
internal class EDMVersionHandlerSettings : EDMSettingsBase
{
    internal const string Ns = "Google.VersionHandler.";

    public BoolEDMSetting verboseLogging              = new BoolEDMSetting(Ns + "VerboseLoggingEnabled", false);
    public BoolEDMSetting enabled                     = new BoolEDMSetting(Ns + "VersionHandlingEnabled", true);
    public BoolEDMSetting cleanUpPromptEnabled        = new BoolEDMSetting(Ns + "CleanUpPromptEnabled", true);
    public BoolEDMSetting renameToCanonicalFilenames  = new BoolEDMSetting(Ns + "RenameToCanonicalFilenames", false);
    public BoolEDMSetting renameToDisableFilesEnabled = new BoolEDMSetting(Ns + "RenameToDisableFilesEnabled", true);
}

internal enum CocoapodsIntegrationMethod { None = 0, Project = 1, Workspace = 2 }

[Serializable]
internal class EDMIOSSettings : EDMSettingsBase
{
    internal const string Ns = "Google.IOSResolver.";

    public BoolEDMSetting verboseLogging                         = new BoolEDMSetting(Ns + "VerboseLoggingEnabled", false);
    public BoolEDMSetting swiftPackageManagerEnabled             = new BoolEDMSetting(Ns + "SwiftPackageManagerEnabled", true);
    public BoolEDMSetting allowEmptyPodfileGeneration            = new BoolEDMSetting(Ns + "AllowEmptyPodfileGeneration", true);
    public BoolEDMSetting podfileGenerationEnabled               = new BoolEDMSetting(Ns + "PodfileEnabled", true);
    public EnumEDMSetting<CocoapodsIntegrationMethod> cocoapodsIntegrationMethod = new EnumEDMSetting<CocoapodsIntegrationMethod>(Ns + "CocoapodsIntegrationMethod", CocoapodsIntegrationMethod.Workspace);
    public BoolEDMSetting podToolExecutionViaShellEnabled        = new BoolEDMSetting(Ns + "PodToolExecutionViaShellEnabled", true);
    public BoolEDMSetting podToolShellExecutionSetLang           = new BoolEDMSetting(Ns + "PodToolShellExecutionSetLang", true);
    public BoolEDMSetting autoPodToolInstallInEditor             = new BoolEDMSetting(Ns + "AutoPodToolInstallInEditor", true);
    public BoolEDMSetting podfileAddUseFrameworks                = new BoolEDMSetting(Ns + "PodfileAddUseFrameworks", true);
    public BoolEDMSetting podfileStaticLinkFrameworks            = new BoolEDMSetting(Ns + "PodfileStaticLinkFrameworks", true);
    public BoolEDMSetting podfileAlwaysAddMainTarget             = new BoolEDMSetting(Ns + "PodfileAlwaysAddMainTarget", true);
    public BoolEDMSetting podfileAllowPodsInMultipleTargets      = new BoolEDMSetting(Ns + "PodfileAllowPodsInMultipleTargets", true);
    public BoolEDMSetting swiftFrameworkSupportWorkaroundEnabled = new BoolEDMSetting(Ns + "SwiftFrameworkSupportWorkaroundEnabled", true);
    public StringEDMSetting swiftLanguageVersion                 = new StringEDMSetting(Ns + "SwiftLanguageVersion", "5.0");
    public BoolEDMSetting podToolPathEnabled                      = new BoolEDMSetting(Ns + "PodToolPathEnabled", false);
    public StringEDMSetting podToolOverridePath                  = new StringEDMSetting(Ns + "PodToolOverridePath", "");
}
