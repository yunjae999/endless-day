using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using DefinePacket;
using ClientServerProtocol;

/// <summary>
/// ¼­¹ö¿ÍÀÇ ¼ÒÄÏ Åë½ÅÀ» Àü´ãÇÏ´Â ¸Å´ÏÀú.
/// UI¸¦ Á÷Á¢ ÂüÁ¶ÇÏÁö ¾Ê°í, ÀÌº¥Æ®(ÄÝ¹é)·Î °á°ú¸¦ ¾Ë¸² ¡æ AuthController µîÀÌ ±¸µ¶ÇØ¼­ »ç¿ë.
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

    // ÀÎº¥Åä¸®´Â °³¼ö ¸ÕÀú ¿Â µÚ Ç×¸ñÀÌ ¿©·¯ ¹ø ³ª´² ¿À¹Ç·Î, ´Ù ¹ÞÀ» ¶§±îÁö ´©Àû
    List<InventoryItemData> _pendingInventoryItems = new List<InventoryItemData>();
    int _expectedInventoryCount = 0;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÀÌº¥Æ® (AuthController µîÀÌ ±¸µ¶)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    public event Action OnConnected;
    public event Action OnDisconnectedEvent;

    public event Action OnCheckUsernameOK;
    public event Action OnCheckUsernameFail;

    public event Action OnRegisterOK;
    public event Action<int> OnRegisterFail;   // int : ErrorCode.RegisterFailReason

    public event Action<LoginResultData> OnLoginOK;
    public event Action<int> OnLoginFail;      // int : ErrorCode.LoginFailReason

    public event Action<List<InventoryItemData>> OnInventoryLoaded;

    public event Action<bool, int, int> OnBuyResult;   // success, itemId, newGold
    public event Action<bool, int, int> OnSellResult;  // success, itemId, newGold

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

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¿¬°á
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    public bool Connect()
    {
        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(new IPEndPoint(IPAddress.Parse(_serverIP), _serverPort));
            IsConnected = true;
            Debug.Log("[Network] ¼­¹ö ¿¬°á ¼º°ø.");

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
            Debug.LogError("[Network] ¼­¹ö ¿¬°á ½ÇÆÐ : " + ex.Message);
            return false;
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¼ö½Å / ¼Û½Å ½º·¹µå
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

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
                        Debug.Log("[Network] ¼­¹ö ¿¬°á ²÷±è.");
                        IsConnected = false;
                        IsLoggedIn = false;
                        _socket = null;
                        _isDisconnected = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[Network] ¼ö½Å ½ÇÆÐ : " + ex.Message);
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
                            Debug.LogError("[Network] ¼Û½Å ½ÇÆÐ : " + ex.Message);
                            IsConnected = false;
                            _socket = null;
                        }
                    }
                }
            }
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¸ÞÀÎ ½º·¹µå¿¡¼­ ÆÐÅ¶ Ã³¸®
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

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
                Debug.Log("[Network] ¾ÆÀÌµð »ç¿ë °¡´É.");
                OnCheckUsernameOK?.Invoke();
                break;

            case ReceiveProtocol.CheckUsernameFail:
                Debug.Log("[Network] ¾ÆÀÌµð Áßº¹.");
                OnCheckUsernameFail?.Invoke();
                break;

            case ReceiveProtocol.RegisterOK:
                Debug.Log("[Network] È¸¿ø°¡ÀÔ ¼º°ø.");
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

            case ReceiveProtocol.BuyResult:
                Handle_BuyResult(packet);
                break;

            case ReceiveProtocol.SellResult:
                Handle_SellResult(packet);
                break;

            default:
                Debug.LogWarning("[Network] ¾Ë ¼ö ¾ø´Â ÇÁ·ÎÅäÄÝ : " + packet._protocol);
                break;
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÆÐÅ¶ ÇÚµé·¯
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    void Handle_ConnectOK(Packet packet)
    {
        Connected_Info info = (Connected_Info)ConvertPacket.UnpackData(packet, typeof(Connected_Info));
        Debug.Log("[Network] ¿¬°á OK - ¼ÒÄÏ ID : " + info._tempSocketId);
        OnConnected?.Invoke();
    }

    void Handle_RegisterFail(Packet packet)
    {
        Register_Fail fail = (Register_Fail)ConvertPacket.UnpackData(packet, typeof(Register_Fail));
        Debug.Log("[Network] È¸¿ø°¡ÀÔ ½ÇÆÐ - ÀÌÀ¯ ÄÚµå : " + fail._reason);
        OnRegisterFail?.Invoke(fail._reason);
    }

    void Handle_LoginFail(Packet packet)
    {
        Login_Fail fail = (Login_Fail)ConvertPacket.UnpackData(packet, typeof(Login_Fail));
        Debug.Log("[Network] ·Î±×ÀÎ ½ÇÆÐ - ÀÌÀ¯ ÄÚµå : " + fail._reason);
        OnLoginFail?.Invoke(fail._reason);
    }

    void Handle_LoginOK(Packet packet)
    {
        Login_Result result = (Login_Result)ConvertPacket.UnpackData(packet, typeof(Login_Result));
        IsLoggedIn = true;
        Debug.Log("[Network] ·Î±×ÀÎ ¼º°ø - ´Ð³×ÀÓ : " + result._nickname);

        // ·Î±×ÀÎ Á÷ÈÄ ¼­¹ö°¡ ÀÌ¾î¼­ ÀÎº¥Åä¸®¸¦ º¸³»¹Ç·Î, ¹ÞÀ» ÁØºñ¸¦ ¹Ì¸® ºñ¿öµÒ
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
        Debug.Log("[Network] ÀÎº¥Åä¸® °³¼ö ¼ö½Å - " + _expectedInventoryCount);

        // ¾ÆÀÌÅÛÀÌ ÇÏ³ªµµ ¾øÀ¸¸é Ç×¸ñ ÆÐÅ¶ ÀÚÃ¼°¡ ¾È ¿À´Ï, ¿©±â¼­ ¹Ù·Î ¿Ï·á Ã³¸®
        if (_expectedInventoryCount == 0)
            OnInventoryLoaded?.Invoke(_pendingInventoryItems);
    }

    void Handle_InventoryItem(Packet packet)
    {
        Inventory_Item item = (Inventory_Item)ConvertPacket.UnpackData(packet, typeof(Inventory_Item));

        _pendingInventoryItems.Add(new InventoryItemData
        {
            SlotIndex = item._slotIndex,
            ItemType = item._itemType,
            ItemId = item._itemId,
            Quantity = item._quantity
        });

        if (_pendingInventoryItems.Count >= _expectedInventoryCount)
        {
            Debug.Log("[Network] ÀÎº¥Åä¸® ¼ö½Å ¿Ï·á - " + _pendingInventoryItems.Count + "°³");
            OnInventoryLoaded?.Invoke(_pendingInventoryItems);
        }
    }

    void Handle_BuyResult(Packet packet)
    {
        Shop_Trade_Result result = (Shop_Trade_Result)ConvertPacket.UnpackData(packet, typeof(Shop_Trade_Result));
        Debug.Log("[Network] ±¸¸Å °á°ú - " + (result._result == 1 ? "¼º°ø" : "½ÇÆÐ") + ", °ñµå : " + result._newGold);
        OnBuyResult?.Invoke(result._result == 1, result._itemId, result._newGold);
    }

    void Handle_SellResult(Packet packet)
    {
        Shop_Trade_Result result = (Shop_Trade_Result)ConvertPacket.UnpackData(packet, typeof(Shop_Trade_Result));
        Debug.Log("[Network] ÆÇ¸Å °á°ú - " + (result._result == 1 ? "¼º°ø" : "½ÇÆÐ") + ", °ñµå : " + result._newGold);
        OnSellResult?.Invoke(result._result == 1, result._itemId, result._newGold);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ¼Û½Å ÇÔ¼ö
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

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

    public void SendBuyItem(int itemId)
    {
        Shop_Buy_Request req = new Shop_Buy_Request { _itemId = itemId };
        Packet packet = ConvertPacket.MakePacket((int)SendProtocol.BuyItem, req);
        lock (_sendQueue)
            _sendQueue.Enqueue(ConvertPacket.ToBytes(packet));
    }

    public void SendSellItem(int itemId)
    {
        Shop_Sell_Request req = new Shop_Sell_Request { _itemId = itemId };
        Packet packet = ConvertPacket.MakePacket((int)SendProtocol.SellItem, req);
        lock (_sendQueue)
            _sendQueue.Enqueue(ConvertPacket.ToBytes(packet));
    }

    /// <summary>ÀÎº¥Åä¸® Ã¢ ´ÝÀ» ¶§ È£Ãâ - À§Ä¡ Æ÷ÇÔÇØ¼­ ÀüÃ¼¸¦ ÇÑ ¹ø¿¡ ÀúÀå ¿äÃ» (ÀÀ´äÀº ¾È ±â´Ù¸²)</summary>
    public void SendSaveInventory(string itemsJson, string equippedJson)
    {
        SaveInventory_Request req = new SaveInventory_Request
        {
            _itemsJson = itemsJson,
            _equippedJson = equippedJson
        };
        Packet packet = ConvertPacket.MakePacket((int)SendProtocol.SaveInventory, req);
        lock (_sendQueue)
            _sendQueue.Enqueue(ConvertPacket.ToBytes(packet));
    }

    /// <summary>°á°úÃ¢ È®ÀÎ ¹öÆ° ´©¸¦ ¶§ È£Ãâ - ÃÖÁ¾ °ñµå¿Í Å¬¸®¾î ¿©ºÎ ÀúÀå ¿äÃ» (ÀÀ´äÀº ¾È ±â´Ù¸²)</summary>
    public void SendSaveDungeonResult(int gold, bool isCleared)
    {
        SaveDungeonResult_Request req = new SaveDungeonResult_Request
        {
            _gold = gold,
            _isCleared = isCleared ? 1 : 0
        };
        Packet packet = ConvertPacket.MakePacket((int)SendProtocol.SaveDungeonResult, req);
        lock (_sendQueue)
            _sendQueue.Enqueue(ConvertPacket.ToBytes(packet));
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Á¾·á / ¿¬°á ²÷±è
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

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
        Debug.Log("[Network] ¼­¹ö¿Í ¿¬°áÀÌ ²÷°å½À´Ï´Ù.");
        OnDisconnectedEvent?.Invoke();
    }
}

/// <summary>·Î±×ÀÎ ¼º°ø ½Ã Àü´ÞµÇ´Â µ¥ÀÌÅÍ (Login_Result ÆÐÅ¶À» Unity ÂÊ¿¡¼­ ´Ù·ç±â ½¬¿î ÇüÅÂ·Î º¯È¯)</summary>
public class LoginResultData
{
    public int UserId;
    public string Nickname;
    public int Gold;
    public int TryCount;
    public bool IsCleared;
    public string UnlockedWeapons;     // JSON ¹®ÀÚ¿­, »ç¿ëÇÏ´Â ÂÊ¿¡¼­ ÆÄ½Ì
    public string EquippedEquipment;   // JSON ¹®ÀÚ¿­, »ç¿ëÇÏ´Â ÂÊ¿¡¼­ ÆÄ½Ì
}

/// <summary>ÀÎº¥Åä¸® Ç×¸ñ ÇÏ³ª (¼­¹ö°¡ ·Î±×ÀÎ Á÷ÈÄ º¸³»ÁÖ´Â º¸À¯ ¸ñ·ÏÀÇ °¢ Çà)</summary>
public class InventoryItemData
{
    public int SlotIndex;
    public int ItemType;   // 1=Àåºñ, 2=¼Òºñ
    public int ItemId;
    public int Quantity;
}