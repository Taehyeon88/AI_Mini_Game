using UnityEngine;

public class LevelUpPanel : Singleton<LevelUpPanel>
{
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private UpgradeCardUI[] _cards;

    private PlayerController _player;

    protected override void Awake()
    {
        base.Awake();

        _player = FindFirstObjectByType<PlayerController>();
        if (_player == null)
        {
            Debug.LogError($"{nameof(LevelUpPanel)}: PlayerController를 씬에서 찾지 못했습니다.", this);
        }
    }

    private void Start()
    {
        // OnEnable은 다른 오브젝트의 Awake(GameManager.Instance 초기화)보다 먼저 실행될 수 있어
        // 씬의 모든 Awake가 끝난 뒤 호출이 보장되는 Start에서 구독한다.
        GameManager.Instance.OnStateChanged += HandleStateChanged;
        HandleStateChanged(GameManager.Instance.State);
    }

    private void OnDisable()
    {
        GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        bool isLevelUp = state == GameState.LevelUpPaused;
        _panelRoot.SetActive(isLevelUp);

        if (isLevelUp)
        {
            ShowCards();
        }
    }

    private void ShowCards()
    {
        UpgradeData[] picks = UpgradeCardSelector.Shuffle3(_player);

        for (int i = 0; i < _cards.Length; i++)
        {
            int index = i;
            _cards[i].Bind(picks[i], () => Select(index));
        }
    }

    public void Select(int index)
    {
        if (GameManager.Instance.State != GameState.LevelUpPaused)
        {
            return;
        }

        if (index < 0 || index >= _cards.Length)
        {
            return;
        }

        UpgradeData data = _cards[index].Data;
        if (data == null)
        {
            return;
        }

        UpgradeService.Apply(data, _player);
        GameManager.Instance.ChangeState(GameState.Playing);
    }
}
