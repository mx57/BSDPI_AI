namespace FluxRoute.Core.Models;

public enum DpiEngineType
{
    Zapret,
    ByeDpi,
    GoodbyeDpi,
    Warp,
    SingBox
}

public enum DpiEngineMode
{
    Zapret = 0,
    ByeDpi = 1,
    Warp = 2,
    Hybrid = 3,
    WarpZapret = 4,
    WarpByeDpi = 5,
    SingBox = 6,
    SingBoxZapret = 7,
    AutoDiscovery = 8
}
