using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using FluentSend.ViewModels;
using Xunit;

namespace FluentSend.Tests;

public class FilesPageViewModelTests
{
    [Fact]
    public void NewInstance_HasEmptyStateFlags()
    {
        var vm = new FilesPageViewModel();
        Assert.Empty(vm.FileHistory);
        Assert.False(vm.HasHistory);
        Assert.True(vm.IsEmpty);
    }

    [Fact]
    public void AddItem_RaisesHasHistoryAndIsEmptyChanges()
    {
        var vm = new FilesPageViewModel();
        var hasHistoryChanges = new List<string>();
        var isEmptyChanges = new List<string>();
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(FilesPageViewModel.HasHistory))
                hasHistoryChanges.Add(e.PropertyName);
            if (e.PropertyName == nameof(FilesPageViewModel.IsEmpty))
                isEmptyChanges.Add(e.PropertyName);
        };

        vm.FileHistory.Add(new FileHistoryItem { FileName = "x" });

        Assert.True(vm.HasHistory);
        Assert.False(vm.IsEmpty);
        Assert.Single(hasHistoryChanges);
        Assert.Single(isEmptyChanges);
    }

    [Fact]
    public void ClearHistory_RemovesAllItems()
    {
        var vm = new FilesPageViewModel();
        vm.FileHistory.Add(new FileHistoryItem { FileName = "a" });
        vm.FileHistory.Add(new FileHistoryItem { FileName = "b" });

        vm.ClearHistoryCommand.Execute(null);

        Assert.Empty(vm.FileHistory);
        Assert.False(vm.HasHistory);
        Assert.True(vm.IsEmpty);
    }

    [Fact]
    public void RemoveItem_RemovesSingleItem()
    {
        var vm = new FilesPageViewModel();
        var a = new FileHistoryItem { FileName = "a" };
        var b = new FileHistoryItem { FileName = "b" };
        vm.FileHistory.Add(a);
        vm.FileHistory.Add(b);

        vm.RemoveItemCommand.Execute(a);

        Assert.Single(vm.FileHistory);
        Assert.Equal("b", vm.FileHistory[0].FileName);
    }
}
