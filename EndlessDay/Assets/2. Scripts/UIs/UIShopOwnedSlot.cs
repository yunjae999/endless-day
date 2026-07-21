using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 상점 "보유 아이템(판매)" 슬롯 전용. 아이콘+개수만 보여준다 (이름/가격은 이미 아는 내 아이템이라 생략).
/// 클릭하면 컨트롤러에 선택을 알리고, 호버하면 이름/설명/가격 툴팁을 띄운다.
/// </summary>
public class UIShopOwnedSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image _icon;
    [SerializeField] TextMeshProUGUI _quantityText;
    [SerializeField] Image _slotBackground;
    [SerializeField] Color _normalColor = Color.white;
    [SerializeField] Color _selectedColor = Color.green;

    int _itemId;
    string _itemName;
    string _description;
    int _price;
    Sprite _iconSprite;
    bool _hasItem;

    UIShopController _controller;

    public int ItemId => _itemId;
    public string ItemName => _itemName;
    public string Description => _description;
    public int Price => _price;
    public Sprite IconSprite => _iconSprite;

    public void Init(UIShopController controller)
    {
        _controller = controller;
        SetSelected(false);
    }

    public void SetContent(int itemId, string itemName, string description, int price, Sprite icon, int quantity)
    {
        _hasItem = true;
        _itemId = itemId;
        _itemName = itemName;
        _description = description;
        _price = price;

        _icon.sprite = icon;
        _iconSprite = icon;

        _quantityText.text = quantity > 1 ? quantity.ToString() : "";
    }

    public void SetSelected(bool selected)
    {
        if (_slotBackground != null)
            _slotBackground.color = selected ? _selectedColor : _normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_hasItem)
            return;

        _controller.OnSellSlotClicked(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_hasItem)
            return;

        // 판매 쪽은 이름/가격이 슬롯에 안 보이니, 툴팁엔 다 넣어줌
        UITooltip._instance.Show(_itemName, _description, eventData.position, _price + " G");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UITooltip._instance.Hide();
    }
}
