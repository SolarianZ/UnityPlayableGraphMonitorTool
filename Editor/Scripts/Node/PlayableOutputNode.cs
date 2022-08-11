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

            // root node occupies one width unit
            LayoutInfo.TreeWidth += 1;
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
            //sourcePlayableNode.SetPosition(new Rect(-400 * sourcePlayableNodeDepth, 200, 0, 0));
            sourcePlayableNode.AddToContainer(Container);
            Children.Add(sourcePlayableNode);

            var sourcePlayableOutputPortIndex = PlayableOutput.GetSourceOutputPort();
            var sourcePlayableOutputPort = sourcePlayableNode.OutputPorts[sourcePlayableOutputPortIndex];
            var selfSourcePort = InternalInputPorts[0];
            var edge = selfSourcePort.ConnectTo(sourcePlayableOutputPort);
            Container.AddElement(edge);
            InternalInputEdges.Add(edge);

            if (Children.Count > 1)
            {
                LayoutInfo.TreeWidth += Children.Count - 1;
            }

            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].CreateAndConnectInputNodes();
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