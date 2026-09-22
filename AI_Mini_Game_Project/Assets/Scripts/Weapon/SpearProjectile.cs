using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SpearProjectile : MonoBehaviour
{
    private Rigidbody2D _rigidbody2D;
    private SpriteRenderer _spriteRenderer;

    private ObjectPool<SpearProjectile> _pool;
    private Transform _target;
    private Vector2 _velocity;
    private int _damage;
    private float _speed;
    private float _homingTurnRate;
    private float _remainingLifetime;
    private bool _isReleased;

    private Vector2 _orbitCenter;
    private float _orbitRadius;
    private float _orbitAngularSpeed;
    private float _orbitAngle;
    private float _remainingOrbitTime;

    private void Awake()
    {
        if (!TryGetComponent(out _rigidbody2D))
        {
            Debug.LogError($"{nameof(SpearProjectile)} requires a Rigidbody2D.", this);
        }

        if (!TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(SpearProjectile)} requires a SpriteRenderer.", this);
        }
    }

    public void Init(Vector2 position, Vector2 direction, Transform target, int damage, float speed, float lifetime,
        float orbitRadius, float orbitDuration, float orbitAngularSpeed, float homingTurnRate,
        Sprite sprite, ObjectPool<SpearProjectile> pool)
    {
        // Physics2D.autoSyncTransforms가 꺼져 있어 rigidbody만 옮기면 다음 물리 스텝 전까지 transform이
        // 옛 위치/회전으로 렌더링된다 — 풀에서 재사용될 때 한 프레임 눈에 띄지 않도록 둘 다 직접 맞춘다.
        _rigidbody2D.position = position;
        transform.position = position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        _rigidbody2D.rotation = angle;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        _target = target;
        _velocity = direction;
        _damage = damage;
        _speed = speed;
        _homingTurnRate = homingTurnRate;
        _remainingLifetime = lifetime;

        _orbitCenter = position;
        _orbitRadius = orbitRadius;
        _orbitAngularSpeed = orbitAngularSpeed;
        _orbitAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _remainingOrbitTime = orbitDuration;

        _spriteRenderer.sprite = sprite;
        _pool = pool;
        _isReleased = false;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        if (_remainingOrbitTime > 0f)
        {
            TickOrbit(Time.fixedDeltaTime);
        }
        else
        {
            TickHoming(Time.fixedDeltaTime);
        }

        _remainingLifetime -= Time.fixedDeltaTime;
        if (_remainingLifetime <= 0f)
        {
            Release();
        }
    }

    private void TickOrbit(float deltaTime)
    {
        _orbitAngle = Mathf.Repeat(_orbitAngle + _orbitAngularSpeed * deltaTime, 360f);

        float rad = _orbitAngle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _orbitRadius;
        _rigidbody2D.MovePosition(_orbitCenter + offset);

        float tangentAngle = _orbitAngle + Mathf.Sign(_orbitAngularSpeed) * 90f;
        float tangentRad = tangentAngle * Mathf.Deg2Rad;
        // _velocity를 접선 방향으로 계속 맞춰둬야 대기 선회가 끝나고 유도로 넘어갈 때
        // TickHoming이 기준으로 삼는 각도가 발사 당시 방향이 아니라 방금 향하던 방향이 된다
        // (안 하면 유도 시작 순간 방향이 크게 튀는 버그가 재발함).
        _velocity = new Vector2(Mathf.Cos(tangentRad), Mathf.Sin(tangentRad));
        SetFacingAngle(tangentAngle);

        _remainingOrbitTime -= deltaTime;
    }

    private void TickHoming(float deltaTime)
    {
        if (_target != null && _target.gameObject.activeInHierarchy
            && _target.TryGetComponent(out IDamageable targetDamageable) && targetDamageable.IsAlive)
        {
            Vector2 toTarget = (Vector2)_target.position - _rigidbody2D.position;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float currentAngle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg;
                float desiredAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
                float newAngle = Mathf.MoveTowardsAngle(currentAngle, desiredAngle, _homingTurnRate * deltaTime);

                float rad = newAngle * Mathf.Deg2Rad;
                _velocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                SetFacingAngle(newAngle);
            }
        }

        _rigidbody2D.MovePosition(_rigidbody2D.position + _velocity * _speed * deltaTime);
    }

    private void SetFacingAngle(float travelAngle)
    {
        float spriteAngle = travelAngle - 90f;
        _rigidbody2D.rotation = spriteAngle;
        transform.rotation = Quaternion.Euler(0f, 0f, spriteAngle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance.State != GameState.Playing || _isReleased)
        {
            return;
        }

        if (!other.TryGetComponent(out IDamageable target))
        {
            return;
        }

        target.TakeDamage(_damage);
        Release();
    }

    private void Release()
    {
        if (_isReleased)
        {
            return;
        }

        _isReleased = true;
        _target = null;
        _pool.Release(this);
    }
}
