# Repository 拆分方案

## 背景

当 Repository 文件过大（超过 800-1000 行）时，会影响代码的可读性和维护性。本文档提供 Repository 拆分的参考方案。

## 拆分时机

- 文件行数超过 **800-1000 行**
- 单个类包含超过 **15-20 个方法**
- 不同功能模块的代码开始混杂

## 方案一：按功能拆分（推荐）

适用于功能复杂的 Repository，如 `TrainRideRepository`。

### 目录结构

```
DataAccess/
└── TrainRide/
    ├── TrainRideRepository.cs          # 主文件，包含通用方法和接口
    ├── TrainRideQueryRepository.cs     # 查询相关操作
    ├── TrainRideCommandRepository.cs   # 增删改操作
    ├── TrainRidePageRepository.cs      # 分页查询相关
    └── TrainRideSql.cs                 # SQL 语句常量（可选）
```

### 文件职责

#### 1. TrainRideRepository.cs（主文件）
```csharp
namespace GuiPiao.DataAccess.TrainRide
{
    /// <summary>
    /// 票务数据访问主类
    /// </summary>
    public partial class TrainRideRepository
    {
        protected readonly string _connectionString = ConfigManager.Instance.DatabaseConnectionString;

        /// <summary>
        /// 标准化日期格式
        /// </summary>
        protected string NormalizeDate(string dateStr)
        {
            // 通用日期处理逻辑
        }
    }
}
```

#### 2. TrainRideQueryRepository.cs（查询操作）
```csharp
namespace GuiPiao.DataAccess.TrainRide
{
    /// <summary>
    /// 票务查询操作
    /// </summary>
    public partial class TrainRideRepository
    {
        // 根据ID查询
        public async Task<TrainRideInfo> GetTrainRideByIdAsync(int id) { }

        // 根据日期查询
        public async Task<IEnumerable<TrainRideInfo>> GetTrainRidesByDateAsync(string date) { }

        // 根据车站查询
        public async Task<IEnumerable<TrainRideInfo>> GetTrainRidesByStationAsync(string station) { }

        // 根据车次查询
        public async Task<IEnumerable<TrainRideInfo>> GetTrainRidesByTrainNoAsync(string trainNo) { }

        // 搜索
        public async Task<IEnumerable<TrainRideInfo>> SearchTrainRidesAsync(string keyword) { }

        // 获取所有
        public async Task<IEnumerable<TrainRideInfo>> GetAllTrainRidesAsync() { }
    }
}
```

#### 3. TrainRideCommandRepository.cs（增删改操作）
```csharp
namespace GuiPiao.DataAccess.TrainRide
{
    /// <summary>
    /// 票务增删改操作
    /// </summary>
    public partial class TrainRideRepository
    {
        // 添加
        public async Task<int> AddTrainRideAsync(TrainRideInfo trainRide) { }

        // 更新
        public async Task<int> UpdateTrainRideAsync(TrainRideInfo trainRide) { }

        // 删除
        public async Task<int> DeleteTrainRideAsync(int id) { }

        // 批量删除
        public async Task<int> DeleteTrainRidesAsync(IEnumerable<int> ids) { }

        // 清空所有数据
        public async Task<int> ClearAllTrainRidesAsync() { }
    }
}
```

#### 4. TrainRidePageRepository.cs（分页查询）
```csharp
namespace GuiPiao.DataAccess.TrainRide
{
    /// <summary>
    /// 票务分页查询操作
    /// </summary>
    public partial class TrainRideRepository
    {
        // 基础分页查询
        public async Task<IEnumerable<TrainRideInfo>> GetTrainRidesByPageAsync(
            int pageIndex, int pageSize, string sortColumn = "id", bool sortDesc = true) { }

        // 带日期范围的分页查询
        public async Task<IEnumerable<TrainRideInfo>> GetTrainRidesByPageAsync(
            int pageIndex, int pageSize, string sortColumn, bool sortDesc,
            DateTime? startDate, DateTime? endDate) { }

        // 获取总记录数
        public async Task<int> GetTotalTrainRidesCountAsync() { }

        // 带日期范围的总记录数
        public async Task<int> GetTotalTrainRidesCountAsync(DateTime? startDate, DateTime? endDate) { }
    }
}
```

#### 5. TrainRideSql.cs（SQL 常量，可选）
```csharp
namespace GuiPiao.DataAccess.TrainRide
{
    /// <summary>
    /// 票务相关 SQL 语句
    /// </summary>
    public static class TrainRideSql
    {
        // 基础列选择
        public const string BaseSelectColumns = @"
            id AS Id,
            ticket_number AS TicketNumber,
            check_in_location AS CheckInLocation,
            depart_station AS DepartStation,
            train_no AS TrainNo,
            arrive_station AS ArriveStation,
            depart_station_pinyin AS DepartStationPinyin,
            arrive_station_pinyin AS ArriveStationPinyin,
            depart_date AS DepartDate,
            depart_time AS DepartTime,
            coach_no AS CoachNo,
            seat_no AS SeatNo,
            money AS Money,
            seat_type AS SeatType,
            additional_info AS AdditionalInfo,
            ticket_purpose AS TicketPurpose,
            ticket_modification_type AS TicketModificationType,
            ticket_type_flags AS TicketTypeFlags,
            payment_channel_flags AS PaymentChannelFlags,
            hint AS Hint,
            depart_station_code AS DepartStationCode,
            arrive_station_code AS ArriveStationCode
        ";

        // 查询语句模板
        public const string SelectById = $@"SELECT {BaseSelectColumns} FROM train_ride_info WHERE id = @Id";

        public const string SelectByDate = $@"SELECT {BaseSelectColumns} FROM train_ride_info WHERE depart_date = @Date";

        // 插入语句
        public const string Insert = @"
            INSERT INTO train_ride_info (
                ticket_number, check_in_location, depart_station, train_no, arrive_station,
                depart_station_pinyin, arrive_station_pinyin, depart_date, depart_time, coach_no,
                seat_no, money, seat_type, additional_info, ticket_purpose, ticket_modification_type,
                ticket_type_flags, payment_channel_flags, hint, depart_station_code, arrive_station_code
            ) VALUES (
                @TicketNumber, @CheckInLocation, @DepartStation, @TrainNo, @ArriveStation,
                @DepartStationPinyin, @ArriveStationPinyin, @DepartDate, @DepartTime, @CoachNo,
                @SeatNo, @Money, @SeatType, @AdditionalInfo, @TicketPurpose, @TicketModificationType,
                @TicketTypeFlags, @PaymentChannelFlags, @Hint, @DepartStationCode, @ArriveStationCode
            );
            SELECT last_insert_rowid();
        ";

        // 更新语句
        public const string Update = @"
            UPDATE train_ride_info
            SET ticket_number = @TicketNumber, check_in_location = @CheckInLocation,
                depart_station = @DepartStation, train_no = @TrainNo, arrive_station = @ArriveStation,
                depart_station_pinyin = @DepartStationPinyin, arrive_station_pinyin = @ArriveStationPinyin,
                depart_date = @DepartDate, depart_time = @DepartTime, coach_no = @CoachNo,
                seat_no = @SeatNo, money = @Money, seat_type = @SeatType,
                additional_info = @AdditionalInfo, ticket_purpose = @TicketPurpose,
                ticket_modification_type = @TicketModificationType, ticket_type_flags = @TicketTypeFlags,
                payment_channel_flags = @PaymentChannelFlags, hint = @Hint,
                depart_station_code = @DepartStationCode, arrive_station_code = @ArriveStationCode
            WHERE id = @Id;
        ";

        // 删除语句
        public const string DeleteById = "DELETE FROM train_ride_info WHERE id = @Id";

        public const string ClearAll = "DELETE FROM train_ride_info";
    }
}
```

## 方案二：按读写分离拆分

适用于读写操作都很复杂的场景。

### 目录结构

```
DataAccess/
├── Base/
│   └── RepositoryBase.cs              # 通用基类
├── Interfaces/
│   └── ITrainRideRepository.cs        # 接口定义
└── Implementations/
    ├── TrainRideReadRepository.cs     # 读操作实现
    └── TrainRideWriteRepository.cs    # 写操作实现
```

### 代码示例

#### 1. ITrainRideRepository.cs（接口）
```csharp
namespace GuiPiao.DataAccess.Interfaces
{
    public interface ITrainRideRepository
    {
        // 查询
        Task<TrainRideInfo> GetByIdAsync(int id);
        Task<IEnumerable<TrainRideInfo>> GetByDateAsync(string date);
        Task<IEnumerable<TrainRideInfo>> GetAllAsync();

        // 命令
        Task<int> AddAsync(TrainRideInfo entity);
        Task<int> UpdateAsync(TrainRideInfo entity);
        Task<int> DeleteAsync(int id);
    }
}
```

#### 2. TrainRideReadRepository.cs（读操作）
```csharp
namespace GuiPiao.DataAccess.Implementations
{
    public class TrainRideReadRepository : RepositoryBase
    {
        public async Task<TrainRideInfo> GetByIdAsync(int id) { }
        public async Task<IEnumerable<TrainRideInfo>> GetByDateAsync(string date) { }
        public async Task<IEnumerable<TrainRideInfo>> GetAllAsync() { }
    }
}
```

#### 3. TrainRideWriteRepository.cs（写操作）
```csharp
namespace GuiPiao.DataAccess.Implementations
{
    public class TrainRideWriteRepository : RepositoryBase
    {
        public async Task<int> AddAsync(TrainRideInfo entity) { }
        public async Task<int> UpdateAsync(TrainRideInfo entity) { }
        public async Task<int> DeleteAsync(int id) { }
    }
}
```

## 拆分建议

| 场景 | 推荐方案 | 说明 |
|------|----------|------|
| 方法数量多，功能复杂 | 方案一 | 按功能细分，职责清晰 |
| 读写操作都很复杂 | 方案二 | 读写分离，便于优化 |
| SQL 语句重复多 | 方案一 + SQL 常量类 | 避免 SQL 重复 |

## 迁移步骤

1. **创建新目录结构**
   ```bash
   mkdir DataAccess/TrainRide
   ```

2. **逐步迁移代码**
   - 先创建主文件 `TrainRideRepository.cs`
   - 将通用方法（如 `NormalizeDate`）移到主文件
   - 按功能将其他方法移到对应的分部类文件

3. **更新命名空间引用**
   - 原文件：`namespace GuiPiao.DataAccess`
   - 新文件：`namespace GuiPiao.DataAccess.TrainRide`

4. **更新 using 语句**
   - 修改所有引用该 Repository 的文件

5. **测试验证**
   - 确保所有功能正常工作
   - 检查是否有遗漏的方法

## 注意事项

1. **保持向后兼容**
   - 可以使用 `using` 别名保持原有引用方式
   ```csharp
   using TrainRideRepository = GuiPiao.DataAccess.TrainRide.TrainRideRepository;
   ```

2. **避免过度拆分**
   - 文件少于 500 行不需要拆分
   - 方法少于 10 个不需要拆分

3. **保持代码一致性**
   - 所有分部类使用相同的命名空间
   - 保持编码风格一致

## 参考标准

| 指标 | 建议拆分阈值 |
|------|-------------|
| 文件行数 | > 800 行 |
| 方法数量 | > 15 个 |
| SQL 语句数量 | > 20 个 |
| 循环依赖 | 存在时考虑拆分 |

---

**创建日期**: 2026-02-21  
**适用项目**: GuiPiao  
**版本**: v1.0
