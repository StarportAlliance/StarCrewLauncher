# StarCrew Launcher — AGENTS.md

纯 AI 维护项目。本文件是 AI Agent 与人类维护者的共同约定，改代码前先读。

## 新克隆后手动操作（仅一次）

```powershell
dotnet tool restore
dotnet husky install
```

## 一键入口

| 入口         | 用途                                                      |
| ------------ | --------------------------------------------------------- |
| `check.bat`  | 唯一入口，本地全部门禁：排版 → 风格 → 构建（与 CI 同构）  |
| `format.bat` | 一键执行格式化：CSharpier 排版 → dotnet format 修风格      |
| `pack.bat`   | 一键打包：发布 → vpk 打包（用法：`pack.bat <版本> [rid]`） |

推送前必须 `check.bat` 全绿。husky 的 pre-push 会跑同一套门禁，
但仅当本批待推送提交涉及代码/构建文件时才触发；
纯文档、图片等杂项推送直接跳过以保速度，推送后由 CI 兜底。

## 新增代码铁律

- 业务类型默认 `internal`（CA1515）；XAML 代码隐藏类保持 `public`。
- 压制分析器一律登记到 `GlobalSuppressions.cs` 并写清理由，禁止散落 `#pragma`。
- `Services`/`ViewModels` 不得依赖 `Microsoft.UI.Xaml`；`Models` 不得依赖 `Services`/`ViewModels`；
  `Services` 不得依赖 `ViewModels`。
- `*.xaml.cs` 只留 MVVM 空壳：UI 逻辑不要往里面加。

## 架构与实现约定（MVVM + DI）

- 组合根在 `App`（MEDI）：`App.Services` 注册全部服务与视图模型；业务依赖一律构造注入，
  页面代码隐藏只经 `App.Services` 取界面层基础件。
- 视图模型放 `StarCrew.Launcher/ViewModels`；用 CommunityToolkit.Mvvm：
  `[ObservableProperty]` 一律用 partial 属性写法，命令用 `[RelayCommand]`。
- 页面导航一律走 `INavigationService`（`NavigationTarget` 枚举），页面间不得互相 new；
  重复导航到当前目标由导航服务按帧去重。
- 用户提示一律经 `INotificationService` 发应用内通知，视图模型不直接接触 UI。
- XAML 绑定只用 `x:Bind`：页面在 `OnNavigatedTo` 里把导航参数赋给 `ViewModel` 属性，随后调用 `Bindings.Update()`。
- 对话框等离不开 UI 的交互留在代码隐藏；决策逻辑必须回视图模型（代码隐藏保持空壳）。
- 除日志外的全部文本文案均放在 resw 文件内；XAML 统一用 `{x:Bind i18n:Loc.Get('Key')}` 引用。

## 注释与文档风格

本项目纯 AI 维护：注释的唯一价值是让 Agent 少花上下文就能看懂代码，
所以总量压到最低——默认不写，只对坑和契约留字：
- 注释跟着契约走：契约都在 `Services`/`Models`/`ViewModels`，接口成员的 summary 必写，签名说不清语义时补
  `<param>`/`<returns>`/`<remarks>`——null 语义、失败/取消语义必须写清。
- 实现类不重复接口注释，仅实现特有坑补一行；类名不自解释时留一行摘要。
- Models/ViewModels 公共成员默认不写；单位、量纲等不自明且易错的语义才留一句。
- 枚举成员自解释不写注释，语义模糊才留一句。
- 页面代码隐藏保持 MVVM 空壳，不写注释。
- `//` 只写为什么，限一行，宁缺勿滥，不复述代码。
- resw/XAML/csproj 注释最简化：resw 只留最短分组标记，占位符图例等一律省略。
- 压制理由限一句。