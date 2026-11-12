using UnityEngine;
using UnityEngine.SceneManagement;

namespace Antchemy
{
    // If the game is started from a scene that has no game objects (for example an empty
    // scene in the editor), load MainGame instead of showing only the splash screen.
    static class SceneGuard
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Check()
        {
            if (UnityEngine.Object.FindAnyObjectByType<GameManager>() != null) return;
            if (Application.CanStreamedLevelBeLoaded("MainGame")) SceneManager.LoadScene("MainGame");
            else Debug.LogError("Ant Colony Colors: the MainGame scene is missing. Use the Antchemy > Setup Main Scene menu.");
        }
    }
}
