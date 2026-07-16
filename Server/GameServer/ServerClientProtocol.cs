namespace ServerClientProtocol
{
    // 서버 기준
    public enum ReceiveProtocol   // 클라 → 서버
    {
        none = 0,
        CheckUsername = 1,
        Register = 2,
        Login = 3,
    }

    public enum SendProtocol      // 서버 → 클라
    {
        none = 0,
        ConnectOK = 1,
        CheckUsernameOK = 2,
        CheckUsernameFail = 3,
        RegisterOK = 4,
        RegisterFail = 5,
        LoginOK = 6,
        LoginFail = 7,
        InventoryCount = 8,
        InventoryItem = 9,
    }
}

namespace ClientServerProtocol
{
    // 클라 기준 (Unity 쪽에서 그대로 대칭으로 사용)
    public enum SendProtocol      // 클라 → 서버
    {
        none = 0,
        CheckUsername = 1,
        Register = 2,
        Login = 3,
    }

    public enum ReceiveProtocol   // 서버 → 클라
    {
        none = 0,
        ConnectOK = 1,
        CheckUsernameOK = 2,
        CheckUsernameFail = 3,
        RegisterOK = 4,
        RegisterFail = 5,
        LoginOK = 6,
        LoginFail = 7,
        InventoryCount = 8,
        InventoryItem = 9,
    }
}