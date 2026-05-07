# 标签功能文档

## 概述

标签功能允许用户为车票添加多个标签，便于分类、筛选和管理车票。

## 数据模型

### TicketTag（标签实体）
```csharp
public class TicketTag
{
    public int Id { get; set; }           // 标签ID
    public string Name { get; set; }      // 标签名称
    public string Color { get; set; }     // 背景颜色（HEX格式）
    public string TextColor { get; set; } // 文字颜色（HEX格式）
    public int SortOrder { get; set; }    // 排序顺序
    public string CreatedAt { get; set; } // 创建时间
}
```

### TrainRideTag（行程标签关联）
```csharp
public class TrainRideTag
{
    public int Id { get; set; }           // 关联ID
    public int TrainRideId { get; set; }  // 行程ID
    public int TagId { get; set; }        // 标签ID
    public string CreatedAt { get; set; } // 创建时间
}
```

## 数据访问层

### TicketTagRepository

提供标签的CRUD操作和关联管理：

**基础CRUD方法：**
- `AddTagAsync(TicketTag tag)` - 添加新标签
- `GetTagByIdAsync(int id)` - 根据ID获取标签
- `GetAllTagsAsync()` - 获取所有标签
- `UpdateTagAsync(TicketTag tag)` - 更新标签
- `DeleteTagAsync(int id)` - 删除标签

**行程标签关联方法：**
- `AddTagToRideAsync(int trainRideId, int tagId)` - 为行程添加标签
- `RemoveTagFromRideAsync(int trainRideId, int tagId)` - 移除行程的标签
- `GetTagsByRideIdAsync(int trainRideId)` - 获取行程的所有标签
- `GetTagsByRideIdsAsync(IEnumerable<int> trainRideIds)` - 批量获取多个行程的标签
- `ClearTagsFromRideAsync(int trainRideId)` - 清空行程的所有标签
- `SetTagsToRideAsync(int trainRideId, IEnumerable<int> tagIds)` - 设置行程的标签（先清空再添加）

## ViewModel实现

### TrainTicketFormViewModelBase

在火车票表单基类中添加了标签相关属性：

```csharp
// 标签相关属性
[ObservableProperty]
private ObservableCollection<TicketTag> _availableTags;    // 所有可用标签

[ObservableProperty]
private ObservableCollection<int> _selectedTagIds;         // 选中的标签ID集合
```

**关键方法：**
- `LoadTagsAsync()` - 异步加载所有可用标签（静态共享，只加载一次）
- `LoadFromTrainRide(TrainRideInfo trainRide)` - 加载车票时同时加载已选中的标签
- `CreateTrainRideInfo()` - 创建车票对象时包含选中的标签信息

### 保存时的标签处理

**添加车票（AddTrainTicketViewModel）：**
```csharp
// 添加车票并获取新ID
int newId = await _trainRideRepository.AddTrainRideAsync(trainRide);

// 保存标签关联
if (SelectedTagIds != null && SelectedTagIds.Count > 0)
{
    await _ticketTagRepository.SetTagsToRideAsync(newId, SelectedTagIds);
}
```

**编辑车票（EditTrainTicketViewModel）：**
```csharp
// 更新车票信息
await _trainRideRepository.UpdateTrainRideAsync(trainRide);

// 更新标签关联
if (SelectedTagIds != null)
{
    await _ticketTagRepository.SetTagsToRideAsync(EditTicketId.Value, SelectedTagIds);
}
```

## UI实现

### TrainTicketFormView（表单用户控件）

在表单中添加了标签选择区域：

**XAML结构：**
```xml
<Border Background="{DynamicResource PanelBackgroundBrush}" ...>
    <StackPanel>
        <TextBlock Text="标签" ... />
        <ItemsControl x:Name="TagsItemsControl" ItemsSource="{Binding AvailableTags}">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <WrapPanel Orientation="Horizontal" />
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Border x:Name="TagBorder"
                            Background="{Binding Color, Converter={StaticResource StringToBrushConverter}}"
                            CornerRadius="12"
                            Padding="10,5"
                            Margin="0,0,8,8"
                            Cursor="Hand"
                            Opacity="0.5"
                            BorderBrush="Transparent"
                            BorderThickness="2"
                            MouseLeftButtonDown="OnTagClicked">
                        <TextBlock Text="{Binding Name}"
                                   Foreground="{Binding TextColor, Converter={StaticResource StringToBrushConverter}}"
                                   FontSize="{DynamicResource BaseFontSize}"/>
                    </Border>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </StackPanel>
</Border>
```

**交互逻辑：**
- 点击标签切换选中/未选中状态
- 未选中：透明度 0.5，无边框
- 选中：透明度 1.0，显示强调色边框
- 选中状态变更时触发 `CheckForChanges()` 更新未保存状态

## 未保存检测

标签选择也纳入了未保存检测机制：

1. **原始值备份** - `BackupOriginalValues()` 备份选中的标签ID
2. **变更检测** - `CheckForChanges()` 比较当前选中标签与原始值
3. **关闭提示** - 关闭窗口时如果有未保存的标签变更会提示用户

## 静态共享优化

为避免内存占用过高，标签数据使用静态共享实例：

```csharp
protected static TicketTagRepository? _sharedTicketTagRepository;
protected static ObservableCollection<TicketTag>? _sharedAvailableTags;
```

- `TicketTagRepository` - 所有窗口共享同一个Repository实例
- `AvailableTags` - 所有窗口共享同一个标签列表，只从数据库加载一次

## 使用流程

### 添加车票时选择标签
1. 打开添加车票窗口
2. 填写车票信息
3. 在标签区域点击选择需要的标签（可多选）
4. 点击保存，标签关联随车票一起保存

### 编辑车票时修改标签
1. 打开编辑车票窗口
2. 已关联的标签显示为选中状态
3. 点击标签添加或移除关联
4. 点击更新保存变更

## 待实现功能

### 标签管理窗口
建议实现独立的标签管理功能：

**功能清单：**
- 查看所有标签列表
- 新增标签（名称、颜色）
- 编辑标签（修改名称、颜色）
- 删除标签（检查关联，确认后删除）
- 标签使用统计（关联车票数量）

**界面设计：**
- 左侧：标签列表（显示颜色、名称、使用次数）
- 右侧：标签编辑表单
- 支持搜索和排序

### 标签筛选
在车票列表中增加按标签筛选功能：
- 标签筛选下拉框/面板
- 支持多选标签（与/或逻辑）
- 快速筛选常用标签

## 相关文件

| 文件 | 说明 |
|------|------|
| `Model/TicketTag.cs` | 标签实体类 |
| `Model/TrainRideTag.cs` | 行程标签关联实体 |
| `DataAccess/TicketTagRepository.cs` | 标签数据访问 |
| `ViewModel/TrainTicketFormViewModelBase.cs` | 表单基类（标签属性） |
| `View/TrainTicketFormView.xaml` | 表单UI（标签选择区域） |
| `View/TrainTicketFormView.xaml.cs` | 标签交互逻辑 |
| `Converters/HexToBrushConverter.cs` | 颜色转换器 |
| `Converters/CollectionContainsConverter.cs` | 集合包含转换器 |
