namespace BSDPI.Core.Models.ChainBuilder;

public sealed class ChainConnection
{
    public Guid SourceNodeId { get; set; }
    public string SourcePortId { get; set; } = "";
    public Guid TargetNodeId { get; set; }
    public string TargetPortId { get; set; } = "";
}
