using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class KnifeProjectile : MonoBehaviour
{
    private Rigidbody2D _rigidbody2D;
    private SpriteRenderer _spriteRenderer;

    private ObjectPool<KnifeProjectile> _pool;
    private Vector2 _velocity;
    private int _damage;
    private float _remainingLifetime;
    private bool _isReleased;

    private void Awake()
    {
        if (!TryGetComponent(out _rigidbody2D))
        {
            Debug.LogError($"{nameof(KnifeProjectile)} requires a Rigidbody2D.", this);
        }

        if (!TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(KnifeProjectile)} requires a SpriteRenderer.", this);
        }
    }

    public void Init(Vector2 position, Vector2 direction, int damage, float speed, float lifetime, Sprite sprite, ObjectPool<KnifeProjectile> pool)
    {
        // Physics2D.autoSyncTransforms가 꺼져 있어 rigidbody만 옮기면 다음 물리 스텝 전까지 transform이
        // 옛 위치/회전으로 렌더링된다 — 풀에서 재사용될 때 한 프레임 눈에 띄지 않도록 둘 다 직접 맞춘다.
        _rigidbody2D.position = position;
        transform.position = position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        _rigidbody2D.rotation = angle;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        _velocity = direction * speed;
        _damage = damage;
        _remainingLifetime = lifetime;
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

        _rigidbody2D.MovePosition(_rigidbody2D.position + _velocity * Time.fixedDeltaTime);

        _remainingLifetime -= Time.fixedDeltaTime;
        if (_remainingLifetime <= 0f)
        {
            Release();
        }
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
        _pool.Release(this);
    }
}
