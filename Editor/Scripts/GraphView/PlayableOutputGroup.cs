using System;
using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Node;
using UnityEngine;
using UnityEngine.Playables;


namespace GBG.PlayableGraphMonitor.Editor.GraphView
{
    public class PlayableOutputGroup
    {
        public PlayableNode SourceNode { get; set; }
        public List<PlayableOutputNode> OutputNodes { get; } = new List<PlayableOutputNode>();
        public List<PlayableOutputGroup> ChildOutputGroups { get; } = new List<PlayableOutputGroup>();


        public float GetOutputsVerticalSize(IReadOnlyDictionary<PlayableHandle, PlayableOutputGroup> outputGroups)
        {
            var verticalSize = 0f;
            foreach (var outputNode in OutputNodes)
            {
                verticalSize += outputNode.GetNodeSize().y + GraphViewNode.VERTICAL_SPACE;
            }

            foreach (var childOutputGroup in ChildOutputGroups)
            {
                var childGroup = outputGroups[childOutputGroup.SourceNode.Playable.GetHandle()];
                verticalSize += childGroup.GetOutputsVerticalSize(outputGroups);
            }

            return verticalSize;
        }

        public void LayoutOutputNodes(Vector2 outputAnchor, out float bottom)
        {
            var outputCount = OutputNodes.Count;
            var mid = outputCount / 2;
            var top = outputAnchor.y;

            // Direct outputs
            if ((outputCount & 1) == 1) // Odd
            {
                OutputNodes[mid].SetPosition(new Rect(outputAnchor, Vector2.zero));
                bottom = outputAnchor.y + OutputNodes[mid].GetNodeSize().y;

                for (int i = 1; i <= mid; i++)
                {
                    // Upward
                    var outputNodeA = OutputNodes[mid - i];
                    var outputPositionA = new Vector2(
                        outputAnchor.x,
                        outputAnchor.y - (GraphViewNode.VERTICAL_SPACE + outputNodeA.GetNodeSize().y) * i
                    );
                    outputNodeA.SetPosition(new Rect(outputPositionA, Vector2.zero));
                    top = outputPositionA.y;

                    // Downward
                    var outputNodeB = OutputNodes[mid + i];
                    var outputPositionB = new Vector2(
                        outputAnchor.x,
                        outputAnchor.y + (GraphViewNode.VERTICAL_SPACE + outputNodeB.GetNodeSize().y) * i
                    );
                    outputNodeB.SetPosition(new Rect(outputPositionB, Vector2.zero));
                    bottom = outputPositionB.y + outputNodeB.GetNodeSize().y;
                }
            }
            else // Even
            {
                bottom = outputAnchor.y;

                for (int i = 0; i < mid; i++)
                {
                    // Upward
                    var outputNodeA = OutputNodes[mid - i - 1];
                    var outputPositionA = new Vector2(
                        outputAnchor.x,
                        outputAnchor.y - GraphViewNode.VERTICAL_SPACE * (i + 1.5f) - outputNodeA.GetNodeSize().y * i
                    );
                    outputNodeA.SetPosition(new Rect(outputPositionA, Vector2.zero));
                    top = outputPositionA.y;

                    // Downward
                    var outputNodeB = OutputNodes[mid + i];
                    var outputPositionB = new Vector2(
                        outputAnchor.x,
                        outputAnchor.y + GraphViewNode.VERTICAL_SPACE * (i + 1.5f) + outputNodeB.GetNodeSize().y * i
                    );
                    outputNodeB.SetPosition(new Rect(outputPositionB, Vector2.zero));
                    bottom = outputPositionB.y + outputNodeB.GetNodeSize().y;
                }
            }

            // Child outputs
            var lastUpperChildIndex = SortAndFindLastUpperChildSourcePlayableNodeIndex();
            // Upper children
            for (int i = lastUpperChildIndex; i >= 0; i--)
            {
                var childOutputGroup = ChildOutputGroups[i];
                for (int j = childOutputGroup.OutputNodes.Count - 1; j >= 0; j--)
                {
                    var outputNode = childOutputGroup.OutputNodes[j];
                    var outputPosition = new Vector2(
                        outputAnchor.x,
                        top - GraphViewNode.VERTICAL_SPACE - outputNode.GetNodeSize().y
                    );
                    outputNode.SetPosition(new Rect(outputPosition, Vector2.zero));
                    top = outputPosition.y;
                }
            }

            // Lower children
            for (int i = lastUpperChildIndex + 1; i < ChildOutputGroups.Count; i++)
            {
                var childOutputGroup = ChildOutputGroups[i];
                for (int j = 0; j < childOutputGroup.OutputNodes.Count; j++)
                {
                    var outputNode = childOutputGroup.OutputNodes[j];
                    var outputPosition = new Vector2(
                        outputAnchor.x,
                        bottom + GraphViewNode.VERTICAL_SPACE + outputNode.GetNodeSize().y
                    );
                    outputNode.SetPosition(new Rect(outputPosition, Vector2.zero));
                    bottom = outputPosition.y;
                }
            }
        }

        private int SortAndFindLastUpperChildSourcePlayableNodeIndex()
        {
            if (ChildOutputGroups.Count == 0)
            {
                return -1;
            }

            ChildOutputGroups.Sort(SortOutputGroupBySourceNodePositionAsc);

            if (ChildOutputGroups[0].SourceNode.Position.y >= SourceNode.Position.y)
            {
                return -1;
            }

            if (ChildOutputGroups[ChildOutputGroups.Count - 1].SourceNode.Position.y <= SourceNode.Position.y)
            {
                return ChildOutputGroups.Count - 1;
            }

            for (int i = 0; i < ChildOutputGroups.Count - 1; i++)
            {
                if (ChildOutputGroups[i].SourceNode.Position.y <= SourceNode.Position.y &&
                    ChildOutputGroups[i + 1].SourceNode.Position.y >= SourceNode.Position.y)
                {
                    return i;
                }
            }

            throw new Exception("This should not happen!");
        }

        private static int SortOutputGroupBySourceNodePositionAsc(PlayableOutputGroup a, PlayableOutputGroup b)
        {
            if (a.SourceNode.Position.y < b.SourceNode.Position.y) return -1;
            if (a.SourceNode.Position.y > b.SourceNode.Position.y) return 1;
            return 0;
        }


        public void Clear()
        {
            SourceNode = null;
            OutputNodes.Clear();
            ChildOutputGroups.Clear();
        }
    }
}