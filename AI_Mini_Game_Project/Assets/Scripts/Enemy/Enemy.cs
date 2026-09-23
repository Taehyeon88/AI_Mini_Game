using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteFrameToggler))]
public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] private Color _hitFlashColor = Color.red;

    private SpriteRenderer _spriteRenderer;
    private SpriteFrameToggler _spriteFrameToggler;

    private EnemyData _data;
    private PlayerController _player;
    private int _currentHP;
    private float _nextContactDamageTime;
    private Color _originalColor;
    private Coroutine _hitFlashCoroutine;

    public int MaxHP => _data != null ? _data.MaxHP : 0;
    public int CurrentHP => _currentHP;
    public bool IsAlive => _currentHP > 0;

    private void Awake()
    {
        if (!TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(Enemy)} requires a SpriteRenderer.", this);
        }
        else
        {
            _originalColor = _spriteRenderer.color;
        }

        if (!TryGetComponent(out _spriteFrameToggler))
        {
            Debug.LogError($"{nameof(Enemy)} requires a {nameof(SpriteFrameToggler)}.", this);
        }
    }

    public void Init(EnemyData data, PlayerController player)
    {
        _data = data;
        _player = player;
        _currentHP = data.MaxHP;
        _nextContactDamageTime = 0f;

        if (_hitFlashCoroutine != null)
        {
            StopCoroutine(_hitFlashCoroutine);
            _hitFlashCoroutine = null;
        }
        _spriteRenderer.color = _originalColor;

        _spriteFrameToggler.SetFrames(data.Sprites, data.AnimInterval);
        _spriteFrameToggler.SetMoving(true);
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        if (!IsAlive || _player == null)
        {
            return;
        }

        transform.position = Vector2.MoveTowards(
            transform.position,
            _player.transform.position,
            _data.MoveSpeed * Time.fixedDeltaTime);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        if (!IsAlive || Time.time < _nextContactDamageTime)
        {
            return;
        }

        if (!other.TryGetComponent(out IDamageable target))
        {
            return;
        }

        target.TakeDamage(_data.AttackPower);
        _nextContactDamageTime = Time.time + _data.ContactDamageInterval;
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive)
        {
            return;
        }

        _currentHP = Mathf.Max(_currentHP - amount, 0);

        if (!IsAlive)
        {
            Die();
            return;
        }

        if (_hitFlashCoroutine != null)
        {
            StopCoroutine(_hitFlashCoroutine);
        }
        _hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        _spriteRenderer.color = _hitFlashColor;
        yield return new WaitForSeconds(_data.HitFlashDuration);
        _spriteRenderer.color = _originalColor;
        _hitFlashCoroutine = null;
    }

    private void Die()
    {
        GameManager.Instance.AddKill();
        PickupManager.Instance.Spawn(transform.position, _data.DropExp);
    }
}
