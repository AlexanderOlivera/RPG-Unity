using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Genera el ejecutable de Windows con las escenas habilitadas en Build Settings.</summary>
public static class AuraPlayerBuild
{
    public static string Execute()
    {
        List<string> scenes = new List<string>();
        EditorBuildSettingsScene[] configured = EditorBuildSettings.scenes;
        for (int i = 0; i < configured.Length; i++)
            if (configured[i].enabled) scenes.Add(configured[i].path);

        if (scenes.Count == 0)
            return "Sin escenas habilitadas en Build Settings";

        const string dir = "Builds/Windows";
        Directory.CreateDirectory(dir);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = dir + "/RPG.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        return "resultado=" + summary.result +
               " escenas=" + scenes.Count +
               " errores=" + summary.totalErrors +
               " tamañoMB=" + (summary.totalSize / 1048576) +
               " salida=" + summary.outputPath;
    }
}
