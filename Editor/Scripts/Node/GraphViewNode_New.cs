using System.Collections.Generic;
using System.Text;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using UNode = UnityEditor.Experimental.GraphView.Node;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public abstract class GraphViewNode_New : UNode
    {
        protected GraphViewNode_New()
        {
            capabilities &= ~Capabilities.Movable;
            capabilities &= ~Capabilities.Deletable;

            // Hide collapse button
            titleButtonContainer.Clear();
            var titleLabel = titleContainer.Q<Label>(name: "title-label");
            titleLabel.style.marginRight = 6;
        }

        // todo: Release referenced assets
        public virtual void Clean()
        {
            Description = null;

            // Disconnect all ports
            for (int i = 0; i < InputPorts.Count; i++)
            {
                InputPorts[i].DisconnectAll();
            }

            for (int i = 0; i < OutputPorts.Count; i++)
            {
                OutputPorts[i].DisconnectAll();
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // Disable contextual menu
        }


        #region Playable

        public abstract IEnumerable<Playable> GetInputPlayables();

        public abstract IEnumerable<Playable> GetOutputPlayables();

        #endregion


        #region Description

        public string Description { get; protected set; }

        private StringBuilder _descBuilder;


        public string GetStateDescription()
        {
            if (_descBuilder == null)
            {
                _descBuilder = new StringBuilder();
            }

            _descBuilder.Clear();
            AppendStateDescriptions(_descBuilder);

            return _descBuilder.ToString();
        }


        protected abstract void AppendStateDescriptions(StringBuilder descBuilder);

        #endregion


        #region Port

        protected List<Port> InputPorts { get; } = new List<Port>();

        protected List<Port> OutputPorts { get; } = new List<Port>();


        public Port GetInputPort(int index)
        {
            return InputPorts[index];
        }

        public Port GetOutputPort(int index)
        {
            return OutputPorts[index];
        }


        protected Port InstantiatePort<TPortData>(Direction direction)
        {
            return InstantiatePort(Orientation.Horizontal, direction, Port.Capacity.Multi, typeof(TPortData));
        }

        #endregion
    }
}