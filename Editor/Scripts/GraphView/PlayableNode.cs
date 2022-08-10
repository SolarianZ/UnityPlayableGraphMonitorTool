using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.GraphView
{
    public class PlayableNode : GraphViewNode
    {
        public Playable Playable { get; }

        public PlayableNode(PlayableGraphView owner, int depth, Playable playable)
            : base(owner, depth)
        {
            Playable = playable;

            CreatePorts();
            RefreshExpandedState();
            RefreshPorts();

            CreateAndConnectInputNodes();
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
                inputPort.portColor = Color.white;
                inputContainer.Add(inputPort);
                InternalInputPorts.Add(inputPort);
            }

            for (int i = 0; i < Playable.GetOutputCount(); i++)
            {
                var outputPort = InstantiatePort<Playable>(Direction.Output);
                outputPort.portColor = Color.white;
                outputContainer.Add(outputPort);
                InternalOutputPorts.Add(outputPort);
            }
        }

        private void CreateAndConnectInputNodes()
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
                var inputPlayableNode = new PlayableNode(Owner, inputPlayableDepth, inputPlayable)
                {
                    title = inputPlayableTypeName
                };
                inputPlayableNode.SetPosition(new Rect(-400 * inputPlayableDepth, 200, 0, 0));
                Owner.AddElement(inputPlayableNode);

                var inputPlayableOutputPortIndex = 0;
                var inputPlayableOutputPort = InternalInputPorts[inputPlayableOutputPortIndex];
                var edge = inputPlayableOutputPort.ConnectTo(inputPlayableNode.OutputPorts[0]);
                Owner.AddElement(edge);
            }
        }
    }
}