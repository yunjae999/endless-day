using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 강화 선택 카드 하나. 클릭하면 어떤 강화인지만 컨트롤러에 알리고,
/// 실제로 적용할지 판단(GameSession.ApplyPerkChoice 호출)은 컨트롤러가 한다.
/// </summary>
public class UIPerkCard : MonoBehaviour
{
    [SerializeField] Image _icon;
    [SerializeField] Image _cardBackground;
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _descriptionText;
    [SerializeField] TextMeshProUGUI _gradeText;
    [SerializeField] Button _button;

    int _perkId;
    UIPerkSelectionController _controller;

    static readonly Dictionary<int, string> _gradeNames = new Dictionary<int, string>
    {
        { 1, "일반" },
        { 2, "레어" },
        { 3, "에픽" },
        { 4, "전설" },
    };

    static readonly Dictionary<int, Color> _gradeColors = new Dictionary<int, Color>
    {
        { 1, Color.white },
        { 2, Color.blue },     // 파랑
        { 3, Color.magenta },  // 보라
        { 4, Color.yellow },      // 노랑
    };

    // 카드 기본 배경색 (등급색과 섞을 베이스) - 프로젝트 UI 톤에 맞춰 조정 가능
    static readonly Color _baseCardColor = new Color(0.16f, 0.18f, 0.21f, 1f);

    public void Init(UIPerkSelectionController controller)
    {
        _controller = controller;
        _button.onClick.AddListener(OnClick);
    }

    public void SetContent(PerkData perk)
    {
        _perkId = perk.PerkID;
        _nameText.text = perk.PerkName;
        _descriptionText.text = perk.Description;

        Color gradeColor = _gradeColors.ContainsKey(perk.Grade) ? _gradeColors[perk.Grade] : Color.white;
        string gradeName = _gradeNames.ContainsKey(perk.Grade) ? _gradeNames[perk.Grade] : "?";

        _gradeText.text = "[" + gradeName + "]";
        _gradeText.color = gradeColor;
        _nameText.color = gradeColor;   // 이름도 등급색으로 - 카드가 한눈에 구분되게

        if (_cardBackground != null)
        {
            // 알파를 낮추면 배경이 비쳐서 투명하게 보이니, 불투명 상태로 기본색과 등급색을 섞음
            Color backgroundColor = gradeColor;
            backgroundColor.a = 1f;
            _cardBackground.color = backgroundColor;
        }

        if (_icon != null)
        {
            Sprite sprite = !string.IsNullOrEmpty(perk.IconPath) ? Resources.Load<Sprite>(perk.IconPath) : null;
            _icon.sprite = sprite;
            _icon.enabled = sprite != null;
        }
    }

    void OnClick()
    {
        _controller.OnCardClicked(_perkId);
    }
}