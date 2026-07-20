using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace GameDB
{
    /// <summary>
    /// MySQL(endlessday DB)에 직접 접근하는 클래스.
    /// 회원가입/로그인/인벤토리/상점 관련 쿼리를 담당.
    /// </summary>
    class DBAgentManager
    {
        const string _serverName = "localhost";
        const int _portNumber = 3306;

        string _dbName;
        MySqlConnection _connection;

        public bool IsConnected { get; private set; }

        // ─────────────────────────────────────────────
        // 연결 / 해제
        // ─────────────────────────────────────────────

        public bool ConnectDB(string id, string pw, string dbName)
        {
            _dbName = dbName;

            string connectionString = string.Format(
                "Server={0};Port={1};Database={2};Uid={3};Pwd={4}",
                _serverName, _portNumber, dbName, id, pw);

            try
            {
                _connection = new MySqlConnection(connectionString);
                _connection.Open();
                IsConnected = true;
                Console.WriteLine("[DB] {0} 연결 성공.", dbName);
                return true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                Console.WriteLine("[DB] 연결 실패 : {0}", ex.Message);
                return false;
            }
        }

        public void DisconnectDB()
        {
            if (_connection != null && _connection.State == System.Data.ConnectionState.Open)
            {
                _connection.Close();
                IsConnected = false;
                Console.WriteLine("[DB] 연결 종료.");
            }
        }

        // ─────────────────────────────────────────────
        // 서버 시작 시 전체 유저 로드 (서버 메모리 캐싱용)
        // ─────────────────────────────────────────────

        public List<UserRow> GetAllUsers()
        {
            List<UserRow> result = new List<UserRow>();
            string query = string.Format(
                "SELECT UserID, Username, Password, Nickname FROM {0}.Users", _dbName);

            try
            {
                MySqlCommand cmd = new MySqlCommand(query, _connection);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new UserRow
                    {
                        UserID = reader.GetInt32("UserID"),
                        Username = reader["Username"].ToString(),
                        PasswordHash = reader["Password"].ToString(),
                        Nickname = reader["Nickname"].ToString()
                    });
                }
                reader.Close();
                Console.WriteLine("[DB] 유저 {0}명 로드 완료.", result.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DB] GetAllUsers 실패 : {0}", ex.Message);
            }
            return result;
        }

        // ─────────────────────────────────────────────
        // 회원가입 : Users + PlayerData 동시 등록, 새 UserID 반환
        // ─────────────────────────────────────────────

        /// <summary>성공 시 새로 생성된 UserID, 실패 시 0</summary>
        public int RegisterUser(string username, string passwordHash, string nickname)
        {
            string queryUser = string.Format(
                "INSERT INTO {0}.Users (Username, Password, Nickname) VALUES (@username, @pw, @nickname)",
                _dbName);

            MySqlTransaction transaction = _connection.BeginTransaction();
            try
            {
                MySqlCommand cmdUser = new MySqlCommand(queryUser, _connection, transaction);
                cmdUser.Parameters.AddWithValue("@username", username);
                cmdUser.Parameters.AddWithValue("@pw", passwordHash);
                cmdUser.Parameters.AddWithValue("@nickname", nickname);
                cmdUser.ExecuteNonQuery();

                int newUserId = (int)cmdUser.LastInsertedId;

                string queryPlayerData = string.Format(
                    "INSERT INTO {0}.PlayerData (UserID, Gold, TryCount, IsCleared, UnlockedWeapons, EquippedEquipment) " +
                    "VALUES (@userId, 0, 0, 0, @unlockedWeapons, @equippedEquipment)", _dbName);

                MySqlCommand cmdPlayerData = new MySqlCommand(queryPlayerData, _connection, transaction);
                cmdPlayerData.Parameters.AddWithValue("@userId", newUserId);
                cmdPlayerData.Parameters.AddWithValue("@unlockedWeapons", "[1]");
                cmdPlayerData.Parameters.AddWithValue("@equippedEquipment", "[0,0,0,0,0,0,0]");
                cmdPlayerData.ExecuteNonQuery();

                transaction.Commit();
                Console.WriteLine("[DB] 회원가입 완료 : {0} (UserID {1})", username, newUserId);
                return newUserId;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine("[DB] RegisterUser 실패 : {0}", ex.Message);
                return 0;
            }
        }

        // ─────────────────────────────────────────────
        // 로그인 성공 시 PlayerData 로드 (실시간 조회 유지)
        // ─────────────────────────────────────────────

        public PlayerDataRow GetPlayerData(int userId)
        {
            string query = string.Format(
                "SELECT Gold, TryCount, IsCleared, UnlockedWeapons, EquippedEquipment " +
                "FROM {0}.PlayerData WHERE UserID = @userId", _dbName);
            try
            {
                MySqlCommand cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@userId", userId);
                MySqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    PlayerDataRow row = new PlayerDataRow
                    {
                        Gold = reader.GetInt32("Gold"),
                        TryCount = reader.GetInt32("TryCount"),
                        IsCleared = reader.GetInt32("IsCleared"),
                        UnlockedWeapons = reader["UnlockedWeapons"].ToString(),
                        EquippedEquipment = reader["EquippedEquipment"].ToString()
                    };
                    reader.Close();
                    return row;
                }
                reader.Close();
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DB] GetPlayerData 실패 : {0}", ex.Message);
                return null;
            }
        }

        // ─────────────────────────────────────────────
        // 로그인 성공 시 보유 인벤토리 로드
        // ─────────────────────────────────────────────

        public List<InventoryItemRow> GetPlayerInventory(int userId)
        {
            List<InventoryItemRow> result = new List<InventoryItemRow>();
            string query = string.Format(
                "SELECT SlotIndex, ItemType, ItemID, Quantity FROM {0}.PlayerInventory WHERE UserID = @userId", _dbName);
            try
            {
                MySqlCommand cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@userId", userId);
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    result.Add(new InventoryItemRow
                    {
                        SlotIndex = reader.GetInt32("SlotIndex"),
                        ItemType = reader.GetInt32("ItemType"),
                        ItemID = reader.GetInt32("ItemID"),
                        Quantity = reader.GetInt32("Quantity")
                    });
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DB] GetPlayerInventory 실패 : {0}", ex.Message);
            }
            return result;
        }

        // ─────────────────────────────────────────────
        // 인벤토리 창 닫을 때 전체 저장
        // ─────────────────────────────────────────────

        /// <summary>기존 PlayerInventory를 싹 지우고, 클라가 보낸 상태로 통째로 다시 씀. 장착 슬롯도 같이 갱신.</summary>
        public bool SaveInventory(int userId, string itemsJson, string equippedJson)
        {
            MySqlTransaction transaction = _connection.BeginTransaction();
            try
            {
                string deleteQuery = string.Format(
                    "DELETE FROM {0}.PlayerInventory WHERE UserID = @userId", _dbName);
                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, _connection, transaction);
                deleteCmd.Parameters.AddWithValue("@userId", userId);
                deleteCmd.ExecuteNonQuery();

                JArray items = JArray.Parse(itemsJson);
                foreach (JToken token in items)
                {
                    JArray entry = (JArray)token;
                    int slotIndex = entry[0].ToObject<int>();
                    int itemType = entry[1].ToObject<int>();
                    int itemId = entry[2].ToObject<int>();
                    int quantity = entry[3].ToObject<int>();

                    string insertQuery = string.Format(
                        "INSERT INTO {0}.PlayerInventory (UserID, SlotIndex, ItemType, ItemID, Quantity) " +
                        "VALUES (@userId, @slot, @type, @id, @qty)", _dbName);
                    MySqlCommand insertCmd = new MySqlCommand(insertQuery, _connection, transaction);
                    insertCmd.Parameters.AddWithValue("@userId", userId);
                    insertCmd.Parameters.AddWithValue("@slot", slotIndex);
                    insertCmd.Parameters.AddWithValue("@type", itemType);
                    insertCmd.Parameters.AddWithValue("@id", itemId);
                    insertCmd.Parameters.AddWithValue("@qty", quantity);
                    insertCmd.ExecuteNonQuery();
                }

                string equipQuery = string.Format(
                    "UPDATE {0}.PlayerData SET EquippedEquipment = @equipped WHERE UserID = @userId", _dbName);
                MySqlCommand equipCmd = new MySqlCommand(equipQuery, _connection, transaction);
                equipCmd.Parameters.AddWithValue("@equipped", equippedJson);
                equipCmd.Parameters.AddWithValue("@userId", userId);
                equipCmd.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine("[DB] SaveInventory 실패 : {0}", ex.Message);
                return false;
            }
        }

        // ─────────────────────────────────────────────
        // 상점 - 구매/판매 (트랜잭션으로 골드/인벤토리 동시 반영)
        // ─────────────────────────────────────────────

        /// <summary>
        /// 구매 반영. 골드 검증은 서버 메모리에서 이미 끝났으므로, 여기선 저장만 한다.
        /// 같은 아이템을 이미 갖고 있으면 개수만 +1 (PlayerInventory에 UserID+ItemID UNIQUE 필요).
        /// </summary>
        public bool BuyItem(int userId, int itemType, int itemId, int newGold)
        {
            MySqlTransaction transaction = _connection.BeginTransaction();
            try
            {
                string goldQuery = string.Format(
                    "UPDATE {0}.PlayerData SET Gold = @gold WHERE UserID = @userId", _dbName);
                MySqlCommand goldCmd = new MySqlCommand(goldQuery, _connection, transaction);
                goldCmd.Parameters.AddWithValue("@gold", newGold);
                goldCmd.Parameters.AddWithValue("@userId", userId);
                goldCmd.ExecuteNonQuery();

                string upsertQuery = string.Format(
                    "INSERT INTO {0}.PlayerInventory (UserID, ItemType, ItemID, Quantity) VALUES (@userId, @itemType, @itemId, 1) " +
                    "ON DUPLICATE KEY UPDATE Quantity = Quantity + 1", _dbName);
                MySqlCommand upsertCmd = new MySqlCommand(upsertQuery, _connection, transaction);
                upsertCmd.Parameters.AddWithValue("@userId", userId);
                upsertCmd.Parameters.AddWithValue("@itemType", itemType);
                upsertCmd.Parameters.AddWithValue("@itemId", itemId);
                upsertCmd.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine("[DB] BuyItem 실패 : {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 판매 반영. 보유 수량을 트랜잭션 안에서 직접 확인 - 없으면 실패(false)로 롤백, 골드도 그대로 유지됨.
        /// </summary>
        public bool SellItem(int userId, int itemId, int newGold)
        {
            MySqlTransaction transaction = _connection.BeginTransaction();
            try
            {
                string checkQuery = string.Format(
                    "SELECT Quantity FROM {0}.PlayerInventory WHERE UserID = @userId AND ItemID = @itemId FOR UPDATE", _dbName);
                MySqlCommand checkCmd = new MySqlCommand(checkQuery, _connection, transaction);
                checkCmd.Parameters.AddWithValue("@userId", userId);
                checkCmd.Parameters.AddWithValue("@itemId", itemId);

                object quantityObj = checkCmd.ExecuteScalar();
                int currentQuantity = quantityObj != null ? Convert.ToInt32(quantityObj) : 0;

                if (currentQuantity < 1)
                {
                    transaction.Rollback();
                    return false;
                }

                if (currentQuantity == 1)
                {
                    string deleteQuery = string.Format(
                        "DELETE FROM {0}.PlayerInventory WHERE UserID = @userId AND ItemID = @itemId", _dbName);
                    MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, _connection, transaction);
                    deleteCmd.Parameters.AddWithValue("@userId", userId);
                    deleteCmd.Parameters.AddWithValue("@itemId", itemId);
                    deleteCmd.ExecuteNonQuery();
                }
                else
                {
                    string decrementQuery = string.Format(
                        "UPDATE {0}.PlayerInventory SET Quantity = Quantity - 1 WHERE UserID = @userId AND ItemID = @itemId", _dbName);
                    MySqlCommand decrementCmd = new MySqlCommand(decrementQuery, _connection, transaction);
                    decrementCmd.Parameters.AddWithValue("@userId", userId);
                    decrementCmd.Parameters.AddWithValue("@itemId", itemId);
                    decrementCmd.ExecuteNonQuery();
                }

                string goldQuery = string.Format(
                    "UPDATE {0}.PlayerData SET Gold = @gold WHERE UserID = @userId", _dbName);
                MySqlCommand goldCmd = new MySqlCommand(goldQuery, _connection, transaction);
                goldCmd.Parameters.AddWithValue("@gold", newGold);
                goldCmd.Parameters.AddWithValue("@userId", userId);
                goldCmd.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine("[DB] SellItem 실패 : {0}", ex.Message);
                return false;
            }
        }
    }

    // ─────────────────────────────────────────────
    // 데이터 클래스
    // ─────────────────────────────────────────────

    class UserRow
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Nickname { get; set; }
    }

    class PlayerDataRow
    {
        public int Gold { get; set; }
        public int TryCount { get; set; }
        public int IsCleared { get; set; }
        public string UnlockedWeapons { get; set; }
        public string EquippedEquipment { get; set; }
    }

    class InventoryItemRow
    {
        public int SlotIndex { get; set; }
        public int ItemType { get; set; }
        public int ItemID { get; set; }
        public int Quantity { get; set; }
    }
}