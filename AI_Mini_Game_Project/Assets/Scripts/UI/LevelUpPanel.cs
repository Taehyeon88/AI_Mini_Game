using UnityEngine;

public class LevelUpPanel : Singleton<LevelUpPanel>
{
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private UpgradeCardUI[] _cards;

    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
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
        UpgradeData[] picks = UpgradeCardSelector.Shuffle3(GameManager.Instance.Player);

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

        UpgradeService.Apply(data, GameManager.Instance.Player);
        GameManager.Instance.ChangeState(GameState.Playing);
    }
}
