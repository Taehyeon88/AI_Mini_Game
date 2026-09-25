using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelExpBar : MonoBehaviour
{
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private Image _expFillImage;

    private void Awake()
    {
        if (_levelText == null)
        {
            Debug.LogError($"{nameof(LevelExpBar)}: _levelText가 연결되지 않았습니다.", this);
        }

        if (_expFillImage == null)
        {
            Debug.LogError($"{nameof(LevelExpBar)}: _expFillImage가 연결되지 않았습니다.", this);
        }
    }

    private void Update()
    {
        PlayerController player = GameManager.Instance.Player;
        if (player == null || _levelText == null || _expFillImage == null)
        {
            return;
        }

        _levelText.text = $"Lv.{player.CurrentLevel}";
        _expFillImage.fillAmount = Mathf.Clamp01((float)player.CurrentExp / player.ExpToNext);
    }
}
