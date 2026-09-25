using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteFrameToggler))]
public class BossController : MonoBehaviour, IDamageable
{
    private const int BoltPoolCapacity = 8;

    [SerializeField] private EnemyData _data;
    [SerializeField] private BossData _bossData;
    [SerializeField] private Color _hitFlashColor = Color.red;

    private SpriteRenderer _spriteRenderer;
    private SpriteFrameToggler _spriteFrameToggler;
    private ObjectPool<EnemyProjectile> _boltPool;
    private readonly List<Enemy> _summonedGhosts = new List<Enemy>();

    private PlayerController _player;
    private EnemySpawner _enemySpawner;
    private int _currentHP;
    private float _nextContactDamageTime;
    private float _boltTimer;
    private float _summonTimer;
    private Color _originalColor;
    private Coroutine _hitFlashCoroutine;

    public int MaxHP => _data != null ? _data.MaxHP : 0;
    public int CurrentHP => _currentHP;
    public bool IsAlive => _currentHP > 0;

    private void Awake()
    {
        if (!TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(BossController)} requires a SpriteRenderer.", this);
        }
        else
        {
            _originalColor = _spriteRenderer.color;
        }

        if (!TryGetComponent(out _spriteFrameToggler))
        {
            Debug.LogError($"{nameof(BossController)} requires a {nameof(SpriteFrameToggler)}.", this);
        }

        if (_data == null || _bossData == null)
        {
            Debug.LogError($"{nameof(BossController)}: _data 또는 _bossData가 연결되지 않았습니다.", this);
            return;
        }

        _boltPool = new ObjectPool<EnemyProjectile>(
            createFunc: () => Instantiate(_bossData.BoltPrefab).GetComponent<EnemyProjectile>(),
            actionOnGet: bolt => bolt.gameObject.SetActive(true),
            actionOnRelease: bolt => bolt.gameObject.SetActive(false),
            actionOnDestroy: bolt => Destroy(bolt.gameObject),
            collectionCheck: true,
            defaultCapacity: BoltPoolCapacity,
            maxSize: 50);
    }

    public void Init(PlayerController player, EnemySpawner enemySpawner)
    {
        _player = player;
        _enemySpawner = enemySpawner;
        _currentHP = _data.MaxHP;
        _nextContactDamageTime = 0f;
        _boltTimer = _bossData.BoltInterval;
        _summonTimer = _bossData.SummonInterval;

        _spriteFrameToggler.SetFrames(_data.Sprites, _data.AnimInterval);
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

        Vector2 position = transform.position;
        Vector2 playerPosition = _player.transform.position;
        float distance = Vector2.Distance(position, playerPosition);
        float step = _data.MoveSpeed * Time.fixedDeltaTime;

        if (distance > _bossData.KeepDistance + _bossData.KeepDistanceDeadzone)
        {
            transform.position = Vector2.MoveTowards(position, playerPosition, step);
            _spriteFrameToggler.SetMoving(true);
        }
        else if (distance < _bossData.KeepDistance - _bossData.KeepDistanceDeadzone)
        {
            transform.position = Vector2.MoveTowards(position, playerPosition, -step);
            _spriteFrameToggler.SetMoving(true);
        }
        else
        {
            _spriteFrameToggler.SetMoving(false);
        }
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        if (!IsAlive || _player == null)
        {
            return;
        }

        _boltTimer -= Time.deltaTime;
        if (_boltTimer <= 0f)
        {
            _boltTimer += _bossData.BoltInterval;
            FireBolt();
        }

        _summonTimer -= Time.deltaTime;
        if (_summonTimer <= 0f)
        {
            _summonTimer += _bossData.SummonInterval;
            SummonGhosts();
        }
    }

    private void FireBolt()
    {
        SoundManager.Instance.PlayBossFire();

        Vector2 origin = transform.position;
        Vector2 direction = ((Vector2)_player.transform.position - origin).normalized;

        EnemyProjectile bolt = _boltPool.Get();
        bolt.Init(origin, direction, _bossData.BoltDamage, _bossData.BoltSpeed, _bossData.BoltLifetime, _bossData.BoltSprite, _boltPool);
    }

    private void SummonGhosts()
    {
        // 풀에서 재사용돼 다른 타입이 된 적을 유령으로 세지 않도록 Data까지 확인한다.
        _summonedGhosts.RemoveAll(ghost =>
            !ghost.gameObject.activeInHierarchy || !ghost.IsAlive || ghost.Data != _bossData.GhostData);

        int room = _bossData.SummonCap - _summonedGhosts.Count;
        if (room <= 0)
        {
            return;
        }

        int count = Mathf.Min(Random.Range(_bossData.SummonMinCount, _bossData.SummonMaxCount + 1), room);
        for (int i = 0; i < count; i++)
        {
            Vector2 position = (Vector2)transform.position + Random.insideUnitCircle.normalized * _bossData.SummonRadius;
            _summonedGhosts.Add(_enemySpawner.SpawnAt(_bossData.GhostData, position));
        }
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
        // 보스 처치 = 즉시 클리어라 EXP 젬은 드랍하지 않는다.
        GameManager.Instance.AddKill();
        GameManager.Instance.ChangeState(GameState.Clear);
        gameObject.SetActive(false);
    }
}
