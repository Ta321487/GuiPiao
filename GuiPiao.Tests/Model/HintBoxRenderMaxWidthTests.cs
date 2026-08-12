using GuiPiao.Model;
using Xunit;

namespace GuiPiao.Tests.Model;

public class HintBoxRenderMaxWidthTests
{
    [Fact]
    public void HintBoxRenderMaxWidth_DoesNotCrossIntoQr()
    {
        var layout = ObservableTicketFaceLayout.FromTemplate(TicketFaceLayout.BlueDefault());
        layout.HintBoxLeft = 240;
        layout.HintBoxWidth = 480;
        layout.QrLeft = 592;

        // 240 + 480 会盖住二维码；渲染上限应停在二维码左侧留白
        Assert.True(layout.HintBoxRenderMaxWidth < layout.HintBoxWidth);
        Assert.True(layout.HintBoxLeft + layout.HintBoxRenderMaxWidth <= layout.QrLeft - 16);
    }

    [Fact]
    public void HintBoxRenderMaxWidth_UsesConfiguredWidth_WhenRoomBeforeQr()
    {
        var layout = ObservableTicketFaceLayout.FromTemplate(TicketFaceLayout.BlueDefault());
        layout.HintBoxLeft = 48;
        layout.HintBoxWidth = 480;
        layout.QrLeft = 620;

        Assert.Equal(480, layout.HintBoxRenderMaxWidth);
    }

    [Fact]
    public void HintBoxRenderMaxWidth_Updates_WhenQrOrHintBoxMoves()
    {
        var layout = ObservableTicketFaceLayout.FromTemplate(TicketFaceLayout.BlueDefault());
        layout.HintBoxLeft = 200;
        layout.HintBoxWidth = 400;
        layout.QrLeft = 500;

        var before = layout.HintBoxRenderMaxWidth;
        layout.QrLeft = 700;
        Assert.True(layout.HintBoxRenderMaxWidth > before);
        Assert.Equal(400, layout.HintBoxRenderMaxWidth);
    }
}
