using UnityEngine;

/// <summary>
/// TitleFrame(Canvas)에 부착. UITitlePanel과 UILoginPanel 사이의 전환만 전담.
/// 각 패널은 서로의 존재를 모르며, 자신의 버튼만 public으로 공개한다.
/// </summary>
public class UITitleFrame : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private UITitlePanel _titlePanel;
    [SerializeField] private UILoginPanel _loginPanel;

    private void Awake()
    {
        _titlePanel.StartGameButton.onClick.AddListener(OnClickStartGame);
        _loginPanel.CloseButton.onClick.AddListener(OnClickCloseLogin);

        // 초기 상태: 타이틀 패널만 보이고, 로그인 패널은 숨김
        _titlePanel.gameObject.SetActive(true);
        _loginPanel.gameObject.SetActive(false);
    }

    private void OnClickStartGame()
    {
        _titlePanel.gameObject.SetActive(false);
        _loginPanel.gameObject.SetActive(true);
    }

    private void OnClickCloseLogin()
    {
        _loginPanel.gameObject.SetActive(false);
        _titlePanel.gameObject.SetActive(true);
    }
}