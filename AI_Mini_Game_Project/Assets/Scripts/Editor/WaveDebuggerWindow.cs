using UnityEditor;
using UnityEngine;

public class WaveDebuggerWindow : EditorWindow
{
    [MenuItem("Window/Debug/Wave Debugger")]
    private static void Open()
    {
        GetWindow<WaveDebuggerWindow>("Wave Debugger");
    }

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play 모드에서만 동작합니다.", MessageType.Info);
            return;
        }

        DrawWaveSection();
        EditorGUILayout.Space();
        DrawUpgradeSection();
    }

    private void DrawWaveSection()
    {
        EditorGUILayout.LabelField("Wave", EditorStyles.boldLabel);

        var spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner == null)
        {
            EditorGUILayout.HelpBox("씬에 EnemySpawner가 없습니다.", MessageType.Warning);
            return;
        }

        for (int i = 0; i < spawner.StageCount; i++)
        {
            if (GUILayout.Button(spawner.DescribeStage(i), GUILayout.Height(30)))
            {
                spawner.DebugForceSpawnStage(i);
            }
        }
    }

    private void DrawUpgradeSection()
    {
        EditorGUILayout.LabelField("Upgrade", EditorStyles.boldLabel);

        var player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            EditorGUILayout.HelpBox("씬에 PlayerController가 없습니다.", MessageType.Warning);
            return;
        }

        foreach (UpgradeData data in Resources.LoadAll<UpgradeData>("Upgrades"))
        {
            if (GUILayout.Button(data.DisplayName, GUILayout.Height(30)))
            {
                UpgradeService.Apply(data, player);
            }
        }
    }
}
