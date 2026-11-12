#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Antchemy.Editor
{
    // Applies Assets/Icons/AppIcon.png (1024x1024, no alpha) to every iOS icon slot.
    [InitializeOnLoad]
    static class AppIconSetup
    {
        const string IconPath = "Assets/Icons/AppIcon.png";
        static AppIconSetup() { EditorApplication.delayCall += Apply; }

        [MenuItem("Antchemy/Apply App Icon")]
        public static void Apply()
        {
            var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (importer == null) return;
            if (importer.textureType != TextureImporterType.Default || importer.mipmapEnabled || importer.alphaSource != TextureImporterAlphaSource.None
                || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false; importer.alphaSource = TextureImporterAlphaSource.None; importer.alphaIsTransparency = false;
                importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null) return;
            bool changed = false;
            foreach (var target in new[] { NamedBuildTarget.iOS })
            {
                foreach (var kind in PlayerSettings.GetSupportedIconKinds(target))
                {
                    var slots = PlayerSettings.GetPlatformIcons(target, kind);
                    foreach (var slot in slots)
                    {
                        if (slot.GetTexture() == icon) continue;
                        for (int i = 0; i < slot.maxLayerCount; i++) slot.SetTexture(icon, i);
                        changed = true;
                    }
                    PlayerSettings.SetPlatformIcons(target, kind, slots);
                }
            }
            if (changed) { AssetDatabase.SaveAssets(); Debug.Log("Ant Colony Colors: app icon applied to iOS icon slots."); }
        }
    }
}
#endif
