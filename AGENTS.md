# StarCrew Launcher — AGENTS.md

纯 AI 维护项目。本文件是 AI Agent 与人类维护者的共同约定，改代码前先读。

## 新克隆后手动操作（仅一次）

```powershell
dotnet tool restore
dotnet husky install
```

## 一键入口

| 入口          | 用途                                                       |
| ------------- | ---------------------------------------------------------- |
| `test.bat`    | 一键运行全套测试 + 覆盖率（TestResults/，含 UI 冒烟）      |
| `test-ui.bat` | 只跑 FlaUI UI 冒烟（先构建主工程再启动真窗口，不点击）     |
| `check.bat`   | 本地全部门禁：排版 → 风格 → 构建 → 测试（与 CI 同构）      |
| `format.bat`  | 一键执行格式化：CSharpier 排版 → dotnet format 修风格      |
| `pack.bat`    | 一键打包：发布 → vpk 打包（用法：`pack.bat <版本> [rid]`） |

提交前必须 `check.bat` 全绿；push 前 husky 会再拦一道（pre-commit：排版+风格+构建；pre-push：全套测试）。

## 工具链

- 排版：CSharpier。
- 风格/语法：Roslyn 全量分析，规则见 `.editorconfig`。
- 单元测试：xUnit + AwesomeAssertions + NSubstitute + coverlet，配置见 `coverlet.runsettings`。
- 架构防腐：`StarCrew.Launcher.Tests/Architecture/`（NetArchTest）。
- 变异测试：Stryker。
  变异对象是 `StarCrew.Launcher.Core` 类库，`break: 60`。
- 依赖审计：`NuGetAudit=all`（restore 即审计）+ Dependabot 按需。
- 云端门禁：CodeQL 扫描（`.github/workflows/codeql.yml`）。

## 更新与打包（Velopack）

- 更新源：自建静态 HTTP，地址常量 `VelopackUpdateClient.DefaultFeedUrl`；
  把 `vpk pack` 产出的 `Releases/` 内容原样部署到该地址即可，无需服务端逻辑。
- 接口：`IUpdateClient`（`VelopackUpdateClient` 实现）+ `AppUpdater.CheckAndPrepareUpdateAsync`（检查→命中下载→安排重启后应用，
  调用方按 `UpdateCheckResult.State` 提示重启）；开发直跑回 NotInstalled，不抛。UI 未接入，需要时在界面层直接构造调用。
- 启动约束：`App` 构造器首行必须是 `VelopackApp.Build().Run()`，否则 `UpdateManager` 构造失败；
  测试宿主用 `VelopackTestBootstrap`（ModuleInitializer）补初始化。
- 打包：`pack.bat <版本> [rid]`（默认 `win-x64`，另有 `win-arm64`），版本须与主工程 `Version` 一致；
  先装同版本 vpk：`dotnet tool install -g vpk --version 1.2.161`（与 NuGet 的 Velopack 包同版本）。
- 主工程 `EnableMsixTooling` 必须保持 `true`（`WindowsPackageType` 仍是 `None`，不会打出 MSIX）：
  设为 `false` 会连带关掉 publish 期的 PRI 生成，`vpk pack` 拿到的就是缺 `StarCrew.Launcher.pri` 的半成品，
  装完启动即崩。

## 新增代码铁律

- 业务类型默认 `internal`（CA1515 门禁）；测试靠 `InternalsVisibleTo` 可见。XAML 代码隐藏类保持 `public`。
- 压制分析器一律登记到各工程的 `GlobalSuppressions.cs` 并写清理由，禁止散落 `#pragma`。
- `Services` 不许依赖 `Microsoft.UI.Xaml`，`Models` 不许依赖 `Services`（架构测试兜底）。
- 测试命名用 `Method_Scenario_Result` 下划线风格（已在 `.editorconfig` 豁免 CA1707）。
- `*.xaml.cs` 不计单元覆盖率（`coverlet.runsettings` 已排除）：UI 逻辑不要往里面加，加了也测不到。
- `*.bat` 必须 CRLF + 纯 ASCII（英文输出）：cmd 不认 LF，中文在非 UTF-8 环境必乱码。
- restore 只做一次：`dotnet restore slnx` 跑一次（CI/check/hook 各自第一步），之后一律 `--no-restore`；
  `dotnet tool restore` 只还原工具清单、不还原 NuGet 包。全新检出无 `obj/`，缺这步构建必挂。
- XAML 里 `Icon="..."` 只能用 WinUI `Symbol` 枚举真实存在的值（如 `Help`，没有 `Info`）；
   写错编译不报错，运行时在 `Microsoft.UI.Xaml.dll` 里直接崩（0xC000027B），且无托管堆栈，只能二分排查。

## 注释与文档风格

- 公共契约才写 `///`：null 语义、回退顺序、副作用、状态转换；构造器与名副其实的成员不写。
- 私有实现默认不写 `///`；`//` 只写为什么、不复述代码，限一行。
- record 的 `<param>` 名称自解释时省略，只留一句类摘要。
- 压制理由限一句，但须说清规则为何不适用。
- 文档只留“现在必须遵守”。