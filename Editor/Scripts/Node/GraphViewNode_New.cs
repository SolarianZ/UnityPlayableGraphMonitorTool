using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using UEdge = UnityEditor.Experimental.GraphView.Edge;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;
using UNode = UnityEditor.Experimental.GraphView.Node;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public abstract class GraphViewNode_New : UNode
    {
        protected List<Port> InputPorts { get; } = new List<Port>();

        protected List<Port> OutputPorts { get; } = new List<Port>();


        protected GraphViewNode_New()
        {
            capabilities &= ~Capabilities.Movable;
            capabilities &= ~Capabilities.Deletable;

            // Hide collapse button
            titleButtonContainer.Clear();
            var titleLabel = titleContainer.Q<Label>(name: "title-label");
            titleLabel.style.marginRight = 6;
        }

        public Port GetInputPort(int index)
        {
            return InputPorts[index];
        }

        public Port GetOutputPort(int index)
        {
            return OutputPorts[index];
        }

        public IEnumerable<Playable> GetInputPlayables()
        {
            Playable playable = default;
            if (!playable.IsValid())
            {
                yield break;
            }

            for (int i = 0; i < playable.GetInputCount(); i++)
            {
                var inputPlayable = playable.GetInput(i);
                yield return inputPlayable;
            }
        }

        public IEnumerable<Playable> GetOutputPlayables()
        {
            Playable playable = default;
            if (!playable.IsValid())
            {
                yield break;
            }

            for (int i = 0; i < playable.GetOutputCount(); i++)
            {
                var outputPlayable = playable.GetOutput(i);
                yield return outputPlayable;
            }
        }

        public void Clean()
        {
            // Disconnect all ports
            // Release referenced assets
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // disable contextual menu
        }
    }
}