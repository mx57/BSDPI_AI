using FluxRoute.AI.Models;
using FluxRoute.AI.Services;
using FluxRoute.Core.Models;

namespace FluxRoute.Core.Tests;

public sealed class StrategyEvolverTests
{
    [Fact]
    public void Evolve_CreatesChild_WithEvolvedOrigin()
    {
        var root = Path.Combine(Path.GetTempPath(), "fr-ai-ev-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "bin"));
        Directory.CreateDirectory(Path.Combine(root, "lists"));

        var regPath = Path.Combine(root, "strategies.json");
        var histPath = Path.Combine(root, "history.jsonl");
        var reg = new AiStrategyRegistry(regPath);
        reg.Load();
        var hist = new AiHistoryStore(histPath);

        var g1 = new StrategyGenome { DisplayName = "p1", DesyncMode = "split", FilterTcp = "80", SplitPosSemantic = "host" };
        var g2 = new StrategyGenome { DisplayName = "p2", DesyncMode = "fake", FilterTcp = "443", FakeTlsMod = "rand" };
        var g3 = new StrategyGenome
        {
            DisplayName = "p3",
            DesyncMode = "multisplit",
            FilterTcp = "443",
            SplitPosSemantic = "host",
            OrchestratorEnabled = false,
        };
        reg.Upsert(g1);
        reg.Upsert(g2);
        reg.Upsert(g3);

        var net = new NetworkFingerprint { Hash = "nh1", Label = "t" };
        for (var i = 0; i < 8; i++)
        {
            hist.Append(new ProbeOutcome
            {
                GenomeId = g1.Id,
                NetworkHash = net.Hash,
                Score = 80,
                SuccessRate = 1,
                AvgLatencyMs = 10,
                ProcessStable = true,
            });
        }

        var evolver = new StrategyEvolver(
            reg,
            hist,
            () => root,
            () => new AiSettings { MaxEvolvedStrategies = 24 },
            new Random(777));

        var child = evolver.Evolve(net);
        Assert.NotNull(child);
        Assert.Equal(StrategyOrigin.Evolved, child!.Origin);
        Assert.Null(child.SourceBatPath);
        Assert.Null(child.BatFileName);
        Assert.Contains(reg.GetGenomes(), x => x.Id == child.Id);
    }

    [Fact]
    public void MutateZapret_HandlesNewPositions()
    {
        var root = Path.Combine(Path.GetTempPath(), "fr-ai-ev-" + Guid.NewGuid().ToString("N"));
        var reg = new AiStrategyRegistry(Path.Combine(root, "reg.json"));
        var hist = new AiHistoryStore(Path.Combine(root, "hist.jsonl"));
        var evolver = new StrategyEvolver(reg, hist, () => root, () => new AiSettings(), new Random(42));

        var g = new StrategyGenome { EngineType = DpiEngineType.Zapret, DesyncMode = "split" };

        // Test OobPos mutation (case 12)
        var method = typeof(StrategyEvolver).GetMethod("MutateZapret", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        // Case 12: OobPos
        method!.Invoke(evolver, [g, 12]);
        Assert.NotNull(g.OobPos);

        // Case 13: DisoobPos
        method!.Invoke(evolver, [g, 13]);
        Assert.NotNull(g.DisoobPos);

        // Case 14: TlsrecPos
        method!.Invoke(evolver, [g, 14]);
        Assert.NotNull(g.TlsrecPos);

        // Verify IsValid accepts these
        Assert.True(StrategyGenomeValidator.IsValid(g));
    }
}
