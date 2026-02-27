using Bookkeeping.Core.Models;

namespace Bookkeeping.Core.Interfaces;

public interface IAccountRepository
{
    Task<IEnumerable<Account>> GetAllAsync(CancellationToken ct = default);
    Task<Account?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
    Task UpdateAsync(Account account, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
