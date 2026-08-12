using GuiPiao.Model;
using Xunit;

namespace GuiPiao.Tests.Model;

public class TicketPreviewDraftIdMaskTests
{
    [Theory]
    [InlineData("110101199001011234", "1101011990****1234")]
    [InlineData("110101900101123", "11010190***123")]
    [InlineData("", "")]
    [InlineData("1101011990", "1101011990")]
    [InlineData("11010119900101", "1101011990****")]
    public void ComputeDefaultIdMask_按真票规则打码(string input, string expected)
    {
        Assert.Equal(expected, TicketPreviewDraft.ComputeDefaultIdMask(input));
    }

    [Fact]
    public void IdNumberChanged_始终刷新掩码()
    {
        var draft = new TicketPreviewDraft(new TripItem { TrainNo = "G1" });
        draft.IdMask = "手改掩码";
        draft.IdNumber = "110101199001011234";
        Assert.Equal("1101011990****1234", draft.IdMask);
    }
}
