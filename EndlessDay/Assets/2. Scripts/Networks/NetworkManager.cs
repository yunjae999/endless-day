using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using DefinePacket;
using ClientServerProtocol;

/// <summary>
/// 서버와의 소켓 통신을 전담하는 매니저.
/// UI를 직접 참조하지 않고, 이벤트(콜백)로 결과를 알림 → AuthController 등이 구독해서 사용.
/// </summary>
public class NetworkManager : MonoBehaviour
{
    [Header("Server Info")]
    public string _serverIP = "127.0.0.1";
    public int _serverPort = 7777;

    Socket _socket;

    Thread _receiveLoop;
    Thread _sendLoop;

    Queue<byte[]> _receiveQueue;
    Queue<byte[]> _sendQueue;

    public bool IsConnected { get; private set; }
    public bool IsLoggedIn { get; private set; }

    bool _isQuit = false;
    bool _isDisconnected = false;

    public static NetworkManager _instance { get; private set; }

    // 인벤토리는 개수 먼저 온 뒤 항목이 여러 번 나눠 오므로, 다 받을 때까지 누적
    List<InventoryItemData> _pendingInventoryItems = new List<InventoryItemData>();
    int _expectedInventoryCount = 0;

    // ─────────────────────────────────────────────
    // 이벤트 (AuthController 등이 구독)
    // ─────────────────────────────────────────────

    public event Action OnConnected;
    public event Action OnDisconnectedEvent;

    public event Action OnCheckUsernameOK;
    public event Action OnCheckUsernameFail;

    public event Action OnRegisterOK;
    public event Action<int> OnRegisterFail;   // int : ErrorCode.RegisterFailReason

    public event Action<LoginResultData> OnLoginOK;
    public event Action<int> OnLoginFail;      // int : ErrorCode.LoginFailReason

    public event Action<List<InventoryItemData>> OnInventoryLoaded;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _receiveQueue = new Queue<byte[]>();
        _sendQueue = new Queue<byte[]>();
    }

    // ─────────────────────────────────────────────
    // 연결
    // ─────────────────────────────────────────────

    public bool Connect()
    {
        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(new IPEndPoint(IPAddress.Parse(_serverIP), _serverPort));
            IsConnected = true;
            Debug.Log("[Network] 서버 연결 성공.");

            _receiveLoop = new Thread(ReceiveProc);
            _sendLoop = new Thread(SendProc);
            _receiveLoop.Start();
            Thread.Sleep(100);
            _sendLoop.Start();
            return true;
        }
        catch (Exception ex)
        {
            IsConnected = false;
            Debug.LogError("[Network] 서버 연결 실패 : " + ex.Message);
            return false;
        }
    }

    // ─────────────────────────────────────────────
    // 수신 / 송신 스레드
    // ─────────────────────────────────────────────

    void ReceiveProc()
    {
        while (!_isQuit)
        {
            if (_socket != null && _socket.Poll(0, SelectMode.SelectRead))
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int sizeLen = _socket.Receive(buffer);
                    if (sizeLen > 0)
                    {
                        lock (_receiveQueue)
                            _receiveQueue.Enqueue(buffer);
                    }
                    else
                    {
                        Debug.Log("[Network] 서버 연결 끊김.");
                        IsConnected = false;
                        IsLoggedIn = false;
                        _socket = null;
                        _isDisconnected = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[Network] 수신 실패 : " + ex.Message);
                    IsConnected = false;
                    IsLoggedIn = false;
                    _socket = null;
                    _isDisconnected = true;
                }
            }
        }
    }

    void SendProc()
    {
        while (!_isQuit)
        {
            lock (_sendQueue)
            {
                while (_sendQueue.Count > 0)
                {
                    byte[] data = _sendQueue.Dequeue();
                    if (_socket != null)
                    {
                        try
                        {
                            _socket.Send(data);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("[Network] 송신 실패 : " + ex.Message);
                            IsConnected = false;
                            _socket = null;
                        }
                    }
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    // 메인 스레드에서 패킷 처리
    // ─────────────────────────────────────────────

    void Update()
    {
        if (_isDisconnected)
        {
            _isDisconnected = false;
            OnDisconnected();
        }

        lock (_receiveQueue)
        {
            while (_receiveQueue.Count > 0)
            {
                byte[] buffer = _receiveQueue.Dequeue();
                Packet packet = (Packet)ConvertPacket.ToStruct(buffer, typeof(Packet));
                ProcessPacket(packet);
            }
        }
    }

    void ProcessPacket(Packet packet)
    {
        switch ((ReceiveProtocol)packet._protocol)
        {
            case ReceiveProtocol.ConnectOK:
                Handle_ConnectOK(packet);
                break;

            case ReceiveProtocol.CheckUsernameOK:
                Debug.Log("[Network] 아이디 사용 가능.");
                OnCheckUsernameOK?.Invoke();
                break;

            case ReceiveProtocol.CheckUsernameFail:
                Debug.Log("[Network] 아이디 중복.");
                OnCheckUsernameFail?.Invoke();
                break;

            case ReceiveProtocol.RegisterOK:
                Debug.Log("[Network] 회원가입 성공.");
                OnRegisterOK?.Invoke();
                break;

            case ReceiveProtocol.RegisterFail:
                Handle_RegisterFail(packet);
                break;

            case ReceiveProtocol.LoginOK:
                Handle_LoginOK(packet);
                break;

            case ReceiveProtocol.LoginFail:
                Handle_LoginFail(packet);
                break;

            case ReceiveProtocol.InventoryCount:
                Handle_InventoryCount(packet);
                break;

            case ReceiveProtocol.InventoryItem:
                Handle_InventoryItem(packet);
                break;

            default:
                Debug.LogWarning("[Network] 알 수 없는 프로토콜 : " + packet._protocol);
                break;
        }
    }

    // ─────────────────────────────────────────────
    // 패킷 핸들러
    // ─────────────────────────────────────────────

    void Handle_ConnectOK(Packet packet)
    {
        Connected_Info info = (Connected_Info)ConvertPacket.UnpackData(packet, typeof(Connected_Info));
        Debug.Log("[Network] 연결 OK - 소켓 ID : " + info._tempSocketId);
        OnConnected?.Invoke();
    }

    void Handle_RegisterFail(Packet packet)
    {
        Register_Fail fail = (Register_Fail)ConvertPacket.UnpackData(packet, typeof(Register_Fail));
        Debug.Log("[Network] 회원가입 실패 - 이유 코드 : " + fail._reason);
        OnRegisterFail?.Invoke(fail._reason);
    }

    void Handle_LoginFail(Packet packet)
    {
        Login_Fail fail = (Login_Fail)ConvertPacket.UnpackData(packet, typeof(Login_Fail));
        Debug.Log("[Network] 로그인 실패 - 이유 코드 : " + fail._reason);
        OnLoginFail?.Invoke(fail._reason);
    }

    void Handle_LoginOK(Packet packet)
    {
        Login_Result result = (Login_Result)ConvertPacket.UnpackData(packet, typeof(Login_Result));
        IsLoggedIn = true;
        Debug.Log("[Network] 로그인 성공 - 닉네임 : " + result._nickname);

        // 로그인 직후 서버가 이어서 인벤토리를 보내므로, 받을 준비를 미리 비워둠
        _pendingInventoryItems.Clear();
        _expectedInventoryCount = 0;

        LoginResultData data = new LoginResultData
        {
            UserId = result._userId,
            Nickname = result._nickname,
            Gold = result._gold,
            TryCount = result._tryCount,
            IsCleared = result._isCleared == 1,
            UnlockedWeapons = result._unlockedWeapons,
            EquippedEquipment = result._equippedEquipment
        };
        OnLoginOK?.Invoke(data);
    }

    void Handle_InventoryCount(Packet packet)
    {
        Inventory_Count count = (Inventory_Count)ConvertPacket.UnpackData(packet, typeof(Inventory_Count));

        _pendingInventoryItems.Clear();
        _expectedInventoryCount = count._count;
        Debug.Log("[Network] 인벤토리 개수 수신 - " + _expectedInventoryCount);

        // 아이템이 하나도 없으면 항목 패킷 자체가 안 오니, 여기서 바로 완료 처리
        if (_expectedInventoryCount == 0)
            OnInventoryLoaded?.Invoke(_pendingInventoryItems);
    }

    void Handle_InventoryItem(Packet packet)
    {
        Inventory_Item item = (Inventory_Item)ConvertPacket.UnpackData(packet, typeof(Inventory_Item));

        _pendingInventoryItems.Add(new InventoryItemData
        {
            ItemType = item._itemType,
            ItemId = item._itemId,
            Quantity = item._quantity
        });

        if (_pendingInventoryItems.Count >= _expectedInventoryCount)
        {
            Debug.Log("[Network] 인벤토리 수신 완료 - " + _pendingInventoryItems.Count + "개");
            OnInventoryLoaded?.Invoke(_pendingInventoryItems);
        }
    }

    // ─────────────────────────────────────────────
    // 송신 함수
    // ─────────────────────────────────────────────

    public void SendCheckUsername(string username)
    {
        CheckUsername_Request req = new CheckUsername_Request { _username = username };
        Packet packet = ConvertPacket.MakePacket((int)SendProtocol.CheckUsername, req);
        lock (_sendQueue)
            _sendQueue.Enqueue(ConvertPacket.ToBytes(packet));
    }

    public void SendRegister(string username, string password, string nickname)
    {
        Register_Request req = new Register_Request
        {
            _username = username,
            _password = password,
            _nickname = nickname
        };
        Packet packet = ConvertPacket.MakePacket((int)SendProtocol.Register, req);
        lock (_sendQueue)
            _sendQueue.Enqueue(ConvertPacket.ToBytes(packet));
    }

    public void SendLogin(string username, string password)
    {
        Login_Request req = new Login_Request
        {
            _username = username,
            _password = password
        };
        Packet packet = ConvertPacket.MakePacket((int)SendProtocol.Login, req);
        lock (_sendQueue)
            _sendQueue.Enqueue(ConvertPacket.ToBytes(packet));
    }

    // ─────────────────────────────────────────────
    // 종료 / 연결 끊김
    // ─────────────────────────────────────────────

    void OnApplicationQuit()
    {
        _isQuit = true;
        if (_socket != null)
        {
            _socket.Close();
            _socket = null;
        }
    }

    void OnDisconnected()
    {
        Debug.Log("[Network] 서버와 연결이 끊겼습니다.");
        OnDisconnectedEvent?.Invoke();
    }
}

/// <summary>로그인 성공 시 전달되는 데이터 (Login_Result 패킷을 Unity 쪽에서 다루기 쉬운 형태로 변환)</summary>
public class LoginResultData
{
    public int UserId;
    public string Nickname;
    public int Gold;
    public int TryCount;
    public bool IsCleared;
    public string UnlockedWeapons;     // JSON 문자열, 사용하는 쪽에서 파싱
    public string EquippedEquipment;   // JSON 문자열, 사용하는 쪽에서 파싱
}

/// <summary>인벤토리 항목 하나 (서버가 로그인 직후 보내주는 보유 목록의 각 행)</summary>
public class InventoryItemData
{
    public int ItemType;   // 1=장비, 2=소비
    public int ItemId;
    public int Quantity;
}