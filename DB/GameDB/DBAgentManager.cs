using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace GameDB
{
    /// <summary>
    /// MySQL(endlessday DB)에 직접 접근하는 클래스.
    /// 회원가입/로그인 관련 쿼리만 담당 (범위: 로그인/회원가입).
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
                cmdPlayerData.Parameters.AddWithValue("@equippedEquipment", "[0,0,0,0]");
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
        // 로그인 성공 시 PlayerData 로드 (실시간 조회 유지 - 골드/진행상황은 계속 바뀌므로 캐싱 안 함)
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
}