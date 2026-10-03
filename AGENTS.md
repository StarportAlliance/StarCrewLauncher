# StarCrew Launcher — AGENTS.md

纯 AI 维护项目。本文件是 AI  agent 与人类维护者的共同约定，改代码前先读。

## 一键入口

| 入口          | 用途                                                   |
| ------------- | ------------------------------------------------------ |
| `test.bat`    | 一键运行全套测试 + 覆盖率（TestResults/，含 UI 冒烟）  |
| `test-ui.bat` | 只跑 FlaUI UI 冒烟（先构建主工程再启动真窗口，不点击） |
| `check.bat`   | 本地全部门禁：排版 → 风格 → 构建 → 测试（与 CI 同构）  |
| `pack.bat`    | 一键打包：发布 → vpk 打包（用法：`pack.bat <版本> [rid]`） |

提交前必须 `check.bat` 全绿；push 前 husky 会再拦一道（pre-commit：排版+风格+构建；pre-push：全套测试）。

## 工具链（对标 Ruff / ESLint / Biome）

- 排版：CSharpier（`dotnet csharpier check .`），零配置，`.cs/.xaml/.csproj` 全管。
- 风格/语法：Roslyn 全量分析（`Directory.Build.props`：`AnalysisMode=All` + `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild`），规则见 `.editorconfig`。
- 单元测试：xUnit + AwesomeAssertions + NSubstitute + coverlet，配置见 `coverlet.runsettings`。
- 架构防腐：`StarCrew.Launcher.Tests/Architecture/`（NetArchTest），分层一破就红。
- 变异测试：Stryker（`stryker-config.json`），`dotnet stryker`，建议每夜跑，不进 PR 门禁。
  变异对象是 `StarCrew.Launcher.Core` 类库（WinExe+XAML 工程 Stryker 跑不起来，
  详见 `stryker-config.json` 头部注释）。当前变异分 61.90%，`break: 60` 卡门；
  存活变异集中在真机 OS 相关的注册表键名字面量，属已知可接受存活。
- 依赖审计：`NuGetAudit=all`（restore 即审计）+ Dependabot 按需。
- 云端门禁：CodeQL 扫描（`.github/workflows/codeql.yml`，每周一次）+ 覆盖率上传到 GitHub Code Quality
  （CI 的“上传覆盖率”步骤，`fail-on-error: false`）。
  2026-10 实测结论：Code Quality 是付费功能（约 $10/月），不付费。替代方案全部免费且等效：
  覆盖率的强制力在 `coverlet.runsettings` 的 80 硬门（CI 挂则合并门用 required status check 卡住即可），
  上传步骤只是显示用，断了也不影响门禁；CodeQL workflow 本身免费，跑出结果后
  “Require code scanning results”规则照常用。只有 bot 评论和“Restrict code coverage”原生规则需要付费，
  这两样是锦上添花，不要为它们付费。
- SDK 对齐：CI 用 `10.0.x` 最新 SDK，本地 `global.json` 定 10.0.400；servicing 小版本也可能改变分析器行为
  （2026-10-02：10.0.401 起 `dotnet format` 对 XAML 绑定的事件处理器报 IDE0060 而本地不报）。
  修这类问题要用文件级 severity（见 `.editorconfig` 的 MainWindow 节），不要依赖成员级压制——后者拦不住 format 的 verify 通道。

## 更新与打包（Velopack）

- 更新源：自建静态 HTTP，地址常量 `VelopackUpdateClient.DefaultFeedUrl`
  （`StarCrew.Launcher.Core/Services/VelopackUpdateClient.cs`）；
  把 `vpk pack` 产出的 `Releases/` 内容原样部署到该地址即可，无需服务端逻辑。
- 接口：`IUpdateClient`（`VelopackUpdateClient` 实现）+ `AppUpdater.CheckAndPrepareUpdateAsync`（检查→命中自动下载→安排退出后应用，
  调用方按 `UpdateCheckResult.State` 提示用户重启）；开发直跑（未经安装）直接回 NotInstalled，不抛异常。
  UI 侧尚未接入，需要时直接在界面层构造 `new AppUpdater(new VelopackUpdateClient(...))` 调用即可。
- 启动约束：`App` 构造器首行必须是 `VelopackApp.Build().Run()`，否则连 `UpdateManager` 都构造不出来；
  测试宿主用 `VelopackTestBootstrap`（ModuleInitializer）补同样的初始化。
- 打包：`pack.bat <版本> [rid]`（默认 `win-x64`，另有 `win-arm64`），版本号必须与主工程 `Version` 一致；
  需先装同版本 vpk：`dotnet tool install -g vpk --version 1.2.161`（与 NuGet 的 Velopack 包同版本）。
- 主工程 `EnableMsixTooling` 必须保持 `true`（`WindowsPackageType` 仍是 `None`，不会打出 MSIX）：
  设为 `false` 会连带关掉 publish 期的 PRI 生成，`vpk pack` 拿到的就是缺 `StarCrew.Launcher.pri` 的半成品，
  装完启动即崩（0xC000027B，2026-10 实测）。

## 新增代码铁律

1. 业务类型默认 `internal`（CA1515 门禁）；测试靠 `InternalsVisibleTo` 可见。XAML 代码隐藏类保持 `public`。
2. 压制分析器一律登记到各工程的 `GlobalSuppressions.cs` 并写清理由，禁止散落 `#pragma`。
3. `Services` 不许依赖 `Microsoft.UI.Xaml`，`Models` 不许依赖 `Services`（架构测试兜底）。
4. 测试命名用 `Method_Scenario_Result` 下划线风格（已在 `.editorconfig` 豁免 CA1707）。
5. `*.xaml.cs` 不计单元覆盖率（`coverlet.runsettings` 已排除）：UI 逻辑不要往里面加，加了也测不到。
6. `*.bat` 必须 CRLF + 纯 ASCII（英文输出）：cmd 不认 LF，中文在非 UTF-8 环境必乱码。
7. restore 只做一次：`dotnet restore slnx` 显式跑一次（CI/check/hook 各自的第一步），之后所有命令一律 `--no-restore`；
   `dotnet tool restore` 只还原工具清单、不还原 NuGet 包，两者不要混淆。全新检出没有 `obj/`，缺了这步构建必挂。
8. XAML 里 `Icon="..."` 只能用 WinUI `Symbol` 枚举真实存在的值（如 `Help`，没有 `Info`）；
   写错编译不报错，运行时在 `Microsoft.UI.Xaml.dll` 里直接崩（0xC000027B），且无托管堆栈，只能二分排查。

## 已填缺口（销账记录）

1. ~~`GameLauncher` 直调 `Process.Start`~~ → 已拆 `StarCrew.Launcher.Core` 类库 +
   `IProcessStarter` / `ISteamGameLocator` / `ISteamEnvironment` 接缝（NSubstitute 覆盖成功分支）。
2. ~~覆盖率/`break`宽松档~~ → 单元覆盖率 line 93%+，硬门 80；变异分 61.90%，`break: 60`。
3. ~~无 UI 测试~~ → `StarCrew.Launcher.UITests`（FlaUI）冒烟：只断言窗口+按钮存在，全程零点击，`test-ui.bat` 独立入口。

## 新克隆后手动操作（仅一次）

```powershell
dotnet tool restore
dotnet husky install
```
