using UnityEngine;
using UnityEngine.Pool;

public class Knife : IWeapon
{
    private const int PrewarmCount = 50;

    private readonly WeaponData _data;
    private readonly Transform _owner;
    private readonly ObjectPool<KnifeProjectile> _pool;

    private int _damageStacks;
    private int _cooldownStacks;
    private int _extraProjectileStacks;
    private float _cooldownTimer;

    public WeaponData Data => _data;
    public int Level => 1 + _damageStacks + _cooldownStacks + _extraProjectileStacks;
    public int CurrentDamage => Mathf.RoundToInt(_data.Damage * (1f + _data.UpgradeDamageRate * _damageStacks));

    private float CurrentCooldown => Mathf.Max(_data.Cooldown * (1f - _data.UpgradeCooldownRate * _cooldownStacks), _data.CooldownFloor);
    private int CurrentProjectileCount => _data.ProjectileCount + _extraProjectileStacks;

    private Sprite CurrentStageSprite
    {
        get
        {
            int index = Mathf.Clamp((Level - 1) / 2, 0, _data.StageSprites.Length - 1);
            return _data.StageSprites[index];
        }
    }

    public Knife(WeaponData data, Transform owner)
    {
        _data = data;
        _owner = owner;

        _pool = new ObjectPool<KnifeProjectile>(
            createFunc: () => Object.Instantiate(_data.Prefab).GetComponent<KnifeProjectile>(),
            actionOnGet: projectile => projectile.gameObject.SetActive(true),
            actionOnRelease: projectile => projectile.gameObject.SetActive(false),
            actionOnDestroy: projectile => Object.Destroy(projectile.gameObject),
            collectionCheck: true,
            defaultCapacity: PrewarmCount,
            maxSize: 200);

        var prewarmed = new KnifeProjectile[PrewarmCount];
        for (int i = 0; i < PrewarmCount; i++)
        {
            prewarmed[i] = _pool.Get();
        }
        for (int i = 0; i < PrewarmCount; i++)
        {
            _pool.Release(prewarmed[i]);
        }
    }

    public void Tick(float deltaTime)
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= deltaTime;
            return;
        }

        Transform target = FindNearestTarget();
        if (target == null)
        {
            // 스펙상 후보 0마리면 발사를 스킵하고 쿨다운도 소모하지 않는다(타겟 생기자마자 바로 발사되게).
            return;
        }

        Fire(target);
        _cooldownTimer = CurrentCooldown;
    }

    public void ApplyUpgrade(WeaponUpgradeType type)
    {
        switch (type)
        {
            case WeaponUpgradeType.Damage:
                _damageStacks++;
                break;
            case WeaponUpgradeType.Cooldown:
                _cooldownStacks++;
                break;
            case WeaponUpgradeType.ExtraProjectile:
                _extraProjectileStacks++;
                break;
        }
    }

    private Transform FindNearestTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(_owner.position, _data.Range);

        Transform nearest = null;
        float nearestSqrDistance = float.MaxValue;

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

            float sqrDistance = ((Vector2)hit.transform.position - (Vector2)_owner.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = hit.transform;
            }
        }

        return nearest;
    }

    private void Fire(Transform target)
    {
        Vector2 origin = _owner.position;
        Vector2 direction = ((Vector2)target.position - origin).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);

        int count = CurrentProjectileCount;
        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) / 2f) * _data.ProjectileSpreadOffset;
            Vector2 spawnPosition = origin + perpendicular * offset;

            KnifeProjectile projectile = _pool.Get();
            projectile.Init(spawnPosition, direction, CurrentDamage, _data.ProjectileSpeed, _data.ProjectileLifetime, CurrentStageSprite, _pool);
        }
    }
}
