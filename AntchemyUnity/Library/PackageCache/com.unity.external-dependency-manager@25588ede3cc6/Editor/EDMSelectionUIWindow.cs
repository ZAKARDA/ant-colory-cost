using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_6000_7_OR_NEWER
using Unity.Scripting.LifecycleManagement;
#endif

#if UNITY_6000_7_OR_NEWER
[NoAutoStaticsCleanup]
#endif
internal class EDMSelectionUIWindow : EditorWindow
{
    private string _message;
    private string _infoUrl;
    private bool _installEdm;

    public static void ShowWindow(string title, string message, string infoUrl, bool installEdm)
    {
        if (Application.isBatchMode)
            return;

        // Close any existing window
        var currentWindow = GetWindow<EDMSelectionUIWindow>();
        if (currentWindow != null)
        {
            currentWindow.Close();
        }

        var window = CreateInstance<EDMSelectionUIWindow>();
        window.titleContent = new GUIContent(title);
        Vector2 fixedSize = new Vector2(440, 100);
        window.position = new Rect(200, 200, 440, 100);
        window.minSize = fixedSize;
        window.maxSize = fixedSize;
        window._message = message;
        window._infoUrl = infoUrl;
        window._installEdm = installEdm;
        window.ShowUtility();
    }

    public void CreateGUI()
    {
        VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"Packages/{EDMInstancesHandling.UedmPackageName}/Editor/UI/EDMSelectionUIWindow.uxml");
        StyleSheet uss = AssetDatabase.LoadAssetAtPath<StyleSheet>($"Packages/{EDMInstancesHandling.UedmPackageName}/Editor/UI/EDMSelectionUIWindow.uss");

        rootVisualElement.Clear();
        rootVisualElement.Add(uxml.Instantiate());
        rootVisualElement.styleSheets.Add(uss);

        var description = rootVisualElement.Q<Label>("description");
        description.text = _message;

        var bottom = rootVisualElement.Q<VisualElement>("bottomContainer");

        // Learn More URL
        var learnMore = bottom.Q<Label>("learnMore");
        learnMore.RegisterCallback<ClickEvent>(evt => Application.OpenURL(_infoUrl));

        var leftButton = bottom.Q<Button>("leftButton");
        var rightButton = bottom.Q<Button>("rightButton");

        if (_installEdm)
        {
            leftButton.text = "Close";
            leftButton.clicked += () => { Close(); };

            rightButton.text = "Use Unity's External Dependency Manager";
            rightButton.clicked += () =>
            {
                // Close must be issued first — domain reload from installation would prevent it otherwise.
                Close();
                EDMManager.InstallEDMSelected();
                EDMAnalytics.SendSwitchDependencyManagerEvent(EDMAnalytics.SwitchType.PopupEdm4uToEdm);
            };
        }
        else
        {
            // "Use Alternative" on the left to make switching away from Unity's EDM a non-default action.
            leftButton.text = "Use Alternative";
            leftButton.clicked += () =>
            {
                Close();
                EDMManager.InstallOtherSelected();
                EDMAnalytics.SendSwitchDependencyManagerEvent(EDMAnalytics.SwitchType.PopupEdmToEdm4u);
            };

            rightButton.text = "Keep Unity's";
            rightButton.clicked += () => { Close(); };
        }
    }
}
