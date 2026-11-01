using FreshMilk.API.Repositories;
using FreshMilk.API.Repositories.Interfaces;
using FreshMilk.API.Services;
using FreshMilk.API.Services.Interfaces;

namespace FreshMilk.API;

public static class Injector
{
    public static IServiceCollection AddFreshMilkDependencies(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IUsersRepository, UsersRepository>();
        services.AddScoped<IMilkTypesRepository, MilkTypesRepository>();
        services.AddScoped<IMilkTypesService, MilkTypesService>();
        services.AddScoped<IMilkRatesRepository, MilkRatesRepository>();
        services.AddScoped<IMilkRatesService, MilkRatesService>();
        services.AddScoped<ISuppliesRepository, SuppliesRepository>();
        services.AddScoped<ISuppliesService, SuppliesService>();
        services.AddScoped<IConsumerOrdersRepository, ConsumerOrdersRepository>();
        services.AddScoped<IConsumerOrdersService, ConsumerOrdersService>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IAppNotificationsRepository, AppNotificationsRepository>();
        services.AddScoped<IAppNotificationsService, AppNotificationsService>();
        return services;
    }
}