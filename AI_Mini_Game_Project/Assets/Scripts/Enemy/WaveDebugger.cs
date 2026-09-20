#if UNITY_EDITOR
using UnityEngine;

public class WaveDebugger : MonoBehaviour
{
    [SerializeField] private EnemySpawner _spawner;

    private void OnGUI()
    {
        if (_spawner == null)
        {
            return;
        }

        var buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            padding = new RectOffset(12, 12, 10, 10)
        };

        GUILayout.BeginArea(new Rect(10, 10, 600, 40 + _spawner.StageCount * 55));
        for (int i = 0; i < _spawner.StageCount; i++)
        {
            if (GUILayout.Button(_spawner.DescribeStage(i), buttonStyle, GUILayout.Height(45)))
            {
                _spawner.DebugForceSpawnStage(i);
            }
        }
        GUILayout.EndArea();
    }
}
#endif
