using Bookkeeping.Core.Interfaces;
using Bookkeeping.Infrastructure.Data;
using Bookkeeping.Infrastructure.Policies;
using Bookkeeping.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace Bookkeeping.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // EF Core — pooled factory so each repo gets its own short-lived context
        services.AddDbContextFactory<BookkeepingContext>(options =>
            options.UseSqlite(connectionString));

        // Polly resilience pipeline (singleton)
        services.AddSingleton<ResiliencePipeline>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<BookkeepingContext>>();
            return ResiliencePolicies.BuildDbPipeline(logger);
        });

        // Repositories
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        return services;
    }
}
