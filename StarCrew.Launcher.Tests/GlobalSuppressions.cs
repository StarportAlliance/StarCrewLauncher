// 分析器压制集中登记处：每条压制必须写清理由，禁止无理由压制。
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Reliability",
    "CA2000:DisposeObjectsBeforeLosingScope",
    Justification = "测试替身 handler 随 HttpClient 一起释放，无泄漏。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Tests.Services.GameVersionCatalogTests.CreateClient(System.Func{System.Net.Http.HttpRequestMessage,System.Net.Http.HttpResponseMessage})"
)]
[assembly: SuppressMessage(
    "Reliability",
    "CA2000:DisposeObjectsBeforeLosingScope",
    Justification = "测试替身 handler 随 HttpClient 一起释放，无泄漏。",
    Scope = "member",
    Target = "~M:StarCrew.Launcher.Tests.Services.DepotDownloaderProvisionerTests.CreateClient(System.Func{System.Net.Http.HttpRequestMessage,System.Net.Http.HttpResponseMessage})"
)]
