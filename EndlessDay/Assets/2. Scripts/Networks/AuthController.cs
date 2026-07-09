using UnityEngine;
using ErrorCode;

/// <summary>
/// 타이틀/로그인 UI와 NetworkManager를 연결하는 컨트롤러.
/// - 버튼 클릭 → NetworkManager로 요청 전송
/// - NetworkManager 이벤트 → UI 상태 반영 (에러 메시지, 팝업, 패널 전환)
/// </summary>
public class AuthController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UITitlePanel _titlePanel;
    [SerializeField] private UILoginPanel _loginPanel;
    [SerializeField] private UIPopup _popup;

    void Start()
    {
        // 타이틀 버튼
        _titlePanel.StartGameButton.onClick.AddListener(OnClickStartGame);

        // 로그인 패널 버튼
        _loginPanel.CheckDuplicateButton.onClick.AddListener(OnClickCheckDuplicate);
        _loginPanel.SignupButton.onClick.AddListener(OnClickSignup);
        _loginPanel.LoginButton.onClick.AddListener(OnClickLogin);

        // 네트워크 이벤트 구독
        NetworkManager._instance.OnConnected += OnServerConnected;
        NetworkManager._instance.OnCheckUsernameOK += OnCheckUsernameOK;
        NetworkManager._instance.OnCheckUsernameFail += OnCheckUsernameFail;
        NetworkManager._instance.OnRegisterOK += OnRegisterOK;
        NetworkManager._instance.OnRegisterFail += OnRegisterFail;
        NetworkManager._instance.OnLoginOK += OnLoginOK;
        NetworkManager._instance.OnLoginFail += OnLoginFail;
    }

    // ─────────────────────────────────────────────
    // 타이틀 → 서버 접속 시도
    // ─────────────────────────────────────────────

    void OnClickStartGame()
    {
        bool connected = NetworkManager._instance.Connect();
        if (!connected)
        {
            _popup.Show("서버 접속에 실패했습니다.", UIPopup.PopupType.Error);
            return;
        }
        // 소켓 연결 자체는 성공, 서버의 ConnectOK 신호(OnConnected)가 올 때까지 대기
    }

    void OnServerConnected()
    {
        // 타이틀 패널은 그대로 두고 로그인 패널만 띄움
        _loginPanel.gameObject.SetActive(true);
    }

    // ─────────────────────────────────────────────
    // 회원가입
    // ─────────────────────────────────────────────

    void OnClickCheckDuplicate()
    {
        string username = _loginPanel.GetSignupID();
        NetworkManager._instance.SendCheckUsername(username);
    }

    void OnCheckUsernameOK()
    {
        _loginPanel.ShowDuplicateResult(true, "사용 가능한 아이디입니다.");
        _loginPanel.SetNicknameInteractable(true);
    }

    void OnCheckUsernameFail()
    {
        _loginPanel.ShowDuplicateResult(false, "이미 사용 중인 아이디입니다.");
        _loginPanel.SetNicknameInteractable(false);
    }

    void OnClickSignup()
    {
        string username = _loginPanel.GetSignupID();
        string password = _loginPanel.GetSignupPassword();
        string passwordConfirm = _loginPanel.GetSignupPasswordConfirm();
        string nickname = _loginPanel.GetSignupNickname();

        if (password != passwordConfirm)
        {
            _loginPanel.ShowSignupError("비밀번호가 일치하지 않습니다.");
            return;
        }

        NetworkManager._instance.SendRegister(username, password, nickname);
    }

    void OnRegisterOK()
    {
        _loginPanel.OnClickTabLogin();
        _popup.Show("회원가입에 성공했습니다. \n로그인 해주세요.", UIPopup.PopupType.Success);
    }

    void OnRegisterFail(int reasonCode)
    {
        RegisterFailReason reason = (RegisterFailReason)reasonCode;
        string message;

        switch (reason)
        {
            case RegisterFailReason.InvalidUsername:
                message = "아이디 형식이 올바르지 않습니다.";
                break;
            case RegisterFailReason.InvalidPassword:
                message = "비밀번호 형식이 올바르지 않습니다.";
                break;
            case RegisterFailReason.DuplicateUsername:
                message = "이미 사용 중인 아이디입니다.";
                break;
            default:
                message = "회원가입에 실패했습니다.";
                break;
        }

        _loginPanel.ShowSignupError(message);
    }

    // ─────────────────────────────────────────────
    // 로그인
    // ─────────────────────────────────────────────

    void OnClickLogin()
    {
        string username = _loginPanel.GetLoginID();
        string password = _loginPanel.GetLoginPassword();
        NetworkManager._instance.SendLogin(username, password);
    }

    void OnLoginOK(LoginResultData data)
    {
        // TODO : VillageScene 전환 (씬 준비되면 여기서 처리)
        _popup.Show("로그인에 성공했습니다. (" + data.Nickname + ")", UIPopup.PopupType.Success);
    }

    void OnLoginFail(int reasonCode)
    {
        LoginFailReason reason = (LoginFailReason)reasonCode;
        string message;

        switch (reason)
        {
            case LoginFailReason.UserNotFound:
                message = "존재하지 않는 아이디입니다.";
                break;
            case LoginFailReason.WrongPassword:
                message = "비밀번호가 일치하지 않습니다.";
                break;
            default:
                message = "로그인에 실패했습니다.";
                break;
        }

        _loginPanel.ShowLoginError(message);
    }
}