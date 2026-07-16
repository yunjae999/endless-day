using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using DefinePacket;
using BCrypt.Net;
using ErrorCode;

namespace GameServer
{
    /// <summary>접속했지만 아직 로그인하지 않은 상태</summary>
    class Guest
    {
        public int SocketId { get; set; }
    }

    /// <summary>로그인에 성공해 정식으로 관리되는 상태</summary>
    class Client
    {
        public int SocketId { get; set; }
        public int UserId { get; set; }
        public string Nickname { get; set; }
        public int Gold { get; set; }   // 로그인 시 로드, 구매/판매마다 여기서 검증+갱신 (DB는 반영만)
    }

    /// <summary>서버 메모리에 캐싱해두는 유저 정보 (로그인/중복확인에 사용, DB 왕복 없이 즉시 검증)</summary>
    class UserInfo
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Nickname { get; set; }
    }

    internal class TCPServer
    {
        short _port;

        Socket _socListen;
        List<Socket> _socketList;

        Dictionary<int, Guest> _guestList;     // key : socketId, 로그인 전
        Dictionary<int, Client> _clientList;   // key : socketId, 로그인 후

        Dictionary<string, UserInfo> _userList; // key : Username, 서버 시작 시 전체 로드 + 회원가입마다 갱신
        Dictionary<int, (int itemType, int price)> _itemPriceCache;   // key : ItemID - 서버 시작 시 전체 로드

        Queue<KeyValuePair<int, byte[]>> _recieveQueue;
        Queue<KeyValuePair<int, byte[]>> _sendQueue;

        Thread _connectLoop;
        Thread _recieveLoop;
        Thread _sendLoop;

        public bool _isQuit { get; set; }

        DBClient _dbClient;

        public TCPServer(short port, DBClient dbClient)
        {
            _port = port;
            _dbClient = dbClient;

            _socketList = new List<Socket>();
            _guestList = new Dictionary<int, Guest>();
            _clientList = new Dictionary<int, Client>();
            _userList = new Dictionary<string, UserInfo>();
            _itemPriceCache = new Dictionary<int, (int, int)>();

            _recieveQueue = new Queue<KeyValuePair<int, byte[]>>();
            _sendQueue = new Queue<KeyValuePair<int, byte[]>>();

            _connectLoop = new Thread(ConnectLoop);
            _recieveLoop = new Thread(ReceiveProc);
            _sendLoop = new Thread(SendProc);

            CreateServer();
        }

        // ─────────────────────────────────────────────
        // 유저 캐시 관리
        // ─────────────────────────────────────────────

        public void AddUserToCache(int userId, string username, string passwordHash, string nickname)
        {
            _userList[username] = new UserInfo
            {
                UserID = userId,
                Username = username,
                PasswordHash = passwordHash,
                Nickname = nickname
            };
            Console.WriteLine("[TCPServer] 유저 캐시 로드 - {0}", username);
        }

        public void AddItemPriceToCache(int itemId, int itemType, int price)
        {
            _itemPriceCache[itemId] = (itemType, price);
        }

        void CreateServer()
        {
            try
            {
                _socListen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                EndPoint ep = new IPEndPoint(IPAddress.Any, _port);
                _socListen.Bind(ep);
                _socListen.Listen(10);

                Console.WriteLine("[TCPServer] Listen 시작 - Port : {0}", _port);

                _connectLoop.Start();
                Thread.Sleep(100);
                _recieveLoop.Start();
                Thread.Sleep(100);
                _sendLoop.Start();
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[TCPServer] 생성 실패 : {0}", ex.Message);
            }
        }

        void ConnectLoop()
        {
            while (!_isQuit)
            {
                if (_socListen.Poll(0, SelectMode.SelectRead))
                {
                    Socket client = _socListen.Accept();
                    int socketId = _socketList.Count;
                    _socketList.Add(client);

                    // 접속하면 우선 Guest로 등록 (로그인 전)
                    _guestList[socketId] = new Guest { SocketId = socketId };

                    Console.WriteLine("[TCPServer] 게스트 연결 - 소켓 : {0}", socketId);

                    Connected_Info info = new Connected_Info { _tempSocketId = socketId };
                    Packet packet = ConvertPacket.MakePacket((int)ServerClientProtocol.SendProtocol.ConnectOK, info);
                    SendTo(socketId, ConvertPacket.ToBytes(packet));
                }
            }
        }

        void ReceiveProc()
        {
            while (!_isQuit)
            {
                for (int n = 0; n < _socketList.Count; n++)
                {
                    Socket now = _socketList[n];
                    if (now != null && now.Poll(0, SelectMode.SelectRead))
                    {
                        try
                        {
                            byte[] buffer = new byte[1024];
                            int sizeLen = _socketList[n].Receive(buffer);
                            if (sizeLen > 0)
                                _recieveQueue.Enqueue(new KeyValuePair<int, byte[]>(n, buffer));
                            else
                            {
                                Console.WriteLine("[TCPServer] {0}번 소켓 연결 끊김.", n);
                                _socketList[n] = null;
                                RemoveGuest(n);
                                RemoveClient(n);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[TCPServer] {0}번 수신 실패 : {1}", n, ex.Message);
                            _socketList[n] = null;
                            RemoveGuest(n);
                            RemoveClient(n);
                        }
                    }
                }
            }
        }

        void SendProc()
        {
            while (!_isQuit)
            {
                while (_sendQueue.Count > 0)
                {
                    KeyValuePair<int, byte[]> send = _sendQueue.Dequeue();
                    int targetId = send.Key;
                    if (targetId < _socketList.Count && _socketList[targetId] != null)
                    {
                        try { _socketList[targetId].Send(send.Value); }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[TCPServer] {0}번 송신 실패 : {1}", targetId, ex.Message);
                            _socketList[targetId] = null;
                        }
                    }
                }
            }
        }

        // ─────────────────────────────────────────────
        // 메인 루프
        // ─────────────────────────────────────────────

        public void MainLoop()
        {
            while (_recieveQueue.Count > 0)
            {
                KeyValuePair<int, byte[]> recv = _recieveQueue.Dequeue();
                int socketId = recv.Key;

                Packet packet = (Packet)ConvertPacket.ToStruct(recv.Value, typeof(Packet));

                switch ((ServerClientProtocol.ReceiveProtocol)packet._protocol)
                {
                    case ServerClientProtocol.ReceiveProtocol.CheckUsername:
                        Handle_CheckUsername(socketId, packet);
                        break;

                    case ServerClientProtocol.ReceiveProtocol.Register:
                        Handle_Register(socketId, packet);
                        break;

                    case ServerClientProtocol.ReceiveProtocol.Login:
                        Handle_Login(socketId, packet);
                        break;

                    case ServerClientProtocol.ReceiveProtocol.BuyItem:
                        Handle_BuyItem(socketId, packet);
                        break;

                    case ServerClientProtocol.ReceiveProtocol.SellItem:
                        Handle_SellItem(socketId, packet);
                        break;

                    default:
                        Console.WriteLine("[TCPServer] 알 수 없는 프로토콜 : {0}", packet._protocol);
                        break;
                }
            }
        }

        // ─────────────────────────────────────────────
        // 패킷 핸들러 (클라이언트 → 서버)
        // ─────────────────────────────────────────────

        void Handle_CheckUsername(int socketId, Packet packet)
        {
            CheckUsername_Request req =
                (CheckUsername_Request)ConvertPacket.UnpackData(packet, typeof(CheckUsername_Request));

            bool isDuplicate = _userList.ContainsKey(req._username);

            ServerClientProtocol.SendProtocol protocol = isDuplicate
                ? ServerClientProtocol.SendProtocol.CheckUsernameFail
                : ServerClientProtocol.SendProtocol.CheckUsernameOK;
            SendEmptyPacket(socketId, (int)protocol);

            Console.WriteLine("[TCPServer] 아이디 중복확인 - {0} : {1}", req._username, isDuplicate ? "중복" : "사용가능");
        }

        void Handle_Register(int socketId, Packet packet)
        {
            Register_Request req =
                (Register_Request)ConvertPacket.UnpackData(packet, typeof(Register_Request));

            Console.WriteLine("[TCPServer] 회원가입 요청 - {0}", req._username);

            if (!IsValidUsername(req._username))
            {
                SendRegisterFail(socketId, RegisterFailReason.InvalidUsername);
                return;
            }

            if (!IsValidPassword(req._password))
            {
                SendRegisterFail(socketId, RegisterFailReason.InvalidPassword);
                return;
            }

            if (_userList.ContainsKey(req._username))
            {
                SendRegisterFail(socketId, RegisterFailReason.DuplicateUsername);
                return;
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(req._password);
            _dbClient.RequestRegister(socketId, req._username, passwordHash, req._nickname);
        }

        void Handle_Login(int socketId, Packet packet)
        {
            Login_Request req =
                (Login_Request)ConvertPacket.UnpackData(packet, typeof(Login_Request));

            Console.WriteLine("[TCPServer] 로그인 요청 - {0}", req._username);

            if (!_userList.TryGetValue(req._username, out UserInfo user))
            {
                SendLoginFail(socketId, LoginFailReason.UserNotFound);
                Console.WriteLine("[TCPServer] 로그인 실패 - 존재하지 않는 아이디 : {0}", req._username);
                return;
            }

            if (!BCrypt.Net.BCrypt.Verify(req._password, user.PasswordHash))
            {
                SendLoginFail(socketId, LoginFailReason.WrongPassword);
                Console.WriteLine("[TCPServer] 로그인 실패 - 비밀번호 불일치 : {0}", req._username);
                return;
            }

            // 비밀번호 일치 → Guest에서 Client로 이동
            MoveGuestToClient(socketId, user.UserID, user.Nickname);

            // 골드/진행상황 등은 계속 바뀌는 값이라 캐싱하지 않고 로그인 시점마다 실시간 조회
            _dbClient.RequestGetPlayerData(socketId, user.UserID);
        }

        // ─────────────────────────────────────────────
        // 상점 - 구매/판매
        // 골드 검증은 서버 메모리(Client.Gold + _itemPriceCache)에서 즉시 처리, DB는 반영만 요청
        // ─────────────────────────────────────────────

        void Handle_BuyItem(int socketId, Packet packet)
        {
            Shop_Buy_Request req =
                (Shop_Buy_Request)ConvertPacket.UnpackData(packet, typeof(Shop_Buy_Request));

            if (!_clientList.ContainsKey(socketId))
                return;
            Client client = _clientList[socketId];

            if (!_itemPriceCache.TryGetValue(req._itemId, out var itemInfo))
            {
                SendTradeResult(socketId, ServerClientProtocol.SendProtocol.BuyResult, false, req._itemId, client.Gold);
                Console.WriteLine("[TCPServer] 구매 실패 - 존재하지 않는 아이템 : {0}", req._itemId);
                return;
            }

            if (client.Gold < itemInfo.price)
            {
                SendTradeResult(socketId, ServerClientProtocol.SendProtocol.BuyResult, false, req._itemId, client.Gold);
                Console.WriteLine("[TCPServer] 구매 실패 - 골드 부족 : {0}", client.Nickname);
                return;
            }

            // 서버 메모리에서 먼저 확정 (낙관적 갱신) - DB 실패 시 OnBuyItemResult에서 롤백
            client.Gold -= itemInfo.price;

            _dbClient.RequestBuyItem(socketId, client.UserId, itemInfo.itemType, req._itemId, client.Gold);
        }

        void Handle_SellItem(int socketId, Packet packet)
        {
            Shop_Sell_Request req =
                (Shop_Sell_Request)ConvertPacket.UnpackData(packet, typeof(Shop_Sell_Request));

            if (!_clientList.ContainsKey(socketId))
                return;
            Client client = _clientList[socketId];

            if (!_itemPriceCache.TryGetValue(req._itemId, out var itemInfo))
            {
                SendTradeResult(socketId, ServerClientProtocol.SendProtocol.SellResult, false, req._itemId, client.Gold);
                Console.WriteLine("[TCPServer] 판매 실패 - 존재하지 않는 아이템 : {0}", req._itemId);
                return;
            }

            int refund = itemInfo.price / 2;   // 판매가 = 구매가의 50%

            // 실제 보유 여부는 DB가 트랜잭션 안에서 확인함 (인벤토리는 실시간 값이 진실이라 여기선 검증 안 함)
            client.Gold += refund;

            _dbClient.RequestSellItem(socketId, client.UserId, req._itemId, client.Gold);
        }

        // ─────────────────────────────────────────────
        // Guest / Client 관리
        // ─────────────────────────────────────────────

        void MoveGuestToClient(int socketId, int userId, string nickname)
        {
            RemoveGuest(socketId);

            _clientList[socketId] = new Client
            {
                SocketId = socketId,
                UserId = userId,
                Nickname = nickname
            };
            Console.WriteLine("[TCPServer] 클라이언트 등록 - {0} (UserID {1})", nickname, userId);
        }

        void RemoveGuest(int socketId)
        {
            if (_guestList.ContainsKey(socketId))
            {
                _guestList.Remove(socketId);
                Console.WriteLine("[TCPServer] 게스트 제거 - 소켓 : {0}", socketId);
            }
        }

        void RemoveClient(int socketId)
        {
            if (_clientList.ContainsKey(socketId))
            {
                Console.WriteLine("[TCPServer] 클라이언트 제거 - {0}", _clientList[socketId].Nickname);
                _clientList.Remove(socketId);
            }
        }

        // ─────────────────────────────────────────────
        // DB 응답 처리 (DBClient가 호출)
        // ─────────────────────────────────────────────

        public void OnRegisterResult(int socketId, bool success, int userId, string username, string passwordHash, string nickname)
        {
            if (success)
            {
                _userList[username] = new UserInfo
                {
                    UserID = userId,
                    Username = username,
                    PasswordHash = passwordHash,
                    Nickname = nickname
                };

                Register_Result result = new Register_Result { _result = 1 };
                Packet packet = ConvertPacket.MakePacket((int)ServerClientProtocol.SendProtocol.RegisterOK, result);
                SendTo(socketId, ConvertPacket.ToBytes(packet));
                Console.WriteLine("[TCPServer] 회원가입 결과 전송 - {0} : 성공", username);
            }
            else
            {
                SendRegisterFail(socketId, RegisterFailReason.ServerError);
                Console.WriteLine("[TCPServer] 회원가입 결과 전송 - {0} : 실패(서버 오류)", username);
            }
        }

        public void OnPlayerDataResult(int socketId, DB_PlayerData_Info info)
        {
            if (!_clientList.ContainsKey(socketId)) return;
            Client client = _clientList[socketId];

            client.Gold = info._gold;   // 이후 구매/판매 검증에 쓸 서버 메모리 캐시

            Login_Result result = new Login_Result
            {
                _result = 1,
                _userId = client.UserId,
                _nickname = client.Nickname,
                _gold = info._gold,
                _tryCount = info._tryCount,
                _isCleared = info._isCleared,
                _unlockedWeapons = info._unlockedWeapons,
                _equippedEquipment = info._equippedEquipment
            };

            Packet packet = ConvertPacket.MakePacket((int)ServerClientProtocol.SendProtocol.LoginOK, result);
            SendTo(socketId, ConvertPacket.ToBytes(packet));
            Console.WriteLine("[TCPServer] 로그인 성공 - UserID : {0}", result._userId);

            // 로그인 응답 보낸 직후, 보유 인벤토리도 이어서 요청 (받는 대로 클라에 그대로 전달)
            _dbClient.RequestGetPlayerInventory(socketId, client.UserId);
        }

        /// <summary>DBClient가 인벤토리 개수를 받으면 호출 - 클라에도 그대로 전달</summary>
        public void OnInventoryCountResult(int socketId, int count)
        {
            Inventory_Count countData = new Inventory_Count { _count = count };
            Packet packet = ConvertPacket.MakePacket((int)ServerClientProtocol.SendProtocol.InventoryCount, countData);
            SendTo(socketId, ConvertPacket.ToBytes(packet));

            Console.WriteLine("[TCPServer] 인벤토리 개수 전달 - 소켓 : {0}, 개수 : {1}", socketId, count);
        }

        /// <summary>DBClient가 인벤토리 항목 하나를 받으면 호출 - 클라에도 그대로 전달</summary>
        public void OnInventoryItemResult(int socketId, int itemType, int itemId, int quantity)
        {
            Inventory_Item itemData = new Inventory_Item
            {
                _itemType = itemType,
                _itemId = itemId,
                _quantity = quantity
            };
            Packet packet = ConvertPacket.MakePacket((int)ServerClientProtocol.SendProtocol.InventoryItem, itemData);
            SendTo(socketId, ConvertPacket.ToBytes(packet));
        }

        /// <summary>DBClient가 구매 반영 결과를 받으면 호출. DB 실패 시 서버 메모리에 낙관적으로 반영해둔 골드를 되돌림</summary>
        public void OnBuyItemResult(int socketId, bool success, int itemId)
        {
            if (!_clientList.ContainsKey(socketId)) return;
            Client client = _clientList[socketId];

            if (!success)
            {
                // DB 저장은 실패했는데 골드는 이미 깎아둔 상태이므로, 가격만큼 되돌림
                if (_itemPriceCache.TryGetValue(itemId, out var itemInfo))
                    client.Gold += itemInfo.price;

                Console.WriteLine("[TCPServer] 구매 DB 반영 실패 - 골드 롤백 : {0}", client.Nickname);
            }

            SendTradeResult(socketId, ServerClientProtocol.SendProtocol.BuyResult, success, itemId, client.Gold);
        }

        /// <summary>DBClient가 판매 반영 결과를 받으면 호출. DB가 보유 수량 부족 등으로 실패시키면 골드도 되돌림</summary>
        public void OnSellItemResult(int socketId, bool success, int itemId)
        {
            if (!_clientList.ContainsKey(socketId)) return;
            Client client = _clientList[socketId];

            if (!success)
            {
                // 실제로 갖고 있지 않아서 DB가 거부한 경우 - 미리 더해둔 환불 골드를 되돌림
                if (_itemPriceCache.TryGetValue(itemId, out var itemInfo))
                    client.Gold -= itemInfo.price / 2;

                Console.WriteLine("[TCPServer] 판매 실패(미보유 등) - 골드 롤백 : {0}", client.Nickname);
            }

            SendTradeResult(socketId, ServerClientProtocol.SendProtocol.SellResult, success, itemId, client.Gold);
        }

        void SendTradeResult(int socketId, ServerClientProtocol.SendProtocol protocol, bool success, int itemId, int currentGold)
        {
            Shop_Trade_Result result = new Shop_Trade_Result
            {
                _result = success ? 1 : 0,
                _itemId = itemId,
                _newGold = currentGold
            };
            Packet packet = ConvertPacket.MakePacket((int)protocol, result);
            SendTo(socketId, ConvertPacket.ToBytes(packet));
        }

        // ─────────────────────────────────────────────
        // 송신 헬퍼
        // ─────────────────────────────────────────────

        public void SendTo(int socketId, byte[] data)
        {
            _sendQueue.Enqueue(new KeyValuePair<int, byte[]>(socketId, data));
        }

        void SendEmptyPacket(int socketId, int protocol)
        {
            Packet packet = new Packet();
            packet._protocol = protocol;
            packet._totalSize = 0;
            packet._data = new byte[1016];
            SendTo(socketId, ConvertPacket.ToBytes(packet));
        }

        void SendRegisterFail(int socketId, RegisterFailReason reason)
        {
            Register_Fail fail = new Register_Fail { _reason = (int)reason };
            Packet packet = ConvertPacket.MakePacket((int)ServerClientProtocol.SendProtocol.RegisterFail, fail);
            SendTo(socketId, ConvertPacket.ToBytes(packet));
        }

        void SendLoginFail(int socketId, LoginFailReason reason)
        {
            Login_Fail fail = new Login_Fail { _reason = (int)reason };
            Packet packet = ConvertPacket.MakePacket((int)ServerClientProtocol.SendProtocol.LoginFail, fail);
            SendTo(socketId, ConvertPacket.ToBytes(packet));
        }

        // ─────────────────────────────────────────────
        // 유효성 검사 (서버 2차)
        // ─────────────────────────────────────────────

        bool IsValidUsername(string username)
        {
            if (string.IsNullOrEmpty(username)) return false;
            if (username.Length < 4 || username.Length > 20) return false;
            foreach (char c in username)
                if (!char.IsLetterOrDigit(c)) return false;
            return true;
        }

        bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;
            if (password.Length < 8 || password.Length > 20) return false;

            bool hasLetter = false, hasDigit = false, hasSpecial = false;
            foreach (char c in password)
            {
                if (char.IsLetter(c)) hasLetter = true;
                if (char.IsDigit(c)) hasDigit = true;
                if (!char.IsLetterOrDigit(c)) hasSpecial = true;
            }
            int typeCount = (hasLetter ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSpecial ? 1 : 0);
            return typeCount >= 2;
        }

        // ─────────────────────────────────────────────
        // 종료
        // ─────────────────────────────────────────────

        public void Release()
        {
            _isQuit = true;
            for (int n = 0; n < _socketList.Count; n++)
            {
                if (_socketList[n] != null)
                {
                    _socketList[n].Close();
                    _socketList[n] = null;
                }
            }
            if (_socListen != null) _socListen.Close();
            Console.WriteLine("[TCPServer] 종료되었습니다.");
        }
    }
}