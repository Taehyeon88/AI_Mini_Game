using UnityEngine;
using UnityEngine.Pool;

public class Spear : IWeapon
{
    private const int PrewarmCount = 50;

    private readonly WeaponData _data;
    private readonly Transform _owner;
    private readonly ObjectPool<SpearProjectile> _pool;

    private float _cooldownTimer;

    public WeaponData Data => _data;
    public int Level => 1;
    public int CurrentDamage => _data.Damage;

    public Spear(WeaponData data, Transform owner)
    {
        _data = data;
        _owner = owner;

        _pool = new ObjectPool<SpearProjectile>(
            createFunc: () => Object.Instantiate(_data.Prefab).GetComponent<SpearProjectile>(),
            actionOnGet: projectile => projectile.gameObject.SetActive(true),
            actionOnRelease: projectile => projectile.gameObject.SetActive(false),
            actionOnDestroy: projectile => Object.Destroy(projectile.gameObject),
            collectionCheck: true,
            defaultCapacity: PrewarmCount,
            maxSize: 200);

        var prewarmed = new SpearProjectile[PrewarmCount];
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
            return;
        }

        Fire(target);
        _cooldownTimer = _data.Cooldown;
    }

    public void ApplyUpgrade(WeaponUpgradeType type)
    {
        // 창은 강화 없는 보조 무기 — 계약상 구현은 필요하지만 아무 것도 하지 않는다.
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

        SpearProjectile projectile = _pool.Get();
        projectile.Init(
            origin,
            direction,
            target,
            CurrentDamage,
            _data.ProjectileSpeed,
            _data.ProjectileLifetime,
            _data.OrbitRadius,
            _data.OrbitDuration,
            _data.OrbitAngularSpeed,
            _data.HomingTurnRate,
            _data.StageSprites[0],
            _pool);
    }
}
