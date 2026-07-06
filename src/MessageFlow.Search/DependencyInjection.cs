using Microsoft.Extensions.DependencyInjection;

namespace MessageFlow.Search;

public static class DependencyInjection
{
    public static IServiceCollection AddMessageFlowSearch(this IServiceCollection services)
    {
        services.AddScoped<ISermonSearchService, SermonSearchService>();
        services.AddScoped<IBibleSearchService, BibleSearchService>();
        services.AddScoped<ISongSearchService, SongSearchService>();
        return services;
    }
}
