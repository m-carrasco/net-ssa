using QuikGraph;
using QuikGraph.Algorithms.Search;
using NetSsa.Instructions;
using NetSsa.Analyses;
using System.Linq;
using System.Collections.Generic;
using System;

namespace NetSsa.Analyses
{
    // This is a non-optimal implementation for Dominance sets.
    // The intent is just to use it to verify the Datalog-based SSA transformation.
    public class Dominance
    {
        private readonly ControlFlowGraph _cfg;
        private readonly AdjacencyGraph<TacInstruction, Edge<TacInstruction>> _graph;
        private readonly ISet<TacInstruction> _unreachable;
        public Dominance(ControlFlowGraph cfg)
        {
            _cfg = cfg;
            _graph = CreateGraph();
            _unreachable = FindUnreachables();
        }

        private AdjacencyGraph<TacInstruction, Edge<TacInstruction>> CreateGraph()
        {
            AdjacencyGraph<TacInstruction, Edge<TacInstruction>> graph = new AdjacencyGraph<TacInstruction, Edge<TacInstruction>>();
            ISet<TacInstruction> leaders = _cfg.Leaders();

            foreach (TacInstruction l in leaders)
            {
                graph.AddVertex(l);
            }

            foreach (TacInstruction l in leaders)
            {
                AddSuccessors(l, graph);
            }

            return graph;
        }
        private void AddSuccessors(TacInstruction l, AdjacencyGraph<TacInstruction, Edge<TacInstruction>> graph)
        {
            foreach (TacInstruction successor in _cfg.BasicBlockSuccessors(l))
            {
                graph.AddEdge(new Edge<TacInstruction>(l, successor));
            }
        }

        // The set of leaders dominated by 'leader'
        public ISet<TacInstruction> Dom(TacInstruction leader)
        {
            _graph.RemoveVertex(leader);

            ISet<TacInstruction> dom = new SortedSet<TacInstruction>(TacInstruction.LabelComparer);
            foreach (var l in _cfg.Leaders().Except(_unreachable))
            {
                dom.Add(l);
            }

            var dfs = new DepthFirstSearchAlgorithm<TacInstruction, Edge<TacInstruction>>(_graph);
            dfs.DiscoverVertex += i => dom.Remove(i);
            dfs.ProcessAllComponents = false;
            foreach (var e in _cfg.Entries())
            {
                if (_graph.ContainsVertex(e))
                {
                    dfs.SetRootVertex(e);
                    dfs.Compute();
                }
            }

            _graph.AddVertex(leader);
            AddSuccessors(leader, _graph);
            return dom;
        }

        private ISet<TacInstruction> FindUnreachables()
        {
            ISet<TacInstruction> unreachable = new SortedSet<TacInstruction>(TacInstruction.LabelComparer);
            foreach (var l in _cfg.Leaders())
            {
                unreachable.Add(l);
            }

            var dfs = new DepthFirstSearchAlgorithm<TacInstruction, Edge<TacInstruction>>(_graph);

            dfs.DiscoverVertex += i => unreachable.Remove(i);
            dfs.Compute();

            return unreachable;
        }
    }
}
