using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeCardUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descText;
    [SerializeField] private Button _button;

    public UpgradeData Data { get; private set; }

    public void Bind(UpgradeData data, Action onSelect)
    {
        Data = data;
        gameObject.SetActive(data != null);

        if (data == null)
        {
            return;
        }

        _icon.sprite = data.Icon;
        _nameText.text = data.DisplayName;
        _descText.text = data.Description;

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onSelect());
    }
}
