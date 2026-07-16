using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Colors = System.Windows.Media.Colors;
using BSDPI.Core.Models.ChainBuilder;

namespace BSDPI.Controls;

public static class NodeAppearance
{
    public static string GetNodeTypeName(ChainNodeType type) => type switch
    {
        ChainNodeType.Program => "Программа",
        ChainNodeType.Probe => "Проверка",
        ChainNodeType.Zapret => "Zapret",
        ChainNodeType.ByeDpi => "ByeDPI",
        ChainNodeType.Warp => "WARP",
        ChainNodeType.Delay => "Задержка",
        ChainNodeType.Log => "Лог",
        ChainNodeType.Internet => "Интернет",
        _ => type.ToString()
    };

    public static Brush GetNodeColor(ChainNodeType type) => type switch
    {
        ChainNodeType.Program => new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50)),
        ChainNodeType.Probe => new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF)),
        ChainNodeType.Zapret => new SolidColorBrush(Color.FromRgb(0xF7, 0x8C, 0x6C)),
        ChainNodeType.ByeDpi => new SolidColorBrush(Color.FromRgb(0xBC, 0x8C, 0xFF)),
        ChainNodeType.Warp => new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)),
        ChainNodeType.Delay => new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E)),
        ChainNodeType.Log => new SolidColorBrush(Color.FromRgb(0x79, 0xC0, 0xFF)),
        ChainNodeType.Internet => new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50)),
        _ => new SolidColorBrush(Colors.Gray)
    };

    public static string GetNodeIcon(ChainNodeType type) => type switch
    {
        ChainNodeType.Program => "\u25B6",
        ChainNodeType.Probe => "\u2714",
        ChainNodeType.Zapret => "\u26A1",
        ChainNodeType.ByeDpi => "\u26A1",
        ChainNodeType.Warp => "\u2601",
        ChainNodeType.Delay => "\u23F3",
        ChainNodeType.Log => "\u2699",
        ChainNodeType.Internet => "\u2B07",
        _ => "\u25CF"
    };
}
