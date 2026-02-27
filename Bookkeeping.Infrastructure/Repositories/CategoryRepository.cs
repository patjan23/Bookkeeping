using Bookkeeping.Core.Interfaces;
using Bookkeeping.Core.Models;
using Bookkeeping.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;

namespace Bookkeeping.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly IDbContextFactory<BookkeepingContext> _factory;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<CategoryRepository> _logger;

    public CategoryRepository(
        IDbContextFactory<BookkeepingContext> factory,
        ResiliencePipeline pipeline,
        ILogger<CategoryRepository> logger)
    {
        _factory = factory;
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching all categories");
        return await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            return await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(token);
        }, ct);
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            return await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, token);
        }, ct);
    }

    public async Task AddAsync(Category category, CancellationToken ct = default)
    {
        _logger.LogInformation("Adding category {Name}", category.Name);
        await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            db.Categories.Add(category);
            await db.SaveChangesAsync(token);
        }, ct);
    }

    public async Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating category {Id} {Name}", category.Id, category.Name);
        await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            db.Categories.Update(category);
            await db.SaveChangesAsync(token);
        }, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting category {Id}", id);
        await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            var entity = await db.Categories.FindAsync(new object[] { id }, token);
            if (entity is not null)
            {
                db.Categories.Remove(entity);
                await db.SaveChangesAsync(token);
            }
        }, ct);
    }
}
