using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 간단한 메시지 팝업. Show()로 띄우고, 확인 버튼 클릭 시 등록한 콜백을 실행한다.
/// UI 작동만 담당 (표시/숨김), 무엇을 보여줄지는 호출하는 쪽(AuthController 등)이 결정.
/// </summary>
public class UIPopup : MonoBehaviour
{
    public enum PopupType
    {
        Alarm,     // 흰색 - 단순 알림
        Success,   // 초록색 - 성공
        Error,     // 빨간색 - 에러
    }

    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _confirmButton;

    Action _onConfirm;

    void Awake()
    {
        _confirmButton.onClick.AddListener(OnClickConfirm);
        _panel.SetActive(false);
    }

    public void Show(string message, PopupType type = PopupType.Alarm, Action onConfirm = null)
    {
        _messageText.text = message;
        _messageText.color = GetColorByType(type);
        _onConfirm = onConfirm;
        _panel.SetActive(true);
    }

    Color GetColorByType(PopupType type)
    {
        switch (type)
        {
            case PopupType.Success:
                return Color.green;
            case PopupType.Error:
                return Color.red;
            case PopupType.Alarm:
            default:
                return Color.white;
        }
    }

    void OnClickConfirm()
    {
        _panel.SetActive(false);
        Action callback = _onConfirm;
        _onConfirm = null;
        callback?.Invoke();
    }
}