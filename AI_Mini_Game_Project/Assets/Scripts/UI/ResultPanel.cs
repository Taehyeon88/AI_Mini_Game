using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultPanel : Singleton<ResultPanel>
{
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _survivalTimeText;
    [SerializeField] private TMP_Text _killCountText;
    [SerializeField] private TMP_Text _finalLevelText;
    [SerializeField] private Button _restartButton;

    protected override void Awake()
    {
        base.Awake();

        _restartButton.onClick.AddListener(HandleRestartClicked);
    }

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
        bool isResult = state == GameState.GameOver || state == GameState.Clear;
        _panelRoot.SetActive(isResult);

        if (isResult)
        {
            ShowResult(state);
        }
    }

    private void ShowResult(GameState state)
    {
        _titleText.text = state == GameState.Clear ? "클리어" : "게임 오버";

        int totalSeconds = Mathf.FloorToInt(GameManager.Instance.ElapsedTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        _survivalTimeText.text = $"생존 시간 {minutes:00}:{seconds:00}";

        _killCountText.text = $"처치 수 {GameManager.Instance.KillCount}";
        _finalLevelText.text = $"최종 레벨 {GameManager.Instance.Player.CurrentLevel}";
    }

    private void HandleRestartClicked()
    {
        // LoadScene은 Time.timeScale(엔진 전역 값)을 초기화하지 않는다.
        // ESC(TogglePause)로 timeScale=0인 채 결과 화면에 진입하는 경로는
        // 현재 코드상 발생하지 않지만(모든 피해·처치 로직이 State==Playing 가드),
        // 재시작 코드가 그 가정에 암묵적으로 의존하지 않도록 방어적으로 복원한다.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
