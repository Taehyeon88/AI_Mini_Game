using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "GameData/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [SerializeField] private string _displayName;
    [SerializeField] private int _damage;
    [SerializeField] private float _cooldown;
    [SerializeField] private float _range;
    [SerializeField] private int _projectileCount;
    [SerializeField] private float _projectileSpeed;
    [SerializeField] private GameObject _prefab;
    [SerializeField] private Sprite[] _stageSprites;
    [SerializeField] private float _projectileLifetime;
    [SerializeField] private float _upgradeDamageRate;
    [SerializeField] private float _upgradeCooldownRate;
    [SerializeField] private float _cooldownFloor;

    [SerializeField] private float _projectileSpreadOffset;

    [SerializeField] private float _pivotAngularSpeed;
    [SerializeField] private float _selfSpinSpeed;

    [SerializeField] private float _orbitRadius;
    [SerializeField] private float _orbitDuration;
    [SerializeField] private float _orbitAngularSpeed;
    [SerializeField] private float _homingTurnRate;

    public string DisplayName => _displayName;
    public int Damage => _damage;
    public float Cooldown => _cooldown;
    public float Range => _range;
    public int ProjectileCount => _projectileCount;
    public float ProjectileSpeed => _projectileSpeed;
    public GameObject Prefab => _prefab;
    public Sprite[] StageSprites => _stageSprites;
    public float ProjectileLifetime => _projectileLifetime;
    public float UpgradeDamageRate => _upgradeDamageRate;
    public float UpgradeCooldownRate => _upgradeCooldownRate;
    public float CooldownFloor => _cooldownFloor;

    public float ProjectileSpreadOffset => _projectileSpreadOffset;

    public float PivotAngularSpeed => _pivotAngularSpeed;
    public float SelfSpinSpeed => _selfSpinSpeed;

    public float OrbitRadius => _orbitRadius;
    public float OrbitDuration => _orbitDuration;
    public float OrbitAngularSpeed => _orbitAngularSpeed;
    public float HomingTurnRate => _homingTurnRate;
}
