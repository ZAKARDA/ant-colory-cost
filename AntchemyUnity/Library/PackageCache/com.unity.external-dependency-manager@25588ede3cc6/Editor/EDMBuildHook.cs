using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

internal class EDMBuildHook : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        var target = report.summary.platform;
        if (target != BuildTarget.Android && target != BuildTarget.iOS)
            return;

        EDMAnalytics.SendBuildTriggeredEvent(
            EDMAnalytics.GetEdmState(),
            report.summary.guid.ToString(),
            target);
    }
}
