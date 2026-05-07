namespace GuiPiao.Messages
{
    /// <summary>
    /// 状态栏消息通知
    /// </summary>
    public class StatusMessageMessage
    {
        /// <summary>
        /// 状态消息
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 是否自动恢复为"就绪"
        /// </summary>
        public bool AutoReset { get; }

        /// <summary>
        /// 自动恢复延迟（秒）
        /// </summary>
        public int ResetDelaySeconds { get; }

        public StatusMessageMessage(string message, bool autoReset = true, int resetDelaySeconds = 2)
        {
            Message = message;
            AutoReset = autoReset;
            ResetDelaySeconds = resetDelaySeconds;
        }
    }
}
