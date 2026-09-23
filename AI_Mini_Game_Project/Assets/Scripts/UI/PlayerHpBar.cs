using UnityEngine;
using UnityEngine.UI;

public class PlayerHpBar : MonoBehaviour
{
    [SerializeField] private PlayerController _player;
    [SerializeField] private Image _fillImage;

    private void Awake()
    {
        if (_player == null)
        {
            Debug.LogError($"{nameof(PlayerHpBar)}: _player가 연결되지 않았습니다.", this);
        }

        if (_fillImage == null)
        {
            Debug.LogError($"{nameof(PlayerHpBar)}: _fillImage가 연결되지 않았습니다.", this);
        }
    }

    private void Update()
    {
        if (_player == null || _fillImage == null)
        {
            return;
        }

        _fillImage.fillAmount = Mathf.Clamp01((float)_player.CurrentHP / _player.MaxHP);
    }
}
