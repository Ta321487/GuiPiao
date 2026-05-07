using System;

namespace GuiPiao.Model
{
    /// <summary>
    /// 增量备份信息
    /// </summary>
    public class IncrementalBackupInfo
    {
        /// <summary>
        /// 备份ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 备份时间
        /// </summary>
        public DateTime BackupTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 备份文件路径
        /// </summary>
        public string BackupPath { get; set; } = string.Empty;

        /// <summary>
        /// 备份开始时间（数据范围）
        /// </summary>
        public DateTime DataStartTime { get; set; }

        /// <summary>
        /// 备份结束时间（数据范围）
        /// </summary>
        public DateTime DataEndTime { get; set; }

        /// <summary>
        /// 备份的记录数量
        /// </summary>
        public int RecordCount { get; set; }

        /// <summary>
        /// 备份文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 备份类型（Incremental/Full）
        /// </summary>
        public string BackupType { get; set; } = "Incremental";

        /// <summary>
        /// 基于哪个全量备份
        /// </summary>
        public string? BaseFullBackupPath { get; set; }

        /// <summary>
        /// 是否已压缩
        /// </summary>
        public bool IsCompressed { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
    }
}
