using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FluxRoute.AI.Models;

public static class GenomeSignature
{
    public static string Compute(StrategyGenome g)
    {
        var payload = new
        {
            g.EngineType,
            g.FilterTcp,
            g.FilterUdp,
            g.DesyncMode,
            g.SplitPos,
            g.SplitPosSemantic,
            g.DisorderPos,
            g.FakePos,
            g.OobPos,
            g.DisoobPos,
            g.TlsrecPos,
            g.FakeTtl,
            g.AutoTtl,
            g.Md5sig,
            g.FakeTlsMod,
            g.FakeSni,
            g.FakeData,
            g.ModHttp,
            g.Tlsminor,
            g.Hosts,
            g.Hostlist,
            g.RepeatCount,
            g.CacheTtl,
            g.Auto,
            g.Timeout,
            g.AutoMode,
            g.DesyncAnyProtocol,
            g.DesyncFooling,
            g.FakeResend,
            g.WarpConfig,
            g.MTU,
            g.GoolEnabled,
            g.PsiphonEnabled,
            g.PsiphonCountry,
            g.ScanEnabled,
            g.Reserved,
            Extra = string.Join('\u001f', g.ExtraArgs),
        };
        var json = JsonSerializer.Serialize(payload);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexStringLower(hash);
    }
}
