namespace FluxRoute.AI.Stats;

public static class WilsonScore
{
    public static double LowerBound(int successes, int trials, double z = 1.96, double? avgSpeedMbps = null)
    {
        if (trials <= 0)
            return 0;

        var phat = successes / (double)trials;
        var z2 = z * z;
        var denom = 1 + z2 / trials;
        var center = phat + z2 / (2 * trials);
        var margin = z * Math.Sqrt((phat * (1 - phat) + z2 / (4 * trials)) / trials);
        var lb = Math.Clamp((center - margin) / denom, 0, 1) * 100.0;

        if (avgSpeedMbps.HasValue && avgSpeedMbps.Value > 0)
        {
            var speedBonus = Math.Min(5.0, avgSpeedMbps.Value / 4.0);
            lb += speedBonus;
        }

        return Math.Round(lb, 2);
    }

    public static double MeanScore(IReadOnlyList<int> scores)
    {
        if (scores.Count == 0)
            return 0;
        return scores.Average();
    }
}
