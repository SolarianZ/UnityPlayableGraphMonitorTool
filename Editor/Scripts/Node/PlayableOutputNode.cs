using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class PlayableOutputNode : GraphViewNode
    {
        public PlayableOutput PlayableOutput { get; }

        public PlayableOutputNode(int depth, PlayableOutput playableOutput)
            : base(depth)
        {
            PlayableOutput = playableOutput;

            CreatePorts();
            RefreshExpandedState();
            RefreshPorts();
        }

        public override void CreateAndConnectInputNodes()
        {
            if (!PlayableOutput.IsOutputValid())
            {
                return;
            }

            var sourcePlayable = PlayableOutput.GetSourcePlayable();
            var sourcePlayableTypeName = sourcePlayable.GetPlayableType().Name;
            var sourcePlayableNodeDepth = Depth + 1;
            var sourcePlayableNode = new PlayableNode(sourcePlayableNodeDepth, sourcePlayable)
            {
                title = sourcePlayableTypeName
            };
            sourcePlayableNode.SetPosition(new Rect(-400 * sourcePlayableNodeDepth, 200, 0, 0));
            sourcePlayableNode.AddToContainer(Container);
            ChildNodes.Add(sourcePlayableNode);

            var sourcePlayableOutputPortIndex = PlayableOutput.GetSourceOutputPort();
            var sourcePlayableOutputPort = sourcePlayableNode.OutputPorts[sourcePlayableOutputPortIndex];
            var selfSourcePort = InternalInputPorts[0];
            var edge = selfSourcePort.ConnectTo(sourcePlayableOutputPort);
            Container.AddElement(edge);
            InternalInputEdges.Add(edge);

            for (int i = 0; i < ChildNodes.Count; i++)
            {
                ChildNodes[i].CreateAndConnectInputNodes();
            }
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