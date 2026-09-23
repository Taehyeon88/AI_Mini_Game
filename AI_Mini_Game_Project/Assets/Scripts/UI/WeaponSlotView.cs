using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _damageText;

    public void SetData(Sprite icon, int damage)
    {
        _icon.sprite = icon;
        _damageText.text = damage.ToString();
    }
}
