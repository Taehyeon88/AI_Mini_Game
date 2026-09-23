using UnityEngine;

[CreateAssetMenu(fileName = "BossData", menuName = "GameData/Boss Data")]
public class BossData : ScriptableObject
{
    [SerializeField] private float _keepDistance;
    [SerializeField] private float _keepDistanceDeadzone;
    [SerializeField] private int _boltDamage;
    [SerializeField] private float _boltSpeed;
    [SerializeField] private float _boltInterval;
    [SerializeField] private float _boltLifetime;
    [SerializeField] private Sprite _boltSprite;
    [SerializeField] private float _summonInterval;
    [SerializeField] private int _summonMinCount;
    [SerializeField] private int _summonMaxCount;
    [SerializeField] private int _summonCap;
    [SerializeField] private float _summonRadius;
    [SerializeField] private EnemyData _ghostData;
    [SerializeField] private GameObject _boltPrefab;

    public float KeepDistance => _keepDistance;
    public float KeepDistanceDeadzone => _keepDistanceDeadzone;
    public int BoltDamage => _boltDamage;
    public float BoltSpeed => _boltSpeed;
    public float BoltInterval => _boltInterval;
    public float BoltLifetime => _boltLifetime;
    public Sprite BoltSprite => _boltSprite;
    public float SummonInterval => _summonInterval;
    public int SummonMinCount => _summonMinCount;
    public int SummonMaxCount => _summonMaxCount;
    public int SummonCap => _summonCap;
    public float SummonRadius => _summonRadius;
    public EnemyData GhostData => _ghostData;
    public GameObject BoltPrefab => _boltPrefab;
}
