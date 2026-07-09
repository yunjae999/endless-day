namespace ServerDBProtocol
{
    // 서버 기준
    public enum SendProtocol       // 서버 → DB
    {
        none = 0,
        GetUsers = 1,          // 서버 시작 시 전체 유저 캐싱용 요청
        Register = 2,          // 회원가입은 DB 쓰기가 필요하므로 그대로 유지
        GetPlayerData = 3,     // 로그인 성공 후 실시간 게임 데이터 조회
    }

    public enum ReceiveProtocol    // DB → 서버
    {
        none = 0,
        UserCount = 100,
        UserInfo = 101,
        RegisterResult = 102,
        PlayerDataResult = 103,
    }
}

namespace DBServerProtocol
{
    // DB 기준
    public enum SendProtocol       // DB → 서버
    {
        none = 0,
        UserCount = 100,
        UserInfo = 101,
        RegisterResult = 102,
        PlayerDataResult = 103,
    }

    public enum ReceiveProtocol    // 서버 → DB
    {
        none = 0,
        GetUsers = 1,
        Register = 2,
        GetPlayerData = 3,
    }
}