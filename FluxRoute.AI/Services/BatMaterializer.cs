using System.Globalization;
using System.Text;
using FluxRoute.AI.Models;
using FluxRoute.Core.Models;
using FluxRoute.Core.Services;

namespace FluxRoute.AI.Services;

public sealed class BatMaterializer
{
    private readonly Func<string> _engineDir;

    public BatMaterializer(Func<string> engineDir)
    {
        _engineDir = engineDir;
    }

    public string WriteProfile(StrategyGenome g)
    {
        var engineDir = _engineDir();
        ProfileBatLauncher.PrepareRuntime(engineDir);

        if (g.EngineType == DpiEngineType.ByeDpi)
            return WriteByeDpiBat(g, engineDir);
        if (g.EngineType == DpiEngineType.Warp)
            return WriteWarpBat(g, engineDir);

        return WriteZapretBat(g, engineDir);
    }

    private string WriteWarpBat(StrategyGenome g, string engineDir)
    {
        var warpDir = Path.Combine(engineDir, "warp");
        Directory.CreateDirectory(warpDir);

        var p = g.ToEngineProfile();
        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine($"cd /d \"{warpDir}\"");

        var cmd = new StringBuilder("start /min \"\" warp-plus.exe");
        cmd.Append($" -b 127.0.0.1:{p.SocksPort}");

        if (!string.IsNullOrWhiteSpace(p.WarpConfig))
        {
            try
            {
                File.WriteAllText(Path.Combine(warpDir, "warp.conf"), p.WarpConfig, new UTF8Encoding(false));
                cmd.Append(" --wgconf \"warp.conf\"");
            }
            catch { /* Ignore IO errors in materializer */ }
        }

        if (p.MTU.HasValue) cmd.Append($" --mtu {p.MTU.Value}");
        if (p.GoolEnabled) cmd.Append(" --gool");
        if (p.PsiphonEnabled)
        {
            cmd.Append(" --cfon");
            if (!string.IsNullOrWhiteSpace(p.PsiphonCountry))
                cmd.Append(" --country ").Append(QuoteIfNeeded(p.PsiphonCountry));
        }
        if (p.ScanEnabled) cmd.Append(" --scan");
        if (!string.IsNullOrWhiteSpace(p.Reserved))
            cmd.Append(" --reserved ").Append(QuoteIfNeeded(p.Reserved));

        foreach (var extra in p.ExtraArgs)
            if (!string.IsNullOrWhiteSpace(extra)) cmd.Append(' ').Append(QuoteIfNeeded(extra));

        sb.AppendLine(cmd.ToString());
        sb.AppendLine("exit");

        var dir = Path.Combine(engineDir, "ai-evolved");
        Directory.CreateDirectory(dir);
        var safeName = SanitizeFileName(g.DisplayName);
        var path = Path.Combine(dir, $"{safeName}.bat");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        return path;
    }

    private string WriteByeDpiBat(StrategyGenome g, string engineDir)
    {
        var byedpiDir = Path.Combine(engineDir, "byedpi");
        Directory.CreateDirectory(byedpiDir);

        var args = ByeDpiEngine.BuildCliArgs(g.ToEngineProfile());
        var argLine = string.Join(" ", args.Select(QuoteIfNeeded));

        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine($"cd /d \"{byedpiDir}\"");
        sb.AppendLine($"start /min \"\" ciadpi.exe {argLine}");
        sb.AppendLine("exit");

        var dir = Path.Combine(engineDir, "ai-evolved");
        Directory.CreateDirectory(dir);
        var safeName = SanitizeFileName(g.DisplayName);
        var path = Path.Combine(dir, $"{safeName}.bat");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        return path;
    }

    private string WriteZapretBat(StrategyGenome g, string engineDir)
    {
        var bin = Path.Combine(engineDir, "bin") + Path.DirectorySeparatorChar;
        var lists = Path.Combine(engineDir, "lists") + Path.DirectorySeparatorChar;
        var (gameFilter, gameTcp, gameUdp) = ReadGameFilter(engineDir);

        var args = BuildWinwsArgs(g);
        var argLine = string.Join(" ", args.Select(QuoteIfNeeded));

        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine($"set \"BIN={bin}\"");
        sb.AppendLine($"set \"LISTS={lists}\"");
        sb.AppendLine($"set \"GameFilter={gameFilter}\"");
        sb.AppendLine($"set \"GameFilterTCP={gameTcp}\"");
        sb.AppendLine($"set \"GameFilterUDP={gameUdp}\"");
        sb.AppendLine($"\"%BIN%winws.exe\" {argLine}");
        sb.AppendLine("exit");

        var dir = Path.Combine(engineDir, "ai-evolved");
        Directory.CreateDirectory(dir);
        var safeName = SanitizeFileName(g.DisplayName);
        var path = Path.Combine(dir, $"{safeName}.bat");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        return path;
    }

    public static IReadOnlyList<string> BuildWinwsArgs(StrategyGenome g)
    {
        return ZapretEngine.BuildWinwsArgs(g.ToEngineProfile());
    }

    private static (string GameFilter, string Tcp, string Udp) ReadGameFilter(string engineDir)
    {
        var flagPath = Path.Combine(engineDir, "utils", "game_filter.enabled");
        if (!File.Exists(flagPath))
            return ("12", "12", "12");

        var mode = File.ReadLines(flagPath).FirstOrDefault()?.Trim().ToLowerInvariant();
        return mode switch
        {
            "all" => ("1024-65535", "1024-65535", "1024-65535"),
            "tcp" => ("1024-65535", "1024-65535", "12"),
            "udp" => ("1024-65535", "12", "1024-65535"),
            _ => ("1024-65535", "1024-65535", "1024-65535"),
        };
    }

    public static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
            sb.Append(invalid.Contains(c) ? '_' : c);
        var s = sb.ToString().Trim();
        return string.IsNullOrEmpty(s) ? "strategy" : s[..Math.Min(s.Length, 80)];
    }

    private static string QuoteIfNeeded(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
            return "\"\"";

        bool needsQuoting = arg.Any(c => c is ' ' or '\t' or '&' or '|' or '>' or '<' or '^' or '"' or '(' or ')' or '%' or '!');
        if (!needsQuoting)
            return arg;

        var sb = new StringBuilder(arg.Length + 2);
        sb.Append('"');
        foreach (var c in arg)
        {
            if (c == '"')
                sb.Append("\\\"");
            else if (c == '%')
                sb.Append("%%");
            else
                sb.Append(c);
        }
        sb.Append('"');
        return sb.ToString();
    }
}
