using Bookkeeping.Core.DTOs;
using Bookkeeping.Core.Interfaces;
using Bookkeeping.Core.Models;
using Bookkeeping.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;

namespace Bookkeeping.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly IDbContextFactory<BookkeepingContext> _factory;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<TransactionRepository> _logger;

    public TransactionRepository(
        IDbContextFactory<BookkeepingContext> factory,
        ResiliencePipeline pipeline,
        ILogger<TransactionRepository> logger)
    {
        _factory = factory;
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching all transactions");
        return await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            return await db.Transactions
                .Include(t => t.Category)
                .Include(t => t.Account)
                .AsNoTracking()
                .OrderByDescending(t => t.Date)
                .ToListAsync(token);
        }, ct);
    }

    public async Task<IEnumerable<Transaction>> GetFilteredAsync(
        DateTime? from, DateTime? to, int? categoryId, TransactionType? type,
        string? search, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching filtered transactions from={From} to={To} category={Cat} type={Type} search={Search}",
            from, to, categoryId, type, search);

        return await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            var query = db.Transactions
                .Include(t => t.Category)
                .Include(t => t.Account)
                .AsNoTracking()
                .AsQueryable();

            if (from.HasValue) query = query.Where(t => t.Date >= from.Value);
            if (to.HasValue)   query = query.Where(t => t.Date <= to.Value);
            if (categoryId.HasValue) query = query.Where(t => t.CategoryId == categoryId.Value);
            if (type.HasValue) query = query.Where(t => t.Type == type.Value);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => EF.Functions.Like(t.Description, $"%{search}%"));

            return await query.OrderByDescending(t => t.Date).ToListAsync(token);
        }, ct);
    }

    public async Task<Transaction?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            return await db.Transactions
                .Include(t => t.Category)
                .Include(t => t.Account)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id, token);
        }, ct);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
    {
        _logger.LogInformation("Adding transaction {Description} {Amount}", transaction.Description, transaction.Amount);
        await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            db.Transactions.Add(transaction);
            await db.SaveChangesAsync(token);
        }, ct);
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating transaction {Id}", transaction.Id);
        await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            db.Transactions.Update(transaction);
            await db.SaveChangesAsync(token);
        }, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting transaction {Id}", id);
        await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            var entity = await db.Transactions.FindAsync(new object[] { id }, token);
            if (entity is not null)
            {
                db.Transactions.Remove(entity);
                await db.SaveChangesAsync(token);
            }
        }, ct);
    }

    public async Task<DashboardSummaryDto> GetMonthlySummaryAsync(int year, int month, CancellationToken ct = default)
    {
        return await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            var txs = await db.Transactions.AsNoTracking()
                .Where(t => t.Date.Year == year && t.Date.Month == month)
                .ToListAsync(token);

            return new DashboardSummaryDto
            {
                Year = year,
                Month = month,
                TotalIncome   = txs.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                TotalExpenses = txs.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
            };
        }, ct);
    }

    public async Task<IEnumerable<MonthlySummaryDto>> GetCategorySummaryAsync(int year, int month, CancellationToken ct = default)
    {
        return await _pipeline.ExecuteAsync(async token =>
        {
            await using var db = await _factory.CreateDbContextAsync(token);
            return await db.Transactions
                .AsNoTracking()
                .Where(t => t.Date.Year == year && t.Date.Month == month)
                .Include(t => t.Category)
                .GroupBy(t => new { t.CategoryId, t.Category.Name, t.Category.Color, t.Type })
                .Select(g => new MonthlySummaryDto
                {
                    CategoryName  = g.Key.Name,
                    CategoryColor = g.Key.Color,
                    Type          = g.Key.Type,
                    TotalAmount   = g.Sum(t => t.Amount),
                    Month         = month,
                    Year          = year
                })
                .OrderByDescending(s => s.TotalAmount)
                .ToListAsync(token);
        }, ct);
    }
}
