#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;

namespace Antchemy.Editor
{
    // Keeps the store name and bundle identifier in sync every time the editor loads.
    [InitializeOnLoad]
    static class AppBranding
    {
        public const string ProductName = "Ant Colony Colors";
        public const string BundleId = "com.antcolonycolors.game";
        static AppBranding()
        {
            if (PlayerSettings.productName != ProductName) PlayerSettings.productName = ProductName;
            if (PlayerSettings.companyName != ProductName) PlayerSettings.companyName = ProductName;
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS) != BundleId) PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != BundleId) PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
        }
    }
}
#endif
