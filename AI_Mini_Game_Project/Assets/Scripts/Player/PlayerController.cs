using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour, IDamageable
{
    [SerializeField] private float _moveSpeed = 7.8f;
    [SerializeField] private float _pickupRadius = 2.3f;
    [SerializeField] private int _maxHP = 100;
    [SerializeField] private float _invincibilityDuration = 0.5f;
    [SerializeField] private float _hitFlashDuration = 0.1f;
    [SerializeField] private Color _hitFlashColor = Color.red;

    private Rigidbody2D _rigidbody2D;
    private SpriteRenderer _spriteRenderer;
    private SpriteFrameToggler _spriteFrameToggler;
    private Vector2 _moveInput;

    private int _currentHP;
    private int _currentExp;
    private int _maxHPBonus;
    private float _moveSpeedBonusRate;
    private float _pickupRadiusBonusRate;
    private float _invincibleUntil;
    private Color _originalColor;
    private Coroutine _hitFlashCoroutine;

    public float MoveSpeed => _moveSpeed * (1f + _moveSpeedBonusRate);
    public float PickupRadius => _pickupRadius * (1f + _pickupRadiusBonusRate);

    public int MaxHP => _maxHP + _maxHPBonus;
    public int CurrentHP => _currentHP;
    public bool IsAlive => _currentHP > 0;

    public int CurrentLevel { get; private set; } = 1;
    public int CurrentExp => _currentExp;
    public int ExpToNext => ExpToNextForLevel(CurrentLevel);

    public PlayerWeapons Weapons { get; private set; }

    // GameManager(§12)·LevelUpPanel(§9)이 아직 없어 여기서는 감지만 하고, 소비는 해당 챕터 구현 시 이 프로퍼티를 폴링한다.
    public bool PauseTogglePressed { get; private set; }
    public bool ConfirmPressed { get; private set; }
    public int UpgradeCardHotkeyIndex { get; private set; } = -1;

    private void Awake()
    {
        if (!TryGetComponent(out _rigidbody2D))
        {
            Debug.LogError($"{nameof(PlayerController)} requires a Rigidbody2D.", this);
        }

        if (!TryGetComponent(out _spriteRenderer))
        {
            Debug.LogError($"{nameof(PlayerController)} requires a SpriteRenderer.", this);
        }
        else
        {
            _originalColor = _spriteRenderer.color;
        }

        TryGetComponent(out _spriteFrameToggler);

        _currentHP = _maxHP;

        Weapons = new PlayerWeapons(transform);
        Weapons.AddWeapon(WeaponKind.Knife);
    }

    private void Update()
    {
        Vector2 input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));

        _moveInput = input.sqrMagnitude > 1f ? input.normalized : input;

        if (_moveInput.x < 0f)
        {
            _spriteRenderer.flipX = true;
        }
        else if (_moveInput.x > 0f)
        {
            _spriteRenderer.flipX = false;
        }

        if (_spriteFrameToggler != null)
        {
            _spriteFrameToggler.SetMoving(_moveInput.sqrMagnitude > 0.0001f);
        }

        PauseTogglePressed = Input.GetKeyDown(KeyCode.Escape);
        ConfirmPressed = Input.GetKeyDown(KeyCode.Space);
        UpgradeCardHotkeyIndex = ReadUpgradeCardHotkeyIndex();

        if (PauseTogglePressed)
        {
            GameManager.Instance.TogglePause();
        }

        if (ConfirmPressed && GameManager.Instance.State == GameState.Title)
        {
            GameManager.Instance.ChangeState(GameState.Playing);
        }

        if (GameManager.Instance.State == GameState.Playing)
        {
            Weapons.Tick(Time.deltaTime);
        }
    }

    private static int ReadUpgradeCardHotkeyIndex()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
        return -1;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            _rigidbody2D.linearVelocity = Vector2.zero;
            return;
        }

        _rigidbody2D.linearVelocity = _moveInput * MoveSpeed;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        if (other.TryGetComponent(out IPickup pickup))
        {
            pickup.OnPickup(this);
        }
    }

    public void AddExp(int amount)
    {
        _currentExp += amount;

        bool leveledUp = false;
        while (_currentExp >= ExpToNext)
        {
            _currentExp -= ExpToNext;
            CurrentLevel++;
            leveledUp = true;
        }

        if (leveledUp)
        {
            GameManager.Instance.ChangeState(GameState.LevelUpPaused);
        }
    }

    public void ApplyStat(StatKind type, float amount)
    {
        switch (type)
        {
            case StatKind.MaxHP:
                _maxHPBonus += Mathf.RoundToInt(amount);
                _currentHP += Mathf.RoundToInt(amount);
                break;
            case StatKind.MoveSpeed:
                _moveSpeedBonusRate += amount;
                break;
            case StatKind.PickupRadius:
                _pickupRadiusBonusRate += amount;
                break;
        }
    }

    // 누적 EXP 표(§6): L2=5, L3=12(+7), L4=22(+10), L5=35(+13), L6=50(+15), L7+=직전+5.
    private static int ExpToNextForLevel(int level)
    {
        switch (level)
        {
            case 1: return 5;
            case 2: return 7;
            case 3: return 10;
            case 4: return 13;
            case 5: return 15;
            default: return 5 * level - 10; // L6부터: 20, 25, 30 ...
        }
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive || Time.time < _invincibleUntil)
        {
            return;
        }

        _currentHP = Mathf.Max(_currentHP - amount, 0);
        _invincibleUntil = Time.time + _invincibilityDuration;

        if (_hitFlashCoroutine != null)
        {
            StopCoroutine(_hitFlashCoroutine);
        }
        _hitFlashCoroutine = StartCoroutine(HitFlashRoutine());

        if (!IsAlive)
        {
            GameManager.Instance.ChangeState(GameState.GameOver);
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        _spriteRenderer.color = _hitFlashColor;
        yield return new WaitForSeconds(_hitFlashDuration);
        _spriteRenderer.color = _originalColor;
        _hitFlashCoroutine = null;
    }
}
