using System;
using System.Collections;
using System.Collections.Generic;

namespace BnbnavNetClient.Models;

public class CalculatedRoute
{
    public List<Node> Nodes { get; } = new();
    public List<Edge> Edges { get; } = new();

    public void AddRouteSegment(Node node, Edge? edge)
    {
        Nodes.Add(node);
        if (edge is not null)
            Edges.Add(edge);
    }

    public void FinaliseRoute()
    {
        Nodes.Reverse();
        Edges.Reverse();
    }
}

public class RoutingException : Exception
{

}

public class NoSuitableEdgeException : RoutingException
{
    
}

public class DisjointNetworkException : RoutingException
{
    
}