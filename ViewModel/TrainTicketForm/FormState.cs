namespace GuiPiao.ViewModel.TrainTicketForm
{
    /// <summary>
    /// 表单状态（用于撤销重做）
    /// </summary>
    public class FormState
    {
        public TrainTicketFormData Data { get; set; } = new TrainTicketFormData();
        public string PropertyName { get; set; } = string.Empty;

        public static FormState FromFormData(TrainTicketFormData data, string propertyName)
        {
            return new FormState
            {
                Data = data,
                PropertyName = propertyName
            };
        }

        public void ApplyTo(TrainTicketFormData target)
        {
            Data.CopyTo(target);
        }
    }
}
