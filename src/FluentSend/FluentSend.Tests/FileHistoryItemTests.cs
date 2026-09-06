using System;
using FluentSend.ViewModels;
using Xunit;

namespace FluentSend.Tests;

public class FileHistoryItemTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1024 * 1024, "1.0 MB")]
    [InlineData(1024L * 1024 * 1024, "1.00 GB")]
    [InlineData(1024L * 1024 * 1024 * 5, "5.00 GB")]
    public void SizeText_FormatsCorrectly(long bytes, string expected)
    {
        var item = new FileHistoryItem { FileName = "x", Size = bytes };
        Assert.Equal(expected, item.SizeText);
    }

    [Fact]
    public void TimeText_UsesKnownFormat()
    {
        var time = new DateTimeOffset(2026, 9, 5, 14, 32, 0, TimeSpan.Zero);
        var item = new FileHistoryItem
        {
            FileName = "f",
            ReceivedTime = time
        };
        Assert.Equal("2026-09-05 14:32", item.TimeText);
    }
}
