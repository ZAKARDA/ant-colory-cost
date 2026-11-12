#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Antchemy.Editor
{
    public static class IOSLaunchArt
    {
        [PostProcessBuild(1010)]
        public static void SetLaunchImage(BuildTarget target, string output)
        {
            if (target != BuildTarget.iOS) return;
            string source = Path.Combine(Application.dataPath, "LaunchLabPortrait.png");
            string destination = Path.Combine(output, "LaunchScreen-iPhonePortrait.png");
            if (!File.Exists(source) || !File.Exists(destination))
                throw new BuildFailedException("The iOS color-lab launch image is missing.");
            File.Copy(source, destination, true);
            Debug.Log("COLONY_IOS_LAUNCH_ART_OK");
        }
    }
}
#endif
