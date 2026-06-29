using System.Collections.Generic;
using FluxRoute.AI.Models;
using FluxRoute.AI.Services;
using FluxRoute.Core.Models;

namespace FluxRoute.Core.Tests;

public sealed class BanditSelectorTests
{
    [Fact]
    public void Pick_WithZeroExploration_PrefersHigherPosteriorMean()
    {
        var path = Path.Combine(Path.GetTempPath(), "fr-ai-bd-" + Guid.NewGuid().ToString("N") + ".json");
        var reg = new AiStrategyRegistry(path);
        reg.Load();

        var gBetter = new StrategyGenome { DisplayName = "a", DesyncMode = "split" };
        var gWorse = new StrategyGenome { DisplayName = "b", DesyncMode = "split" };
        reg.Upsert(gBetter);
        reg.Upsert(gWorse);

        const string net = "nh";
        reg.RecordBanditSuccess(gBetter.Id, net, 100);
        reg.RecordBanditSuccess(gBetter.Id, net, 100);
        reg.RecordBanditSuccess(gBetter.Id, net, 100);
        reg.RecordBanditFailure(gWorse.Id, net, 100);

        var sel = new BanditSelector(reg, aiSettings: null, new Random(42));
        var counts = new Dictionary<Guid, int>
        {
            [gBetter.Id] = 0,
            [gWorse.Id] = 0,
        };

        var list = new List<StrategyGenome> { gBetter, gWorse };
        for (var i = 0; i < 400; i++)
        {
            var p = sel.Pick(list, net, explorationPermil: 0);
            Assert.NotNull(p);
            counts[p.Id]++;
        }

        Assert.True(counts[gBetter.Id] > counts[gWorse.Id]);
    }

    [Fact]
    public void Pick_WithFullExploration_ReturnsCandidate()
    {
        var path = Path.Combine(Path.GetTempPath(), "fr-ai-bd2-" + Guid.NewGuid().ToString("N") + ".json");
        var reg = new AiStrategyRegistry(path);
        reg.Load();
        var g = new StrategyGenome { DisplayName = "only", DesyncMode = "split" };
        reg.Upsert(g);
        var sel = new BanditSelector(reg, aiSettings: null, new Random(1));
        var p = sel.Pick([g], "x", explorationPermil: 1000);
        Assert.Same(g, p);
    }

    [Fact]
    public void Pick_Ucb1_ChoosesUnderExploredCandidate()
    {
        var path = Path.Combine(Path.GetTempPath(), "fr-ai-ucb1-" + Guid.NewGuid().ToString("N") + ".json");
        var reg = new AiStrategyRegistry(path);
        reg.Load();

        var gPopular = new StrategyGenome { Id = Guid.NewGuid(), DisplayName = "popular" };
        var gRare = new StrategyGenome { Id = Guid.NewGuid(), DisplayName = "rare" };
        reg.Upsert(gPopular);
        reg.Upsert(gRare);

        const string net = "nh";
        // popular has many trials (100 successes, 0 failures) -> mean=1.0, n=100
        for (int i = 0; i < 100; i++) reg.RecordBanditSuccess(gPopular.Id, net, 100);
        // rare has very few trials (1 success, 0 failures) -> mean=1.0, n=1
        reg.RecordBanditSuccess(gRare.Id, net, 100);

        // UCB1 should favor rare because its exploration bonus is much higher.
        // Popular: 1.0 + sqrt(2*ln(101)/100) ≈ 1.0 + 0.3 = 1.3
        // Rare: 1.0 + sqrt(2*ln(101)/1) ≈ 1.0 + 3.0 = 4.0

        var sel = new BanditSelector(reg, aiSettings: () => new AiSettings { UseUcb1 = true }, new Random(42));
        var p = sel.Pick(new List<StrategyGenome> { gPopular, gRare }, net, explorationPermil: 0);

        Assert.NotNull(p);
        Assert.Equal(gRare.Id, p.Id);
    }
}
