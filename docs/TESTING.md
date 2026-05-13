# 自动化测试与开发流程（GuiPiao）

本文说明本仓库的测试工程位置、如何运行测试、推荐开发流程，以及供其他开发者或 AI 协作时使用的提示词。

## 技术栈

- 主工程：`GuiPiao`（.NET 8、WPF）
- 测试工程：`GuiPiao.Tests`（xUnit、`Microsoft.NET.Test.Sdk`）
- 解决方案：`GuiPiao.sln`（已包含测试项目）

## 运行测试

在项目根目录（与 `GuiPiao.sln` 同级）执行：

```bash
dotnet test
```

或指定路径：

```bash
dotnet test GuiPiao.sln
dotnet test GuiPiao.Tests\GuiPiao.Tests.csproj
```

生成测试结果文件（例如给 CI 用）：

```bash
dotnet test GuiPiao.sln --logger "trx;LogFileName=TestResults.trx"
```

Visual Studio：**测试 → 运行所有测试**。测试资源管理器里灰色问号表示「已发现尚未在本轮运行」；点「全部运行」后会出现通过/失败标记。

## 测试工程与主工程关系

- 测试代码**仅**放在 `GuiPiao.Tests` 目录下。
- 主工程 `GuiPiao.csproj` 中已通过 `<Compile Remove="GuiPiao.Tests\**\*" />` 排除测试目录，避免 SDK 默认把子文件夹中的 `.cs` 编进主程序。

## 测试文件目录约定

测试文件夹尽量与主工程对应，便于查找与扩展，例如：

| 主工程 | 测试 |
|--------|------|
| `Converters/` | `GuiPiao.Tests/Converters/` |
| `ViewModel/TrainTicketForm/` | `GuiPiao.Tests/ViewModel/TrainTicketForm/` |
| `Utils/` | `GuiPiao.Tests/Utils/` |
| `Services/` | `GuiPiao.Tests/Services/` |
| `Model/` | `GuiPiao.Tests/Model/` |

## 推荐开发流程（Definition of Done）

1. 在 `GuiPiao` 中实现或修改业务逻辑；可测逻辑尽量独立于 `Application.Current`、真实弹窗与固定用户路径。
2. 在 `GuiPiao.Tests` 中**新增或更新**用例：`[Fact]` / `[Theory]` 写清「输入 → 期望输出或状态」。
3. 本地执行 `dotnet test`，**全部通过**后再视为该任务完成。
4. 改动范围保持最小：不做过无关重构；不擅自新增与本任务无关的文档。

## 测试编写优先级

**适合单元测试（优先）：**

- 值转换器（`IValueConverter`）
- 纯工具与布局逻辑（如 `CommonUtils`、`DashboardLayoutManager`、`DragDropHelper.MoveItem`）
- 表单校验与规则（如 `FormValidator`、`BusinessRuleEngine`、`DataTransformer`）
- 模型上的纯计算属性、枚举映射（不涉及全局单例读配置时）

**适合集成测试：**

- 依赖 SQLite 的验证或仓储：使用**临时数据库文件**或受控路径，用例结束清理，避免污染用户数据。

**谨慎或需拆分后再测：**

- 强依赖未启动的 WPF `Application`、整窗 UI 流程；宜抽接口/纯函数后再测，或单独做少量 E2E。

## 给其他 AI / 协作者的提示词（可直接复制）

```text
你是本仓库的协作开发者。仓库为 .NET 8 WPF 桌面应用（GuiPiao），测试工程为 GuiPiao.Tests（xUnit）。你必须遵守以下流程，不得只改业务代码而不顾测试与可验证性。

必须遵守的流程：

1. 实现或修改业务逻辑时，优先把可测逻辑放在不依赖 Application.Current、真实 UI 弹窗、硬编码用户目录的地方；若必须依赖，应通过接口/注入或已有抽象隔离，便于替换。

2. 同步在 GuiPiao.Tests 中补充或更新测试：
   - 新行为：新增或扩展 [Fact] / [Theory]，写清「给定输入 → 期望输出/状态」。
   - 修改行为：更新对应断言或用例说明；删除已失效用例需说明原因。
   - 测试文件目录尽量与主工程对应（如 Converters/、ViewModel/TrainTicketForm/、Utils/、Services/）。

3. 完成后运行并确保通过：dotnet test（针对 GuiPiao.sln 或 GuiPiao.Tests.csproj）。不得在未运行测试的情况下声称「已完成」；若环境无法执行，须明确说明并给出应运行的命令与预期结果。

4. 主工程 GuiPiao.csproj 已排除 GuiPiao.Tests\**：禁止把测试代码放进主项目并被主工程编译；测试只放在 GuiPiao.Tests。

5. 改动范围：只改任务所需文件；禁止无关大重构、禁止随意新增文档（除非用户要求）。

测试编写原则：

- 优先：转换器、纯工具类、校验器、DataTransformer、BusinessRuleEngine、FormValidator、DashboardLayoutManager、DragDropHelper.MoveItem、模型上的纯属性/枚举行为等。
- 集成测试：涉及 SQLite / DatabaseValidationService 时，使用临时数据库文件并在用例中清理。
- 避免：在单元测试里强依赖未启动的 WPF Application、真实注册表路径、网络；若不可避免，改为小函数抽取后再测或单独标注为集成/E2E。

交付标准（Definition of Done）：

- 业务需求已实现。
- GuiPiao.Tests 中有对应用例（或明确说明为何无法单测及替代验证方式）。
- dotnet test 全绿，无新增可避免的编译警告（与项目现有一致的 NU 等可注明）。

沟通要求：用中文回复用户（若用户要求中文）；说明「改了什么、为什么、如何验证」。

若违反以上流程，视为未完成。
```

## 相关文件

- `GuiPiao.Tests/GuiPiao.Tests.csproj`：测试项目配置与包引用
- `GuiPiao.csproj`：主工程及对 `GuiPiao.Tests` 的排除项

---

文档随测试策略演进可继续补充本节（例如 CI 配置、覆盖率要求等）。
