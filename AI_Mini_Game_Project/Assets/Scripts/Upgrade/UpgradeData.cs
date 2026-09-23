using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeData", menuName = "GameData/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [SerializeField] private UpgradeCategory _category;
    [SerializeField] private string _displayName;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private WeaponKind _targetWeapon;
    [SerializeField] private WeaponUpgradeType _weaponUpgradeType;
    [SerializeField] private StatKind _statType;
    [SerializeField] private float _amount;

    public UpgradeCategory Category => _category;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public WeaponKind TargetWeapon => _targetWeapon;
    public WeaponUpgradeType WeaponUpgradeType => _weaponUpgradeType;
    public StatKind StatType => _statType;
    public float Amount => _amount;
}
