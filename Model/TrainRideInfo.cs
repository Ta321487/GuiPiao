using System.Collections.Generic;

namespace GuiPiao.Model
{
    public class TrainRideInfo
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; }
        public string CheckInLocation { get; set; }
        public string DepartStation { get; set; }
        public string TrainNo { get; set; }
        public string ArriveStation { get; set; }
        public string DepartStationPinyin { get; set; }
        public string ArriveStationPinyin { get; set; }
        public string DepartDate { get; set; }
        public string DepartTime { get; set; }
        public string CoachNo { get; set; }
        public string SeatNo { get; set; }
        public decimal Money { get; set; }
        public string SeatType { get; set; }
        public string AdditionalInfo { get; set; }
        public string TicketPurpose { get; set; }
        public string TicketModificationType { get; set; }
        public int TicketTypeFlags { get; set; }
        public int PaymentChannelFlags { get; set; }
        public string Hint { get; set; }
        public string DepartStationCode { get; set; }
        public string ArriveStationCode { get; set; }

        /// <summary>
        /// 行程状态（0-未出行, 1-已完成, 2-已改签, 3-已退票）
        /// </summary>
        public int Status { get; set; } = (int)TrainRideStatus.NotTraveled;

        /// <summary>
        /// 行程关联的标签列表（非数据库字段，用于UI展示）
        /// </summary>
        public List<TicketTag> Tags { get; set; } = new List<TicketTag>();
    }
}