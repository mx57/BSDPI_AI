using System.Collections.Generic;
using System.Threading.Tasks;
using BSDPI.AI.Models;
using BSDPI.AI.Services;
using BSDPI.AI.Stats;
using BSDPI.Core.Models;
using BSDPI.Core.Services;

namespace BSDPI.Core.Tests;

public sealed class AiOrchestratorServiceTests
{
    private sealed class FakeConnectivityChecker : IConnectivityChecker
    {
        public bool IsCurlAvailable => false;
        public Task<CheckResult> CheckAsync(TargetEntry target, CancellationToken ct = default) => Task.FromResult(new CheckResult());
        public Task<CheckResult> CheckAsync(TargetEntry target, bool useCurlForHttp, CancellationToken ct = default) => Task.FromResult(new CheckResult());
        public Task<CheckResult> CheckViaSocks5Async(TargetEntry target, string socksHost, int socksPort, CancellationToken ct = default) => Task.FromResult(new CheckResult());
        public Task<(double successRate, List<CheckResult> results)> CheckAllAsync(IEnumerable<TargetEntry> targets, CancellationToken ct = default) => Task.FromResult((0d, new List<CheckResult>()));
        public Task<(double successRate, List<CheckResult> results)> CheckAllAsync(IEnumerable<TargetEntry> targets, bool useCurlForHttp, int maxParallelChecks, CancellationToken ct = default) => Task.FromResult((0d, new List<CheckResult>()));
        public Task<CheckResult> CheckWarpAsync(CancellationToken ct = default) => Task.FromResult(new CheckResult());
    }

    private static AiOrchestratorService BuildOrchestrator(string engineDir, out AiStrategyRegistry registry, out AiHistoryStore history)
    {
        var regPath = Path.Combine(engineDir, "registry.json");
        var histPath = Path.Combine(engineDir, "history.jsonl");
        registry = new AiStrategyRegistry(regPath);
        history = new AiHistoryStore(histPath);

        var fingerprints = new NetworkFingerprintProvider();
        var networkWatcher = new NetworkChangeWatcher(fingerprints);
        var bandit = new BanditSelector(registry);
        var evolver = new StrategyEvolver(registry, history, () => engineDir, () => new AiSettings());
        var engineManager = new DpiEngineManager(engineDir);
        var connectivity = new FakeConnectivityChecker();

        return new AiOrchestratorService(
            getProfiles: () => [],
            getActiveProfile: () => null,
            switchProfile: _ => Task.CompletedTask,
            getTargetsPath: () => Path.Combine(engineDir, "targets.txt"),
            notifyScoreUpdate: (_, _) => Task.CompletedTask,
            engineDir: () => engineDir,
            aiSettings: () => new AiSettings(),
            refreshProfiles: () => Task.CompletedTask,
            isAnyEngineRunning: () => false,
            ensureProtectionRunning: () => Task.CompletedTask,
            connectivity,
            engineManager,
            fingerprints,
            networkWatcher,
            registry,
            history,
            bandit,
            evolver);
    }

    private static string MakeEngineDir()
    {
        var engineDir = Path.Combine(Path.GetTempPath(), "bsdpi-orch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(engineDir);
        Directory.CreateDirectory(Path.Combine(engineDir, "bin"));
        // Stub executable so ProfileBatLauncher considers the launch plan valid.
        File.WriteAllText(Path.Combine(engineDir, "bin", "winws.exe"), "");
        Directory.CreateDirectory(Path.Combine(engineDir, "lists"));
        return engineDir;
    }

    private static void WriteProfile(string engineDir, string name, string args)
    {
        File.WriteAllText(
            Path.Combine(engineDir, name + ".bat"),
            $"bin\\winws.exe {args}\r\n");
    }

    [Fact]
    public void SyncRegistryFromEngine_ParsesBuiltinBatsIntoRegistry_AndPersists()
    {
        var engineDir = MakeEngineDir();
        try
        {
            WriteProfile(engineDir, "myprofile", "--filter-tcp 80 --dpi-desync fake --dpi-desync-split-pos midsld --dpi-desync-fake-tls-mod rand");

            using var orch = BuildOrchestrator(engineDir, out var registry, out _);
            orch.SyncRegistryFromEngine();

            var genomes = registry.GetGenomes().ToList();
            Assert.Single(genomes);

            var g = genomes[0];
            Assert.Equal("myprofile", g.DisplayName);
            Assert.Equal(StrategyOrigin.Builtin, g.Origin);
            Assert.Equal("80", g.FilterTcp);
            Assert.Equal("fake", g.DesyncMode);
            Assert.Equal("midsld", g.SplitPosSemantic);
            Assert.Equal("rand", g.FakeTlsMod);

            // Registry file is persisted to disk.
            Assert.True(File.Exists(Path.Combine(engineDir, "registry.json")));
        }
        finally
        {
            Directory.Delete(engineDir, recursive: true);
        }
    }

    [Fact]
    public async Task SyncRegistryFromEngineAsync_ParsesBuiltinBats()
    {
        var engineDir = MakeEngineDir();
        try
        {
            WriteProfile(engineDir, "asyncprofile", "--filter-tcp 443 --dpi-desync fake");

            using var orch = BuildOrchestrator(engineDir, out var registry, out _);
            await orch.SyncRegistryFromEngineAsync();

            var g = Assert.Single(registry.GetGenomes());
            Assert.Equal("asyncprofile", g.DisplayName);
            Assert.Equal("443", g.FilterTcp);
            Assert.Equal("fake", g.DesyncMode);
        }
        finally
        {
            Directory.Delete(engineDir, recursive: true);
        }
    }

    [Fact]
    public void SyncRegistryFromEngine_RemovesOrphanedBuiltins_WhenBatDeleted()
    {
        var engineDir = MakeEngineDir();
        try
        {
            WriteProfile(engineDir, "keep", "--filter-tcp 80");
            var dropPath = Path.Combine(engineDir, "drop.bat");
            File.WriteAllText(dropPath, "bin\\winws.exe --filter-tcp 80\r\n");

            using var orch = BuildOrchestrator(engineDir, out var registry, out _);
            orch.SyncRegistryFromEngine();
            Assert.Equal(2, registry.GetGenomes().Count());

            File.Delete(dropPath);
            orch.SyncRegistryFromEngine();

            var remaining = registry.GetGenomes().ToList();
            Assert.Single(remaining);
            Assert.Equal("keep", remaining[0].DisplayName);
        }
        finally
        {
            Directory.Delete(engineDir, recursive: true);
        }
    }

    [Fact]
    public void SyncRegistryFromEngine_PreservesOrchestratorEnabledFlag_AcrossResync()
    {
        var engineDir = MakeEngineDir();
        try
        {
            WriteProfile(engineDir, "toggle", "--filter-tcp 80");

            using var orch = BuildOrchestrator(engineDir, out var registry, out _);
            orch.SyncRegistryFromEngine();

            var id = registry.GetGenomes().Single().Id;
            var g = registry.GetById(id)!;
            g.OrchestratorEnabled = false;
            registry.Upsert(g);
            registry.Save();

            // Re-sync with no change to the .bat — the disabled flag must survive.
            orch.SyncRegistryFromEngine();

            var after = registry.GetById(id)!;
            Assert.False(after.OrchestratorEnabled);
            Assert.Equal("toggle", after.DisplayName);
        }
        finally
        {
            Directory.Delete(engineDir, recursive: true);
        }
    }

    [Fact]
    public void SyncRegistryFromEngine_IgnoresEngineDirWhenMissing()
    {
        var engineDir = Path.Combine(Path.GetTempPath(), "bsdpi-orch-missing-" + Guid.NewGuid().ToString("N"));
        using var orch = BuildOrchestrator(engineDir, out var registry, out _);
        orch.SyncRegistryFromEngine();
        Assert.Empty(registry.GetGenomes());
    }
}
