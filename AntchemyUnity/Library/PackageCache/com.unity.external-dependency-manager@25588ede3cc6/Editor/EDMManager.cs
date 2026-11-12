using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;
#if UNITY_6000_7_OR_NEWER
using Unity.Scripting.LifecycleManagement;
#endif

[InitializeOnLoad]
#if UNITY_6000_7_OR_NEWER
[NoAutoStaticsCleanup]
#endif
internal class EDMManager
{
    const string UedmSelectionKey = "Unity_Prompt_EDM_Selection";

    private static bool _uedmEnabled
    {
        get => EDMInstancesHandling.Settings.EdmEnabled;
        set { EDMInstancesHandling.Settings.EdmEnabled = value; }
    }

    static EDMManager()
    {
        ClearAllEvents();
        Events.registeringPackages += OnPackagesRegistering;

        // Update/delayCall is not called in batch mode
        // In batch mode we wait for "afterAssemblyReload"
        if (Application.isBatchMode)
        {
            // Asset importer worker process also runs in batch mode and static initializers are called
            if (AssetDatabase.IsAssetImportWorkerProcess())
                return;
            AssemblyReloadEvents.afterAssemblyReload += Init;
        }
        else
        {
            EditorApplication.delayCall += Init;
        }
    }

    [MenuItem("Assets/External Dependency Manager/Change To Unity's EDM")]
    public static void EnableEDM()
    {
        // send analytics event
        InstallEDMSelected();
        EDMAnalytics.SendSwitchDependencyManagerEvent(EDMAnalytics.SwitchType.MenuItemEdm4uToEdm);
    }

    [MenuItem("Assets/External Dependency Manager/Change To Alternative EDM")]
    public static void EnableEDM4U()
    {
        // send analytics event
        InstallOtherSelected();
        EDMAnalytics.SendSwitchDependencyManagerEvent(EDMAnalytics.SwitchType.MenuItemEdmToEdm4u);
    }

    [MenuItem("Assets/External Dependency Manager/Change To Unity's EDM", true)]
    private static bool EnableEDM_Validate()
    {
        return !_uedmEnabled;
    }

    [MenuItem("Assets/External Dependency Manager/Change To Alternative EDM", true)]
    private static bool EnableEDM4U_Validate()
    {
        return _uedmEnabled && EDMInstancesHandling.WasEdm4UInstalled();
    }

    private static void BeforeCompilation(object o)
    {
        // Ensure at least some instance of EDM is enabled. If no active EDM assemblies - run Init to fix it
        CompilationPipeline.compilationStarted -= BeforeCompilation;
        if (!EDMInstancesHandling.AnyEdmExists())
            Init();
    }

    private static void ClearAllEvents()
    {
        ResetCallbacks();
        Events.registeringPackages -= OnPackagesRegistering;
    }

    private static void ResetCallbacks()
    {
        AssemblyReloadEvents.afterAssemblyReload -= Init;
        EditorApplication.delayCall -= Init;
        CompilationPipeline.compilationStarted -= BeforeCompilation;
    }

    private static void UpdateEdmState()
    {
        // In case the platforms switched, the scripting defines might need to be updated for the current platform
        if (EDMInstancesHandling.Settings.EdmEnabled)
            EDMInstancesHandling.RemoveSymbolForAllPlatforms(EDMInstancesHandling.UedmDisabledString);
        else
            EDMInstancesHandling.AddSymbolForCurrentPlatform(EDMInstancesHandling.UedmDisabledString);
    }

    private static void Init()
    {
        ResetCallbacks();
        // It might be that EDM4U will be manually deleted causing compilation errors and no domain reload
        // In that case Init would not be triggered. This will ensure that this is triggered on next compilation even with compilation errors.
        CompilationPipeline.compilationStarted += BeforeCompilation;
        var promptSelectionImmediately = SessionState.GetBool(UedmSelectionKey, false);

        UpdateEdmState();

        SessionState.SetBool(UedmSelectionKey, false);
        if (promptSelectionImmediately)
        {
            // If Unity_Prompt_EDM_Selection is set - we disabled the U EDM or EDM4U (depending if U EDM was installed first or not)
            // And we want to prompt user to select the EDM now - closing the prompt means no action needs to be done
            if (!EDMInstancesHandling.IsEdmActive())
                ShowEnableUEDMDialog();
          	else
                ShowEnableEDM4UDialog();
            
            EDMInstancesHandling.Settings.EdmWasInstalled = true;
            return;
        }

        if (EDMInstancesHandling.Settings.SelectionPresented)
        {
            ManageEDMInstallation(EDMInstancesHandling.Settings.EdmEnabled);
        }
        else if (!EDMInstancesHandling.Edm4UExists())
        {
            ManageEDMInstallation(true);
        }
        else
        {
            // Will be presenting the selection now
            if (EDMInstancesHandling.Settings.EdmWasInstalled)
            {
                // In this case U EDM was already installed and EDM4U was just added
                // We want to first disable EDM4U to avoid potential conflicts
                // and then prompt the user to select which EDM they want to use.
                // If they select EDM4U we can just re-enable it,
                // but if they select U EDM we can just leave EDM4U disabled with no recompile/domain reload.
                SessionState.SetBool(UedmSelectionKey, true);
                EDMInstancesHandling.SwitchToUedm();
            }
            else
            {
                // In this case EDM4U was already installed or both EDM4U and U EDM installed at the same time
                // We want to first disable U EDM to avoid potential conflicts
                // and then prompt the user to select which EDM they want to use.
                // If they select U EDM we can just re-enable it,
                // but if they select EDM4U we can just leave U EDM disabled with no recompile/domain reload.
                SessionState.SetBool(UedmSelectionKey, true);
                EDMInstancesHandling.SwitchToEdm4U();
            }
        }

        // If EDM4U was completely deleted by the user - reset the selection dialog state
        // so that the next time it's installed we show the selection dialog again
        if (!EDMInstancesHandling.WasEdm4UInstalled())
            EDMInstancesHandling.Settings.SelectionPresented = false;

        EDMInstancesHandling.Settings.EdmWasInstalled = true;

        // Analytic - is EDM the only dependency manager?
        if (EDMInstancesHandling.WasEdm4UInstalled())
        {
            var edmState = EDMInstancesHandling.IsEdmActive()
                ? EDMAnalytics.EdmState.EdmAndEdm4uPresentEdmActive
                : EDMAnalytics.EdmState.EdmAndEdm4uPresentEdm4uActive;
            EDMAnalytics.SendInitEvent(EDMAnalytics.OtherResolverStatus.OtherResolversPresent, edmState);
        }
        else
            EDMAnalytics.SendInitEvent(EDMAnalytics.OtherResolverStatus.OnlyEdmPresent, EDMAnalytics.EdmState.EdmAloneAndActive);
    }

    private static void ShowEnableEDM4UDialog()
    {
        // send analytics event
        EDMAnalytics.SendShowSwitchPopupEvent(false);
        
        EDMInstancesHandling.Settings.SelectionPresented = true;
        // U EDM enabled by default if we prompt to install EDM4U
        _uedmEnabled = true;

        EDMSelectionUIWindow.ShowWindow(
            "Change Dependency Manager?",
            "You installed an alternative external dependency manager, but your project currently uses Unity’s External Dependency Manager. " +
            "It's recommended to remain with Unity’s External Dependency Manager.",
            "https://docs.unity3d.com/Packages/com.unity.external-dependency-manager@latest?subfolder=/manual/get-started-with-edm.html#select-a-dependency-manager",
            false
        );
    }

    private static void ShowEnableUEDMDialog()
    {
        // send analytics event
        EDMAnalytics.SendShowSwitchPopupEvent(true);
        
        EDMInstancesHandling.Settings.SelectionPresented = true;
        // U EDM disabled by default if we prompt to install it
        _uedmEnabled = false;

        EDMSelectionUIWindow.ShowWindow(
            "Change Dependency Manager?",
            "Your project currently uses an alternative external dependency manager. " +
            "It's recommended to switch to Unity’s External Dependency Manager.",
            "https://docs.unity3d.com/Packages/com.unity.external-dependency-manager@latest?subfolder=/manual/get-started-with-edm.html#select-a-dependency-manager",
            true
        );
    }

    internal static void InstallEDMSelected()
    {
        ManageEDMInstallation(true);
    }

    internal static void InstallOtherSelected()
    {
        ManageEDMInstallation(false);
    }

    static void ManageEDMInstallation(bool uedmEnabled)
    {
        _uedmEnabled = uedmEnabled;
        if (uedmEnabled)
        {
            EDMInstancesHandling.SwitchToUedm();
        }
        else
        {
            if (EDMInstancesHandling.WasEdm4UInstalled())
                EDMInstancesHandling.SwitchToEdm4U();
            else
                ManageEDMInstallation(true);
        }
    }

    static void OnPackagesRegistering(PackageRegistrationEventArgs args)
    {
        if (args.removed.Count > 0)
        {
            foreach (var package in args.removed)
            {
                // U EDM Libraries package was removed - reset state
                if (package.name == EDMInstancesHandling.UedmPackageName)
                {
                    Events.registeringPackages -= OnPackagesRegistering;
                    EDMInstancesHandling.ResetState();
                    return;
                }
            }
        }
    }
}
