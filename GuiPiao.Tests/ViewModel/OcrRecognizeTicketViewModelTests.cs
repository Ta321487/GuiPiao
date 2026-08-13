using System.Collections.Generic;
using GuiPiao.Models;
using GuiPiao.ViewModel;
using Xunit;

namespace GuiPiao.Tests.ViewModel;

public class OcrRecognizeTicketViewModelTests
{
    [Fact]
    public void JoinOcrTexts_OrdersByPositionTopThenLeft()
    {
        var results = new List<OcrResult>
        {
            new() { Text = "右", Position = new List<List<double>> { new() { 100, 10 }, new() { 120, 10 } } },
            new() { Text = "左", Position = new List<List<double>> { new() { 10, 10 }, new() { 30, 10 } } },
            new() { Text = "下", Position = new List<List<double>> { new() { 10, 80 }, new() { 30, 80 } } }
        };

        var text = OcrRecognizeTicketViewModel.JoinOcrTexts(results);
        Assert.Equal("左 右 下", text);
    }
}
