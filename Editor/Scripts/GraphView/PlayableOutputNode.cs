using UnityEditor.Experimental.GraphView;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace GBG.PlayableGraphMonitor.Editor.GraphView
{
    public class PlayableOutputNode : GraphViewNode
    {
        public PlayableOutput PlayableOutput { get; }

        public PlayableOutputNode(PlayableGraphView owner, int depth, PlayableOutput playableOutput)
            : base(owner, depth)
        {
            PlayableOutput = playableOutput;

            CreatePorts();
            RefreshExpandedState();
            RefreshPorts();

            CreateAndConnectInputNodes();
        }


        private void CreatePorts()
        {
            if (!PlayableOutput.IsOutputValid())
            {
                return;
            }

            var inputPort = InstantiatePort<Playable>(Direction.Input);
            inputPort.portColor = Color.white;
            inputContainer.Add(inputPort);
            InternalInputPorts.Add(inputPort);
        }

        private void CreateAndConnectInputNodes()
        {
            if (!PlayableOutput.IsOutputValid())
            {
                return;
            }

            var inputPlayable = PlayableOutput.GetSourcePlayable();
            var inputPlayableDepth = Depth + 1;
            var inputPlayableTypeName = inputPlayable.GetPlayableType().Name;
            var inputPlayableNode = new PlayableNode(Owner, inputPlayableDepth, inputPlayable)
            {
                title = inputPlayableTypeName
            };
            inputPlayableNode.SetPosition(new Rect(-400 * inputPlayableDepth, 200, 0, 0));
            Owner.AddElement(inputPlayableNode);

            var inputPlayableOutputPortIndex = PlayableOutput.GetSourceOutputPort();
            var inputPlayableOutputPort = InternalInputPorts[inputPlayableOutputPortIndex];
            var edge = inputPlayableOutputPort.ConnectTo(inputPlayableNode.OutputPorts[0]);
            Owner.AddElement(edge);
        }
    }
}