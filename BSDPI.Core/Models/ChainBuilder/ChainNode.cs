namespace BSDPI.Core.Models.ChainBuilder;

public sealed class ChainNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ChainNodeType NodeType { get; set; }
    public string Label { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public string? ProgramPath { get; set; }
    public string[]? TargetSites { get; set; }
    public string? ZapretArgs { get; set; }
    public string? ByeDpiArgs { get; set; }
    public int? DelayMs { get; set; }
    public int? WarpPort { get; set; }
    public string? LogMessage { get; set; }
}
