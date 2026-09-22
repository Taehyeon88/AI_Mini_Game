public interface IWeapon
{
    WeaponData Data { get; }
    int Level { get; }
    int CurrentDamage { get; }
    void Tick(float deltaTime);
    void ApplyUpgrade(WeaponUpgradeType type);
}
