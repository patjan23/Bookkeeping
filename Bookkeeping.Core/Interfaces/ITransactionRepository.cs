using Bookkeeping.Core.DTOs;
using Bookkeeping.Core.Models;

namespace Bookkeeping.Core.Interfaces;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<Transaction>> GetFilteredAsync(
        DateTime? from, DateTime? to, int? categoryId, TransactionType? type,
        string? search, CancellationToken ct = default);
    Task<Transaction?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Transaction transaction, CancellationToken ct = default);
    Task UpdateAsync(Transaction transaction, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<DashboardSummaryDto> GetMonthlySummaryAsync(int year, int month, CancellationToken ct = default);
    Task<IEnumerable<MonthlySummaryDto>> GetCategorySummaryAsync(int year, int month, CancellationToken ct = default);
}
