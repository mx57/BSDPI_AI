using FluxRoute.Core.Models;

namespace FluxRoute.Core.Services;

public static class ProfileScoringService
{
    public static int Calculate(
        bool processStarted,
        bool processStable,
        IReadOnlyList<CheckResult> checks,
        bool requireWinwsProcess)
    {
        var warpCheck = checks.FirstOrDefault(x => x.Key == "Warp");
        var hasWarpCheck = warpCheck != null;
        var isWarpActive = warpCheck?.Ok == true;

        if (requireWinwsProcess && !processStarted)
            return 0;

        var score = 0;

        if (processStarted)
            score += 20;

        if (processStable)
            score += 15;

        // Warp bonus: if we have a warp check and it's active, it's a good sign
        if (isWarpActive)
            score += 10;

        if (checks.Count > 0)
        {
            var otherChecks = checks.Where(x => x.Key != "Warp").ToList();
            if (otherChecks.Count > 0)
            {
                var successRate = otherChecks.Count(x => x.Ok) / (double)otherChecks.Count;
                score += (int)Math.Round(successRate * 55);
                score += CalculateLatencyBonus(otherChecks);

                if (otherChecks.All(x => !x.Ok))
                    score -= 20;
            }
            else if (hasWarpCheck)
            {
                // If only warp check is present
                score += isWarpActive ? 55 : 0;
            }
        }

        if (requireWinwsProcess && processStarted && !processStable)
            score = Math.Min(score - 20, 35);

        return Math.Clamp(score, 0, 100);
    }

    private static int CalculateLatencyBonus(IReadOnlyList<CheckResult> checks)
    {
        var timings = checks
            .Where(x => x.Ok && x.ElapsedMs is not null)
            .Select(x => x.ElapsedMs!.Value)
            .ToList();

        if (timings.Count == 0)
            return 0;

        var avg = timings.Average();

        if (avg <= 500) return 10;
        if (avg <= 1000) return 7;
        if (avg <= 2000) return 4;
        return 1;
    }
}
