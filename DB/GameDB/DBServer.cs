using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using DefinePacket;
using DBServerProtocol;

namespace GameDB
{
    internal class DBServer
    {
        short _port;

        Socket _socListen;
        Socket _serverSocket;   // GameServer 연결 소켓 (1개만)

        Queue<byte[]> _recieveQueue;
        Queue<byte[]> _sendQueue;

        Thread _connectLoop;
        Thread _recieveLoop;
        Thread _sendLoop;

        DBAgentManager _db;

        public bool _isQuit { get; set; }

        public DBServer(short port, DBAgentManager db)
        {
            _port = port;
            _db = db;
            _recieveQueue = new Queue<byte[]>();
            _sendQueue = new Queue<byte[]>();
            _connectLoop = new Thread(ConnectLoop);
            _recieveLoop = new Thread(ReceiveProc);
            _sendLoop = new Thread(SendProc);

            CreateServer();
        }

        void CreateServer()
        {
            try
            {
                _socListen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                EndPoint ep = new IPEndPoint(IPAddress.Any, _port);
                _socListen.Bind(ep);
                _socListen.Listen(1);

                Console.WriteLine("[DBServer] Listen 시작 - Port : {0}", _port);

                _connectLoop.Start();
                Thread.Sleep(100);
                _recieveLoop.Start();
                Thread.Sleep(100);
                _sendLoop.Start();
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DBServer] 생성 실패 : {0}", ex.Message);
            }
        }

        void ConnectLoop()
        {
            while (!_isQuit)
            {
                if (_serverSocket == null && _socListen.Poll(0, SelectMode.SelectRead))
                {
                    _serverSocket = _socListen.Accept();
                    Console.WriteLine("[DBServer] GameServer 연결됨.");
                }
            }
        }

        void ReceiveProc()
        {
            while (!_isQuit)
            {
                if (_serverSocket != null && _serverSocket.Poll(0, SelectMode.SelectRead))
                {
                    try
                    {
                        byte[] buffer = new byte[1024];
                        int sizeLen = _serverSocket.Receive(buffer);
                        if (sizeLen > 0)
                            _recieveQueue.Enqueue(buffer);
                        else
                        {
                            Console.WriteLine("[DBServer] GameServer 연결 끊김.");
                            _serverSocket = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[DBServer] 수신 실패 : {0}", ex.Message);
                        _serverSocket = null;
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
                    byte[] data = _sendQueue.Dequeue();
                    if (_serverSocket != null)
                    {
                        try { _serverSocket.Send(data); }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[DBServer] 송신 실패 : {0}", ex.Message);
                            _serverSocket = null;
                        }
                    }
                }
            }
        }

        // ─────────────────────────────────────────────
        // 메인 루프 - 패킷 처리
        // ─────────────────────────────────────────────

        public void MainLoop()
        {
            while (_recieveQueue.Count > 0)
            {
                byte[] buffer = _recieveQueue.Dequeue();
                Packet packet = (Packet)ConvertPacket.ToStruct(buffer, typeof(Packet));

                Console.WriteLine("[DBServer] 패킷 수신 - Protocol : {0}", packet._protocol);

                switch ((ReceiveProtocol)packet._protocol)
                {
                    case ReceiveProtocol.GetUsers:
                        Handle_GetUsers();
                        break;

                    case ReceiveProtocol.Register:
                        Handle_Register(packet);
                        break;

                    case ReceiveProtocol.GetPlayerData:
                        Handle_GetPlayerData(packet);
                        break;

                    case ReceiveProtocol.GetPlayerInventory:
                        Handle_GetPlayerInventory(packet);
                        break;

                    default:
                        Console.WriteLine("[DBServer] 알 수 없는 프로토콜 : {0}", packet._protocol);
                        break;
                }
            }
        }

        // ─────────────────────────────────────────────
        // 패킷 핸들러
        // ─────────────────────────────────────────────

        void Handle_GetUsers()
        {
            List<UserRow> users = _db.GetAllUsers();

            DB_UserCount countData = new DB_UserCount { _count = users.Count };
            Packet countPacket = ConvertPacket.MakePacket((int)SendProtocol.UserCount, countData);
            _sendQueue.Enqueue(ConvertPacket.ToBytes(countPacket));

            foreach (UserRow user in users)
            {
                DB_UserInfo infoData = new DB_UserInfo
                {
                    _userId = user.UserID,
                    _username = user.Username,
                    _passwordHash = user.PasswordHash,
                    _nickname = user.Nickname
                };
                Packet infoPacket = ConvertPacket.MakePacket((int)SendProtocol.UserInfo, infoData);
                _sendQueue.Enqueue(ConvertPacket.ToBytes(infoPacket));
            }

            Console.WriteLine("[DBServer] 유저 목록 전송 완료 - {0}명", users.Count);
        }

        void Handle_Register(Packet packet)
        {
            DB_Register_Request req =
                (DB_Register_Request)ConvertPacket.UnpackData(packet, typeof(DB_Register_Request));

            int newUserId = _db.RegisterUser(req._username, req._passwordHash, req._nickname);

            DB_Register_Result result = new DB_Register_Result
            {
                _result = newUserId > 0 ? 1 : 0,
                _userId = newUserId
            };
            Packet resultPacket = ConvertPacket.MakePacket((int)SendProtocol.RegisterResult, result);
            _sendQueue.Enqueue(ConvertPacket.ToBytes(resultPacket));

            Console.WriteLine("[DBServer] 회원가입 처리 - {0} : {1}", req._username, newUserId > 0 ? "성공" : "실패");
        }

        void Handle_GetPlayerData(Packet packet)
        {
            DB_GetPlayerData_Request req =
                (DB_GetPlayerData_Request)ConvertPacket.UnpackData(packet, typeof(DB_GetPlayerData_Request));

            PlayerDataRow row = _db.GetPlayerData(req._userId);

            DB_PlayerData_Info info = new DB_PlayerData_Info
            {
                _gold = row?.Gold ?? 0,
                _tryCount = row?.TryCount ?? 0,
                _isCleared = row?.IsCleared ?? 0,
                _unlockedWeapons = row?.UnlockedWeapons ?? "[]",
                _equippedEquipment = row?.EquippedEquipment ?? "[]"
            };
            Packet resultPacket = ConvertPacket.MakePacket((int)SendProtocol.PlayerDataResult, info);
            _sendQueue.Enqueue(ConvertPacket.ToBytes(resultPacket));

            Console.WriteLine("[DBServer] PlayerData 전송 - UserID : {0}", req._userId);
        }

        void Handle_GetPlayerInventory(Packet packet)
        {
            DB_GetPlayerInventory_Request req =
                (DB_GetPlayerInventory_Request)ConvertPacket.UnpackData(packet, typeof(DB_GetPlayerInventory_Request));

            List<InventoryItemRow> items = _db.GetPlayerInventory(req._userId);

            DB_InventoryCount countData = new DB_InventoryCount { _count = items.Count };
            Packet countPacket = ConvertPacket.MakePacket((int)SendProtocol.InventoryCount, countData);
            _sendQueue.Enqueue(ConvertPacket.ToBytes(countPacket));

            foreach (InventoryItemRow item in items)
            {
                DB_InventoryItem itemData = new DB_InventoryItem
                {
                    _itemType = item.ItemType,
                    _itemId = item.ItemID,
                    _quantity = item.Quantity
                };
                Packet itemPacket = ConvertPacket.MakePacket((int)SendProtocol.InventoryItem, itemData);
                _sendQueue.Enqueue(ConvertPacket.ToBytes(itemPacket));
            }

            Console.WriteLine("[DBServer] 인벤토리 전송 완료 - UserID : {0}, 개수 : {1}", req._userId, items.Count);
        }

        public void Release()
        {
            _isQuit = true;
            if (_serverSocket != null) _serverSocket.Close();
            if (_socListen != null) _socListen.Close();
            Console.WriteLine("[DBServer] 종료되었습니다.");
        }
    }
}