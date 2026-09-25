using UnityEngine;
using UnityEngine.UI;

public class PlayerHpBar : MonoBehaviour
{
    [SerializeField] private Image _fillImage;

    private void Awake()
    {
        if (_fillImage == null)
        {
            Debug.LogError($"{nameof(PlayerHpBar)}: _fillImage가 연결되지 않았습니다.", this);
        }
    }

    private void Update()
    {
        PlayerController player = GameManager.Instance.Player;
        if (player == null || _fillImage == null)
        {
            return;
        }

        _fillImage.fillAmount = Mathf.Clamp01((float)player.CurrentHP / player.MaxHP);
    }
}
