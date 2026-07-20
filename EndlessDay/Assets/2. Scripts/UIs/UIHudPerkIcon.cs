using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>HUD 강화 아이콘 줄에 표시되는 아이콘 하나. 클릭은 없고, 호버하면 툴팁으로 이름/설명을 보여줌.</summary>
public class UIHudPerkIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image _icon;
    [SerializeField] TextMeshProUGUI _stackText;

    string _perkName;
    string _description;

    public void SetContent(PerkData perk, int stackCount)
    {
        _perkName = perk.PerkName;
        _description = perk.Description;

        if (!string.IsNullOrEmpty(perk.IconPath))
        {
            Sprite sprite = Resources.Load<Sprite>(perk.IconPath);
            _icon.sprite = sprite;
        }
        // Image.enabled는 항상 true로 유지 - false로 두면 마우스 호버 감지(Raycast)까지 막혀버림

        _stackText.text = stackCount > 1 ? "x" + stackCount : "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UITooltip._instance.Show(_perkName, _description, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UITooltip._instance.Hide();
    }
}