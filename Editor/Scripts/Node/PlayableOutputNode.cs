using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor.Experimental.GraphView;
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
                    sourcePlayableNode.AddToContainer(Container);
                    sourcePlayableNode.AddFlag(NodeFlag.Active);

                    var sourcePlayableOutputPort = sourcePlayableNode.OutputPorts[0];
                    var selfSourcePort = InternalInputPorts[0];
                    var edge = selfSourcePort.ConnectTo(sourcePlayableOutputPort);
                    Container.AddElement(edge);
                    InternalInputs.Add(new NodeInput(edge, sourcePlayableNode, 0));
                }
            }

            for (int i = InternalInputs.Count - 1; i >= 0; i--)
            {
                var input = InternalInputs[i];
                if (!input.Node.CheckFlag(NodeFlag.Active))
                {
                    Container.RemoveElement(input.Edge);
                    input.Node.RemoveFromContainer();

                    InternalInputs.RemoveAt(i);
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
    }
}