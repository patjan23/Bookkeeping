using Bookkeeping.Core.Interfaces;
using Bookkeeping.Core.Models;
using Bookkeeping.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;

namespace Bookkeeping.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly IDbContextFactory<BookkeepingContext> _factory;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<AccountRepository> _logger;

    public AccountRepository(
        IDbContextFactory<BookkeepingContext> factory,
        ResiliencePipeline pipeline,
        ILogger<AccountRepository> logger)
    {
        _factory = factory;
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task<IEnumerable<Account>> GetAllAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching all accounts");
        return await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            return await db.Accounts.AsNoTracking().OrderBy(a => a.Name).ToListAsync(token);
        }, ct);
    }

    public async Task<Account?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            return await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, token);
        }, ct);
    }

    public async Task AddAsync(Account account, CancellationToken ct = default)
    {
        _logger.LogInformation("Adding account {Name}", account.Name);
        await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            db.Accounts.Add(account);
            await db.SaveChangesAsync(token);
        }, ct);
    }

    public async Task UpdateAsync(Account account, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating account {Id} {Name}", account.Id, account.Name);
        await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            db.Accounts.Update(account);
            await db.SaveChangesAsync(token);
        }, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting account {Id}", id);
        await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            var entity = await db.Accounts.FindAsync(new object[] { id }, token);
            if (entity is not null)
            {
                db.Accounts.Remove(entity);
                await db.SaveChangesAsync(token);
            }
        }, ct);
    }
}
