using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 슬롯에 마우스를 올렸을 때 뜨는 툴팁. Canvas 최상단에 하나만 두고 인벤토리/상점 슬롯이 공유한다.
/// 어떤 아이템인지는 모르고, "이 텍스트를 이 위치 근처에 보여줘/숨겨줘"만 담당.
/// </summary>
public class UITooltip : MonoBehaviour
{
    public static UITooltip _instance { get; private set; }

    [SerializeField] RectTransform _rect;
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _descriptionText;
    [SerializeField] TextMeshProUGUI _priceText;   // 상점이 아니면 빈 문자열로 넘기면 됨

    Vector2  _positionOffset = new Vector2(0f, -30f);
    void Awake()
    {
        _instance = this;
        Hide();
    }

    public void Show(string itemName, string description, Vector2 screenPosition, string priceText = "")
    {
        _nameText.text = itemName;
        _descriptionText.text = description;

        bool hasPrice = !string.IsNullOrEmpty(priceText);
        _priceText.gameObject.SetActive(hasPrice);
        if (hasPrice)
            _priceText.text = priceText;

        _rect.position = screenPosition + _positionOffset;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}