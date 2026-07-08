using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 타이틀 씬의 로그인/회원가입 패널(UILoginPanel) UI 작동만 담당하는 스크립트.
/// - 로그인/회원가입 탭 전환
/// - 입력값 읽기, 에러/결과 메시지 표시, 필드 활성/비활성 등 UI 상태 제어
/// 유효성 검사 및 서버 통신 로직은 별도 스크립트(예: AuthController)에서
/// 이 스크립트의 public 메서드/프로퍼티를 호출하는 방식으로 연결.
/// </summary>
public class UILoginPanel : MonoBehaviour
{
    [Header("Tab Buttons")]
    [SerializeField] private Button _tabLoginButton;
    [SerializeField] private Button _tabSignupButton;

    [Header("Tab Underline Indicators")]
    [SerializeField] private GameObject _underlineLogin;
    [SerializeField] private GameObject _underlineSignup;

    [Header("Forms")]
    [SerializeField] private GameObject _loginForm;
    [SerializeField] private GameObject _signupForm;

    [Header("Login Form Fields")]
    [SerializeField] private TMP_InputField _loginIDInput;
    [SerializeField] private TMP_InputField _loginPasswordInput;
    [SerializeField] private Button _loginButton;
    [SerializeField] private TextMeshProUGUI _loginErrorText;

    [Header("Close Button")]
    [SerializeField] private Button _closeButton;

    [Header("Signup Form Fields")]
    [SerializeField] private TMP_InputField _signupIDInput;
    [SerializeField] private Button _checkDuplicateButton;
    [SerializeField] private TextMeshProUGUI _duplicateResultText;
    [SerializeField] private TMP_InputField _signupPasswordInput;
    [SerializeField] private TMP_InputField _signupPasswordConfirmInput;
    [SerializeField] private TMP_InputField _signupNicknameInput;
    [SerializeField] private Button _signupButton;
    [SerializeField] private TextMeshProUGUI _signupErrorText;

    // 외부 스크립트가 버튼 클릭에 로직을 연결할 수 있도록 참조를 공개 (읽기 전용)
    public Button LoginButton
    {
        get { return _loginButton; }
    }

    public Button SignupButton
    {
        get { return _signupButton; }
    }

    public Button CheckDuplicateButton
    {
        get { return _checkDuplicateButton; }
    }

    public Button CloseButton
    {
        get { return _closeButton; }
    }

    private void Awake()
    {
        _tabLoginButton.onClick.AddListener(OnClickTabLogin);
        _tabSignupButton.onClick.AddListener(OnClickTabSignup);

        OnClickTabLogin();
        SetNicknameInteractable(false);
        ClearAllMessages();
    }

    // ---------------- 탭 전환 (순수 UI) ----------------

    public void OnClickTabLogin()
    {
        _loginForm.SetActive(true);
        _signupForm.SetActive(false);
        _underlineLogin.SetActive(true);
        _underlineSignup.SetActive(false);
    }

    public void OnClickTabSignup()
    {
        _loginForm.SetActive(false);
        _signupForm.SetActive(true);
        _underlineLogin.SetActive(false);
        _underlineSignup.SetActive(true);
    }

    // ---------------- 입력값 읽기 ----------------

    public string GetLoginID() => _loginIDInput.text;
    public string GetLoginPassword() => _loginPasswordInput.text;

    public string GetSignupID() => _signupIDInput.text;
    public string GetSignupPassword() => _signupPasswordInput.text;
    public string GetSignupPasswordConfirm() => _signupPasswordConfirmInput.text;
    public string GetSignupNickname() => _signupNicknameInput.text;

    // ---------------- UI 상태 제어 (외부 스크립트가 호출) ----------------

    public void SetNicknameInteractable(bool interactable)
    {
        _signupNicknameInput.interactable = interactable;
    }

    public void ShowLoginError(string message)
    {
        _loginErrorText.text = message;
    }

    public void ShowSignupError(string message)
    {
        _signupErrorText.text = message;
    }

    public void ShowDuplicateResult(bool isSuccess, string message)
    {
        _duplicateResultText.color = isSuccess ? Color.green : Color.red;
        _duplicateResultText.text = message;
    }

    public void ClearAllMessages()
    {
        _loginErrorText.text = "";
        _signupErrorText.text = "";
        _duplicateResultText.text = "";
    }
}