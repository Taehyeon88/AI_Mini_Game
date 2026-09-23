using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Enemy _enemyPrefab;
    [SerializeField] private Transform _enemyParent;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private WaveStage[] _stages;
    [SerializeField] private float _spawnEdgeMargin = 1f;
    [SerializeField] private float _despawnDistance = 31f;

    private PlayerController _player;
    private ObjectPool<Enemy> _pool;
    private readonly List<Enemy> _activeEnemies = new List<Enemy>();
    private WaveStage _currentStage;
    private float _spawnTimer;
#if UNITY_EDITOR
    private int _debugForcedStageIndex = -1;
#endif

    private void Awake()
    {
        _player = FindFirstObjectByType<PlayerController>();
        if (_player == null)
        {
            Debug.LogError($"{nameof(EnemySpawner)}: PlayerController를 씬에서 찾지 못했습니다.", this);
        }

        _pool = new ObjectPool<Enemy>(
            createFunc: () => Instantiate(_enemyPrefab, _enemyParent),
            actionOnGet: e => e.gameObject.SetActive(true),
            actionOnRelease: e => e.gameObject.SetActive(false),
            actionOnDestroy: e => Destroy(e.gameObject),
            collectionCheck: true,
            defaultCapacity: 16,
            maxSize: 60);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        UpdateCurrentStage();
        TickSpawn();
        PollActiveEnemies();
    }

    private void UpdateCurrentStage()
    {
#if UNITY_EDITOR
        if (_debugForcedStageIndex >= 0)
        {
            _currentStage = _stages[_debugForcedStageIndex];
            return;
        }
#endif
        float elapsed = GameManager.Instance.ElapsedTime;
        WaveStage candidate = _stages[0];

        for (int i = 0; i < _stages.Length; i++)
        {
            if (elapsed >= _stages[i].StartTime)
            {
                candidate = _stages[i];
            }
        }

        _currentStage = candidate;
    }

    private void TickSpawn()
    {
        if (_currentStage == null || _currentStage.EnemyPool.Length == 0)
        {
            return;
        }

        if (_activeEnemies.Count >= _currentStage.MaxConcurrent)
        {
            return;
        }

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer < _currentStage.SpawnInterval)
        {
            return;
        }

        _spawnTimer -= _currentStage.SpawnInterval;
        SpawnOne();
    }

    private void SpawnOne()
    {
        EnemyData[] pool = _currentStage.EnemyPool;
        EnemyData data = pool[Random.Range(0, pool.Length)];

        SpawnAt(data, GetRandomOffscreenPosition());
    }

    public Enemy SpawnAt(EnemyData data, Vector2 position)
    {
        Enemy enemy = _pool.Get();
        enemy.transform.position = position;
        enemy.Init(data, _player);
        _activeEnemies.Add(enemy);
        return enemy;
    }

    private Vector2 GetRandomOffscreenPosition()
    {
        float halfHeight = _mainCamera.orthographicSize;
        float halfWidth = halfHeight * _mainCamera.aspect;
        Vector2 center = _mainCamera.transform.position;

        int edge = Random.Range(0, 4);
        float x;
        float y;

        switch (edge)
        {
            case 0:
                x = Random.Range(-halfWidth, halfWidth);
                y = halfHeight + _spawnEdgeMargin;
                break;
            case 1:
                x = Random.Range(-halfWidth, halfWidth);
                y = -halfHeight - _spawnEdgeMargin;
                break;
            case 2:
                x = -halfWidth - _spawnEdgeMargin;
                y = Random.Range(-halfHeight, halfHeight);
                break;
            default:
                x = halfWidth + _spawnEdgeMargin;
                y = Random.Range(-halfHeight, halfHeight);
                break;
        }

        return center + new Vector2(x, y);
    }

    private void PollActiveEnemies()
    {
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = _activeEnemies[i];
            bool dead = !enemy.IsAlive;
            bool farAway = !dead && Vector2.Distance(
                enemy.transform.position, _mainCamera.transform.position) >= _despawnDistance;

            if (dead || farAway)
            {
                _activeEnemies.RemoveAt(i);
                _pool.Release(enemy);
            }
        }
    }

#if UNITY_EDITOR
    public int StageCount => _stages.Length;

    public string DescribeStage(int index)
    {
        WaveStage stage = _stages[index];
        var names = new List<string>();
        foreach (EnemyData data in stage.EnemyPool)
        {
            names.Add(data.name);
        }

        return $"Wave {index} ({stage.StartTime}s~, {string.Join("+", names)})";
    }

    public void DebugForceSpawnStage(int stageIndex)
    {
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            _pool.Release(_activeEnemies[i]);
        }
        _activeEnemies.Clear();

        _debugForcedStageIndex = stageIndex;
        _currentStage = _stages[stageIndex];
        _spawnTimer = 0f;

        while (_activeEnemies.Count < _currentStage.MaxConcurrent)
        {
            SpawnOne();
        }
    }
#endif
}
