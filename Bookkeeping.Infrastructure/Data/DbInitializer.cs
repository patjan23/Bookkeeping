using Bookkeeping.Core.Models;
using Microsoft.Extensions.Logging;

namespace Bookkeeping.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(BookkeepingContext context, ILogger logger)
    {
        await context.Database.EnsureCreatedAsync();

        if (context.Accounts.Any())
        {
            logger.LogInformation("Database already seeded — skipping");
            return;
        }

        logger.LogInformation("Seeding database with initial data");

        var accounts = new List<Account>
        {
            new() { Name = "Cash",        Type = AccountType.Cash,       Balance = 500m },
            new() { Name = "Main Bank",   Type = AccountType.Bank,       Balance = 3_200m },
            new() { Name = "Credit Card", Type = AccountType.CreditCard, Balance = -450m },
            new() { Name = "Savings",     Type = AccountType.Savings,    Balance = 10_000m }
        };
        context.Accounts.AddRange(accounts);

        var categories = new List<Category>
        {
            new() { Name = "Salary",       Type = TransactionType.Income,  Color = "#38A169" },
            new() { Name = "Freelance",    Type = TransactionType.Income,  Color = "#3182CE" },
            new() { Name = "Rent",         Type = TransactionType.Expense, Color = "#E53E3E" },
            new() { Name = "Groceries",    Type = TransactionType.Expense, Color = "#DD6B20" },
            new() { Name = "Utilities",    Type = TransactionType.Expense, Color = "#805AD5" },
            new() { Name = "Transport",    Type = TransactionType.Expense, Color = "#D69E2E" },
            new() { Name = "Entertainment",Type = TransactionType.Expense, Color = "#319795" },
            new() { Name = "Healthcare",   Type = TransactionType.Expense, Color = "#C53030" }
        };
        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        var now = DateTime.Now;
        var transactions = new List<Transaction>
        {
            new() { Amount = 4_500m,  Description = "Monthly salary",       Date = new DateTime(now.Year, now.Month, 1),  Type = TransactionType.Income,  CategoryId = categories[0].Id, AccountId = accounts[1].Id },
            new() { Amount = 800m,    Description = "Client project",        Date = new DateTime(now.Year, now.Month, 5),  Type = TransactionType.Income,  CategoryId = categories[1].Id, AccountId = accounts[1].Id },
            new() { Amount = 1_200m,  Description = "Apartment rent",        Date = new DateTime(now.Year, now.Month, 2),  Type = TransactionType.Expense, CategoryId = categories[2].Id, AccountId = accounts[1].Id },
            new() { Amount = 320m,    Description = "Weekly groceries x4",   Date = new DateTime(now.Year, now.Month, 3),  Type = TransactionType.Expense, CategoryId = categories[3].Id, AccountId = accounts[0].Id },
            new() { Amount = 150m,    Description = "Electricity + Internet", Date = new DateTime(now.Year, now.Month, 4), Type = TransactionType.Expense, CategoryId = categories[4].Id, AccountId = accounts[1].Id },
            new() { Amount = 90m,     Description = "Monthly transit pass",   Date = new DateTime(now.Year, now.Month, 6), Type = TransactionType.Expense, CategoryId = categories[5].Id, AccountId = accounts[0].Id },
            new() { Amount = 65m,     Description = "Cinema & dinner",        Date = new DateTime(now.Year, now.Month, 8), Type = TransactionType.Expense, CategoryId = categories[6].Id, AccountId = accounts[2].Id },
            new() { Amount = 200m,    Description = "Dental check-up",        Date = new DateTime(now.Year, now.Month, 10),Type = TransactionType.Expense, CategoryId = categories[7].Id, AccountId = accounts[2].Id },
        };
        context.Transactions.AddRange(transactions);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeding complete: {AccountCount} accounts, {CategoryCount} categories, {TransactionCount} transactions",
            accounts.Count, categories.Count, transactions.Count);
    }
}
