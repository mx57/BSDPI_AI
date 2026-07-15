using System.Text.Json;
using BSDPI.Core.Models.ChainBuilder;

namespace BSDPI.Core.Services.ChainBuilder;

public sealed class ChainStore
{
    private readonly string _chainsDir;

    public ChainStore(string baseDir)
    {
        _chainsDir = Path.Combine(baseDir, "chains");
        Directory.CreateDirectory(_chainsDir);
    }

    public IReadOnlyList<ChainDefinition> LoadAll()
    {
        var chains = new List<ChainDefinition>();
        foreach (var file in Directory.GetFiles(_chainsDir, "*.chain.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var chain = JsonSerializer.Deserialize<ChainDefinition>(json);
                if (chain is not null)
                    chains.Add(chain);
            }
            catch
            {
            }
        }
        return chains;
    }

    public void Save(ChainDefinition chain)
    {
        var path = Path.Combine(_chainsDir, $"{chain.Id}.chain.json");
        var json = JsonSerializer.Serialize(chain, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public void Delete(Guid chainId)
    {
        var path = Path.Combine(_chainsDir, $"{chainId}.chain.json");
        if (File.Exists(path))
            File.Delete(path);
    }
}
