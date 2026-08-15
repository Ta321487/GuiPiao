using System.Collections.Generic;
using GuiPiao.Model;
using GuiPiao.Model.Sync;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GuiPiao.Services;

/// <summary>同步 payload / 协议 JSON（字段名稳定，供手机端契约）。</summary>
public static class SyncPayloadSerializer
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        }
    };

    public static string ToJson(object value) => JsonConvert.SerializeObject(value, Settings);

    public static T? FromJson<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        return JsonConvert.DeserializeObject<T>(json, Settings);
    }

    public static string Ride(TrainRideInfo ride) => ToJson(new
    {
        ride.SyncId,
        ride.TicketNumber,
        ride.CheckInLocation,
        ride.DepartStation,
        ride.TrainNo,
        ride.ArriveStation,
        ride.DepartStationPinyin,
        ride.ArriveStationPinyin,
        ride.DepartDate,
        ride.DepartTime,
        ride.ArriveTime,
        ride.ArriveDayOffset,
        ride.CoachNo,
        ride.SeatNo,
        ride.Money,
        ride.SeatType,
        ride.AdditionalInfo,
        ride.TicketPurpose,
        ride.TicketModificationType,
        ride.TicketTypeFlags,
        ride.PaymentChannelFlags,
        ride.Hint,
        ride.DepartStationCode,
        ride.ArriveStationCode,
        ride.Status,
        ride.UpdatedAt,
        ride.DeletedAt
    });

    public static string Tag(TicketTag tag) => ToJson(new
    {
        tag.SyncId,
        tag.Name,
        tag.Color,
        tag.TextColor,
        tag.SortOrder,
        tag.IsDefault,
        tag.UpdatedAt,
        tag.DeletedAt
    });

    public static string RideTags(string rideSyncId, IEnumerable<string> tagSyncIds) =>
        ToJson(new
        {
            RideSyncId = rideSyncId,
            TagSyncIds = tagSyncIds
        });

    public static TrainRideInfo? ParseRide(string? payload)
    {
        var dto = FromJson<RidePayload>(payload);
        if (dto == null || string.IsNullOrWhiteSpace(dto.SyncId)) return null;
        return new TrainRideInfo
        {
            SyncId = dto.SyncId,
            TicketNumber = dto.TicketNumber ?? string.Empty,
            CheckInLocation = dto.CheckInLocation ?? string.Empty,
            DepartStation = dto.DepartStation ?? string.Empty,
            TrainNo = dto.TrainNo ?? string.Empty,
            ArriveStation = dto.ArriveStation ?? string.Empty,
            DepartStationPinyin = dto.DepartStationPinyin ?? string.Empty,
            ArriveStationPinyin = dto.ArriveStationPinyin ?? string.Empty,
            DepartDate = dto.DepartDate ?? string.Empty,
            DepartTime = dto.DepartTime ?? string.Empty,
            ArriveTime = dto.ArriveTime ?? string.Empty,
            ArriveDayOffset = dto.ArriveDayOffset,
            CoachNo = dto.CoachNo ?? string.Empty,
            SeatNo = dto.SeatNo ?? string.Empty,
            Money = dto.Money,
            SeatType = dto.SeatType ?? string.Empty,
            AdditionalInfo = dto.AdditionalInfo ?? string.Empty,
            TicketPurpose = dto.TicketPurpose ?? string.Empty,
            TicketModificationType = dto.TicketModificationType ?? string.Empty,
            TicketTypeFlags = dto.TicketTypeFlags,
            PaymentChannelFlags = dto.PaymentChannelFlags,
            Hint = dto.Hint ?? string.Empty,
            DepartStationCode = dto.DepartStationCode ?? string.Empty,
            ArriveStationCode = dto.ArriveStationCode ?? string.Empty,
            Status = dto.Status,
            UpdatedAt = dto.UpdatedAt ?? string.Empty,
            DeletedAt = dto.DeletedAt
        };
    }

    public static TicketTag? ParseTag(string? payload)
    {
        var dto = FromJson<TagPayload>(payload);
        if (dto == null || string.IsNullOrWhiteSpace(dto.SyncId)) return null;
        return new TicketTag
        {
            SyncId = dto.SyncId,
            Name = dto.Name ?? string.Empty,
            Color = dto.Color ?? string.Empty,
            TextColor = dto.TextColor ?? string.Empty,
            SortOrder = dto.SortOrder,
            IsDefault = dto.IsDefault,
            UpdatedAt = dto.UpdatedAt ?? string.Empty,
            DeletedAt = dto.DeletedAt
        };
    }

    public static RideTagsPayload? ParseRideTags(string? payload) => FromJson<RideTagsPayload>(payload);

    private class RidePayload
    {
        public string? SyncId { get; set; }
        public string? TicketNumber { get; set; }
        public string? CheckInLocation { get; set; }
        public string? DepartStation { get; set; }
        public string? TrainNo { get; set; }
        public string? ArriveStation { get; set; }
        public string? DepartStationPinyin { get; set; }
        public string? ArriveStationPinyin { get; set; }
        public string? DepartDate { get; set; }
        public string? DepartTime { get; set; }
        public string? ArriveTime { get; set; }
        public int ArriveDayOffset { get; set; }
        public string? CoachNo { get; set; }
        public string? SeatNo { get; set; }
        public decimal Money { get; set; }
        public string? SeatType { get; set; }
        public string? AdditionalInfo { get; set; }
        public string? TicketPurpose { get; set; }
        public string? TicketModificationType { get; set; }
        public int TicketTypeFlags { get; set; }
        public int PaymentChannelFlags { get; set; }
        public string? Hint { get; set; }
        public string? DepartStationCode { get; set; }
        public string? ArriveStationCode { get; set; }
        public int Status { get; set; }
        public string? UpdatedAt { get; set; }
        public string? DeletedAt { get; set; }
    }

    private class TagPayload
    {
        public string? SyncId { get; set; }
        public string? Name { get; set; }
        public string? Color { get; set; }
        public string? TextColor { get; set; }
        public int SortOrder { get; set; }
        public bool IsDefault { get; set; }
        public string? UpdatedAt { get; set; }
        public string? DeletedAt { get; set; }
    }

    public class RideTagsPayload
    {
        public string? RideSyncId { get; set; }
        public List<string>? TagSyncIds { get; set; }
    }
}
