using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 상점 "구매 목록" 슬롯 전용. 아이콘+이름+가격을 항상 보여준다.
/// 클릭하면 컨트롤러에 선택을 알리고, 호버하면 설명 툴팁을 띄운다.
/// </summary>
public class UIShopSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image _icon;
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _priceText;
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

    public void SetContent(int itemId, string itemName, string description, int price, Sprite icon)
    {
        _hasItem = true;
        _itemId = itemId;
        _itemName = itemName;
        _description = description;
        _price = price;

        _icon.sprite = icon;
        _iconSprite = icon;

        _nameText.text = itemName;
        _priceText.text = price + " G";
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

        _controller.OnBuySlotClicked(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_hasItem || string.IsNullOrEmpty(_description))
            return;

        UITooltip._instance.Show(_itemName, _description, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UITooltip._instance.Hide();
    }
}