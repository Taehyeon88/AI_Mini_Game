using System.Collections.Generic;
using UnityEngine;

public class PlayerWeapons
{
    private readonly Transform _owner;
    private readonly Dictionary<WeaponKind, IWeapon> _weapons = new Dictionary<WeaponKind, IWeapon>();

    public PlayerWeapons(Transform owner)
    {
        _owner = owner;
    }

    public bool AddWeapon(WeaponKind kind)
    {
        if (_weapons.ContainsKey(kind))
        {
            return false;
        }

        WeaponData data = Resources.Load<WeaponData>($"Weapons/{kind}Data");
        if (data == null)
        {
            Debug.LogError($"WeaponData를 찾을 수 없습니다: Weapons/{kind}Data");
            return false;
        }

        IWeapon weapon = CreateWeapon(kind, data);
        if (weapon == null)
        {
            return false;
        }

        _weapons[kind] = weapon;
        return true;
    }

    public bool TryGet(WeaponKind kind, out IWeapon weapon)
    {
        return _weapons.TryGetValue(kind, out weapon);
    }

    public void Tick(float deltaTime)
    {
        foreach (IWeapon weapon in _weapons.Values)
        {
            weapon.Tick(deltaTime);
        }
    }

    private IWeapon CreateWeapon(WeaponKind kind, WeaponData data)
    {
        switch (kind)
        {
            case WeaponKind.Knife:
                return new Knife(data, _owner);
            case WeaponKind.Axe:
                return new Axe(data, _owner);
            case WeaponKind.Spear:
                return new Spear(data, _owner);
            default:
                Debug.LogError($"{kind} 무기는 아직 구현되지 않았습니다.");
                return null;
        }
    }
}
