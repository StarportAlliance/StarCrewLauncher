# StarCrew Launcher — AGENTS.md

纯 AI 维护项目。本文件是 AI  agent 与人类维护者的共同约定，改代码前先读。

## 一键入口

| 入口        | 用途                                                  |
| ----------- | ----------------------------------------------------- |
| `test.bat`  | 一键运行全套测试 + 覆盖率（TestResults/）             |
| `check.bat` | 本地全部门禁：排版 → 风格 → 构建 → 测试（与 CI 同构） |

提交前必须 `check.bat` 全绿；push 前 husky 会再拦一道（pre-commit：排版+风格+构建；pre-push：全套测试）。

## 工具链（对标 Ruff / ESLint / Biome）

- 排版：CSharpier（`dotnet csharpier check .`），零配置，`.cs/.xaml/.csproj` 全管。
- 风格/语法：Roslyn 全量分析（`Directory.Build.props`：`AnalysisMode=All` + `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild`），规则见 `.editorconfig`。
- 单元测试：xUnit + AwesomeAssertions + NSubstitute + coverlet，配置见 `coverlet.runsettings`。
- 架构防腐：`StarCrew.Launcher.Tests/Architecture/`（NetArchTest），分层一破就红。
- 变异测试：Stryker（`stryker-config.json`），`dotnet stryker`，建议每夜跑，不进 PR 门禁。
  **当前被工具限制阻塞**（已验证：Stryker 5.0 corrupt WinUI 生成文件，`mutate` 排除无效，
  详见 `stryker-config.json` 头部注释）；AGENTS.md 缺口第 1 条（抽类库）完成后解封。
- 依赖审计：`NuGetAudit=all`（restore 即审计）+ Dependabot 按需。

## 新增代码铁律

1. 业务类型默认 `internal`（CA1515 门禁）；测试靠 `InternalsVisibleTo` 可见。XAML 代码隐藏类保持 `public`。
2. 压制分析器一律登记到 `StarCrew.Launcher/GlobalSuppressions.cs` 并写清理由，禁止散落 `#pragma`。
3. `Services` 不许依赖 `Microsoft.UI.Xaml`，`Models` 不许依赖 `Services`（架构测试兜底）。
4. 测试命名用 `Method_Scenario_Result` 下划线风格（已在 `.editorconfig` 豁免 CA1707）。
5. `*.xaml.cs` 不计单元覆盖率（`coverlet.runsettings` 已排除）：UI 逻辑不要往里面加，加了也测不到。
6. `*.bat` 必须 CRLF + 纯 ASCII（英文输出）：cmd 不认 LF，中文在非 UTF-8 环境必乱码。

## 已知缺口（按顺序填）

1. `GameLauncher` 直调 `Process.Start`、`SteamGameLocator` 直读注册表/文件，无接缝：
   下一步拆 `IProcessStarter` / `ISteamLocator`（NSubstitute 已引入就是为此准备的），
   届时把 `steam://` 成功分支、库枚举分支补上测试。
2. 覆盖率数字门槛、`stryker-config.json` 的 `break` 当前都是宽松档（只出报告不卡门），
   第 1 条完成后收紧到 line 80 / break 60。
3. WinUI 真机 UI 测试（FlaUI）暂无，`MainWindow` 大改时靠手动冒烟。

## 新克隆后手动操作（仅一次）

```powershell
dotnet tool restore
dotnet husky install
```
