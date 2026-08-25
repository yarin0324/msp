namespace Common.Security
{
    /// <summary>
    /// API Gateway 轉發至下游微服務的標準安全性 HTTP 標頭常數
    /// </summary>
    public static class SecurityConstants
    {
        public const string HeaderUserId = "X-User-Id";
        public const string HeaderUserRoles = "X-User-Roles";
        public const string HeaderUserEmail = "X-User-Email";
        public const string HeaderUserName = "X-User-Name";
    }
}
