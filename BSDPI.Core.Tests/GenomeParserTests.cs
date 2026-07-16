using BSDPI.AI.Models;
using BSDPI.AI.Services;
using BSDPI.Core.Models;
using BSDPI.Core.Services;

namespace BSDPI.Core.Tests;

public sealed class GenomeParserTests
{
    [Fact]
    public void FromByeDpiArgs_ParsesKnownFlags()
    {
        var args = new[]
        {
            "--split", "host",
            "--disorder", "2",
            "--fake", "sni",
            "--ttl", "64",
            "--md5sig",
            "--fake-tls-mod", "rand",
        };
        var g = GenomeParser.FromByeDpiArgs(args, "byedpi", StrategyOrigin.Manual);

        Assert.Equal(DpiEngineType.ByeDpi, g.EngineType);
        Assert.Equal("host", g.SplitPosSemantic);
        Assert.Equal("2", g.DisorderPos);
        Assert.Equal("sni", g.FakePos);
        Assert.Equal(64, g.FakeTtl);
        Assert.True(g.Md5sig);
        Assert.Equal("rand", g.FakeTlsMod);
        // NormalizeByeDpi forces the canonical desync mode for ByeDpi genomes.
        Assert.Equal("split", g.DesyncMode);
    }

    [Fact]
    public void FromByeDpiArgs_KeepsUnknownFlagsInExtraArgs()
    {
        var args = new[]
        {
            "--split", "host",
            "--mystery-flag", "value",
            "positional-arg",
        };
        var g = GenomeParser.FromByeDpiArgs(args, "byedpi", StrategyOrigin.Manual);

        Assert.Contains("--mystery-flag", g.ExtraArgs);
        Assert.Contains("value", g.ExtraArgs);
        Assert.Contains("positional-arg", g.ExtraArgs);
    }

    [Fact]
    public void FromByeDpiArgs_InvalidTtlValue_KeepsFakeTtlNull()
    {
        // A known flag with a non-numeric value is dropped (FakeTtl stays null); only unknown flags land in ExtraArgs.
        var args = new[] { "--ttl", "not-a-number" };
        var g = GenomeParser.FromByeDpiArgs(args, "byedpi", StrategyOrigin.Manual);

        Assert.Null(g.FakeTtl);
    }

    [Fact]
    public void FromByeDpiArgs_ValueTakingFlagDoesNotConsumeFollowingFlag()
    {
        // --fake-sni takes a value, but the next token is a flag, so it gets no value (null).
        // The following "--split host" is then parsed normally.
        var args = new[] { "--fake-sni", "--split", "host" };
        var g = GenomeParser.FromByeDpiArgs(args, "byedpi", StrategyOrigin.Manual);

        Assert.Null(g.FakeSni);
        Assert.Equal("host", g.SplitPosSemantic);
    }

    [Fact]
    public void FromLaunchPlan_ParsesZapretKnownFlags_AndKeepsUnknownInExtra()
    {
        var args = new[]
        {
            "--filter-tcp", "80",
            "--dpi-desync", "fake",
            "--dpi-desync-split-pos", "midsld",
            "--dpi-desync-fake-tls-mod", "rand",
            "--unknown-flag", "x",
        };
        var plan = new WinwsLaunchPlan(@"C:\engine\bin\winws.exe", args, @"C:\engine", @"C:\engine\p.bat");
        var g = GenomeParser.FromLaunchPlan(plan, "zapret", StrategyOrigin.Builtin);

        Assert.Equal(DpiEngineType.Zapret, g.EngineType);
        Assert.Equal("80", g.FilterTcp);
        Assert.Equal("fake", g.DesyncMode);
        Assert.Equal("midsld", g.SplitPosSemantic);
        Assert.Equal("rand", g.FakeTlsMod);
        Assert.Contains("--unknown-flag", g.ExtraArgs);
        Assert.Contains("x", g.ExtraArgs);
    }

    [Fact]
    public void FromLaunchPlan_SplitPosNumericVsSemantic()
    {
        var numeric = new WinwsLaunchPlan("", new[] { "--dpi-desync-split-pos", "42" }, "", "");
        var g1 = GenomeParser.FromLaunchPlan(numeric, "n", StrategyOrigin.Builtin);
        Assert.Equal(42, g1.SplitPos);
        Assert.Null(g1.SplitPosSemantic);

        var semantic = new WinwsLaunchPlan("", new[] { "--dpi-desync-split-pos", "endhost" }, "", "");
        var g2 = GenomeParser.FromLaunchPlan(semantic, "s", StrategyOrigin.Builtin);
        Assert.Equal("endhost", g2.SplitPosSemantic);
        Assert.Null(g2.SplitPos);
    }

    [Fact]
    public void FromLaunchPlan_AutoTtlVariants()
    {
        Assert.True(GenomeParser.FromLaunchPlan(new WinwsLaunchPlan("", new[] { "--dpi-desync-autottl" }, "", ""), "a", StrategyOrigin.Builtin).AutoTtl);
        Assert.True(GenomeParser.FromLaunchPlan(new WinwsLaunchPlan("", new[] { "--dpi-desync-autottl", "1" }, "", ""), "a", StrategyOrigin.Builtin).AutoTtl);
        Assert.False(GenomeParser.FromLaunchPlan(new WinwsLaunchPlan("", new[] { "--dpi-desync-autottl", "0" }, "", ""), "a", StrategyOrigin.Builtin).AutoTtl);
        Assert.False(GenomeParser.FromLaunchPlan(new WinwsLaunchPlan("", new[] { "--dpi-desync-autottl", "no" }, "", ""), "a", StrategyOrigin.Builtin).AutoTtl);
    }

    [Fact]
    public void FromLaunchPlan_InvalidTtlValue_KeepsFakeTtlNull()
    {
        // A known zapret flag with a non-numeric value is dropped (FakeTtl stays null).
        var plan = new WinwsLaunchPlan("", new[] { "--dpi-desync-ttl", "abc" }, "", "");
        var g = GenomeParser.FromLaunchPlan(plan, "z", StrategyOrigin.Builtin);

        Assert.Null(g.FakeTtl);
    }

    [Fact]
    public void RoundTrip_ViaBatMaterializer_PreservesSignature()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "bsdpi-gp-rt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tmp, "bin"));
        Directory.CreateDirectory(Path.Combine(tmp, "lists"));

        var g = new StrategyGenome
        {
            DisplayName = "RT",
            DesyncMode = "split",
            FilterTcp = "80,443",
            SplitPosSemantic = "host",
            Origin = StrategyOrigin.Builtin,
        };
        var sig0 = GenomeSignature.Compute(g);

        var args = BatMaterializer.BuildWinwsArgs(g);
        var plan = new WinwsLaunchPlan(Path.Combine(tmp, "bin", "winws.exe"), args, tmp, "");
        var g2 = GenomeParser.FromLaunchPlan(plan, "RT", StrategyOrigin.Builtin);
        Assert.Equal(sig0, GenomeSignature.Compute(g2));

        Directory.Delete(tmp, recursive: true);
    }
}
