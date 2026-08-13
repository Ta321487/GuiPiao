using System.IO;
using GuiPiao.Services;
using Xunit;

namespace GuiPiao.Tests.Services;

public class TicketTextExtractorTests
{
    private readonly TicketTextExtractor _extractor = new();

    [Fact]
    public void Extract_SmsStyle_StillWorks()
    {
        var draft = _extractor.Extract(
            "您已购 3月15日 G1234次 北京南-上海虹桥 08:00开 加05车12A号 二等座 票价553.00元");

        Assert.Equal("G1234", draft.TrainNo);
        Assert.Equal("北京南", draft.DepartStation);
        Assert.Equal("上海虹桥", draft.ArriveStation);
        Assert.Equal("05", draft.CoachNo);
        Assert.True(draft.IsJiaChe);
        Assert.Equal("12A", draft.SeatNo);
        Assert.Equal("二等座", draft.SeatType);
        Assert.Equal("553.00", draft.MoneyText);
    }

    [Fact]
    public void Extract_PaperTicket_CanonicalXxCheXxxHao()
    {
        // 国铁票面常见印刷串（紧凑）：发到站、车次、日期时间开、XX车XXX号、席别、￥价
        var draft = _extractor.Extract(
            "九江站 深圳东站 K1020 2024年01月25日18:55开04车104号 硬座 ￥163.5元");

        Assert.Equal("票面", draft.SourceHint);
        Assert.Equal("K1020", draft.TrainNo);
        Assert.Equal("九江", draft.DepartStation);
        Assert.Equal("深圳东", draft.ArriveStation);
        Assert.Equal("2024-01-25", draft.DepartDate);
        Assert.Equal("18:55", draft.DepartTime);
        Assert.Equal("04", draft.CoachNo);
        Assert.Equal("104", draft.SeatNo);
        Assert.Equal("新空调硬座", draft.SeatType);
        Assert.Equal("163.5", draft.MoneyText);
        Assert.DoesNotContain("车厢", draft.FieldsNeedingReview);
    }

    [Fact]
    public void Extract_PaperTicket_OcrMissingCoachDigits_SeatOnly()
    {
        // OCR 把「04车104号」粘成「车104号」时：至少填座位，车厢标核对
        var draft = _extractor.Extract(
            "九江站 深圳东站 K1020 2024年01月25日 18:55 开 车104号 二等座 ¥163.5元");

        Assert.Equal("K1020", draft.TrainNo);
        Assert.Equal("九江", draft.DepartStation);
        Assert.Equal("深圳东", draft.ArriveStation);
        Assert.Equal("2024-01-25", draft.DepartDate);
        Assert.Equal("18:55", draft.DepartTime);
        Assert.True(string.IsNullOrEmpty(draft.CoachNo));
        Assert.Equal("104", draft.SeatNo);
        Assert.Equal("二等座", draft.SeatType);
        Assert.Equal("163.5", draft.MoneyText);
        Assert.Contains("车厢", draft.FieldsNeedingReview);
    }

    [Fact]
    public void Extract_PaperTicket_HighSpeedSeatLetter()
    {
        var draft = _extractor.Extract(
            "北京南站 上海虹桥站 G1234 2026年03月15日08:00开05车12A号 二等座 ¥553.0元 检票口A12");

        Assert.Equal("G1234", draft.TrainNo);
        Assert.Equal("北京南", draft.DepartStation);
        Assert.Equal("上海虹桥", draft.ArriveStation);
        Assert.Equal("05", draft.CoachNo);
        Assert.Equal("12A", draft.SeatNo);
        Assert.Equal("A12", draft.CheckInLocation);
    }

    [Fact]
    public void Extract_PaperTicket_SleeperBerth()
    {
        var draft = _extractor.Extract(
            "北京站 上海站 T123 2026年01月01日22:00开10车003号中铺 硬卧 ￥300元");

        Assert.Equal("10", draft.CoachNo);
        Assert.Equal("003中铺", draft.SeatNo);
        Assert.Equal("新空调硬卧", draft.SeatType);
    }

    [Fact]
    public void Extract_NoSeat_SetsFlag()
    {
        var draft = _extractor.Extract(
            "沈阳站 长春站 K123 2026年05月01日10:00开05车无座 硬座 ￥28元");

        Assert.Equal("05", draft.CoachNo);
        Assert.True(draft.IsNoSeat);
        Assert.True(string.IsNullOrEmpty(draft.SeatNo));
        Assert.Equal("新空调硬座", draft.SeatType);
    }

    [Fact]
    public void MapSeatType_Aliases()
    {
        Assert.Equal("新空调硬座", TicketTextExtractor.MapSeatTypeToFormOption("硬座"));
        Assert.Equal("新空调硬卧", TicketTextExtractor.MapSeatTypeToFormOption("硬卧"));
        Assert.Equal("新空调软卧", TicketTextExtractor.MapSeatTypeToFormOption("软卧"));
    }

    [Fact]
    public void Extract_OrderDetail_LabeledFields()
    {
        // 12306/第三方订单详情截图 OCR：标签+值，常有到达时间，无票面「站」「开」
        var draft = _extractor.Extract(
            "订单详情 车次 G1234 出发站 北京南 到达站 上海虹桥 " +
            "乘车日期 2026年03月15日 开车时间 08:00 到达时间 12:30 " +
            "座位 05车12A号 席别 二等座 票价 553.00元 检票口 A12 电子客票号 E123456789");

        Assert.Equal("订单", draft.SourceHint);
        Assert.Equal("G1234", draft.TrainNo);
        Assert.Equal("北京南", draft.DepartStation);
        Assert.Equal("上海虹桥", draft.ArriveStation);
        Assert.Equal("2026-03-15", draft.DepartDate);
        Assert.Equal("08:00", draft.DepartTime);
        Assert.Equal("12:30", draft.ArriveTime);
        Assert.Equal("05", draft.CoachNo);
        Assert.Equal("12A", draft.SeatNo);
        Assert.Equal("二等座", draft.SeatType);
        Assert.Equal("553.00", draft.MoneyText);
        Assert.Equal("A12", draft.CheckInLocation);
        Assert.Equal("E123456789", draft.TicketNumber);
    }

    [Fact]
    public void Extract_OrderDetail_ArrowStations_AndArriveDao()
    {
        // App 卡片式：站名箭头 + 开/到，无显式标签
        var draft = _extractor.Extract(
            "订单详情 G1234 北京南 → 上海虹桥 2026年3月15日 08:00开 12:30到 " +
            "05车12A号 二等座 票价¥553.0元");

        Assert.Equal("订单", draft.SourceHint);
        Assert.Equal("G1234", draft.TrainNo);
        Assert.Equal("北京南", draft.DepartStation);
        Assert.Equal("上海虹桥", draft.ArriveStation);
        Assert.Equal("2026-03-15", draft.DepartDate);
        Assert.Equal("08:00", draft.DepartTime);
        Assert.Equal("12:30", draft.ArriveTime);
        Assert.Equal("05", draft.CoachNo);
        Assert.Equal("12A", draft.SeatNo);
        Assert.Equal("553.0", draft.MoneyText);
    }

    [Fact]
    public void Extract_OrderDetail_PrefersTicketPrice_OverServiceFee()
    {
        var draft = _extractor.Extract(
            "订单详情 车次 K1020 出发站 九江 到达站 深圳东 " +
            "乘车日期 2024年01月25日 开车时间 18:55 " +
            "04车104号 硬座 票价163.5元 保险费3.00元 服务费5元");

        Assert.Equal("163.5", draft.MoneyText);
        Assert.Equal("新空调硬座", draft.SeatType);
    }

    [Fact]
    public void Extract_ZhiXing_Sms_TimeRange_AndArrow()
    {
        // 公开资料中智行短信典型结构化字段（脱敏）
        var draft = _extractor.Extract(
            "【智行】订单号：ZHZH2026040412345，G1023次，北京南→上海虹桥，08:15-12:48，二等座，¥553.00");

        Assert.Equal("智行", draft.SourceHint);
        Assert.Equal("G1023", draft.TrainNo);
        Assert.Equal("北京南", draft.DepartStation);
        Assert.Equal("上海虹桥", draft.ArriveStation);
        Assert.Equal("08:15", draft.DepartTime);
        Assert.Equal("12:48", draft.ArriveTime);
        Assert.Equal("二等座", draft.SeatType);
        Assert.Equal("553.00", draft.MoneyText);
        Assert.Equal("ZHZH2026040412345", draft.TicketNumber);
    }

    [Fact]
    public void Extract_ZhiXing_Sms_DingGouCompact()
    {
        var draft = _extractor.Extract(
            "【智行】您订购的3月15日G1234次北京南到上海虹桥，05车12A号，票价553元，订单号：ZHX123456，已支付成功。");

        Assert.Equal("智行", draft.SourceHint);
        Assert.Equal("G1234", draft.TrainNo);
        Assert.Equal("北京南", draft.DepartStation);
        Assert.Equal("上海虹桥", draft.ArriveStation);
        Assert.Equal("05", draft.CoachNo);
        Assert.Equal("12A", draft.SeatNo);
        Assert.Equal("553", draft.MoneyText);
    }

    [Fact]
    public void Extract_Ctrip_Sms_FromTo()
    {
        var draft = _extractor.Extract(
            "【携程网】亲爱的用户，您预订的火车票已经出票成功！订单编号CT123456789，" +
            "乘坐的火车是G1234次列车，从北京南前往上海虹桥，发车时间为2026年3月15日08:00，" +
            "05车12A号二等座，票价553.00元。");

        Assert.Equal("携程", draft.SourceHint);
        Assert.Equal("G1234", draft.TrainNo);
        Assert.Equal("北京南", draft.DepartStation);
        Assert.Equal("上海虹桥", draft.ArriveStation);
        Assert.Equal("2026-03-15", draft.DepartDate);
        Assert.Equal("08:00", draft.DepartTime);
        Assert.Equal("05", draft.CoachNo);
        Assert.Equal("12A", draft.SeatNo);
        Assert.Equal("二等座", draft.SeatType);
    }

    [Fact]
    public void Extract_Fliggy_Sms_OriginDest()
    {
        var draft = _extractor.Extract(
            "【飞猪】尊敬的客户，您预订的火车票已经成功，订单号为FZ987654321，" +
            "出发日期为3月15日，出发地为北京南，目的地为上海虹桥，车次G1234次，" +
            "08:00开，05车12A号，二等座，票价553元。");

        Assert.Equal("飞猪", draft.SourceHint);
        Assert.Equal("G1234", draft.TrainNo);
        Assert.Equal("北京南", draft.DepartStation);
        Assert.Equal("上海虹桥", draft.ArriveStation);
        Assert.Equal("05", draft.CoachNo);
        Assert.Equal("12A", draft.SeatNo);
    }

    [Fact]
    public void Extract_RailwayService_Sms_SeatThenCoach()
    {
        // 公开资料中【铁路客服】旧短信：座位在前、车厢「号车」、时刻+站+发车
        var draft = _extractor.Extract(
            "【铁路客服】订单EB6836546，张三，您已经购买了编号1F，12号车，1月6日G584，09:00武汉站发车。");

        Assert.Equal("短信", draft.SourceHint);
        Assert.Equal("G584", draft.TrainNo);
        Assert.Equal("武汉", draft.DepartStation);
        Assert.Equal("12", draft.CoachNo);
        Assert.Equal("1F", draft.SeatNo);
        Assert.Equal("09:00", draft.DepartTime);
        Assert.Equal("EB6836546", draft.TicketNumber);
    }

    [Fact]
    public void Extract_EmailHtml_StripsTags()
    {
        var html =
            "<html><body><p>购票成功通知</p><p>车次：G1234次</p>" +
            "<p>出发站：北京南&nbsp;到达站：上海虹桥</p>" +
            "<p>乘车日期：2026年03月15日</p><p>开车时间：08:00</p>" +
            "<p>座位：05车12A号</p><p>席别：二等座</p><p>票价：553.00元</p></body></html>";

        var draft = _extractor.Extract(html);
        Assert.Equal("邮件", draft.SourceHint);
        Assert.Equal("G1234", draft.TrainNo);
        Assert.Equal("北京南", draft.DepartStation);
        Assert.Equal("上海虹桥", draft.ArriveStation);
        Assert.Equal("05", draft.CoachNo);
        Assert.Equal("12A", draft.SeatNo);
        Assert.Equal("二等座", draft.SeatType);
        Assert.Equal("553.00", draft.MoneyText);
    }

    [Fact]
    public void Extract_BenrenChePiaoCard()
    {
        var draft = _extractor.Extract(
            "本人车票\nG1234\n北京南-上海虹桥\n03月15日 08:00\n二等座 05车12A号\n已支付");

        Assert.Equal("本人车票", draft.SourceHint);
        Assert.Equal("G1234", draft.TrainNo);
        Assert.Equal("北京南", draft.DepartStation);
        Assert.Equal("上海虹桥", draft.ArriveStation);
        Assert.Equal("08:00", draft.DepartTime);
        Assert.Equal("05", draft.CoachNo);
        Assert.Equal("12A", draft.SeatNo);
    }

    [Fact]
    public void Extract_ShareCard()
    {
        var draft = _extractor.Extract(
            "【行程分享】G1234次 北京南→上海虹桥 2026年3月15日 08:00开 05车12A号 二等座 票价553元");

        Assert.Equal("分享卡", draft.SourceHint);
        Assert.Equal("G1234", draft.TrainNo);
        Assert.Equal("北京南", draft.DepartStation);
        Assert.Equal("上海虹桥", draft.ArriveStation);
        Assert.Equal("2026-03-15", draft.DepartDate);
        Assert.Equal("08:00", draft.DepartTime);
        Assert.Equal("05", draft.CoachNo);
    }

    [Theory]
    [InlineData("sms-12306.txt", "G1234", "北京南")]
    [InlineData("sms-zhixing.txt", "G1023", "北京南")]
    [InlineData("paper-ticket-ocr.txt", "K1020", "九江")]
    [InlineData("share-card.txt", "G1234", "北京南")]
    public void Extract_SamplesDirectory_CoreFields(string fileName, string trainNo, string depart)
    {
        var path = FindSample(fileName);
        if (path == null) return;

        var text = File.ReadAllText(path);
        var draft = _extractor.Extract(text);
        Assert.Equal(trainNo, draft.TrainNo);
        Assert.Equal(depart, draft.DepartStation);
    }

    private static string? FindSample(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Samples", "Ocr", fileName);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        return null;
    }
}
