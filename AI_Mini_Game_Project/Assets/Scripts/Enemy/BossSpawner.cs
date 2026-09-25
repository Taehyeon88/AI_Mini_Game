using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [SerializeField] private BossController _bossPrefab;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private float _spawnTime = 120f;
    [SerializeField] private float _spawnEdgeMargin = 2f;

    private bool _spawned;

    private void Update()
    {
        if (_spawned || GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        if (GameManager.Instance.ElapsedTime < _spawnTime)
        {
            return;
        }

        _spawned = true;
        SpawnBoss();
    }

    private void SpawnBoss()
    {
        Vector2 center = _mainCamera.transform.position;
        Vector2 position = center + new Vector2(0f, _mainCamera.orthographicSize + _spawnEdgeMargin);

        BossController boss = Instantiate(_bossPrefab, position, Quaternion.identity);
        boss.Init(GameManager.Instance.Player, _enemySpawner);
        GameManager.Instance.NotifyBossSpawned(boss);
    }

#if UNITY_EDITOR
    public bool IsBossSpawned => _spawned;

    public void DebugForceSpawnBoss()
    {
        if (_spawned) return;

        _spawned = true;
        SpawnBoss();
    }
#endif
}
