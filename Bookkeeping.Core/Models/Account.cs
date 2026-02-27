namespace Bookkeeping.Core.Models;

public class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
