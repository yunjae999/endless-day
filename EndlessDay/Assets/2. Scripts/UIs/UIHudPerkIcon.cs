using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD 강화 아이콘 줄에 표시되는 아이콘 하나. 클릭 등 상호작용 없음, 표시만.</summary>
public class UIHudPerkIcon : MonoBehaviour
{
    [SerializeField] Image _icon;
    [SerializeField] TextMeshProUGUI _stackText;

    public void SetContent(PerkData perk, int stackCount)
    {
        if (!string.IsNullOrEmpty(perk.IconPath))
        {
            Sprite sprite = Resources.Load<Sprite>(perk.IconPath);
            _icon.sprite = sprite;
            _icon.enabled = sprite != null;
        }

        _stackText.text = stackCount > 1 ? "x" + stackCount : "";
    }
}
