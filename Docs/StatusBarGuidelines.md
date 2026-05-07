# 状态栏(StatusBar)使用规范

## 概述

本文档定义了项目中所有窗口状态栏的统一实现规范，确保UI一致性和可维护性。

**核心原则：状态栏内容应符合窗口本身的功能和内容，避免冗余信息。**

---

## 设计原则

### 1. 内容相关性原则

状态栏显示的内容必须与窗口核心功能相关：

| 窗口类型 | 应显示 | 不显示 |
|----------|--------|--------|
| **数据列表/管理窗口** | 记录总数、筛选结果数 | 无关的系统状态 |
| **地图/可视化窗口** | 当前视图状态、选中项信息 | 重复的数据统计 |
| **设置/配置窗口** | 保存状态、验证结果 | 业务数据数量 |
| **预览/详情窗口** | 当前查看项信息 | 全局统计 |

### 2. 信息必要性原则

**必须显示统计的情况：**
- 窗口核心功能是数据列表展示（如 MainWindow 的车票列表）
- 用户需要了解数据总量（如 LogManagerWindow 的日志管理）
- 有分页、筛选等影响数据量的操作

**不需要显示统计的情况：**
- 可视化界面本身已展示数据（如 MapWindow 的地图已显示行程）
- 单条数据详情查看
- 设置配置类窗口

### 3. 文案一致性原则

- 状态文本使用简洁的动词或形容词（如"就绪"、"加载中"、"已完成"）
- 统计标签使用名词+冒号（如"车票："、"日志："）
- 单位统一使用"条"（适用于数据记录）

---

## 统一结构

### 基础结构（仅状态）

适用于不需要显示统计的窗口：

```xml
<StatusBar Background="{DynamicResource StatusBarBackgroundBrush}">
    <StatusBarItem>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="🟢 " />
            <TextBlock Text="{Binding StatusMessage}" />
        </StackPanel>
    </StatusBarItem>
</StatusBar>
```

### 完整结构（状态+统计）

适用于需要显示统计的窗口：

```xml
<StatusBar Background="{DynamicResource StatusBarBackgroundBrush}">
    <!-- 左侧：状态信息 -->
    <StatusBarItem>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="🟢 " />
            <TextBlock Text="{Binding StatusMessage}" />
        </StackPanel>
    </StatusBarItem>
    
    <!-- 右侧：统计信息 -->
    <StatusBarItem HorizontalAlignment="Right">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="[项目名]：" />
            <TextBlock Text="{Binding [CountProperty]}" />
            <TextBlock Text=" 条" />
        </StackPanel>
    </StatusBarItem>
</StatusBar>
```

---

## 实现要求

### 1. XAML 要求

| 项目 | 要求 |
|------|------|
| 控件 | 必须使用 `StatusBar` |
| 背景色 | 必须绑定 `StatusBarBackgroundBrush` |
| 字体大小 | 继承全局样式，不需要额外设置 |
| 结构 | 左侧状态 + 右侧统计（如需要） |

### 2. ViewModel 要求

每个窗口的ViewModel应包含：

```csharp
[ObservableProperty]
private string _statusMessage = "就绪";  // 状态文本

// 可选：统计属性（根据窗口需要决定是否添加）
public int [Item]Count { get; }
```

### 3. 状态图标

| 状态 | 图标 | 使用场景 |
|------|------|----------|
| 正常/就绪 | 🟢 | 空闲状态 |
| 加载中 | ⏳ | 数据加载中 |
| 警告 | 🟡 | 有警告信息 |
| 错误 | 🔴 | 发生错误 |

---

## 现有实现参考

### MainWindow.xaml（数据列表窗口 - 需要统计）

```xml
<StatusBar DockPanel.Dock="Bottom" Background="{DynamicResource StatusBarBackgroundBrush}">
    <StatusBarItem>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="🟢 " FontSize="{DynamicResource SmallFontSize}" />
            <TextBlock Text="就绪" FontSize="{DynamicResource SmallFontSize}" />
        </StackPanel>
    </StatusBarItem>
    <StatusBarItem HorizontalAlignment="Right">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="车票：" FontSize="{DynamicResource SmallFontSize}" />
            <TextBlock Text="{Binding TotalItems}" FontSize="{DynamicResource SmallFontSize}" />
            <TextBlock Text=" 条" FontSize="{DynamicResource SmallFontSize}" />
        </StackPanel>
    </StatusBarItem>
</StatusBar>
```

**设计理由：**
- 主窗口核心是车票数据列表
- 用户需要了解当前数据总量
- 有分页功能，统计信息有助于定位

---

### LogManagerWindow.xaml（管理窗口 - 需要统计）

```xml
<StatusBar Grid.Row="3" Background="{DynamicResource StatusBarBackgroundBrush}">
    <StatusBarItem>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="🟢 " VerticalAlignment="Center" />
            <TextBlock Text="{Binding StatusMessage}" VerticalAlignment="Center" />
        </StackPanel>
    </StatusBarItem>
    <StatusBarItem HorizontalAlignment="Right">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="日志：" VerticalAlignment="Center" />
            <TextBlock Text="{Binding TotalLogCount}" VerticalAlignment="Center" />
            <TextBlock Text=" 条" VerticalAlignment="Center" />
        </StackPanel>
    </StatusBarItem>
</StatusBar>
```

**设计理由：**
- 日志管理窗口核心是日志列表
- 用户需要了解日志总量
- 有筛选功能，统计反映筛选结果

---

### MapWindow.xaml（可视化窗口 - 不需要统计）

```xml
<StatusBar Grid.Row="2" Background="{DynamicResource StatusBarBackgroundBrush}">
    <StatusBarItem>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="🟢 " />
            <TextBlock Text="{Binding StatusMessage}" />
        </StackPanel>
    </StatusBarItem>
</StatusBar>
```

**设计理由：**
- 地图本身已可视化展示所有行程
- 地图上已显示标记和线路，重复显示数量无意义
- 状态消息足以反映当前操作（如"显示已完成行程"）

---

## 新增窗口检查清单

创建新窗口时，请根据窗口类型选择合适的状态栏结构：

### 第一步：确定窗口类型

- [ ] **数据列表/管理窗口** → 使用完整结构（状态+统计）
- [ ] **地图/可视化窗口** → 使用基础结构（仅状态）
- [ ] **设置/配置窗口** → 使用基础结构（仅状态）
- [ ] **预览/详情窗口** → 使用基础结构（仅状态）

### 第二步：实现状态栏

- [ ] 使用 `StatusBar` 控件
- [ ] 背景绑定 `StatusBarBackgroundBrush`
- [ ] 左侧显示状态图标 + 状态文本
- [ ] 如需要，右侧显示统计信息（标签 + 数量 + 单位）
- [ ] ViewModel 包含 `StatusMessage` 属性
- [ ] 如需要，ViewModel 包含统计数量属性
- [ ] 状态栏位于窗口底部

### 第三步：文案检查

- [ ] 状态文本符合窗口功能（如"就绪"、"加载中"）
- [ ] 统计标签使用正确名词（如"车票："、"日志："）
- [ ] 避免显示与窗口无关的信息

---

## 主题支持

状态栏自动支持深色/浅色主题切换，通过以下资源：

- `StatusBarBackgroundBrush` - 背景色
- `TextPrimaryBrush` - 前景色（通过全局样式）
- `SmallFontSize` - 字体大小

主题定义位于：
- `Themes/DarkTheme.xaml`
- `Themes/LightTheme.xaml`

---

## 更新历史

| 日期 | 修改内容 |
|------|----------|
| 2026-03-18 | 统一所有窗口状态栏实现，创建本文档 |
| 2026-03-18 | 添加内容相关性原则，明确统计信息的显示规则 |
