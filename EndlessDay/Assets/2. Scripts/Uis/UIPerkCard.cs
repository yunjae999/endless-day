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
        { 2, new Color(0.35f, 0.55f, 1f) },     // 파랑
        { 3, new Color(0.65f, 0.35f, 0.95f) },  // 보라
        { 4, new Color(1f, 0.84f, 0.2f) },      // 노랑
    };

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

        _gradeText.text = gradeName;
        _gradeText.color = gradeColor;
        _nameText.color = gradeColor;   // 이름도 등급색으로 - 카드가 한눈에 구분되게
    }

    void OnClick()
    {
        _controller.OnCardClicked(_perkId);
    }
}