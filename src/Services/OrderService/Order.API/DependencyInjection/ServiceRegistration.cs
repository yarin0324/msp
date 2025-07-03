using OrderService.WebApi.Services;

namespace OrderService.WebApi.DependencyInjection
{
    /// <summary>
    /// 自動化注入: 使用Scrutor套件實作
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// 自動註冊 Application 和 Infrastructure 層的服務到 DI 容器。
        /// </summary>
        /// <param name="services">DI 服務集合</param>
        /// <returns>更新後的服務集合</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.Scan(scan => scan
                // 掃描 Application 層的用例實現
                .FromApplicationDependencies()
                .AddClasses(classes => classes.InNamespaces("OrderService.Application.UseCases"))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                // 掃描 Infrastructure 層的技術實現
                .FromApplicationDependencies()
                .AddClasses(classes => classes.InNamespaces(
                    "OrderService.Infrastructure.Repositories",
                    "OrderService.Infrastructure.Messaging"))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                // 掃描 WebApi 層的服務（Facade）
                .FromApplicationDependencies()
                .AddClasses(classes => classes.InNamespaces("OrderService.WebApi.Services"))
                .AsSelf()
                .WithScopedLifetime());

            return services;
        }

        /// <summary>
        /// 使用反射式IoC注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        //public static IServiceCollection AddAdapterServices(this IServiceCollection services)
        //{
        //    // Define types that need matching
        //    Type scopedRegistration = typeof(ScopedRegistrationAttribute);
        //    Type singletonRegistration = typeof(SingletonRegistrationAttribute);
        //    Type transientRegistration = typeof(TransientRegistrationAttribute);

        //    var servicesTypes = Assembly.Load("Service");

        //    var types = servicesTypes.GetTypes()
        //        .Where(p => (p.IsDefined(scopedRegistration, true) || p.IsDefined(transientRegistration, true) || p.IsDefined(singletonRegistration, true)) && p.IsInterface == false)
        //        .Select(s => new { Implementation = s.GetInterface($"I{s.Name}"), Service = s })
        //        .Where(x => x.Service != null);

        //    foreach (var type in types)
        //    {
        //        if (type.Service.IsDefined(scopedRegistration, false))
        //        {
        //            services.AddScoped(type.Implementation, type.Service);
        //        }

        //        if (type.Service.IsDefined(transientRegistration, false))
        //        {
        //            services.AddTransient(type.Implementation, type.Service);
        //        }

        //        if (type.Service.IsDefined(singletonRegistration, false))
        //        {
        //            services.AddSingleton(type.Implementation, type.Service);
        //        }
        //    }

        //    return services;
        //}
    }
}
