using System;
using System.Runtime.InteropServices;

namespace DefinePacket
{
    // ─────────────────────────────────────────────
    // 기본 패킷 구조 (고정 1024byte)
    // ─────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    public struct Packet
    {
        [MarshalAs(UnmanagedType.U4)]
        public int _protocol;
        [MarshalAs(UnmanagedType.U4)]
        public int _totalSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1016)]
        public byte[] _data;
    }

    // ─────────────────────────────────────────────
    // 클라 ↔ 서버 패킷 데이터 구조체
    // ─────────────────────────────────────────────

    /// <summary>서버 → 클라 : 접속 OK</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Connected_Info
    {
        public int _tempSocketId;
    }

    /// <summary>클라 → 서버 : 아이디 중복확인 요청</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CheckUsername_Request
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string _username;
    }

    /// <summary>클라 → 서버 : 회원가입 요청 (비밀번호는 평문으로 전송, 서버에서 해싱)</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Register_Request
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string _username;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string _password;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string _nickname;
    }

    /// <summary>서버 → 클라 : 회원가입 결과</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Register_Result
    {
        public int _result;   // 1 = 성공, 0 = 실패
    }

    /// <summary>서버 → 클라 : 회원가입 실패 이유 (ErrorCode.RegisterFailReason)</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Register_Fail
    {
        public int _reason;
    }

    /// <summary>클라 → 서버 : 로그인 요청</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Login_Request
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string _username;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string _password;
    }

    /// <summary>서버 → 클라 : 로그인 결과 + PlayerData</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Login_Result
    {
        public int _result;        // 1 = 성공, 0 = 실패
        public int _userId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string _nickname;
        public int _gold;
        public int _tryCount;
        public int _isCleared;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string _unlockedWeapons;     // JSON 문자열
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string _equippedEquipment;   // JSON 문자열
    }

    /// <summary>서버 → 클라 : 로그인 실패 이유 (ErrorCode.LoginFailReason)</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Login_Fail
    {
        public int _reason;
    }

    // ─────────────────────────────────────────────
    // 서버 ↔ DB 패킷 데이터 구조체
    // ─────────────────────────────────────────────

    /// <summary>서버 → DB : 회원가입 요청 (비밀번호는 서버에서 이미 BCrypt 해싱 완료)</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DB_Register_Request
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string _username;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string _passwordHash;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string _nickname;
    }

    /// <summary>DB → 서버 : 회원가입 결과</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DB_Register_Result
    {
        public int _result;   // 1 = 성공, 0 = 실패
        public int _userId;   // 성공 시 새로 생성된 UserID (실패면 0)
    }

    /// <summary>DB → 서버 : 서버 시작 시 전체 유저 수</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DB_UserCount
    {
        public int _count;
    }

    /// <summary>DB → 서버 : 유저 1명 정보 (전체 유저 목록 캐싱용, 유저 수만큼 반복 전송)</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DB_UserInfo
    {
        public int _userId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string _username;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string _passwordHash;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string _nickname;
    }

    /// <summary>서버 → DB : PlayerData 조회 요청</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DB_GetPlayerData_Request
    {
        public int _userId;
    }

    /// <summary>DB → 서버 : PlayerData 조회 결과</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DB_PlayerData_Info
    {
        public int _gold;
        public int _tryCount;
        public int _isCleared;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string _unlockedWeapons;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string _equippedEquipment;
    }

    /// <summary>서버 → DB : 보유 인벤토리 조회 요청</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DB_GetPlayerInventory_Request
    {
        public int _userId;
    }

    /// <summary>DB → 서버 : 인벤토리 아이템 개수 (먼저 전송)</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DB_InventoryCount
    {
        public int _count;
    }

    /// <summary>DB → 서버 : 인벤토리 아이템 1개 (개수만큼 반복 전송)</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DB_InventoryItem
    {
        public int _slotIndex;
        public int _itemType;   // 1=장비, 2=소비
        public int _itemId;
        public int _quantity;
    }

    /// <summary>서버 → 클라 : 인벤토리 아이템 개수 (먼저 전송)</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Inventory_Count
    {
        public int _count;
    }

    /// <summary>서버 → 클라 : 인벤토리 아이템 1개 (개수만큼 반복 전송)</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Inventory_Item
    {
        public int _slotIndex;
        public int _itemType;
        public int _itemId;
        public int _quantity;
    }

    /// <summary>클라 → 서버 : 인벤토리 전체 저장(인벤토리 창 닫을 때). 위치 포함해서 한 번에 통째로</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SaveInventory_Request
    {
        // itemsJson 예: [[슬롯,타입,아이템ID,개수], ...]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 900)]
        public string _itemsJson;
        // equippedJson 예: [0,0,0,1002,0,0,0] (7칸)
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string _equippedJson;
    }

    /// <summary>서버 → DB : 인벤토리 전체 저장 (기존 것 지우고 통째로 새로 씀)</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DB_SaveInventory_Request
    {
        public int _userId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 900)]
        public string _itemsJson;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string _equippedJson;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DB_SaveInventory_Result
    {
        public int _result;
    }

    /// <summary>서버 → DB : 구매 반영 (골드는 서버가 이미 검증/계산 완료, DB는 저장만)</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DB_BuyItem_Request
    {
        public int _userId;
        public int _itemType;
        public int _itemId;
        public int _newGold;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DB_BuyItem_Result
    {
        public int _result;
    }

    /// <summary>서버 → DB : 판매 반영. 보유 수량 검증은 DB가 트랜잭션 안에서 직접 확인</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DB_SellItem_Request
    {
        public int _userId;
        public int _itemId;
        public int _newGold;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DB_SellItem_Result
    {
        public int _result;   // 1=성공, 0=실패(보유 수량 부족 등)
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Shop_Buy_Request
    {
        public int _itemId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Shop_Sell_Request
    {
        public int _itemId;
    }

    /// <summary>서버 → 클라 : 구매/판매 결과. 실패해도 이유는 없음(가격/보유량은 서버가 이미 검증했으므로 단순 성공여부만)</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Shop_Trade_Result
    {
        public int _result;      // 1=성공, 0=실패
        public int _itemId;
        public int _newGold;     // 성공 시 갱신된 골드
    }

    // ─────────────────────────────────────────────
    // 변환 유틸
    // ─────────────────────────────────────────────

    public class ConvertPacket
    {
        public static byte[] ToBytes(object obj)
        {
            int dataSize = Marshal.SizeOf(obj);
            IntPtr buff = Marshal.AllocHGlobal(dataSize);
            Marshal.StructureToPtr(obj, buff, false);
            byte[] data = new byte[dataSize];
            Marshal.Copy(buff, data, 0, dataSize);
            Marshal.FreeHGlobal(buff);
            return data;
        }

        public static object ToStruct(byte[] data, Type type)
        {
            IntPtr buff = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, buff, data.Length);
            object obj = Marshal.PtrToStructure(buff, type);
            Marshal.FreeHGlobal(buff);
            return obj;
        }

        public static Packet MakePacket(int protocol, object data)
        {
            byte[] dataBytes = ToBytes(data);
            Packet packet = new Packet();
            packet._protocol = protocol;
            packet._totalSize = dataBytes.Length;
            packet._data = new byte[1016];
            Array.Copy(dataBytes, packet._data, dataBytes.Length);
            return packet;
        }

        public static object UnpackData(Packet packet, Type type)
        {
            byte[] dataBytes = new byte[packet._totalSize];
            Array.Copy(packet._data, dataBytes, packet._totalSize);
            return ToStruct(dataBytes, type);
        }
    }
}