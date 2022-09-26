using System.Text;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class PlayableOutputNode : GraphViewNode
    {
        public PlayableOutput PlayableOutput { get; }

        public PlayableOutputNode(PlayableOutput playableOutput)
        {
            PlayableOutput = playableOutput;

            CreatePorts();
            RefreshExpandedState();
            RefreshPorts();
        }


        public override void Update()
        {
            base.Update();

            if (!PlayableOutput.IsOutputValid())
            {
                return;
            }

            // mark all child nodes inactive
            for (int i = 0; i < InternalInputs.Count; i++)
            {
                InternalInputs[i].Node.RemoveFlag(NodeFlag.Active);
            }

            var sourcePlayable = PlayableOutput.GetSourcePlayable();
            if (sourcePlayable.IsValid())
            {
                var sourcePlayableNodeIndex = FindChildPlayableNode(sourcePlayable);
                if (sourcePlayableNodeIndex >= 0)
                {
                    InternalInputs[sourcePlayableNodeIndex].Node.AddFlag(NodeFlag.Active);
                }
                else
                {
                    // create new node
                    var sourcePlayableNode = PlayableNodeFactory.CreateNode(sourcePlayable);
                    sourcePlayableNode.AddToView(Container, this);
                    sourcePlayableNode.AddFlag(NodeFlag.Active);

                    var sourcePlayableOutputPort = sourcePlayableNode.OutputPorts[0];
                    var selfSourcePort = InternalInputPorts[0];
                    var edge = selfSourcePort.ConnectTo(sourcePlayableOutputPort);
                    edge.capabilities &= ~Capabilities.Movable;
                    edge.capabilities &= ~Capabilities.Deletable;
                    edge.capabilities &= ~Capabilities.Selectable;
                    Container.AddElement(edge);
                    InternalInputs.Add(new NodeInput(edge, sourcePlayableNode, 0));

                    AddFlag(NodeFlag.HierarchyDirty);
                }
            }

            // check and update children
            for (int i = InternalInputs.Count - 1; i >= 0; i--)
            {
                var input = InternalInputs[i];
                if (!input.Node.CheckFlag(NodeFlag.Active))
                {
                    Container.RemoveElement(input.Edge);
                    input.Node.RemoveFromView();

                    InternalInputs.RemoveAt(i);

                    AddFlag(NodeFlag.HierarchyDirty);

                    continue;
                }

                InternalInputs[i].Node.Update();
            }
        }


        protected int FindChildPlayableNode(Playable playable)
        {
            for (int i = 0; i < InternalInputs.Count; i++)
            {
                if (((PlayableNode)InternalInputs[i].Node).Playable.Equals(playable))
                {
                    return i;
                }
            }

            return -1;
        }


        private void CreatePorts()
        {
            if (!PlayableOutput.IsOutputValid())
            {
                return;
            }

            var inputPort = InstantiatePort<Playable>(Direction.Input);
            inputPort.portName = "Source";
            inputPort.portColor = Color.white;
            inputContainer.Add(inputPort);
            InternalInputPorts.Add(inputPort);
        }


        #region Description

        protected override void AppendStateDescriptions(StringBuilder descBuilder)
        {
            descBuilder.Append("Type: ").AppendLine(PlayableOutput.GetPlayableOutputType().Name)
                .Append("IsValid: ").AppendLine(PlayableOutput.IsOutputValid().ToString());
            if (PlayableOutput.IsOutputValid())
            {
                descBuilder.Append("Name: ").AppendLine(PlayableOutput.GetEditorName())
                    .Append("Weight: ").AppendLine(PlayableOutput.GetWeight().ToString("F3"))
                    .Append("ReferenceObject: ").AppendLine(PlayableOutput.GetReferenceObject()?.name ?? "Null")
                    .Append("UserData: ").AppendLine(PlayableOutput.GetUserData()?.name ?? "Null")
                    .Append("SourceOutputPort: ").AppendLine(PlayableOutput.GetSourceOutputPort().ToString());
            }
        }

        #endregion
    }
}
