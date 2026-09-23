public static class UpgradeService
{
    public static void Apply(UpgradeData data, PlayerController player)
    {
        switch (data.Category)
        {
            case UpgradeCategory.NewWeapon:
                player.Weapons.AddWeapon(data.TargetWeapon);
                break;

            case UpgradeCategory.WeaponBuff:
                if (player.Weapons.TryGet(data.TargetWeapon, out IWeapon weapon))
                {
                    weapon.ApplyUpgrade(data.WeaponUpgradeType);
                }
                break;

            case UpgradeCategory.StatBuff:
                player.ApplyStat(data.StatType, data.Amount);
                break;
        }
    }
}
