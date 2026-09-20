using UnityEngine;

[System.Serializable]
public class WaveStage
{
    [SerializeField] private float _startTime;
    [SerializeField] private float _spawnInterval;
    [SerializeField] private int _maxConcurrent;
    [SerializeField] private EnemyData[] _enemyPool;

    public float StartTime => _startTime;
    public float SpawnInterval => _spawnInterval;
    public int MaxConcurrent => _maxConcurrent;
    public EnemyData[] EnemyPool => _enemyPool;
}
