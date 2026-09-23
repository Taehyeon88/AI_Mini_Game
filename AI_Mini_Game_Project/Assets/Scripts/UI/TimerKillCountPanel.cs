using TMPro;
using UnityEngine;

public class TimerKillCountPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _killCountText;

    private void Update()
    {
        int totalSeconds = Mathf.CeilToInt(GameManager.Instance.RemainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        _timerText.text = $"{minutes:00}:{seconds:00}";

        _killCountText.text = $"처치 {GameManager.Instance.KillCount}";
    }
}
