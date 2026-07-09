using FluxRoute.Core.Models;
using FluxRoute.Core.Services;

namespace FluxRoute.Core.Extensions;

public static class DpiModeExtensions
{
    public static string ToRunMode(this DpiEngineMode mode)
    {
        return mode switch
        {
            DpiEngineMode.Zapret => DpiRunMode.Standalone,
            DpiEngineMode.ByeDpi => DpiRunMode.Standalone,
            DpiEngineMode.Warp => DpiRunMode.Warp,
            DpiEngineMode.Hybrid => DpiRunMode.Hybrid,
            DpiEngineMode.WarpZapret => DpiRunMode.WarpZapret,
            DpiEngineMode.WarpByeDpi => DpiRunMode.WarpByeDpi,
            _ => DpiRunMode.Standalone
        };
    }

    public static string ToRunMode(this int modeValue)
    {
        if (Enum.IsDefined(typeof(DpiEngineMode), modeValue))
        {
            return ((DpiEngineMode)modeValue).ToRunMode();
        }
        return DpiRunMode.Standalone;
    }
}
