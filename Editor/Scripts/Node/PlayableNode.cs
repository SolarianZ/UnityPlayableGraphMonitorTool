using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class PlayableNode : GraphViewNode
    {
        public Playable Playable { get; }

        public PlayableNode(int depth, Playable playable)
            : base(depth)
        {
            Playable = playable;

            CreatePorts();
            RefreshExpandedState();
            RefreshPorts();
        }

        public override void CreateAndConnectInputNodes()
        {
            if (!Playable.IsValid())
            {
                return;
            }

            var inputPlayableDepth = Depth + 1;
            for (int i = 0; i < Playable.GetInputCount(); i++)
            {
                var inputPlayable = Playable.GetInput(i);
                var inputPlayableTypeName = inputPlayable.GetPlayableType().Name;
                var inputPlayableNode = new PlayableNode(inputPlayableDepth, inputPlayable)
                {
                    title = inputPlayableTypeName
                };
                //inputPlayableNode.SetPosition(new Rect(-400 * inputPlayableDepth, 200, 0, 0));
                inputPlayableNode.AddToContainer(Container);
                Children.Add(inputPlayableNode);

                var inputPlayableNodeOutputPort = inputPlayableNode.OutputPorts[0];
                var selfInputPort = InternalInputPorts[i];
                var edge = selfInputPort.ConnectTo(inputPlayableNodeOutputPort);
                Container.AddElement(edge);
                InternalInputEdges.Add(edge);
            }

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
            if (!Playable.IsValid())
            {
                return;
            }

            for (int i = 0; i < Playable.GetInputCount(); i++)
            {
                var inputPort = InstantiatePort<Playable>(Direction.Input);
                inputPort.portName = $"Input {i}";
                inputPort.portColor = Color.white;
                inputContainer.Add(inputPort);
                InternalInputPorts.Add(inputPort);
            }

            for (int i = 0; i < Playable.GetOutputCount(); i++)
            {
                var outputPort = InstantiatePort<Playable>(Direction.Output);
                outputPort.portName = $"Output {i}";
                outputPort.portColor = Color.white;
                outputContainer.Add(outputPort);
                InternalOutputPorts.Add(outputPort);
            }
        }
    }
}