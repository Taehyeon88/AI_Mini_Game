using System.Collections.Generic;
using UnityEngine;

public class Axe : IWeapon
{
    private readonly WeaponData _data;
    private readonly Transform _owner;
    private readonly Transform[] _blades;
    private readonly SpriteRenderer[] _bladeRenderers;
    private readonly Dictionary<IDamageable, float> _hitCooldowns = new Dictionary<IDamageable, float>();
    private readonly List<IDamageable> _cooldownKeysSnapshot = new List<IDamageable>();

    private int _damageStacks;
    private float _pivotAngle;
    private float _selfSpinAngle;

    public WeaponData Data => _data;
    public int Level => 1 + _damageStacks;
    public int CurrentDamage => Mathf.RoundToInt(_data.Damage * (1f + _data.UpgradeDamageRate * _damageStacks));

    private Sprite CurrentStageSprite
    {
        get
        {
            int index = Mathf.Clamp((Level - 1) / 2, 0, _data.StageSprites.Length - 1);
            return _data.StageSprites[index];
        }
    }

    public Axe(WeaponData data, Transform owner)
    {
        _data = data;
        _owner = owner;

        int bladeCount = _data.ProjectileCount;
        _blades = new Transform[bladeCount];
        _bladeRenderers = new SpriteRenderer[bladeCount];

        for (int i = 0; i < bladeCount; i++)
        {
            GameObject blade = Object.Instantiate(_data.Prefab, owner);
            _blades[i] = blade.transform;
            _bladeRenderers[i] = blade.GetComponent<SpriteRenderer>();
            _bladeRenderers[i].sprite = CurrentStageSprite;
        }

        UpdateBladeTransforms();
    }

    public void Tick(float deltaTime)
    {
        _pivotAngle = Mathf.Repeat(_pivotAngle + _data.PivotAngularSpeed * deltaTime, 360f);
        _selfSpinAngle = Mathf.Repeat(_selfSpinAngle + _data.SelfSpinSpeed * deltaTime, 360f);

        UpdateBladeTransforms();
        ApplyContactDamage(deltaTime);
    }

    public void ApplyUpgrade(WeaponUpgradeType type)
    {
        if (type != WeaponUpgradeType.Damage)
        {
            return;
        }

        _damageStacks++;

        Sprite sprite = CurrentStageSprite;
        foreach (SpriteRenderer renderer in _bladeRenderers)
        {
            renderer.sprite = sprite;
        }
    }

    private void UpdateBladeTransforms()
    {
        // 도끼 전용 궤도 반경 필드가 없어(orbitRadius는 창 전용) Range를 궤도 반경으로 재사용한다 —
        // 아래 ApplyContactDamage의 접촉판정 반경과 항상 같은 값이어야 블레이드가 도는 궤적과
        // 실제 맞는 범위가 시각적으로 일치한다.
        int count = _blades.Length;
        for (int i = 0; i < count; i++)
        {
            float angle = _pivotAngle + (360f / count) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _data.Range;

            _blades[i].position = (Vector2)_owner.position + offset;
            _blades[i].rotation = Quaternion.Euler(0f, 0f, _selfSpinAngle);
        }
    }

    private void ApplyContactDamage(float deltaTime)
    {
        _cooldownKeysSnapshot.Clear();
        _cooldownKeysSnapshot.AddRange(_hitCooldowns.Keys);

        foreach (IDamageable key in _cooldownKeysSnapshot)
        {
            float remaining = _hitCooldowns[key] - deltaTime;
            if (remaining <= 0f)
            {
                _hitCooldowns.Remove(key);
            }
            else
            {
                _hitCooldowns[key] = remaining;
            }
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(_owner.position, _data.Range);

        foreach (Collider2D hit in hits)
        {
            if (hit.transform == _owner)
            {
                continue;
            }

            if (!hit.TryGetComponent(out IDamageable damageable) || !damageable.IsAlive)
            {
                continue;
            }

            if (_hitCooldowns.ContainsKey(damageable))
            {
                continue;
            }

            damageable.TakeDamage(CurrentDamage);
            _hitCooldowns[damageable] = _data.Cooldown;
        }
    }
}
