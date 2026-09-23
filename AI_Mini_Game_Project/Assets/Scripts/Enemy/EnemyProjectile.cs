using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyProjectile : MonoBehaviour
{
    private Rigidbody2D _rigidbody2D;
    private SpriteRenderer _spriteRenderer;

    private ObjectPool<EnemyProjectile> _pool;
    private Vector2 _velocity;
    private int _damage;
    private float _remainingLifetime;
    private bool _isReleased;

    private void Awake()
    {
        if (!TryGetComponent(out _rigidbody2D))
        {
            Debug.LogError($"{nameof(EnemyProjectile)} requires a Rigidbody2D.", this);
        }

        if (!TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(EnemyProjectile)} requires a SpriteRenderer.", this);
        }
    }

    public void Init(Vector2 position, Vector2 direction, int damage, float speed, float lifetime, Sprite sprite, ObjectPool<EnemyProjectile> pool)
    {
        // KnifeProjectile과 동일: autoSyncTransforms가 꺼져 있어 rigidbody·transform 둘 다 직접 맞춘다.
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

        // 보스 위치에서 생성돼 보스와 겹쳐 있으므로, 충돌 매트릭스와 별개로 플레이어만 맞힌다(§7).
        if (!other.TryGetComponent(out PlayerController player))
        {
            return;
        }

        player.TakeDamage(_damage);
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
