using GuiPiao.Services;
using Xunit;

namespace GuiPiao.Tests.Services;

public class TableTicketImportParserTests
{
    [Fact]
    public void ParseRows_LegacyCsvHeaders_按列序映射()
    {
        var headers = TableTicketImportParser.LegacyCsvHeaders;
        var row = new[]
        {
            "A123", "检票口A", "北京南", "G1", "上海虹桥",
            "2026-08-18", "08:00", "12:30", "0",
            "02", "01A", "553.5", "二等座"
        };

        var rides = TableTicketImportParser.ParseRows(headers, [row]);

        Assert.Single(rides);
        Assert.Equal("G1", rides[0].TrainNo);
        Assert.Equal("北京南", rides[0].DepartStation);
        Assert.Equal("上海虹桥", rides[0].ArriveStation);
        Assert.Equal(553.5m, rides[0].Money);
        Assert.Equal(0, rides[0].ArriveDayOffset);
    }

    [Fact]
    public void ParseRows_ExportStyleHeaders_按别名映射且列序可变()
    {
        var headers = new[] { "车次", "出发站", "到达站", "出发日期", "出发时间", "到达时间", "票价", "票号" };
        var row = new[] { "G2", "天津", "南京南", "2026/8/18", "9:05", "12:40(+1)", "¥680.00", "B9" };

        var rides = TableTicketImportParser.ParseRows(headers, [row]);

        Assert.Single(rides);
        Assert.Equal("G2", rides[0].TrainNo);
        Assert.Equal("天津", rides[0].DepartStation);
        Assert.Equal("南京南", rides[0].ArriveStation);
        Assert.Equal("2026-08-18", rides[0].DepartDate);
        Assert.Equal("09:05", rides[0].DepartTime);
        Assert.Equal("12:40", rides[0].ArriveTime);
        Assert.Equal(1, rides[0].ArriveDayOffset);
        Assert.Equal(680m, rides[0].Money);
        Assert.Equal("B9", rides[0].TicketNumber);
    }

    [Fact]
    public void ParseRows_无表头时_按Legacy列序回退()
    {
        var row = new[]
        {
            "T1", "", "甲站", "K100", "乙站",
            "2026-01-01", "10:00", "11:00", "0",
            "1", "2", "10", "硬座"
        };

        var rides = TableTicketImportParser.ParseRows(null, [row]);

        Assert.Single(rides);
        Assert.Equal("K100", rides[0].TrainNo);
        Assert.Equal("甲站", rides[0].DepartStation);
    }

    [Fact]
    public void SplitCsvLine_支持引号内逗号()
    {
        var fields = TableTicketImportParser.SplitCsvLine("\"北京,南\",G1,上海");

        Assert.Equal(3, fields.Count);
        Assert.Equal("北京,南", fields[0]);
        Assert.Equal("G1", fields[1]);
        Assert.Equal("上海", fields[2]);
    }

    [Fact]
    public void ParseMoney_去掉货币符号()
    {
        Assert.Equal(12.5m, TableTicketImportParser.ParseMoney("¥12.50"));
        Assert.Equal(12.5m, TableTicketImportParser.ParseMoney("￥12.50"));
        Assert.Equal(0m, TableTicketImportParser.ParseMoney(""));
    }

    [Fact]
    public void ParseArriveTime_支持跨天展示()
    {
        var (time, offset) = TableTicketImportParser.ParseArriveTime("04:52(+2)", null);
        Assert.Equal("04:52", time);
        Assert.Equal(2, offset);
    }
}
