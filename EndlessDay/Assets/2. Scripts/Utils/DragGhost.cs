using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드래그 중 마우스를 따라다니는 아이콘. Canvas 최상단에 하나만 배치해서 모든 슬롯이 공유한다.
/// 어떤 슬롯이 무슨 아이템을 드래그 중인지는 전혀 모르고, "보여줘/따라가게 해줘/숨겨줘"만 담당한다.
/// </summary>
public class DragGhost : MonoBehaviour
{
    public static DragGhost _instance { get; private set; }

    [SerializeField] Image _icon;
    RectTransform _rect;

    void Awake()
    {
        _instance = this;
        _rect = GetComponent<RectTransform>();
        Hide();
    }

    public void Show(Sprite sprite)
    {
        _icon.sprite = sprite;
        _icon.enabled = true;
        gameObject.SetActive(true);
    }

    public void MoveTo(Vector2 screenPosition)
    {
        _rect.position = screenPosition;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
