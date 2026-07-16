using BSDPI.AI.Models;
using BSDPI.AI.Services;
using BSDPI.Core.Models;

namespace BSDPI.Core.Tests;

public sealed class AiStrategyImportExportTests
{
    [Fact]
    public void GetTrainedGenomes_ExcludesBuiltins()
    {
        var reg = new AiStrategyRegistry(Path.Combine(Path.GetTempPath(), "reg-" + Guid.NewGuid().ToString("N") + ".json"));
        reg.Upsert(new StrategyGenome { DisplayName = "builtin", Origin = StrategyOrigin.Builtin });
        reg.Upsert(new StrategyGenome { DisplayName = "evolved", Origin = StrategyOrigin.Evolved });
        reg.Upsert(new StrategyGenome { DisplayName = "manual", Origin = StrategyOrigin.Manual });

        var trained = reg.GetTrainedGenomes().Select(g => g.DisplayName).ToList();
        Assert.Contains("evolved", trained);
        Assert.Contains("manual", trained);
        Assert.DoesNotContain("builtin", trained);
    }

    [Fact]
    public void ExportImport_RoundTrip_PreservesTrainedGenomes()
    {
        var srcPath = Path.Combine(Path.GetTempPath(), "reg-src-" + Guid.NewGuid().ToString("N") + ".json");
        var reg = new AiStrategyRegistry(srcPath);
        var g = new StrategyGenome
        {
            DisplayName = "GenA",
            Origin = StrategyOrigin.Evolved,
            EngineType = DpiEngineType.ByeDpi,
            SplitPosSemantic = "host",
            FakeTtl = 64,
        };
        reg.Upsert(g);
        reg.Upsert(new StrategyGenome { DisplayName = "builtin", Origin = StrategyOrigin.Builtin });
        var sig0 = g.Signature;

        var exportFile = Path.Combine(Path.GetTempPath(), "strategies-" + Guid.NewGuid().ToString("N") + ".json");
        reg.ExportStrategies(exportFile);

        var dstPath = Path.Combine(Path.GetTempPath(), "reg-dst-" + Guid.NewGuid().ToString("N") + ".json");
        var target = new AiStrategyRegistry(dstPath);
        var imported = target.ImportStrategies(exportFile);

        Assert.Equal(1, imported);
        var loaded = target.GetTrainedGenomes().Single();
        Assert.Equal("GenA", loaded.DisplayName);
        Assert.Equal(DpiEngineType.ByeDpi, loaded.EngineType);
        Assert.Equal("host", loaded.SplitPosSemantic);
        Assert.Equal(64, loaded.FakeTtl);
        Assert.Equal(sig0, loaded.Signature);
    }

    [Fact]
    public void ImportStrategies_MergeReplacesExisting_AndAddsNew()
    {
        var reg = new AiStrategyRegistry(Path.Combine(Path.GetTempPath(), "reg-merge-" + Guid.NewGuid().ToString("N") + ".json"));

        var existing = new StrategyGenome { DisplayName = "Old", Origin = StrategyOrigin.Evolved, FakeTtl = 10 };
        reg.Upsert(existing);

        var updated = new StrategyGenome { Id = existing.Id, DisplayName = "New", Origin = StrategyOrigin.Evolved, FakeTtl = 99 };
        var fresh = new StrategyGenome { DisplayName = "Added", Origin = StrategyOrigin.Manual };
        var file = Path.Combine(Path.GetTempPath(), "imp-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(file, System.Text.Json.JsonSerializer.Serialize(new[] { updated, fresh }));

        var added = reg.ImportStrategies(file);
        Assert.Equal(1, added);

        Assert.Equal(2, reg.GetTrainedGenomes().Count);
        var merged = reg.GetById(existing.Id)!;
        Assert.Equal("New", merged.DisplayName);
        Assert.Equal(99, merged.FakeTtl);
    }

    [Fact]
    public void ImportStrategies_NonMergeSkipsExisting()
    {
        var reg = new AiStrategyRegistry(Path.Combine(Path.GetTempPath(), "reg-nonmerge-" + Guid.NewGuid().ToString("N") + ".json"));
        var existing = new StrategyGenome { DisplayName = "Old", Origin = StrategyOrigin.Evolved };
        reg.Upsert(existing);

        var updated = new StrategyGenome { Id = existing.Id, DisplayName = "New", Origin = StrategyOrigin.Evolved };
        var file = Path.Combine(Path.GetTempPath(), "imp2-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(file, System.Text.Json.JsonSerializer.Serialize(new[] { updated }));

        var added = reg.ImportStrategies(file, merge: false);
        Assert.Equal(0, added);
        Assert.Equal("Old", reg.GetById(existing.Id)!.DisplayName);
    }
}
