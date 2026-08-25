using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Common.Security
{
    /// <summary>
    /// 當前請求使用者上下文介面（下游服務透過 Gateway 轉發 Header 取得用戶身分）
    /// </summary>
    public interface ICurrentUserContext
    {
        string? UserId { get; }
        string? UserName { get; }
        string? Email { get; }
        IEnumerable<string> Roles { get; }
        bool IsAuthenticated { get; }
    }

    public class CurrentUserContext : ICurrentUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId => 
            _httpContextAccessor.HttpContext?.Request.Headers[SecurityConstants.HeaderUserId].FirstOrDefault() 
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

        public string? UserName => 
            _httpContextAccessor.HttpContext?.Request.Headers[SecurityConstants.HeaderUserName].FirstOrDefault()
            ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;

        public string? Email => 
            _httpContextAccessor.HttpContext?.Request.Headers[SecurityConstants.HeaderUserEmail].FirstOrDefault()
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

        public IEnumerable<string> Roles
        {
            get
            {
                var headerRoles = _httpContextAccessor.HttpContext?.Request.Headers[SecurityConstants.HeaderUserRoles].FirstOrDefault();
                if (!string.IsNullOrEmpty(headerRoles))
                {
                    return headerRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }

                return _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(c => c.Value)
                    ?? Enumerable.Empty<string>();
            }
        }

        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
    }
}
