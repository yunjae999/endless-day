using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum SlotType
{
    Inventory,
    Equip,
}

/// <summary>
/// 슬롯 하나(인벤토리 그리드 칸 또는 장착 슬롯)의 표시 + 드래그 시작/드롭 감지 + 호버 툴팁.
/// 실제로 옮길지 말지 판단은 하지 않고, 컨트롤러에게 "여기서 저기로 옮겨줘"라고 요청만 한다.
/// </summary>
public class UIInventorySlot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image _icon;
    [SerializeField] TextMeshProUGUI _quantityText;

    SlotType _slotType;
    int _slotIndex;
    UIInventoryController _controller;
    bool _hasItem;

    string _itemName;
    string _description;

    public SlotType SlotType => _slotType;
    public int SlotIndex => _slotIndex;

    /// <summary>컨트롤러가 슬롯을 생성할 때 자기 위치/종류를 알려주기 위해 호출</summary>
    public void Init(UIInventoryController controller, SlotType slotType, int slotIndex)
    {
        _controller = controller;
        _slotType = slotType;
        _slotIndex = slotIndex;
    }

    public void SetContent(Sprite icon, int quantity, string itemName = "", string description = "")
    {
        _hasItem = true;
        _icon.sprite = icon;
        _icon.enabled = true;
        _quantityText.text = quantity > 1 ? quantity.ToString() : "";
        _itemName = itemName;
        _description = description;
    }

    public void SetEmpty()
    {
        _hasItem = false;
        _icon.sprite = null;
        _icon.enabled = false;
        _quantityText.text = "";
        _itemName = "";
        _description = "";
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_hasItem)
            return;

        UITooltip._instance.Hide();   // 드래그 시작하면 툴팁은 방해되니 숨김
        DragGhost._instance.Show(_icon.sprite);
        _icon.enabled = false;   // 원래 자리는 비워 보이게 (드래그 중임을 표시)
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_hasItem)
            return;

        DragGhost._instance.MoveTo(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DragGhost._instance.Hide();
        // 드롭 성공 시엔 컨트롤러가 곧바로 SetContent/SetEmpty를 다시 호출해 갱신함.
        // 실패했다면(빈 곳에 드롭 등) 여기서 원래 아이콘을 되살림.
        _icon.enabled = _hasItem;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        UIInventorySlot sourceSlot = eventData.pointerDrag.GetComponent<UIInventorySlot>();
        if (sourceSlot == null || sourceSlot == this)
            return;

        _controller.RequestMove(sourceSlot, this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_hasItem)
            return;

        UITooltip._instance.Show(_itemName, _description, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UITooltip._instance.Hide();
    }
}