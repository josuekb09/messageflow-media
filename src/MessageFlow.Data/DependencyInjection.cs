using MessageFlow.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MessageFlow.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddMessageFlowData(this IServiceCollection services)
    {
        return services.AddMessageFlowData(MessageFlowDatabase.DefaultDatabasePath);
    }

    public static IServiceCollection AddMessageFlowData(
        this IServiceCollection services,
        string databasePath)
    {
        MessageFlowDatabase.EnsureDatabaseDirectory(databasePath);

        var connectionString = MessageFlowDatabase.CreateConnectionString(databasePath);

        services.AddDbContext<MessageFlowDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<ISermonRepository, SermonRepository>();

        return services;
    }
}
