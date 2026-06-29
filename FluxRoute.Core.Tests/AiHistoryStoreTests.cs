using System.Text.Json;
using FluxRoute.AI.Models;
using FluxRoute.AI.Services;
using Xunit;

namespace FluxRoute.Core.Tests;

public sealed class AiHistoryStoreTests
{
    [Fact]
    public void Append_DoesNotCorruptCache_IfCacheWasNull()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "fr-hist-" + Guid.NewGuid().ToString("N") + ".jsonl");
        try
        {
            var store = new AiHistoryStore(path);
            var outcome1 = new ProbeOutcome { GenomeId = Guid.NewGuid(), NetworkHash = "net1", Score = 100 };
            var outcome2 = new ProbeOutcome { GenomeId = Guid.NewGuid(), NetworkHash = "net2", Score = 80 };

            // Manually put one outcome into the file
            File.WriteAllText(path, JsonSerializer.Serialize(outcome1) + Environment.NewLine);

            // Act
            // This will trigger the bug: _cache is null, so it will be initialized with ONLY outcome2
            store.Append(outcome2);

            // Assert
            var all = store.LoadAll();

            // Expected: outcome1 and outcome2
            // Buggy actual: only outcome2
            Assert.Equal(2, all.Count);
            Assert.Contains(all, x => x.NetworkHash == "net1");
            Assert.Contains(all, x => x.NetworkHash == "net2");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void LoadAll_HandlesCorruptLines()
    {
        var path = Path.Combine(Path.GetTempPath(), "fr-hist-corrupt-" + Guid.NewGuid().ToString("N") + ".jsonl");
        try
        {
            var outcome1 = new ProbeOutcome { GenomeId = Guid.NewGuid(), NetworkHash = "net1", Score = 100 };
            var validJson = JsonSerializer.Serialize(outcome1);
            File.WriteAllText(path, validJson + "\n{invalid json}\n" + validJson + "\n");

            var store = new AiHistoryStore(path);
            var all = store.LoadAll();

            Assert.Equal(2, all.Count);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void RotateOldEntries_KeepsRecentEntries()
    {
        var path = Path.Combine(Path.GetTempPath(), "fr-hist-rotate-" + Guid.NewGuid().ToString("N") + ".jsonl");
        try
        {
            var store = new AiHistoryStore(path);
            var old = new ProbeOutcome { Timestamp = DateTimeOffset.UtcNow.AddDays(-20), Score = 50 };
            var recent = new ProbeOutcome { Timestamp = DateTimeOffset.UtcNow.AddDays(-1), Score = 90 };

            store.Append(old);
            store.Append(recent);

            store.RotateOldEntries(14);

            var all = store.LoadAll();
            Assert.Single(all);
            Assert.Equal(90, all[0].Score);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
