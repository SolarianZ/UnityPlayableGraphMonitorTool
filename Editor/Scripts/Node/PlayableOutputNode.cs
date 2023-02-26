using System.Text;
using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class PlayableOutputNode : GraphViewNode
    {
        public PlayableOutput PlayableOutput { get; private set; }

        private int _outputIndex = -1;


        public PlayableOutputNode()
        {
            CreatePorts();
            // RefreshExpandedState(); // Expensive
            RefreshPorts();
        }

        public void Update(PlayableOutput playableOutput, int outputIndex)
        {
            var playableOutputChanged = false;
            if (PlayableOutput.GetHandle() != playableOutput.GetHandle() || _outputIndex != outputIndex)
            {
                PlayableOutput = playableOutput;
                _outputIndex = outputIndex;
                playableOutputChanged = true;

                // Expensive operations
                var playableOutputTypeName = playableOutput.GetPlayableOutputType().Name;
                var playableOutputEditorName = playableOutput.GetEditorName();
                title = $"#{_outputIndex} [{playableOutputEditorName}]\n{playableOutputTypeName}";

                this.SetNodeStyle(playableOutput.GetPlayableOutputNodeColor());
            }

            OnUpdate(playableOutputChanged);
        }

        protected virtual void OnUpdate(bool playableOutputChanged)
        {
        }


        #region Layout

        public override void CalculateLayout(Vector2 origin, out Vector2 hierarchySize)
        {
            hierarchySize = GetHierarchySize();
            var nodePos = CalculateSubTreeRootNodePosition(hierarchySize, origin);
            SetPosition(new Rect(nodePos, Vector2.zero));

            origin.x -= GetNodeSize().x - HORIZONTAL_SPACE;
            for (int i = 0; i < InputPorts.Count; i++)
            {
                var childNode = GetFirstConnectedInputNode(InputPorts[i]);
                if (childNode == null || childNode.LayoutCalculated)
                {
                    continue;
                }

                childNode.CalculateLayout(origin, out var childHierarchySize);
                origin.y += childHierarchySize.y;
            }

            LayoutCalculated = true;
        }

        public override Vector2 GetHierarchySize()
        {
            if (CachedHierarchySize != null)
            {
                return CachedHierarchySize.Value;
            }

            if (InputPorts.Count == 0)
            {
                CachedHierarchySize = GetNodeSize();
                return CachedHierarchySize.Value;
            }

            var subHierarchySize = Vector2.zero;
            for (int i = 0; i < InputPorts.Count; i++)
            {
                var childNode = GetFirstConnectedInputNode(InputPorts[i]);
                Vector2 childSize;
                if (childNode == null)
                {
                    childSize = Vector2.zero;
                }
                else if (childNode.LayoutCalculated)
                {
                    childSize = GetNodeSize();
                }
                else
                {
                    childSize = childNode.GetHierarchySize();
                }

                subHierarchySize.x = Mathf.Max(subHierarchySize.x, childSize.x);
                subHierarchySize.y += childSize.y;
            }

            subHierarchySize.y += (InputPorts.Count - 1) * VERTICAL_SPACE;

            var hierarchySize = GetNodeSize() + new Vector2(HORIZONTAL_SPACE, 0);
            hierarchySize.y = Mathf.Max(hierarchySize.y, subHierarchySize.y);
            CachedHierarchySize = hierarchySize;

            return CachedHierarchySize.Value;
        }

        #endregion


        #region Description

        // For debugging
        public override string ToString()
        {
            if (PlayableOutput.IsOutputValid())
            {
                return PlayableOutput.GetPlayableOutputType().Name;
            }

            return GetType().Name;
        }

        protected override void AppendNodeDescription(StringBuilder descBuilder)
        {
            if (!PlayableOutput.IsOutputValid())
            {
                descBuilder.AppendLine("Invalid PlayableOutput");
                return;
            }

            descBuilder.Append("#").Append(_outputIndex.ToString())
                .Append(" Type: ").AppendLine(PlayableOutput.GetPlayableOutputType().Name)
                .AppendLine(LINE)
                .Append("Name: ").AppendLine(PlayableOutput.GetEditorName())
                .AppendLine(LINE)
                .AppendLine("IsValid: True")
                .Append("IsNull: ").AppendLine(PlayableOutput.IsOutputNull().ToString())
                .Append("ReferenceObject: ").AppendLine(PlayableOutput.GetReferenceObject()?.name ?? "Null")
                .Append("UserData: ").AppendLine(PlayableOutput.GetUserData()?.name ?? "Null")
                .AppendLine(LINE)
                .AppendLine("Source Input:")
                .Append("    SourceOutputPort: ").AppendLine(PlayableOutput.GetSourceOutputPort().ToString())
                .Append("    Weight: ").AppendLine(PlayableOutput.GetWeight().ToString("F3"));
        }

        #endregion


        #region Port

        public Port InputPort { get; private set; }


        private void CreatePorts()
        {
            InputPort = InstantiatePort<Playable>(Direction.Input);
            InputPort.portName = "Source";
            InputPort.portColor = Color.white;

            inputContainer.Add(InputPort);
            InputPorts.Add(InputPort);
        }

        #endregion
    }
}