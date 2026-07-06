using System.Globalization;
using FluxRoute.AI.Models;
using FluxRoute.Core.Services;

namespace FluxRoute.AI.Services;

public static class BatGenomeParser
{
    private static readonly HashSet<string> KnownPrefixes =
    [
        "--filter-tcp", "--filter-udp", "--dpi-desync", "--dpi-desync-split-pos",
        "--dpi-desync-disorder-pos", "--dpi-desync-fake-pos", "--dpi-desync-oob-pos",
        "--dpi-desync-disoob-pos", "--dpi-desync-tlsrec-pos",
        "--dpi-desync-fake-tls-mod", "--dpi-desync-ttl", "--dpi-desync-autottl",
        "--dpi-desync-any-protocol", "--dpi-desync-fooling", "--dpi-desync-fake-resend",
        "--hostlist", "--new", "--dpi-desync-repeats"
    ];

    public static StrategyGenome FromLaunchPlan(WinwsLaunchPlan plan, string displayName, StrategyOrigin origin)
    {
        var g = new StrategyGenome
        {
            DisplayName = displayName,
            Origin = origin,
            SourceBatPath = plan.SourceProfilePath,
            BatFileName = string.IsNullOrEmpty(plan.SourceProfilePath)
                ? null
                : Path.GetFileName(plan.SourceProfilePath),
            DesyncMode = "split",
        };

        var args = plan.Arguments.ToList();
        var i = 0;
        while (i < args.Count)
        {
            var a = args[i];
            if (!a.StartsWith('-'))
            {
                g.ExtraArgs.Add(a);
                i++;
                continue;
            }

            var flag = a;
            string? val = null;
            var takesValue = FlagTakesValue(flag);
            if (takesValue && i + 1 < args.Count && !args[i + 1].StartsWith('-'))
            {
                val = args[i + 1];
                i += 2;
            }
            else
            {
                i++;
            }

            if (!KnownPrefixes.Contains(flag))
            {
                g.ExtraArgs.Add(flag);
                if (val is not null)
                    g.ExtraArgs.Add(val);
                continue;
            }

            switch (flag)
            {
                case "--filter-tcp":
                    if (val is not null)
                        g.FilterTcp = val;
                    break;
                case "--filter-udp":
                    if (val is not null)
                        g.FilterUdp = val;
                    break;
                case "--dpi-desync":
                    if (val is not null)
                        g.DesyncMode = val;
                    break;
                case "--dpi-desync-split-pos":
                    ApplySplitPos(g, val);
                    break;
                case "--dpi-desync-disorder-pos":
                    g.DisorderPos = val;
                    break;
                case "--dpi-desync-fake-pos":
                    g.FakePos = val;
                    break;
                case "--dpi-desync-oob-pos":
                    g.OobPos = val;
                    break;
                case "--dpi-desync-disoob-pos":
                    g.DisoobPos = val;
                    break;
                case "--dpi-desync-tlsrec-pos":
                    g.TlsrecPos = val;
                    break;
                case "--dpi-desync-fake-tls-mod":
                    g.FakeTlsMod = val;
                    break;
                case "--dpi-desync-ttl":
                    if (val is not null && int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ttl))
                        g.FakeTtl = ttl;
                    break;
                case "--dpi-desync-autottl":
                    g.AutoTtl = string.IsNullOrEmpty(val) || val is "1" or "true" or "yes";
                    if (val is not null && int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var autottl))
                        g.AutoTtl = autottl != 0;
                    break;
                case "--dpi-desync-any-protocol":
                    g.DesyncAnyProtocol = val;
                    break;
                case "--dpi-desync-fooling":
                    g.DesyncFooling = val;
                    break;
                case "--dpi-desync-fake-resend":
                    g.FakeResend = val;
                    break;
                case "--hostlist":
                    g.Hostlist = val;
                    break;
                case "--new":
                    break;
                case "--dpi-desync-repeats":
                    if (val is not null && int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rep))
                        g.RepeatCount = rep;
                    break;
            }
        }

        StrategyGenomeValidator.Normalize(g);
        return g;
    }

    private static void ApplySplitPos(StrategyGenome g, string? val)
    {
        if (string.IsNullOrEmpty(val))
            return;

        if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
        {
            g.SplitPos = n;
            g.SplitPosSemantic = null;
            return;
        }

        var sem = new[] { "host", "endhost", "midsld", "sniext", "endsld", "method", "extlen" };
        foreach (var s in sem)
        {
            if (val.Equals(s, StringComparison.OrdinalIgnoreCase))
            {
                g.SplitPosSemantic = s.ToLowerInvariant();
                g.SplitPos = null;
                return;
            }
        }

        g.SplitPosSemantic = val;
        g.SplitPos = null;
    }

    private static bool FlagTakesValue(string flag) =>
        flag is "--filter-tcp" or "--filter-udp" or "--dpi-desync" or "--dpi-desync-split-pos"
            or "--dpi-desync-disorder-pos" or "--dpi-desync-fake-pos" or "--dpi-desync-oob-pos"
            or "--dpi-desync-disoob-pos" or "--dpi-desync-tlsrec-pos"
            or "--dpi-desync-fake-tls-mod" or "--dpi-desync-ttl" or "--dpi-desync-autottl"
            or "--dpi-desync-any-protocol" or "--dpi-desync-fooling" or "--dpi-desync-fake-resend"
            or "--hostlist" or "--dpi-desync-repeats";
}
