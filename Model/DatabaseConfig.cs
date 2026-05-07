namespace GuiPiao.Model
{
    /// <summary>
    /// 数据库配置
    /// </summary>
    public class DatabaseConfig
    {
        /// <summary>
        /// 数据库文件路径
        /// </summary>
        public string DatabasePath { get; set; } = string.Empty;

        /// <summary>
        /// 是否使用自定义路径
        /// </summary>
        public bool UseCustomPath { get; set; } = false;

        #region 自动备份策略配置

        /// <summary>
        /// 是否启用自动备份
        /// </summary>
        public bool AutoBackupEnabled { get; set; } = true;

        /// <summary>
        /// 备份类型 (Full/Incremental/Smart)
        /// Full: 只执行全量备份
        /// Incremental: 只执行增量备份
        /// Smart: 智能混合模式（按频率自动切换）
        /// </summary>
        public string BackupType { get; set; } = "Full";

        /// <summary>
        /// 备份时机 (OnExit/OnStartup/Weekly/Monthly)
        /// </summary>
        public string BackupTiming { get; set; } = "OnExit";

        /// <summary>
        /// 最多保留全量备份数量
        /// </summary>
        public int MaxBackupCount { get; set; } = 10;

        /// <summary>
        /// 全量备份频率 (Weekly/Monthly)
        /// </summary>
        public string FullBackupFrequency { get; set; } = "Weekly";

        /// <summary>
        /// 最多保留全量备份数量（增量模式下）
        /// </summary>
        public int MaxFullBackupCount { get; set; } = 5;

        /// <summary>
        /// 增量备份频率 (Daily/OnExit)
        /// </summary>
        public string IncrementalBackupFrequency { get; set; } = "Daily";

        /// <summary>
        /// 最多保留增量备份数量
        /// </summary>
        public int MaxIncrementalBackupCount { get; set; } = 30;

        /// <summary>
        /// 备份路径
        /// </summary>
        public string BackupPath { get; set; } = string.Empty;

        /// <summary>
        /// 备份后是否自动压缩
        /// </summary>
        public bool AutoCompress { get; set; } = true;

        /// <summary>
        /// 备份失败是否弹出提示
        /// </summary>
        public bool ShowErrorOnFail { get; set; } = true;

        #endregion

        #region 数据库维护与优化

        /// <summary>
        /// 退出时是否自动校验
        /// </summary>
        public bool AutoVerifyOnExit { get; set; } = true;

        /// <summary>
        /// 每月是否自动整理碎片
        /// </summary>
        public bool AutoDefragmentMonthly { get; set; } = true;

        #endregion
    }
}
