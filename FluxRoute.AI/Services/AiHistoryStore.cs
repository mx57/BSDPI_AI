using System.Globalization;
using System.Text;
using System.Text.Json;
using FluxRoute.AI.Models;

namespace FluxRoute.AI.Services;

public sealed class AiHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private readonly string _path;
    private readonly object _gate = new();
    private List<ProbeOutcome>? _cache;

    public AiHistoryStore(string path)
    {
        _path = path;
    }

    public void Append(ProbeOutcome outcome)
    {
        var line = JsonSerializer.Serialize(outcome, JsonOptions) + Environment.NewLine;
        lock (_gate)
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(_path, line);

            if (_cache != null)
            {
                _cache.Add(outcome);
            }
        }
    }

    public List<ProbeOutcome> LoadRecent(TimeSpan window)
    {
        var cutoff = DateTimeOffset.UtcNow - window;
        return LoadAll().Where(o => o.Timestamp >= cutoff).ToList();
    }

    public List<ProbeOutcome> LoadFor(Guid genomeId, string networkHash)
    {
        return LoadAll().Where(o => o.GenomeId == genomeId && o.NetworkHash == networkHash).ToList();
    }

    public List<ProbeOutcome> LoadForNetwork(string networkHash)
    {
        return LoadAll().Where(o => o.NetworkHash == networkHash).ToList();
    }

    public List<ProbeOutcome> LoadAll()
    {
        lock (_gate)
        {
            if (_cache != null)
                return [.. _cache];

            if (!File.Exists(_path))
                return [];

            var list = new List<ProbeOutcome>();
            foreach (var raw in File.ReadLines(_path))
            {
                var line = raw.Trim();
                if (line.Length == 0)
                    continue;
                try
                {
                    var o = JsonSerializer.Deserialize<ProbeOutcome>(line, JsonOptions);
                    if (o is not null)
                        list.Add(o);
                }
                catch
                {
                }
            }

            _cache = [.. list];
            return list;
        }
    }

    public void RotateOldEntries(int keepDays)
    {
        if (keepDays <= 0)
            return;

        var cutoff = DateTimeOffset.UtcNow.AddDays(-keepDays);
        List<ProbeOutcome> kept;
        lock (_gate)
        {
            kept = LoadAll().Where(o => o.Timestamp >= cutoff).ToList();
        }

        lock (_gate)
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var fs = File.Create(_path);
            using var sw = new StreamWriter(fs);
            foreach (var o in kept.OrderBy(x => x.Timestamp))
                sw.WriteLine(JsonSerializer.Serialize(o, JsonOptions));

            _cache = [.. kept];
        }
    }

    /// <summary>
    /// Generates a CSV string from the AI history.
    /// BOLT ⚡: Uses InvariantCulture for numbers to prevent CSV corruption in locales where comma is a decimal separator.
    /// BOLT ⚡: Supports optional pre-calculated displayNameMap for significantly faster export of large histories.
    /// </summary>
    public string GetHistoryCsv(Func<Guid, string> getDisplayName, Dictionary<Guid, string>? displayNameMap = null)
    {
        var all = LoadAll().OrderByDescending(x => x.Timestamp).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Strategy,Network,Score,SuccessRate,LatencyMs,Stable,FailedTargets,FailureSig");

        foreach (var o in all)
        {
            string strategy;
            if (displayNameMap != null && displayNameMap.TryGetValue(o.GenomeId, out var mappedName))
                strategy = mappedName;
            else
                strategy = getDisplayName(o.GenomeId);

            var failedTargets = string.Join("|", o.FailedTargetKeys);

            sb.Append(o.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")).Append(',');
            sb.Append(CsvEscape(strategy)).Append(',');
            sb.Append(CsvEscape(o.NetworkHash)).Append(',');
            sb.Append(o.Score).Append(',');
            sb.Append(o.SuccessRate.ToString("F2", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(o.AvgLatencyMs.ToString("F1", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(o.ProcessStable).Append(',');
            sb.Append(CsvEscape(failedTargets)).Append(',');
            sb.Append(CsvEscape(o.FailureSignature ?? ""));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
