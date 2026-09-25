using UnityEngine;
using UnityEngine.UI;

public class BossHpBar : MonoBehaviour
{
    [SerializeField] private RectTransform _barRoot;
    [SerializeField] private Image _fillImage;
    [SerializeField] private float _shownY = 40f;
    [SerializeField] private float _hiddenY = -80f;
    [SerializeField] private float _slideSpeed = 400f;

    private IDamageable _boss;

    private void Awake()
    {
        if (_barRoot == null)
        {
            Debug.LogError($"{nameof(BossHpBar)}: _barRoot가 연결되지 않았습니다.", this);
        }

        if (_fillImage == null)
        {
            Debug.LogError($"{nameof(BossHpBar)}: _fillImage가 연결되지 않았습니다.", this);
        }
    }

    private void Start()
    {
        if (_barRoot != null)
        {
            _barRoot.anchoredPosition = new Vector2(_barRoot.anchoredPosition.x, _hiddenY);
        }
    }

    private void OnEnable()
    {
        GameManager.OnBossSpawned += HandleBossSpawned;
    }

    private void OnDisable()
    {
        GameManager.OnBossSpawned -= HandleBossSpawned;
    }

    private void HandleBossSpawned(IDamageable boss)
    {
        _boss = boss;
    }

    private void Update()
    {
        if (_barRoot == null || _fillImage == null)
        {
            return;
        }

        bool show = _boss != null && _boss.IsAlive;
        if (_boss != null)
        {
            _fillImage.fillAmount = Mathf.Clamp01((float)_boss.CurrentHP / _boss.MaxHP);
        }

        // 클리어 후 timeScale과 무관하게 슬라이드 아웃되도록 unscaledDeltaTime 사용.
        Vector2 position = _barRoot.anchoredPosition;
        float targetY = show ? _shownY : _hiddenY;
        position.y = Mathf.MoveTowards(position.y, targetY, _slideSpeed * Time.unscaledDeltaTime);
        _barRoot.anchoredPosition = position;
    }
}
