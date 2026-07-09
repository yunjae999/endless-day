namespace ErrorCode
{
    public enum RegisterFailReason
    {
        None = 0,
        InvalidUsername = 1,    // 아이디 형식 오류
        InvalidPassword = 2,    // 비밀번호 형식 오류
        DuplicateUsername = 3,  // 이미 존재하는 아이디
        ServerError = 4,        // DB 저장 실패 등 서버 내부 오류
    }

    public enum LoginFailReason
    {
        None = 0,
        UserNotFound = 1,   // 존재하지 않는 아이디
        WrongPassword = 2,  // 비밀번호 불일치
    }
}