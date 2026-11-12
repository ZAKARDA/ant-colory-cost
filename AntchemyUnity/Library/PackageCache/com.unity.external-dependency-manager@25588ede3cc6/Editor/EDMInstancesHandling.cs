using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using UnityEditor.Build;
using UnityEngine;
using Directory = System.IO.Directory;
#if UNITY_6000_7_OR_NEWER
using Unity.Scripting.LifecycleManagement;
#endif

[assembly: InternalsVisibleTo("EdmTests")]

#if UNITY_6000_7_OR_NEWER
[NoAutoStaticsCleanup]
#endif
internal class EDMInstancesHandling
{
    #region External Dependency Manager Project Settings

#if UNITY_6000_7_OR_NEWER
    [NoAutoStaticsCleanup]
#endif
    internal static class ExternalDependencyManagerSettingsManager
    {
        private static string SettingsPath => Path.Combine("ProjectSettings", "ExternalDependencyManagerSettings.json");

        private static ExternalDependencyManagerSettings _instance;

        public static ExternalDependencyManagerSettings Instance
        {
            get
            {
                if (_instance == null)
                    Load();
                return _instance;
            }
        }

        static void Load()
        {
            if (File.Exists(SettingsPath))
            {
                _instance = JsonUtility.FromJson<ExternalDependencyManagerSettings>(
                    File.ReadAllText(SettingsPath));
            }
            else
            {
                _instance = new ExternalDependencyManagerSettings();
            }
        }

        public static void Save()
        {
            var json = JsonUtility.ToJson(Instance, true);
            File.WriteAllText(SettingsPath, json);
        }
    }

    [Serializable]
    internal class ExternalDependencyManagerSettings
    {
        public bool edmEnabled = false;
        public bool selectionPresented = false;
        public bool edmWasInstalled = false;

        public bool EdmEnabled
        {
            get => edmEnabled;
            set
            {
                if (edmEnabled == value)
                    return;
                edmEnabled = value;
                Save();
            }
        }

        public bool SelectionPresented
        {
            get => selectionPresented;
            set
            {
                if (selectionPresented == value)
                    return;
                selectionPresented = value;
                Save();
            }
        }

        public bool EdmWasInstalled
        {
            get => edmWasInstalled;
            set
            {
                if (edmWasInstalled == value)
                    return;
                edmWasInstalled = value;
                Save();
            }
        }

        public void Save()
        {
            ExternalDependencyManagerSettingsManager.Save();
        }
    }

    internal static ExternalDependencyManagerSettings Settings => ExternalDependencyManagerSettingsManager.Instance;

    #endregion

    private class EdmInstance
    {
        public string VersionHandler;
        public readonly List<string> Assemblies = new ();
    }

    internal const string UedmPackageName = "com.unity.external-dependency-manager";
    internal const string Edm4UPackageName = "com.google.external-dependency-manager";
    internal const string UedmDisabledString = "UNITY_EDM_DISABLED";
    internal const string DummyPackageMarker = ".edm_dummy";
    internal static string Edm4UEmbeddedDir => Path.Combine(Application.dataPath, "../Packages", Edm4UPackageName);

    internal static PackageInfo GetPackageByName(string name)
    {
        var allPackages = PackageInfo.GetAllRegisteredPackages();
        foreach (var package in allPackages)
        {
            if (package.name == name)
                return package;
        }

        return null;
    }

    /// <summary>
    /// Checks if EDM4U was previously (or is) installed in the project, either by checking for the define constraint on assemblies or by looking for the dummy package that replaces it when disabled.
    /// </summary>
    /// <returns>True if EDM4U was or is installed, false otherwise.</returns>
    internal static bool WasEdm4UInstalled()
    {
        var assemblies = GetEdmAssemblies();
        foreach (var dllPath in assemblies)
        {
            var importer = AssetImporter.GetAtPath(dllPath) as PluginImporter;
            if (importer != null)
            {
                if (importer.GetCompatibleWithEditor())
                    return true;
                var defineConstraints = new List<string>(importer.DefineConstraints);
                if (defineConstraints.Contains(UedmDisabledString))
                    return true;
            }
        }

        // If there is edm4u package, it doesn't matter if it's a dummy or a real one -
        // either means that edm4u was installed at some point
        return PackageInfo.IsPackageRegistered(Edm4UPackageName);
    }

    /// <summary>
    /// Retrieves the paths of all EDM assemblies in the project.
    /// </summary>
    /// <returns>A list of paths to EDM assemblies.</returns>
    internal static List<string> GetEdmAssemblies(string inFolders = null)
    {
        var edm4uAssembliesArray = new[]
        {
            "Google.VersionHandler",
            "Google.VersionHandlerImpl",
            "Google.IOSResolver",
            "Google.JarResolver",
            "Google.PackageManagerResolver",
        };

        var assemblies = new List<string>();
        // Ensure asset database is up-to-date
        AssetDatabase.Refresh();
        var uedmPackage = GetPackageByName(UedmPackageName);
        foreach (var dllName in edm4uAssembliesArray)
        {
            var guids = inFolders != null ? AssetDatabase.FindAssets(dllName, new[] { inFolders }) : AssetDatabase.FindAssets(dllName);
            foreach (var guid in guids)
            {
                var dllPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!dllPath.EndsWith(".dll"))
                    continue;
                // Filter out UEDM Libraries
                if (uedmPackage != null && Path.GetFullPath(dllPath).StartsWith(Path.GetFullPath(uedmPackage.resolvedPath)))
                    continue;
                assemblies.Add(dllPath);
            }
        }

        return assemblies;
    }

    /// <summary>
    /// Retrieves the paths of local (in Assets) EDM assemblies that can be disabled.
    /// </summary>
    /// <returns>A list of paths to local EDM assemblies.</returns>
    static List<string> GetLocalEDMAssemblies()
    {
        return GetEdmAssemblies("Assets");
    }

    private static string GetCurrentPlatformSymbols()
    {
        var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        return PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
    }

    /// <summary>
    /// Internal helper to add a symbol for all platforms.
    /// </summary>
    internal static void AddSymbolForCurrentPlatform(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
            return;

        var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        var symbols = GetCurrentPlatformSymbols();
        if (!symbols.Contains(symbol))
        {
            symbols += $";{symbol}";
            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, string.Join(";", symbols));
        }
    }

    /// <summary>
    /// Removes preprocessor define for all platforms
    /// </summary>
    internal static void RemoveSymbolForAllPlatforms(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
            return;

        foreach (BuildTargetGroup buildTarget in Enum.GetValues(typeof(BuildTargetGroup)))
        {
            try
            {
                var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTarget);
                var symbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
                if (symbols.Contains(symbol))
                {
                    var symbolsArray = new List<string>(symbols.Split(';'));
                    symbolsArray.RemoveAll(s => s == symbol);
                    PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, string.Join(";", symbolsArray));
                }
            }
            catch (ArgumentException)
            {
                // continue
            }
        }
    }

    /// <summary>
    /// Checks if the given symbol exists on any valid platform.
    /// </summary>
    internal static bool IsSymbolEnabledOnCurrentPlatform(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
            return false;

        var symbols = GetCurrentPlatformSymbols();
        return symbols.Contains(symbol);
    }

    /// <summary>
    /// Checks if the given package path contains a marker file indicating that it's a dummy package used to replace EDM4U when disabled.
    /// </summary>
    /// <param name="packagePath"></param>
    /// <returns></returns>
    static bool IsDummyPackage(string packagePath)
    {
        return File.Exists(Path.Combine(packagePath, DummyPackageMarker));
    }


    static Dictionary<string, EdmInstance> GetAssembliesByEdmInstance(List<string> assemblies)
    {
        var instancesDict = new Dictionary<string, EdmInstance>();
        foreach (var assembly in assemblies)
        {
            var path = new FileInfo(assembly).Directory;

            if (!assembly.Contains("Google.VersionHandler.dll"))
                path = path?.Parent;

            if (path == null)
                continue;

            var pathString = path.ToString();

            if (!instancesDict.TryGetValue(pathString, out var instance))
            {
                instance = new EdmInstance();
                instancesDict[pathString] = instance;
            }

            if (assembly.Contains("Google.VersionHandler.dll"))
                instancesDict[pathString].VersionHandler = assembly;
            else
                instancesDict[pathString].Assemblies.Add(assembly);
        }

        return instancesDict;
    }


    /// <summary>
    /// Enables UEDM and disables the EDM4U package and its associated assemblies.
    /// </summary>
    internal static void CloseEdm4UWindows()
    {
        var windowTypeNames = new System.Collections.Generic.HashSet<string> {
            "GooglePlayServices.SettingsDialog",
            "Google.IOSResolverSettingsDialog",
            "Google.SettingsDialog"
        };
        foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
        {
            if (windowTypeNames.Contains(window.GetType().FullName))
                window.Close();
        }
    }

    internal static void SetLegacyPersistence(bool enabled)
    {
        persistenceEnabledField?.SetValue(null, enabled);
    }

    internal static bool GetLegacyPersistence()
    {
        if (persistenceEnabledField == null)
            return false;
        return (bool)persistenceEnabledField.GetValue(null);
    }

    internal static void SwitchToUedm()
    {
        SetLegacyPersistence(false);
        CloseEdm4UWindows();
        ReadSettingsFromLegacySettings();
        var stateDirty = false;
        if (Edm4UExists())
        {
            var uedm = GetPackageByName(UedmPackageName);
            var edm4U = GetPackageByName(Edm4UPackageName);
            if (edm4U != null)
            {
                // Even if EDM4U still shows as added from registry,
                // we might already have our embedded dummy, but domain reload didn't happen yet
                var edmDummyExists = Directory.Exists(Edm4UEmbeddedDir) && IsDummyPackage(Edm4UEmbeddedDir);
                if (edm4U.source == PackageSource.Embedded)
                {
                    if (Directory.Exists(edm4U.resolvedPath) && !edmDummyExists)
                    {
                        var moveTo = Edm4UEmbeddedDir + "_backup_edm";
                        if (!Directory.Exists(moveTo))
                            FileUtil.CopyFileOrDirectory(edm4U.resolvedPath, moveTo);

                        if (File.Exists(Path.Combine(moveTo, "package.json")))
                            File.Move(Path.Combine(moveTo, "package.json"), Path.Combine(moveTo, "package_backup_edm.json"));

                        if (File.Exists(Path.Combine(moveTo, "package.json.meta")))
                            File.Move(Path.Combine(moveTo, "package.json.meta"), Path.Combine(moveTo, "package_backup_edm.json.meta"));

                        FileUtil.DeleteFileOrDirectory(edm4U.resolvedPath);
                    }
                }

                if (!edmDummyExists && uedm != null && uedm.resolvedPath != null)
                {
                    var resources = Path.Combine(uedm.resolvedPath, "Resources~");
                    var dummyPackage = Path.Combine(resources, Edm4UPackageName);

                    FileUtil.CopyFileOrDirectory(dummyPackage, Edm4UEmbeddedDir);
                }
            }

            // Disable EDM4U assemblies
            var edm4UAssemblies = GetLocalEDMAssemblies();
            var edmInstances = GetAssembliesByEdmInstance(edm4UAssemblies);
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var edmInstanceKvp in edmInstances)
                {
                    var versionHandlerEnabled = false;
                    var edmInstance = edmInstanceKvp.Value;
                    if (edmInstance.VersionHandler != null)
                    {
                        var importer = AssetImporter.GetAtPath(edmInstance.VersionHandler) as PluginImporter;
                        if (importer != null && importer.GetCompatibleWithEditor())
                        {
                            versionHandlerEnabled = true;
                            importer.SetCompatibleWithEditor(false);
                            EnsureLibraryDefineConstraint(UedmDisabledString, importer);
                            importer.SaveAndReimport();
                        }
                    }

                    // Now disable all EDM4U assemblies related to this VersionHandler
                    // If VersionHandler did not exist for these assemblies or was disabled - disable them only if they are enabled
                    foreach (var assembly in edmInstance.Assemblies)
                    {
                        var importer = AssetImporter.GetAtPath(assembly) as PluginImporter;
                        if (importer == null)
                            continue;
                        if (versionHandlerEnabled || importer.GetCompatibleWithEditor())
                        {
                            importer.SetCompatibleWithEditor(false);
                            EnsureLibraryDefineConstraint(UedmDisabledString, importer);
                            importer.SaveAndReimport();
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            stateDirty = true;
        }

        // Enables U EDM
        if (!IsEdmActive())
        {
            RemoveSymbolForAllPlatforms(UedmDisabledString);
            stateDirty = true;
        }

        if (!stateDirty)
            return;

        AssetDatabase.Refresh();
        Client.Resolve();
    }

    static void EnsureLibraryDefineConstraint(string symbol, PluginImporter importer)
    {
        if (importer != null)
        {
            var defineConstraints = new List<string>(importer.DefineConstraints);
            if (defineConstraints.Contains(symbol))
                return;
            defineConstraints.Add(symbol);
            importer.DefineConstraints = defineConstraints.ToArray();
        }
    }

    static bool PluginHasDefineConstraint(string symbol, PluginImporter importer)
    {
        if (importer != null)
        {
            var defineConstraints = new List<string>(importer.DefineConstraints);
            return defineConstraints.Contains(symbol);
        }

        return false;
    }

    /// <summary>
    /// Checks if UEDM is enabled (UNITY_EDM_DISABLED symbol is NOT defined on the current platform).
    /// </summary>
    internal static bool IsEdmActive()
    {
        return !IsSymbolEnabledOnCurrentPlatform(UedmDisabledString);
    }

    /// <summary>
    /// Checks if any EDM is enabled in the project, either as a package or as enabled assemblies.
    /// </summary>
    /// <returns></returns>
    internal static bool AnyEdmExists()
    {
        return Edm4UExists() || IsEdmActive();
    }

    /// <summary>
    ///  Checks if the EDM4U package exists and IS ENABLED in the project, either as an embedded package or as enabled assemblies.
    /// </summary>
    /// <returns></returns>
    internal static bool Edm4UExists()
    {
        var edm4UAssemblies = GetEdmAssemblies();
        foreach (var dllPath in edm4UAssemblies)
        {
            var importer = AssetImporter.GetAtPath(dllPath) as PluginImporter;

            if (importer == null || !importer.GetCompatibleWithEditor() ||
                (PluginHasDefineConstraint(UedmDisabledString, importer) && IsEdmActive()))
                continue;
            return true;
        }

        return false;
    }

    internal static void ReadSettingsFromLegacySettings()
    {
        ReadLegacySettings();
        EDMSetting.InvokeRead();
        EDMResolverSettings.instance.SaveWithoutBridging();
    }

    static readonly Type projectSettingsType =
        Type.GetType("Google.ProjectSettings, Google.VersionHandlerImpl");

    static readonly FieldInfo persistenceEnabledField =
        projectSettingsType?.GetField(
            "persistenceEnabled", BindingFlags.Static | BindingFlags.NonPublic);

    static readonly FieldInfo projectSettingsField =
        projectSettingsType?.GetField(
            "projectSettings", BindingFlags.Static | BindingFlags.NonPublic);

    static readonly FieldInfo loggerField =
        projectSettingsType?.GetField(
            "logger", BindingFlags.Static | BindingFlags.NonPublic);

    static readonly FieldInfo checkoutFileField =
        projectSettingsType?.GetField(
            "checkoutFile", BindingFlags.Static | BindingFlags.NonPublic);

    static readonly FieldInfo loadedSettingsField =
        projectSettingsType?.GetField(
            "loadedSettings", BindingFlags.Static | BindingFlags.NonPublic);

    private static void ReadLegacySettings()
    {
        if (!File.Exists(LegacySettingsFile))
            return;

        var settingsInstance = projectSettingsField.GetValue(null);
        var setString = settingsInstance.GetType().GetMethod("SetString");
        var deleteKey = settingsInstance.GetType().GetMethod("DeleteKey");

        // Clear existing project keys before loading.
        var keys = (IEnumerable<string>)settingsInstance.GetType()
            .GetProperty("Keys").GetValue(settingsInstance);
        foreach (var key in new List<string>(keys))
            deleteKey.Invoke(settingsInstance, new object[] { key });

        using var stream = new FileStream(
            LegacySettingsFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        using var reader = XmlReader.Create(stream);
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.Name != "projectSetting")
                continue;

            var key = reader.GetAttribute("name");
            if (String.IsNullOrEmpty(key))
                continue;

            var value = reader.GetAttribute("value");
            if (String.IsNullOrEmpty(value))
                deleteKey.Invoke(settingsInstance, new object[] { key });
            else
                setString.Invoke(settingsInstance, new object[] { key, value });
        }

        // Mark as loaded so ProjectSettings.LoadIfEmpty() won't re-read the file.
        loadedSettingsField.SetValue(null, settingsInstance);
    }

    static EDMInstancesHandling()
    {
        AssemblyReloadEvents.beforeAssemblyReload += FlushLegacySettingsWrite;
        EditorApplication.quitting += FlushLegacySettingsWrite;
    }

    #region Threaded Legacy Settings Writer

    private static readonly string LegacySettingsFile =
        Path.Combine("ProjectSettings", "GvhProjectSettings.xml");

    private static readonly object _writeLock = new object();
    private static Task _pendingWrite;
    private static Dictionary<string, string> _pendingSnapshot;

    // Captures a snapshot of in-memory settings on the main thread,
    // then writes GvhProjectSettings.xml on a background thread.
    // If there is more than one write queued - the last one is written.
    private static void QueueWriteLegacySettings()
    {
        // Checkout must happen on the main thread (may show UI).
        var logger = loggerField.GetValue(null);
        var logMethod = loggerField.FieldType.GetMethod("Log");
        var checkoutFile = (Delegate)checkoutFileField.GetValue(null);
        if (!(bool)checkoutFile.DynamicInvoke(LegacySettingsFile, logger))
        {
            logMethod?.Invoke(logger, new object[]
            {
                $"Unable to checkout '{LegacySettingsFile}'. Project settings were not saved!",
                Google.LogLevel.Error
            });
            return;
        }

        // Snapshot the in-memory dictionary on the main thread.
        var snapshot = CaptureSettingsSnapshot();
        if (snapshot.Count == 0 && !File.Exists(LegacySettingsFile))
            return;

        lock (_writeLock)
        {
            _pendingSnapshot = snapshot;

            if (_pendingWrite == null || _pendingWrite.IsCompleted)
                _pendingWrite = Task.Run(WriteLegacySettingsLoop);
        }
    }

    private static Dictionary<string, string> CaptureSettingsSnapshot()
    {
        var settingsInstance = projectSettingsField.GetValue(null);
        var keys = (IEnumerable<string>)settingsInstance.GetType()
            .GetProperty("Keys")?.GetValue(settingsInstance);

        var snapshot = new Dictionary<string, string>();
        if (keys == null)
            return snapshot;

        foreach (var key in keys)
        {
            var value = EDMSetting.Edm4uProjectSettings.GetString(key);
            if (!String.IsNullOrEmpty(key) && !String.IsNullOrEmpty(value))
                snapshot[key] = value;
        }

        return snapshot;
    }

    // Background loop that writes the latest snapshot to the disk.
    // Keeps looping while new snapshots arrive during the write.
    private static void WriteLegacySettingsLoop()
    {
        while (true)
        {
            Dictionary<string, string> snapshot;
            lock (_writeLock)
            {
                if (_pendingSnapshot == null)
                    return;
                snapshot = _pendingSnapshot;
                _pendingSnapshot = null;
            }

            WriteLegacySettingsFromSnapshot(snapshot);
        }
    }

    private static void WriteLegacySettingsFromSnapshot(Dictionary<string, string> snapshot)
    {
        const int maxRetries = 10;
        const int retryDelayMs = 50;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                using var stream = new FileStream(
                    LegacySettingsFile,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.ReadWrite);

                using var writer = XmlWriter.Create(stream,
                    new XmlWriterSettings
                    {
                        Encoding = new UTF8Encoding(false),
                        Indent = true,
                        IndentChars = "  ",
                        NewLineChars = "\n",
                        NewLineHandling = NewLineHandling.Replace,
                    });

                writer.WriteStartElement("projectSettings");
                foreach (var kvp in snapshot)
                {
                    writer.WriteStartElement("projectSetting");
                    writer.WriteAttributeString("name", kvp.Key);
                    writer.WriteAttributeString("value", kvp.Value);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                return;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                if (attempt < maxRetries - 1)
                {
                    System.Threading.Thread.Sleep(retryDelayMs);
                    continue;
                }

                Debug.LogError(
                    $"Unable to write to '{LegacySettingsFile}' after {maxRetries} attempts ({e}). " +
                    "Project settings were not saved!");
            }
        }
    }

    // Blocks until any pending background write completes.
    // Called before domain reload and editor quit.
    internal static void FlushLegacySettingsWrite()
    {
        Task task;
        lock (_writeLock)
        {
            task = _pendingWrite;
        }

        task?.Wait();
    }

    #endregion

    internal static void BridgeSettingsToLegacySettings()
    {
        // InvokeWrite only updates the in-memory dictionary.
        // File I/O happens on a background thread via QueueWriteLegacySettings.
        var persistenceBefore = GetLegacyPersistence();
        try
        {
            SetLegacyPersistence(false);
            EDMSetting.InvokeWrite();
            QueueWriteLegacySettings();
        }
        finally
        {
            SetLegacyPersistence(persistenceBefore);
        }
    }

    /// <summary>
    /// Re-enables the EDM4U package and its associated assemblies.
    /// </summary>
    internal static void SwitchToEdm4U(bool bridgeSettings = true)
    {
        if (bridgeSettings)
            BridgeSettingsToLegacySettings();
        SetLegacyPersistence(true);
        var stateDirty = false;
        var backupPath = Edm4UEmbeddedDir + "_backup_edm";

        var edmDummyExists = Directory.Exists(Edm4UEmbeddedDir) && IsDummyPackage(Edm4UEmbeddedDir);
        // Delete any dummy package that replaced EDM4U
        if (edmDummyExists)
        {
            Directory.Delete(Edm4UEmbeddedDir, true);
            stateDirty = true;
        }

        // Check that backup exists and there is no other embedded package at this location
        if (Directory.Exists(backupPath) && !Directory.Exists(Edm4UEmbeddedDir))
        {
            // Restore package.json name if it was renamed
            var backupPackageJson = Path.Combine(backupPath, "package_backup_edm.json");
            var backupPackageJsonMeta = Path.Combine(backupPath, "package_backup_edm.json.meta");
            var restoredPackageJson = Path.Combine(backupPath, "package.json");
            var restoredPackageJsonMeta = Path.Combine(backupPath, "package.json.meta");
            if (File.Exists(backupPackageJson))
                File.Move(backupPackageJson, restoredPackageJson);
            if (File.Exists(backupPackageJsonMeta))
                File.Move(backupPackageJsonMeta, restoredPackageJsonMeta);

            // Move backup back to original location
            Directory.Move(backupPath, Edm4UEmbeddedDir);
            stateDirty = true;
        }

        var edm4UAssemblies = GetLocalEDMAssemblies();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var dllPath in edm4UAssemblies)
            {
                var importer = AssetImporter.GetAtPath(dllPath) as PluginImporter;
                if (importer != null)
                {
                    var importerDirty = false;
                    var constraints = new List<string>(importer.DefineConstraints);
                    if (constraints.Remove(UedmDisabledString))
                    {
                        importer.SetCompatibleWithEditor(true);
                        importer.DefineConstraints = constraints.ToArray();
                        importerDirty = true;
                    }

                    if (importerDirty)
                        importer.SaveAndReimport();
                    stateDirty |= importerDirty;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        stateDirty = IsEdmActive() || stateDirty;
        AddSymbolForCurrentPlatform(UedmDisabledString);
        // No refresh needed if nothing changed
        if (!stateDirty)
            return;

        AssetDatabase.Refresh();
        Client.Resolve();
    }

    internal static void ResetState()
    {
        if (WasEdm4UInstalled())
            SwitchToEdm4U(bridgeSettings: false);
        Settings.EdmWasInstalled = false;
        Settings.EdmEnabled = false;
        Settings.SelectionPresented = false;
        RemoveSymbolForAllPlatforms(UedmDisabledString);
    }
}