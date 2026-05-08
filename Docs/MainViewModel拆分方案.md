# MainViewModel 分析与拆分方案

## 一、当前 MainViewModel 结构分析

**当前代码统计**：约 2139 行，包含 20+ 个功能模块

### 1.1 功能模块梳理

| 模块 | 行数范围 | 功能描述 | 依赖关系 |
|------|----------|----------|----------|
| **高级检索区** | L30-L42 | 搜索条件绑定属性 | 独立 |
| **行程列表数据** | L44-L65 | TripItems 集合和 CollectionView | 依赖数据访问 |
| **布局配置** | L82-L227 | 左/右/底层面板宽度、锁定状态 | 依赖 UISettingsService |
| **分页功能** | L229-L294 | 分页按钮、页码、折叠展开 | 依赖 GeneralSettingsService |
| **数据访问服务** | L296-L307 | Repository 和 Service 引用 | 核心依赖 |
| **仪表盘图表** | L308-L324 | DashboardCharts 集合、列数配置 | 依赖 DashboardSettingsService |
| **构造函数** | L325-L474 | 初始化、事件订阅、Messenger 注册 | 依赖所有服务 |
| **布局设置方法** | L476-L527 | 加载/保存布局、应用滚动条样式 | 依赖 UISettingsService |
| **排序功能** | L529-L655 | 排序状态初始化、保存、列头指示器 | 依赖 UISettingsService |
| **启动页面** | L657-L706 | 启动页面设置、恢复上次页面 | 依赖 GeneralSettingsService |
| **数据加载** | L708-L927 | 加载行程数据、分组、分页 | 依赖 TrainRideRepository |
| **菜单命令** | L1129-L1196 | 存储/票务/行程/工具/配置/帮助菜单 | 独立 |
| **快捷功能区** | L1198-L1259 | 新增/OCR/地图/预览/备份/设置 | 依赖多个服务 |
| **高级检索命令** | L1261-L1277 | 搜索/清空条件 | 独立 |
| **仪表盘功能** | L1279-L1549 | 图表初始化、加载、刷新、定时器 | 依赖 DashboardSettingsService |
| **日志功能** | L1551-L1589 | 导出/清空日志 | 依赖 LogService |
| **行程操作** | L1591-L1731 | 双击处理、查看/编辑/删除行程 | 依赖 TrainRideRepository |
| **分页命令** | L1733-L1762 | 上一页/下一页/跳转 | 依赖数据加载 |
| **导出功能** | L1772-L1982 | 导出行程列表到 Excel/CSV/PDF | 依赖 ExportService |
| **资源释放** | L2004-L2057 | IDisposable 实现、事件取消订阅 | 依赖所有服务 |
| **事件处理** | L2059-L2136 | 仪表盘配置保存、日志变更等 | 依赖多个服务 |

### 1.2 依赖的服务和 Repository

```csharp
// Repository
private readonly TrainRideRepository _trainRideRepository;

// Services
private readonly GeneralSettingsService _generalSettingsService;
private readonly LogService _logService;
private readonly IChartDataService _chartDataService;
private readonly DashboardSettingsService _dashboardSettingsService;
private readonly ExportService _exportService;
private readonly ExportSettingsService _exportSettingsService;
private readonly ConfirmationService _confirmationService;
public UISettingsService UISettingsService { get; }
```

### 1.3 Messenger 消息订阅

| 消息类型 | 处理逻辑 | 所属模块 |
|----------|----------|----------|
| SettingsChangedMessage | 刷新行程列表或日志 | 设置联动 |
| LayoutChangedMessage | 实时应用布局设置 | 布局管理 |
| DataGridColumnsChangedMessage | 更新列配置 | 表格设置 |
| GroupSettingChangedMessage | 重新加载数据应用分组 | 数据分组 |
| UISettingsChangedMessage | 应用滚动条样式、日志显示设置 | UI 设置 |
| LogColorsChangedMessage | 刷新日志列表 | 日志管理 |

---

## 二、拆分方案设计

### 2.1 拆分原则

1. **单一职责原则**：每个 ViewModel 只负责一个功能领域
2. **保持现有功能不变**：拆分后所有功能行为完全一致
3. **最小化改动**：利用现有项目结构，不引入复杂框架
4. **清晰的依赖关系**：通过构造函数注入依赖
5. **Messenger 通信**：保持现有消息机制进行模块间通信

### 2.2 拆分后的结构

```
ViewModel/
├── MainViewModel.cs                    # 主 ViewModel，协调各模块
├── LayoutViewModel.cs                  # 布局管理模块
├── TripListViewModel.cs                # 行程列表模块（数据+分页+分组）
├── DashboardViewModel.cs               # 仪表盘模块
├── LogPanelViewModel.cs                # 日志面板模块
├── MenuViewModel.cs                    # 菜单命令模块
├── QuickActionsViewModel.cs            # 快捷功能区模块
├── SearchPanelViewModel.cs             # 高级检索区模块
└── Common/
    ├── ViewModelBase.cs                # 基础 ViewModel 类
    └── IDisposableViewModel.cs         # 带资源释放的基类
```

---

## 三、各模块详细设计

### 3.1 MainViewModel（协调器）

**职责**：
- 作为 View 的主要 DataContext
- 聚合各子 ViewModel
- 处理跨模块协调
- 管理生命周期（初始化、释放）

**代码规模**：约 200-300 行

**核心属性**：
```csharp
public partial class MainViewModel : ObservableObject, IDisposable
{
    public LayoutViewModel Layout { get; }
    public TripListViewModel TripList { get; }
    public DashboardViewModel Dashboard { get; }
    public LogPanelViewModel LogPanel { get; }
    public MenuViewModel Menu { get; }
    public QuickActionsViewModel QuickActions { get; }
    public SearchPanelViewModel SearchPanel { get; }
    
    // 需要暴露给 View 的聚合属性
    public UISettingsService UISettingsService => Layout.UISettingsService;
}
```

**XAML 绑定调整**：
```xml
<!-- 原绑定 -->
<TextBox Text="{Binding DepartStation}" />
<Button Command="{Binding SearchCommand}" />

<!-- 新绑定 -->
<TextBox Text="{Binding SearchPanel.DepartStation}" />
<Button Command="{Binding SearchPanel.SearchCommand}" />
```

---

### 3.2 LayoutViewModel（布局管理）

**职责**：
- 左/右/底层面板宽度和锁定状态
- 分割条可见性
- 滚动条样式
- 操作按钮显示设置
- 日志面板显示设置

**代码规模**：约 150-200 行

**核心属性**：
```csharp
public partial class LayoutViewModel : ObservableObject
{
    [ObservableProperty] private int _leftPanelWidth = 180;
    [ObservableProperty] private bool _leftPanelLocked = true;
    [ObservableProperty] private int _rightPanelWidth = 220;
    [ObservableProperty] private bool _rightPanelLocked = true;
    [ObservableProperty] private int _bottomPanelHeight = 250;
    [ObservableProperty] private bool _bottomPanelLocked = true;
    
    [ObservableProperty] private string _scrollbarStyle = "Normal";
    [ObservableProperty] private bool _showActionButtonsOnHover = true;
    [ObservableProperty] private bool _showTimestamp = true;
    [ObservableProperty] private bool _showModuleSource = true;
    [ObservableProperty] private string _logRowHeight = "Standard";
    
    // 计算属性
    public GridLength LeftColumnWidth => new GridLength(LeftPanelWidth);
    public bool LeftSplitterVisible => !LeftPanelLocked;
    // ... 其他计算属性
    
    public UISettingsService UISettingsService { get; }
}
```

**方法**：
- `LoadLayoutSettings()` - 加载布局配置
- `SaveLayoutSettings()` - 保存布局配置
- `ApplyScrollbarStyle()` - 应用滚动条样式

---

### 3.3 TripListViewModel（行程列表）

**职责**：
- 行程数据加载（分页、排序、分组）
- CollectionView 管理
- 选中项管理
- 分页控制
- 表格折叠/展开
- 行程操作（查看、编辑、删除）
- 数据导出

**代码规模**：约 400-500 行

**核心属性**：
```csharp
public partial class TripListViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<TripItem> _tripItems;
    [ObservableProperty] private TripItem? _selectedTripItem;
    [ObservableProperty] private ICollectionView? _tripItemsView;
    
    // 分页
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalItems = 0;
    [ObservableProperty] private ObservableCollection<int> _paginationButtons;
    
    // 表格折叠
    [ObservableProperty] private bool _isTripListExpanded = true;
    
    // 排序状态
    private string _currentSortColumn = "id";
    private bool _currentSortDesc = true;
}
```

**核心方法**：
- `LoadTripItemsAsync()` - 加载行程数据
- `RecreateCollectionView()` - 重新创建 CollectionView
- `ApplyGroupingToCollectionView()` - 应用分组
- `SaveDataSortInfoAsync()` - 保存排序信息
- `PreviousPageCommand/NextPageCommand/GoToPageCommand` - 分页命令
- `ViewTripCommand/EditTripCommand/DeleteTripCommand` - 行程操作
- `OnExportTripListAsync()` - 导出数据

---

### 3.4 DashboardViewModel（仪表盘）

**职责**：
- 仪表盘图表管理
- 图表数据加载
- 自动刷新定时器
- 布局配置

**代码规模**：约 250-300 行

**核心属性**：
```csharp
public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<DashboardChartViewModel> _dashboardCharts = new();
    [ObservableProperty] private int _dashboardColumns = 2;
    
    public bool HasDashboardCharts => DashboardCharts.Count > 0;
    public DashboardConfig DashboardConfig => _dashboardSettingsService.Config;
}
```

**核心方法**：
- `InitializeDashboardAsync()` - 初始化仪表盘
- `LoadDashboardChartsAsync()` - 加载图表
- `RefreshDashboardDataAsync()` - 刷新数据
- `SetupWeeklyRefreshTimer()` - 设置定时器
- `StatisticsConfigCommand` - 打开设置
- `RefreshStatisticsCommand` - 手动刷新

---

### 3.5 LogPanelViewModel（日志面板）

**职责**：
- 日志数据加载
- 日志导出/清空
- 日志变更监听

**代码规模**：约 100-150 行

**核心属性**：
```csharp
public partial class LogPanelViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<LogItem> _logItems;
}
```

**核心方法**：
- `LoadLogItemsAsync()` - 加载日志
- `ExportLog()` - 导出日志
- `ClearLog()` - 清空日志
- `OnLogsChanged()` - 日志变更处理

---

### 3.6 MenuViewModel（菜单命令）

**职责**：
- 所有菜单命令（存储、票务、行程、工具、配置、帮助）
- 设置窗口打开
- 日志管理器打开

**代码规模**：约 100-150 行

**核心命令**：
```csharp
[RelayCommand] private void StorageMenuCommand(string action)
[RelayCommand] private void TicketMenuCommand(string action)
[RelayCommand] private void TripMenuCommand(string action)
[RelayCommand] private void ToolsMenuCommand(string action)
[RelayCommand] private void ConfigMenuCommand(string action)
[RelayCommand] private void HelpMenuCommand(string action)
[RelayCommand] private void OpenLogManager()
[RelayCommand] private void OpenSettings(string? pageName = null)
```

---

### 3.7 QuickActionsViewModel（快捷功能区）

**职责**：
- 快捷功能按钮命令
- 新票录入、OCR识别、地图、预览、备份、设置

**代码规模**：约 100-150 行

**核心命令**：
```csharp
[RelayCommand] public void NewTicketRecordCommand()
[RelayCommand] private void OcrRecognizeTicketCommand()
[RelayCommand] private void OpenTicketMap()
[RelayCommand] private void TicketPreviewCommand()
[RelayCommand] private void BackupRestoreDatabaseCommand()
[RelayCommand] private void SystemConfigCommand()
```

---

### 3.8 SearchPanelViewModel（高级检索区）

**职责**：
- 搜索条件属性
- 搜索/清空命令

**代码规模**：约 50-80 行

**核心属性**：
```csharp
public partial class SearchPanelViewModel : ObservableObject
{
    [ObservableProperty] private bool _isSearchExpanded;
    [ObservableProperty] private string _departStation;
    [ObservableProperty] private string _arriveStation;
    [ObservableProperty] private string _trainNo;
    [ObservableProperty] private string _dateRange;
    [ObservableProperty] private string _status;
}
```

**核心命令**：
```csharp
[RelayCommand] private void SearchCommand()
[RelayCommand] private void ClearConditionCommand()
```

---

## 四、模块间通信方案

### 4.1 保持现有 Messenger 机制

各子 ViewModel 独立订阅需要的消息：

```csharp
// TripListViewModel 订阅分组设置变更
WeakReferenceMessenger.Default.Register<GroupSettingChangedMessage>(this, async (recipient, message) =>
{
    await LoadTripItemsAsync();
});

// LayoutViewModel 订阅布局变更
WeakReferenceMessenger.Default.Register<LayoutChangedMessage>(this, (recipient, message) =>
{
    LeftPanelWidth = message.LeftPanelWidth;
    // ...
});
```

### 4.2 父子 ViewModel 通信

通过事件或委托进行父子通信：

```csharp
// MainViewModel 中协调
public MainViewModel()
{
    // 当行程列表需要刷新仪表盘时
    TripList.DataChanged += async (s, e) => await Dashboard.RefreshDashboardDataAsync();
    
    // 当搜索执行时
    SearchPanel.SearchExecuted += async (s, e) => await TripList.LoadTripItemsAsync();
}
```

---

## 五、实施步骤

### 步骤 1：创建基础类（低风险）

1. 创建 `ViewModelBase.cs` - 可选，如果不需要额外功能可以省略
2. 验证项目编译正常

### 步骤 2：提取独立模块（低风险）

按以下顺序提取，每个步骤后验证编译和运行：

1. **SearchPanelViewModel** - 完全独立，无依赖
2. **MenuViewModel** - 依赖 MessageBoxWindow，无状态依赖
3. **QuickActionsViewModel** - 依赖 ServiceManager，需要注入

### 步骤 3：提取半独立模块（中风险）

1. **LogPanelViewModel** - 依赖 LogService，需要处理日志变更事件
2. **LayoutViewModel** - 依赖 UISettingsService，需要处理 Messenger

### 步骤 4：提取核心模块（高风险）

1. **DashboardViewModel** - 依赖 DashboardSettingsService、IChartDataService
2. **TripListViewModel** - 最复杂，依赖 TrainRideRepository、GeneralSettingsService

### 步骤 5：重构 MainViewModel（高风险）

1. 将 MainViewModel 改为协调器模式
2. 聚合所有子 ViewModel
3. 更新 XAML 绑定

### 步骤 6：全面测试

1. 功能测试：验证所有功能正常
2. 内存测试：验证无内存泄漏
3. 性能测试：验证无性能退化

---

## 六、XAML 绑定更新对照表

| 原绑定 | 新绑定 | 所属模块 |
|--------|--------|----------|
| `{Binding DepartStation}` | `{Binding SearchPanel.DepartStation}` | SearchPanel |
| `{Binding SearchCommand}` | `{Binding SearchPanel.SearchCommand}` | SearchPanel |
| `{Binding TripItems}` | `{Binding TripList.TripItems}` | TripList |
| `{Binding SelectedTripItem}` | `{Binding TripList.SelectedTripItem}` | TripList |
| `{Binding CurrentPage}` | `{Binding TripList.CurrentPage}` | TripList |
| `{Binding PaginationButtons}` | `{Binding TripList.PaginationButtons}` | TripList |
| `{Binding IsTripListExpanded}` | `{Binding TripList.IsTripListExpanded}` | TripList |
| `{Binding ToggleTripListCommand}` | `{Binding TripList.ToggleTripListCommand}` | TripList |
| `{Binding LeftPanelWidth}` | `{Binding Layout.LeftPanelWidth}` | Layout |
| `{Binding LeftColumnWidth}` | `{Binding Layout.LeftColumnWidth}` | Layout |
| `{Binding LeftSplitterVisible}` | `{Binding Layout.LeftSplitterVisible}` | Layout |
| `{Binding ScrollbarStyle}` | `{Binding Layout.ScrollbarStyle}` | Layout |
| `{Binding ShowTimestamp}` | `{Binding Layout.ShowTimestamp}` | Layout |
| `{Binding DashboardCharts}` | `{Binding Dashboard.DashboardCharts}` | Dashboard |
| `{Binding HasDashboardCharts}` | `{Binding Dashboard.HasDashboardCharts}` | Dashboard |
| `{Binding DashboardColumns}` | `{Binding Dashboard.DashboardColumns}` | Dashboard |
| `{Binding StatisticsConfigCommand}` | `{Binding Dashboard.StatisticsConfigCommand}` | Dashboard |
| `{Binding LogItems}` | `{Binding LogPanel.LogItems}` | LogPanel |
| `{Binding ExportLogCommand}` | `{Binding LogPanel.ExportLogCommand}` | LogPanel |
| `{Binding StorageMenuCommand}` | `{Binding Menu.StorageMenuCommand}` | Menu |
| `{Binding OpenSettingsCommand}` | `{Binding Menu.OpenSettingsCommand}` | Menu |
| `{Binding NewTicketRecordCommand}` | `{Binding QuickActions.NewTicketRecordCommand}` | QuickActions |
| `{Binding OpenTicketMapCommand}` | `{Binding QuickActions.OpenTicketMapCommand}` | QuickActions |

---

## 七、风险评估与缓解

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|--------|------|----------|
| 绑定失效 | 中 | 高 | 逐模块替换，每次验证 |
| 事件重复订阅 | 中 | 高 | 确保 Dispose 正确取消订阅 |
| 循环依赖 | 低 | 高 | 使用 Messenger 解耦 |
| 内存泄漏 | 中 | 中 | 确保所有 ViewModel 正确释放 |
| 性能下降 | 低 | 中 | 保持现有数据加载逻辑 |

---

## 八、预期收益

1. **代码可维护性**：每个模块 < 500 行，易于理解和修改
2. **测试友好**：可以单独测试每个 ViewModel
3. **团队协作**：不同开发者可同时修改不同模块
4. **功能隔离**：修改一个模块不影响其他模块
5. **代码复用**：子 ViewModel 可在其他页面复用

---

## 九、总结

本拆分方案遵循以下原则：

1. **渐进式重构**：分步骤实施，每步可验证
2. **保持功能不变**：所有现有功能行为完全一致
3. **利用现有机制**：继续使用 Messenger 进行通信
4. **最小化改动**：不引入新框架，使用现有项目结构

通过将 MainViewModel 拆分为 7 个子模块，可以将代码从 2139 行减少到每个模块 200-500 行，大大提升代码的可维护性和可读性。
