using FluxRoute.AI.Models;
using FluxRoute.AI.Services;

namespace FluxRoute.Core.Tests;

public sealed class AiStrategyRegistryTests
{
    [Fact]
    public void GetActiveGenomes_ExcludesDisabled()
    {
        var path = Path.Combine(Path.GetTempPath(), "fr-reg-" + Guid.NewGuid().ToString("N") + ".json");
        var reg = new AiStrategyRegistry(path);
        reg.Load();
        var a = new StrategyGenome { DisplayName = "a", DesyncMode = "split", FilterTcp = "80", SplitPosSemantic = "host" };
        var b = new StrategyGenome
        {
            DisplayName = "b",
            DesyncMode = "fake",
            FilterTcp = "443",
            FakeTlsMod = "rand",
            OrchestratorEnabled = false,
        };
        reg.Upsert(a);
        reg.Upsert(b);
        reg.Save();
        reg.Load();
        var active = reg.GetActiveGenomes();
        Assert.Single(active);
        Assert.Equal(a.Id, active[0].Id);
    }

    [Fact]
    public void SetOrchestratorEnabled_Persists()
    {
        var path = Path.Combine(Path.GetTempPath(), "fr-reg-" + Guid.NewGuid().ToString("N") + ".json");
        var reg = new AiStrategyRegistry(path);
        reg.Load();
        var a = new StrategyGenome { DisplayName = "a", DesyncMode = "split", FilterTcp = "80", SplitPosSemantic = "host" };
        reg.Upsert(a);
        reg.Save();
        reg.SetOrchestratorEnabled(a.Id, false);
        var reg2 = new AiStrategyRegistry(path);
        reg2.Load();
        Assert.False(reg2.GetById(a.Id)!.OrchestratorEnabled);
    }

    [Fact]
    public void GetBanditSnapshot_ReturnsCorrectEntries()
    {
        var path = Path.Combine(Path.GetTempPath(), "fr-reg-" + Guid.NewGuid().ToString("N") + ".json");
        var reg = new AiStrategyRegistry(path);
        var gid = Guid.NewGuid();
        var net = "net1";

        reg.RecordBanditSuccess(gid, net, 100);
        reg.RecordBanditFailure(gid, net, 200);

        var snapshot = reg.GetBanditSnapshot(net);
        Assert.True(snapshot.ContainsKey(gid));
        Assert.Equal(2, snapshot[gid].Alpha); // 1 (initial) + 1 (success)
        Assert.Equal(2, snapshot[gid].Beta);  // 1 (initial) + 1 (failure)
    }

    [Fact]
    public void GetAggregatedStatsSnapshot_CalculatesCorrectly()
    {
        var path = Path.Combine(Path.GetTempPath(), "fr-reg-" + Guid.NewGuid().ToString("N") + ".json");
        var reg = new AiStrategyRegistry(path);
        var gid = Guid.NewGuid();

        reg.RecordBanditSuccess(gid, "net1", 100);
        reg.RecordBanditSuccess(gid, "net2", 150);
        reg.RecordBanditFailure(gid, "net1", 200);

        var snapshot = reg.GetAggregatedStatsSnapshot();
        Assert.True(snapshot.ContainsKey(gid));
        Assert.Equal(3, snapshot[gid].Alpha); // 1 (initial) + 1 + 1 (successes)
        Assert.Equal(2, snapshot[gid].Beta);  // 1 (initial) + 1 (failure)
    }
}
