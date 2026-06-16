using FluxRoute.AI.Models;
using FluxRoute.Core.Models;

namespace FluxRoute.AI.Services;

public static class StrategyGenomeValidator
{
    private static readonly HashSet<string> FakeModes =
        ["fake", "fakesplit", "fakedisorder", "multidisorder", "multisplit"];

    public static bool IsValid(StrategyGenome g)
    {
        if (g.EngineType == DpiEngineType.ByeDpi)
            return IsValidByeDpi(g);
        if (g.EngineType == DpiEngineType.Warp)
            return IsValidWarp(g);
        return IsValidZapret(g);
    }

    private static bool IsValidZapret(StrategyGenome g)
    {
        if (string.IsNullOrWhiteSpace(g.DesyncMode))
            return false;

        if (!string.IsNullOrEmpty(g.FakeTlsMod) && !FakeModes.Contains(g.DesyncMode.ToLowerInvariant()))
            return false;

        return true;
    }

    private static bool IsValidWarp(StrategyGenome g)
    {
        // Warp strategies are valid if they are just Warp engine type for now
        return g.EngineType == DpiEngineType.Warp;
    }

    private static bool IsValidByeDpi(StrategyGenome g)
    {
        if (string.IsNullOrWhiteSpace(g.DisorderPos) &&
            string.IsNullOrWhiteSpace(g.SplitPosSemantic) &&
            string.IsNullOrWhiteSpace(g.FakePos) &&
            string.IsNullOrWhiteSpace(g.TlsrecPos) &&
            string.IsNullOrWhiteSpace(g.OobPos))
            return false;

        return true;
    }

    public static void Normalize(StrategyGenome g)
    {
        if (g.EngineType == DpiEngineType.ByeDpi)
        {
            NormalizeByeDpi(g);
            return;
        }

        NormalizeZapret(g);
    }

    private static void NormalizeZapret(StrategyGenome g)
    {
        if (!string.IsNullOrEmpty(g.FakeTlsMod) && !FakeModes.Contains(g.DesyncMode.ToLowerInvariant()))
            g.FakeTlsMod = null;

        if (g.FakeTtl is < 1 or > 128)
            g.FakeTtl = null;
    }

    private static void NormalizeByeDpi(StrategyGenome g)
    {
        g.DesyncMode = "split";
        g.AutoTtl = false;

        if (g.FakeTtl is < 1 or > 128)
            g.FakeTtl = null;
    }
}
