using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using DefinePacket;

namespace GameServer
{
    internal class DBClient
    {
        string _ip;
        short _port;

        Socket _socket;

        Queue<byte[]> _receiveQueue;
        Queue<byte[]> _sendQueue;

        Thread _receiveLoop;
        Thread _sendLoop;

        public bool IsConnected { get; private set; }
        public bool _isQuit { get; set; }

        TCPServer _tcpServer;

        // 회원가입/PlayerData 요청은 소켓당 한 번에 하나씩 진행된다고 가정하고 socketId로 매칭
        Queue<(int socketId, string username, string passwordHash, string nickname)> _pendingRegister;
        Queue<int> _pendingGetPlayerData;
        Queue<int> _pendingGetInventory;
        Queue<(int socketId, int itemId)> _pendingBuyItem;
        Queue<(int socketId, int itemId)> _pendingSellItem;

        // 인벤토리는 개수+항목 여러 개가 순서대로 오므로, "지금 누구 걸 받는 중인지" 별도로 기억
        int _currentInventorySocketId = -1;

        public DBClient(string ip, short port)
        {
            _ip = ip;
            _port = port;
            _receiveQueue = new Queue<byte[]>();
            _sendQueue = new Queue<byte[]>();
            _receiveLoop = new Thread(ReceiveProc);
            _sendLoop = new Thread(SendProc);

            _pendingRegister = new Queue<(int, string, string, string)>();
            _pendingGetPlayerData = new Queue<int>();
            _pendingGetInventory = new Queue<int>();
            _pendingBuyItem = new Queue<(int, int)>();
            _pendingSellItem = new Queue<(int, int)>();
        }

        public void SetTCPServer(TCPServer server)
        {
            _tcpServer = server;
        }

        // ─────────────────────────────────────────────
        // 연결
        // ─────────────────────────────────────────────

        public bool Connect()
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Connect(new IPEndPoint(IPAddress.Parse(_ip), _port));
                IsConnected = true;
                Console.WriteLine("[DBClient] GameDB 연결 성공. ({0}:{1})", _ip, _port);

                _receiveLoop.Start();
                Thread.Sleep(100);
                _sendLoop.Start();
                Thread.Sleep(100);
                return true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                Console.WriteLine("[DBClient] GameDB 연결 실패 : {0}", ex.Message);
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
                            _receiveQueue.Enqueue(buffer);
                        else
                        {
                            Console.WriteLine("[DBClient] GameDB 연결 끊김.");
                            IsConnected = false;
                            _socket = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[DBClient] 수신 실패 : {0}", ex.Message);
                        IsConnected = false;
                        _socket = null;
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
                    if (_socket != null)
                    {
                        try { _socket.Send(data); }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[DBClient] 송신 실패 : {0}", ex.Message);
                            IsConnected = false;
                            _socket = null;
                        }
                    }
                }
            }
        }

        // ─────────────────────────────────────────────
        // 메인 루프 - GameDB 응답 처리
        // ─────────────────────────────────────────────

        public void MainLoop()
        {
            while (_receiveQueue.Count > 0)
            {
                byte[] buffer = _receiveQueue.Dequeue();
                Packet packet = (Packet)ConvertPacket.ToStruct(buffer, typeof(Packet));

                switch ((ServerDBProtocol.ReceiveProtocol)packet._protocol)
                {
                    case ServerDBProtocol.ReceiveProtocol.UserCount:
                        Handle_UserCount(packet);
                        break;

                    case ServerDBProtocol.ReceiveProtocol.UserInfo:
                        Handle_UserInfo(packet);
                        break;

                    case ServerDBProtocol.ReceiveProtocol.RegisterResult:
                        Handle_RegisterResult(packet);
                        break;

                    case ServerDBProtocol.ReceiveProtocol.PlayerDataResult:
                        Handle_PlayerDataResult(packet);
                        break;

                    case ServerDBProtocol.ReceiveProtocol.InventoryCount:
                        Handle_InventoryCount(packet);
                        break;

                    case ServerDBProtocol.ReceiveProtocol.InventoryItem:
                        Handle_InventoryItem(packet);
                        break;

                    case ServerDBProtocol.ReceiveProtocol.BuyItemResult:
                        Handle_BuyItemResult(packet);
                        break;

                    case ServerDBProtocol.ReceiveProtocol.SellItemResult:
                        Handle_SellItemResult(packet);
                        break;

                    case ServerDBProtocol.ReceiveProtocol.SaveInventoryResult:
                        Handle_SaveInventoryResult(packet);
                        break;

                    default:
                        Console.WriteLine("[DBClient] 알 수 없는 프로토콜 : {0}", packet._protocol);
                        break;
                }
            }
        }

        // ─────────────────────────────────────────────
        // 응답 핸들러 → TCPServer로 결과 전달
        // ─────────────────────────────────────────────

        void Handle_UserCount(Packet packet)
        {
            DB_UserCount countData =
                (DB_UserCount)ConvertPacket.UnpackData(packet, typeof(DB_UserCount));
            Console.WriteLine("[DBClient] 유저 수 수신 - {0}명", countData._count);
        }

        void Handle_UserInfo(Packet packet)
        {
            DB_UserInfo info =
                (DB_UserInfo)ConvertPacket.UnpackData(packet, typeof(DB_UserInfo));

            _tcpServer?.AddUserToCache(info._userId, info._username, info._passwordHash, info._nickname);
        }

        void Handle_RegisterResult(Packet packet)
        {
            DB_Register_Result result =
                (DB_Register_Result)ConvertPacket.UnpackData(packet, typeof(DB_Register_Result));

            if (_pendingRegister.Count == 0) return;
            var pending = _pendingRegister.Dequeue();

            bool success = result._result == 1;
            _tcpServer?.OnRegisterResult(pending.socketId, success, result._userId,
                pending.username, pending.passwordHash, pending.nickname);
        }

        void Handle_PlayerDataResult(Packet packet)
        {
            DB_PlayerData_Info info =
                (DB_PlayerData_Info)ConvertPacket.UnpackData(packet, typeof(DB_PlayerData_Info));

            if (_pendingGetPlayerData.Count == 0) return;
            int socketId = _pendingGetPlayerData.Dequeue();

            _tcpServer?.OnPlayerDataResult(socketId, info);
        }

        void Handle_InventoryCount(Packet packet)
        {
            DB_InventoryCount count =
                (DB_InventoryCount)ConvertPacket.UnpackData(packet, typeof(DB_InventoryCount));

            if (_pendingGetInventory.Count == 0) return;
            _currentInventorySocketId = _pendingGetInventory.Dequeue();

            _tcpServer?.OnInventoryCountResult(_currentInventorySocketId, count._count);
        }

        void Handle_InventoryItem(Packet packet)
        {
            DB_InventoryItem item =
                (DB_InventoryItem)ConvertPacket.UnpackData(packet, typeof(DB_InventoryItem));

            _tcpServer?.OnInventoryItemResult(_currentInventorySocketId, item._slotIndex, item._itemType, item._itemId, item._quantity);
        }

        void Handle_SaveInventoryResult(Packet packet)
        {
            DB_SaveInventory_Result result =
                (DB_SaveInventory_Result)ConvertPacket.UnpackData(packet, typeof(DB_SaveInventory_Result));

            Console.WriteLine("[DBClient] 인벤토리 저장 결과 : " + (result._result == 1 ? "성공" : "실패"));
        }

        void Handle_BuyItemResult(Packet packet)
        {
            DB_BuyItem_Result result =
                (DB_BuyItem_Result)ConvertPacket.UnpackData(packet, typeof(DB_BuyItem_Result));

            if (_pendingBuyItem.Count == 0) return;
            var pending = _pendingBuyItem.Dequeue();

            _tcpServer?.OnBuyItemResult(pending.socketId, result._result == 1, pending.itemId);
        }

        void Handle_SellItemResult(Packet packet)
        {
            DB_SellItem_Result result =
                (DB_SellItem_Result)ConvertPacket.UnpackData(packet, typeof(DB_SellItem_Result));

            if (_pendingSellItem.Count == 0) return;
            var pending = _pendingSellItem.Dequeue();

            _tcpServer?.OnSellItemResult(pending.socketId, result._result == 1, pending.itemId);
        }

        // ─────────────────────────────────────────────
        // 요청 함수
        // ─────────────────────────────────────────────

        /// <summary>서버 시작 시 1회 호출 - 전체 유저를 캐싱하기 위해 요청</summary>
        public void RequestGetUsers()
        {
            Packet packet = new Packet();
            packet._protocol = (int)ServerDBProtocol.SendProtocol.GetUsers;
            packet._totalSize = 0;
            packet._data = new byte[1016];
            _sendQueue.Enqueue(ConvertPacket.ToBytes(packet));
            Console.WriteLine("[DBClient] 유저 목록 요청.");
        }

        public void RequestRegister(int socketId, string username, string passwordHash, string nickname)
        {
            _pendingRegister.Enqueue((socketId, username, passwordHash, nickname));

            DB_Register_Request req = new DB_Register_Request
            {
                _username = username,
                _passwordHash = passwordHash,
                _nickname = nickname
            };
            Packet packet = ConvertPacket.MakePacket((int)ServerDBProtocol.SendProtocol.Register, req);
            _sendQueue.Enqueue(ConvertPacket.ToBytes(packet));
        }

        public void RequestGetPlayerData(int socketId, int userId)
        {
            _pendingGetPlayerData.Enqueue(socketId);

            DB_GetPlayerData_Request req = new DB_GetPlayerData_Request { _userId = userId };
            Packet packet = ConvertPacket.MakePacket((int)ServerDBProtocol.SendProtocol.GetPlayerData, req);
            _sendQueue.Enqueue(ConvertPacket.ToBytes(packet));
        }

        public void RequestGetPlayerInventory(int socketId, int userId)
        {
            _pendingGetInventory.Enqueue(socketId);

            DB_GetPlayerInventory_Request req = new DB_GetPlayerInventory_Request { _userId = userId };
            Packet packet = ConvertPacket.MakePacket((int)ServerDBProtocol.SendProtocol.GetPlayerInventory, req);
            _sendQueue.Enqueue(ConvertPacket.ToBytes(packet));
        }

        public void RequestBuyItem(int socketId, int userId, int itemType, int itemId, int newGold)
        {
            _pendingBuyItem.Enqueue((socketId, itemId));

            DB_BuyItem_Request req = new DB_BuyItem_Request
            {
                _userId = userId,
                _itemType = itemType,
                _itemId = itemId,
                _newGold = newGold
            };
            Packet packet = ConvertPacket.MakePacket((int)ServerDBProtocol.SendProtocol.BuyItem, req);
            _sendQueue.Enqueue(ConvertPacket.ToBytes(packet));
        }

        public void RequestSellItem(int socketId, int userId, int itemId, int newGold)
        {
            _pendingSellItem.Enqueue((socketId, itemId));

            DB_SellItem_Request req = new DB_SellItem_Request
            {
                _userId = userId,
                _itemId = itemId,
                _newGold = newGold
            };
            Packet packet = ConvertPacket.MakePacket((int)ServerDBProtocol.SendProtocol.SellItem, req);
            _sendQueue.Enqueue(ConvertPacket.ToBytes(packet));
        }

        public void RequestSaveInventory(int userId, string itemsJson, string equippedJson)
        {
            DB_SaveInventory_Request req = new DB_SaveInventory_Request
            {
                _userId = userId,
                _itemsJson = itemsJson,
                _equippedJson = equippedJson
            };
            Packet packet = ConvertPacket.MakePacket((int)ServerDBProtocol.SendProtocol.SaveInventory, req);
            _sendQueue.Enqueue(ConvertPacket.ToBytes(packet));
        }

        public void Release()
        {
            _isQuit = true;
            if (_socket != null)
            {
                _socket.Close();
                _socket = null;
            }
            Console.WriteLine("[DBClient] 종료되었습니다.");
        }
    }
}