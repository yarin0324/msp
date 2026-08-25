using System.Security.Claims;
using Common.Security;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace WebApiGateway.Transforms
{
    /// <summary>
    /// YARP Claims 轉發轉換器：將 Gateway 驗證通過的 JWT Claims 自動轉為 X-User-* 標頭傳遞給下游微服務
    /// </summary>
    public class ClaimsTransformProvider : ITransformProvider
    {
        public void ValidateRoute(TransformRouteValidationContext context) { }
        public void ValidateCluster(TransformClusterValidationContext context) { }

        public void Apply(TransformBuilderContext context)
        {
            context.AddRequestTransform(transformContext =>
            {
                var user = transformContext.HttpContext.User;
                if (user?.Identity?.IsAuthenticated == true)
                {
                    // 1. 轉發 User ID (sub / NameIdentifier)
                    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        transformContext.ProxyRequest.Headers.Remove(SecurityConstants.HeaderUserId);
                        transformContext.ProxyRequest.Headers.Add(SecurityConstants.HeaderUserId, userId);
                    }

                    // 2. 轉發 User Name
                    var userName = user.Identity.Name ?? user.FindFirst(ClaimTypes.Name)?.Value;
                    if (!string.IsNullOrEmpty(userName))
                    {
                        transformContext.ProxyRequest.Headers.Remove(SecurityConstants.HeaderUserName);
                        transformContext.ProxyRequest.Headers.Add(SecurityConstants.HeaderUserName, userName);
                    }

                    // 3. 轉發 Email
                    var email = user.FindFirst(ClaimTypes.Email)?.Value;
                    if (!string.IsNullOrEmpty(email))
                    {
                        transformContext.ProxyRequest.Headers.Remove(SecurityConstants.HeaderUserEmail);
                        transformContext.ProxyRequest.Headers.Add(SecurityConstants.HeaderUserEmail, email);
                    }

                    // 4. 轉發 Roles (逗號分隔)
                    var roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value);
                    var roleString = string.Join(",", roles);
                    if (!string.IsNullOrEmpty(roleString))
                    {
                        transformContext.ProxyRequest.Headers.Remove(SecurityConstants.HeaderUserRoles);
                        transformContext.ProxyRequest.Headers.Add(SecurityConstants.HeaderUserRoles, roleString);
                    }
                }

                return ValueTask.CompletedTask;
            });
        }
    }
}
