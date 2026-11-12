using UnityEditor;
using UnityEditor.SceneManagement;

namespace Antchemy.Editor
{
    // Pressing Play always starts from MainGame, even if another (or an empty) scene is open.
    [InitializeOnLoad]
    static class PlayFromMainScene
    {
        const string ScenePath = "Assets/Scenes/MainGame.unity";
        static PlayFromMainScene() { EditorApplication.delayCall += Apply; }
        static void Apply()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (scene != null) EditorSceneManager.playModeStartScene = scene;
        }
    }
}
