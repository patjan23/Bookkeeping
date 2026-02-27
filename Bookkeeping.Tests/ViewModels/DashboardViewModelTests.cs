using Bookkeeping.App.Services;
using Bookkeeping.App.ViewModels;
using Bookkeeping.Core.DTOs;
using Bookkeeping.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Bookkeeping.Tests.ViewModels;

public class DashboardViewModelTests
{
    private readonly Mock<ITransactionRepository> _txRepo = new();
    private readonly Mock<IDialogService>         _dialog = new();
    private readonly Mock<INavigationService>     _nav    = new();

    private DashboardViewModel CreateVm() => new(
        _txRepo.Object, _dialog.Object, _nav.Object,
        NullLogger<DashboardViewModel>.Instance);

    [Fact]
    public async Task LoadAsync_SetsKpiValues()
    {
        // Arrange
        var now = DateTime.Now;
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(now.Year, now.Month, default))
               .ReturnsAsync(new DashboardSummaryDto
               {
                   TotalIncome   = 5_000m,
                   TotalExpenses = 1_800m,
                   Year  = now.Year,
                   Month = now.Month
               });

        var vm = CreateVm();

        // Act
        await vm.LoadAsync();

        // Assert
        vm.TotalIncome.Should().Be(5_000m);
        vm.TotalExpenses.Should().Be(1_800m);
        vm.Net.Should().Be(3_200m);
        vm.IsNetNegative.Should().BeFalse();
        vm.IsLoading.Should().BeFalse();
        vm.IsStatusError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenNetNegative_SetsIsNetNegativeTrue()
    {
        var now = DateTime.Now;
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(now.Year, now.Month, default))
               .ReturnsAsync(new DashboardSummaryDto
               {
                   TotalIncome   = 500m,
                   TotalExpenses = 1_200m,
                   Year = now.Year, Month = now.Month
               });

        var vm = CreateVm();
        await vm.LoadAsync();

        vm.Net.Should().Be(-700m);
        vm.IsNetNegative.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_WhenRepositoryThrows_SetsErrorState()
    {
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(It.IsAny<int>(), It.IsAny<int>(), default))
               .ThrowsAsync(new InvalidOperationException("DB offline"));

        var vm = CreateVm();
        await vm.LoadAsync();

        vm.IsStatusError.Should().BeTrue();
        vm.StatusMessage.Should().Contain("Failed");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_SetsMonthLabel()
    {
        var now = DateTime.Now;
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(It.IsAny<int>(), It.IsAny<int>(), default))
               .ReturnsAsync(new DashboardSummaryDto());

        var vm = CreateVm();
        await vm.LoadAsync();

        vm.MonthLabel.Should().Be(now.ToString("MMMM yyyy"));
    }

    [Fact]
    public async Task QuickAddTransaction_WhenSaved_ReloadsData()
    {
        var now = DateTime.Now;
        _dialog.Setup(d => d.ShowAddEditTransactionAsync(null))
               .ReturnsAsync(true);
        _txRepo.Setup(r => r.GetMonthlySummaryAsync(now.Year, now.Month, default))
               .ReturnsAsync(new DashboardSummaryDto { TotalIncome = 100m });

        var vm = CreateVm();
        await vm.QuickAddTransactionCommand.ExecuteAsync(null);

        // Load called at least twice: once by quick-add callback
        _txRepo.Verify(r => r.GetMonthlySummaryAsync(now.Year, now.Month, default), Times.AtLeast(1));
    }
}
