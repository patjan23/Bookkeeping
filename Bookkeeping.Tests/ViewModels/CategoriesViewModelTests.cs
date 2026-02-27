using Bookkeeping.App.ViewModels;
using Bookkeeping.Core.Interfaces;
using Bookkeeping.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Bookkeeping.Tests.ViewModels;

public class CategoriesViewModelTests
{
    private readonly Mock<ICategoryRepository> _repo = new();

    private CategoriesViewModel CreateVm() => new(
        _repo.Object,
        NullLogger<CategoriesViewModel>.Instance);

    private static List<Category> SampleCategories() =>
    [
        new() { Id = 1, Name = "Salary",    Type = TransactionType.Income,  Color = "#38A169" },
        new() { Id = 2, Name = "Groceries", Type = TransactionType.Expense, Color = "#E53E3E" }
    ];

    [Fact]
    public async Task LoadAsync_PopulatesCategories()
    {
        _repo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(SampleCategories());

        var vm = CreateVm();
        await vm.LoadAsync();

        vm.Categories.Should().HaveCount(2);
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void StartAdd_SetsIsEditingTrueAndClearsFields()
    {
        var vm = CreateVm();
        vm.StartAddCommand.Execute(null);

        vm.IsEditing.Should().BeTrue();
        vm.EditName.Should().BeEmpty();
    }

    [Fact]
    public void StartEdit_WhenCategorySelected_PopulatesEditFields()
    {
        var cat = SampleCategories()[1];
        var vm  = CreateVm();
        vm.SelectedCategory = cat;

        vm.StartEditCommand.Execute(null);

        vm.IsEditing.Should().BeTrue();
        vm.EditName.Should().Be("Groceries");
        vm.EditType.Should().Be(TransactionType.Expense);
        vm.EditColor.Should().Be("#E53E3E");
    }

    [Fact]
    public void CancelEdit_ClosesEditPanel()
    {
        var vm = CreateVm();
        vm.StartAddCommand.Execute(null);
        vm.CancelEditCommand.Execute(null);

        vm.IsEditing.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_WhenNameEmpty_DoesNotCallRepo()
    {
        var vm = CreateVm();
        vm.StartAddCommand.Execute(null);
        vm.EditName = string.Empty; // leave blank

        await vm.SaveCommand.ExecuteAsync(null);

        _repo.Verify(r => r.AddAsync(It.IsAny<Category>(), default), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WhenValid_AddsAndReloads()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<Category>(), default)).Returns(Task.CompletedTask);
        _repo.Setup(r => r.GetAllAsync(default)).ReturnsAsync(SampleCategories());

        var vm = CreateVm();
        vm.StartAddCommand.Execute(null);
        vm.EditName  = "Rent";
        vm.EditType  = TransactionType.Expense;
        vm.EditColor = "#805AD5";

        await vm.SaveCommand.ExecuteAsync(null);

        _repo.Verify(r => r.AddAsync(It.Is<Category>(c => c.Name == "Rent"), default), Times.Once);
        vm.IsEditing.Should().BeFalse();
    }
}
