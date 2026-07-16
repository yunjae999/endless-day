using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ShopSlotContext
{
    Buy,
    Sell,
}

/// <summary>
/// 상점 슬롯 하나 (구매 목록 칸 또는 보유 아이템 칸 공용).
/// 클릭하면 컨트롤러에 "이 아이템 선택됨"을 알리고, 호버하면 툴팁을 띄운다.
/// 실제로 살지 팔지 판단은 하지 않고, 선택 사실만 컨트롤러에게 전달한다.
/// </summary>
public class UIShopSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image _icon;
    [SerializeField] TextMeshProUGUI _quantityText;   // 보유 아이템 칸에서만 사용, 상점 목록 칸은 항상 빈 문자열
    [SerializeField] GameObject _selectedHighlight;   // 선택됐을 때 켜지는 테두리 등

    int _itemId;
    string _itemName;
    string _description;
    int _price;
    bool _hasItem;

    UIShopController _controller;
    ShopSlotContext _context;

    public int ItemId => _itemId;
    public ShopSlotContext Context => _context;

    public void Init(UIShopController controller, ShopSlotContext context)
    {
        _controller = controller;
        _context = context;
        SetSelected(false);
    }

    public void SetContent(int itemId, string itemName, string description, int price, Sprite icon, int quantity = 0)
    {
        _hasItem = true;
        _itemId = itemId;
        _itemName = itemName;
        _description = description;
        _price = price;

        _icon.sprite = icon;
        _icon.enabled = true;
        _quantityText.text = quantity > 1 ? quantity.ToString() : "";
    }

    public void SetEmpty()
    {
        _hasItem = false;
        _itemId = 0;
        _icon.sprite = null;
        _icon.enabled = false;
        _quantityText.text = "";
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (_selectedHighlight != null)
            _selectedHighlight.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_hasItem)
            return;

        _controller.OnSlotClicked(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_hasItem)
            return;

        UITooltip._instance.Show(_itemName, _description, eventData.position, _price + " G");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UITooltip._instance.Hide();
    }
}