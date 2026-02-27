using Bookkeeping.App.Services;
using Bookkeeping.App.ViewModels;
using Bookkeeping.Core.Interfaces;
using Bookkeeping.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.IO;

namespace Bookkeeping.Tests.ViewModels;

public class TransactionsViewModelTests
{
    private readonly Mock<ITransactionRepository> _repo   = new();
    private readonly Mock<IDialogService>         _dialog = new();

    private TransactionsViewModel CreateVm() => new(
        _repo.Object, _dialog.Object,
        NullLogger<TransactionsViewModel>.Instance);

    private static List<Transaction> SampleTransactions() =>
    [
        new() { Id = 1, Amount = 100m, Description = "Salary",  Type = TransactionType.Income,
                Date = DateTime.Today, Category = new Category { Name = "Salary" },
                Account = new Account { Name = "Bank" } },
        new() { Id = 2, Amount = 50m,  Description = "Groceries", Type = TransactionType.Expense,
                Date = DateTime.Today, Category = new Category { Name = "Food" },
                Account = new Account { Name = "Cash" } }
    ];

    [Fact]
    public async Task LoadAsync_PopulatesTransactions()
    {
        _repo.Setup(r => r.GetFilteredAsync(null, null, null, null, null, default))
             .ReturnsAsync(SampleTransactions());

        var vm = CreateVm();
        await vm.LoadAsync();

        vm.Transactions.Should().HaveCount(2);
        vm.IsLoading.Should().BeFalse();
        vm.IsStatusError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenRepoThrows_SetsErrorState()
    {
        _repo.Setup(r => r.GetFilteredAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int?>(), It.IsAny<TransactionType?>(),
                It.IsAny<string?>(), default))
             .ThrowsAsync(new IOException("locked"));

        var vm = CreateVm();
        await vm.LoadAsync();

        vm.IsStatusError.Should().BeTrue();
        vm.StatusMessage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DeleteTransaction_WhenConfirmed_CallsDeleteAndReloads()
    {
        var tx = SampleTransactions()[0];
        _repo.Setup(r => r.GetFilteredAsync(null, null, null, null, null, default))
             .ReturnsAsync(SampleTransactions());
        _repo.Setup(r => r.DeleteAsync(1, default)).Returns(Task.CompletedTask);
        _dialog.Setup(d => d.Confirm(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var vm = CreateVm();
        vm.SelectedTransaction = tx;

        await vm.DeleteTransactionCommand.ExecuteAsync(null);

        _repo.Verify(r => r.DeleteAsync(1, default), Times.Once);
        vm.IsStatusError.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTransaction_WhenNotConfirmed_DoesNotDelete()
    {
        var tx = SampleTransactions()[0];
        _dialog.Setup(d => d.Confirm(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var vm = CreateVm();
        vm.SelectedTransaction = tx;

        await vm.DeleteTransactionCommand.ExecuteAsync(null);

        _repo.Verify(r => r.DeleteAsync(It.IsAny<int>(), default), Times.Never);
    }

    [Fact]
    public async Task AddTransaction_WhenSaved_ReloadsAndSetsStatus()
    {
        _dialog.Setup(d => d.ShowAddEditTransactionAsync(null)).ReturnsAsync(true);
        _repo.Setup(r => r.GetFilteredAsync(null, null, null, null, null, default))
             .ReturnsAsync(SampleTransactions());

        var vm = CreateVm();
        await vm.AddTransactionCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().Contain("added");
        _repo.Verify(r => r.GetFilteredAsync(null, null, null, null, null, default), Times.Once);
    }
}
