using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BSDPI.Controls;
using BSDPI.Core.Models.ChainBuilder;
using BSDPI.Core.Services.ChainBuilder;

namespace BSDPI.ViewModels;

public partial class ChainBuilderViewModel : ObservableObject
{
    private readonly ChainStore _store;
    private ChainDefinition? _activeChain;
    private NodeCanvas? _canvas;

    public ObservableCollection<ChainNode> Nodes { get; } = [];
    public ObservableCollection<ChainConnection> Connections { get; } = [];
    public ObservableCollection<ChainDefinition> ChainList { get; } = [];

    [ObservableProperty] private string _activeChainName = "";
    [ObservableProperty] private int _nodeCount;
    [ObservableProperty] private int _connectionCount;
    [ObservableProperty] private ChainNode? _selectedNode;

    public ChainBuilderViewModel(ChainStore store)
    {
        _store = store;
        RefreshChainList();
    }

    public void SetCanvas(NodeCanvas canvas) => _canvas = canvas;

    public void RefreshChainList()
    {
        ChainList.Clear();
        foreach (var chain in _store.LoadAll().OrderBy(c => c.Name))
            ChainList.Add(chain);
    }

    [RelayCommand]
    private void NewChain()
    {
        _activeChain = new ChainDefinition { Name = $"Цепочка {ChainList.Count + 1}" };
        _store.Save(_activeChain);
        ChainList.Add(_activeChain);
        LoadChain(_activeChain);
    }

    public void LoadChain(ChainDefinition chain)
    {
        _activeChain = chain;
        ActiveChainName = chain.Name;
        Nodes.Clear();
        Connections.Clear();
        _canvas?.ClearAll();

        foreach (var node in chain.Nodes)
        {
            Nodes.Add(node);
            _canvas?.AddNode(node);
        }
        foreach (var conn in chain.Connections)
        {
            Connections.Add(conn);
            var source = Nodes.FirstOrDefault(n => n.Id == conn.SourceNodeId);
            var target = Nodes.FirstOrDefault(n => n.Id == conn.TargetNodeId);
            if (source is not null && target is not null)
                _canvas?.AddConnection(source, conn.SourcePortId, target, conn.TargetPortId);
        }
        NodeCount = Nodes.Count;
        ConnectionCount = Connections.Count;
    }

    [RelayCommand]
    private void AddNode(string type)
    {
        if (_activeChain is null)
            NewChain();

        if (!Enum.TryParse<ChainNodeType>(type, true, out var nodeType)) return;

        var chain = _activeChain!;

        var node = new ChainNode
        {
            NodeType = nodeType,
            Label = NodeAppearance.GetNodeTypeName(nodeType),
            X = 200 + Random.Shared.Next(0, 200),
            Y = 100 + Random.Shared.Next(0, 300)
        };

        switch (nodeType)
        {
            case ChainNodeType.Delay:
                node.DelayMs = 1000;
                node.Label = "Задержка 1с";
                break;
            case ChainNodeType.Warp:
                node.WarpPort = 40000;
                node.Label = "WARP :40000";
                break;
        }

        chain.Nodes.Add(node);
        _store.Save(chain);
        Nodes.Add(node);
        _canvas?.AddNode(node);
        NodeCount = Nodes.Count;
    }

    public void ConnectNodes(ChainNode source, string sourcePort, ChainNode target, string targetPort)
    {
        if (_activeChain is null) return;

        var exists = Connections.Any(c =>
            c.SourceNodeId == source.Id && c.SourcePortId == sourcePort &&
            c.TargetNodeId == target.Id && c.TargetPortId == targetPort);
        if (exists) return;

        var conn = new ChainConnection
        {
            SourceNodeId = source.Id,
            SourcePortId = sourcePort,
            TargetNodeId = target.Id,
            TargetPortId = targetPort
        };

        _activeChain.Connections.Add(conn);
        _store.Save(_activeChain);
        Connections.Add(conn);
        _canvas?.AddConnection(source, sourcePort, target, targetPort);
        ConnectionCount = Connections.Count;
    }

    [RelayCommand]
    private void DeleteSelectedNode(ChainNode? node)
    {
        if (_activeChain is null || node is null) return;

        _activeChain.Nodes.Remove(node);
        var removedConns = _activeChain.Connections
            .Where(c => c.SourceNodeId == node.Id || c.TargetNodeId == node.Id)
            .ToList();
        foreach (var c in removedConns) _activeChain.Connections.Remove(c);

        _store.Save(_activeChain);
        _canvas?.RemoveNode(node.Id);
        Nodes.Remove(node);
        Connections.Clear();
        foreach (var c in _activeChain.Connections) Connections.Add(c);
        NodeCount = Nodes.Count;
        ConnectionCount = Connections.Count;
        SelectedNode = null;
    }

    [RelayCommand]
    private void DeleteChain(ChainDefinition? chain)
    {
        if (chain is null) return;
        _store.Delete(chain.Id);
        ChainList.Remove(chain);
        if (_activeChain?.Id == chain.Id)
        {
            _activeChain = null;
            Nodes.Clear();
            Connections.Clear();
            _canvas?.ClearAll();
            ActiveChainName = "";
            NodeCount = 0;
            ConnectionCount = 0;
        }
    }

    public void RenameActiveChain(string name)
    {
        if (_activeChain is null) return;
        _activeChain.Name = name;
        ActiveChainName = name;
        _store.Save(_activeChain);
    }
}
