using System.Reflection;

namespace TennisClub.Api.Common.Endpoints;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        var endpointType = typeof(IEndpoint);
        var types = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => endpointType.IsAssignableFrom(t)
                && !t.IsInterface
                && !t.IsAbstract);

        foreach (var type in types)
        {
            services.AddScoped(endpointType, type);
        }

        return services;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var endpoints = scope.ServiceProvider.GetServices<IEndpoint>();
        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(app);
        }
        return app;
    }
}
