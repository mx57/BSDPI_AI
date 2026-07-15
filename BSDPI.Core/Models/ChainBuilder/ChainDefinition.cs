namespace BSDPI.Core.Models.ChainBuilder;

public sealed class ChainDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public List<ChainNode> Nodes { get; set; } = [];
    public List<ChainConnection> Connections { get; set; } = [];
}
