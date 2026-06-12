using System;

namespace GuiPiao.Model;

/// <summary>
///     811 票面站名按汉字字数（1～5）分别存储字间距与左边距微调；<see cref="ReferenceHanCount" /> 字时使用 <see cref="ObservableTicketFaceLayout.DepartStationLeft" /> 作为基准。
/// </summary>
public static class StationFaceHanCountLayout
{
    public const int MinHanCount = 1;
    public const int MaxHanCount = 5;
    public const int ReferenceHanCount = 4;

    public static int GetBodyHanCount(string? rawStation)
    {
        var body = TicketPreviewDraft.TrimTrailingStation(rawStation);
        if (string.IsNullOrWhiteSpace(body)) return 0;
        var t = body.Trim();
        return TicketPreviewDraft.IsOneToFiveAllHanStationBody(t) ? t.Length : 0;
    }

    public static bool IsAdjustableHanCount(int hanCount) => hanCount is >= MinHanCount and <= MaxHanCount;

    public static int GetDepartSpacing(ObservableTicketFaceLayout layout, int hanCount) =>
        hanCount switch
        {
            1 => layout.DepartStationSpacing1,
            2 => layout.DepartStationSpacing2,
            3 => layout.DepartStationSpacing3,
            4 => layout.DepartStationSpacing4,
            5 => layout.DepartStationSpacing5,
            _ => layout.DepartStationCharacterSpacing
        };

    public static void SetDepartSpacing(ObservableTicketFaceLayout layout, int hanCount, int value)
    {
        switch (hanCount)
        {
            case 1: layout.DepartStationSpacing1 = value; break;
            case 2: layout.DepartStationSpacing2 = value; break;
            case 3: layout.DepartStationSpacing3 = value; break;
            case 4: layout.DepartStationSpacing4 = value; break;
            case 5: layout.DepartStationSpacing5 = value; break;
            default: layout.DepartStationCharacterSpacing = value; break;
        }
    }

    public static double GetDepartLeftOffset(ObservableTicketFaceLayout layout, int hanCount) =>
        hanCount switch
        {
            1 => layout.DepartStationLeftOffset1,
            2 => layout.DepartStationLeftOffset2,
            3 => layout.DepartStationLeftOffset3,
            4 => layout.DepartStationLeftOffset4,
            5 => layout.DepartStationLeftOffset5,
            _ => 0
        };

    public static void SetDepartLeftOffset(ObservableTicketFaceLayout layout, int hanCount, double value)
    {
        switch (hanCount)
        {
            case 1: layout.DepartStationLeftOffset1 = value; break;
            case 2: layout.DepartStationLeftOffset2 = value; break;
            case 3: layout.DepartStationLeftOffset3 = value; break;
            case 4: layout.DepartStationLeftOffset4 = value; break;
            case 5: layout.DepartStationLeftOffset5 = value; break;
        }
    }

    public static double GetDepartCanvasLeft(ObservableTicketFaceLayout layout, int hanCount) =>
        layout.DepartStationLeft + GetDepartLeftOffset(layout, hanCount);

    public static int GetArriveSpacing(ObservableTicketFaceLayout layout, int hanCount) =>
        hanCount switch
        {
            1 => layout.ArriveStationSpacing1,
            2 => layout.ArriveStationSpacing2,
            3 => layout.ArriveStationSpacing3,
            4 => layout.ArriveStationSpacing4,
            5 => layout.ArriveStationSpacing5,
            _ => layout.ArriveStationCharacterSpacing
        };

    public static void SetArriveSpacing(ObservableTicketFaceLayout layout, int hanCount, int value)
    {
        switch (hanCount)
        {
            case 1: layout.ArriveStationSpacing1 = value; break;
            case 2: layout.ArriveStationSpacing2 = value; break;
            case 3: layout.ArriveStationSpacing3 = value; break;
            case 4: layout.ArriveStationSpacing4 = value; break;
            case 5: layout.ArriveStationSpacing5 = value; break;
            default: layout.ArriveStationCharacterSpacing = value; break;
        }
    }

    public static double GetArriveLeftOffset(ObservableTicketFaceLayout layout, int hanCount) =>
        hanCount switch
        {
            1 => layout.ArriveStationLeftOffset1,
            2 => layout.ArriveStationLeftOffset2,
            3 => layout.ArriveStationLeftOffset3,
            4 => layout.ArriveStationLeftOffset4,
            5 => layout.ArriveStationLeftOffset5,
            _ => 0
        };

    public static void SetArriveLeftOffset(ObservableTicketFaceLayout layout, int hanCount, double value)
    {
        switch (hanCount)
        {
            case 1: layout.ArriveStationLeftOffset1 = value; break;
            case 2: layout.ArriveStationLeftOffset2 = value; break;
            case 3: layout.ArriveStationLeftOffset3 = value; break;
            case 4: layout.ArriveStationLeftOffset4 = value; break;
            case 5: layout.ArriveStationLeftOffset5 = value; break;
        }
    }

    public static double GetArriveCanvasLeft(ObservableTicketFaceLayout layout, int hanCount) =>
        layout.ArriveStationLeft + GetArriveLeftOffset(layout, hanCount);
}
