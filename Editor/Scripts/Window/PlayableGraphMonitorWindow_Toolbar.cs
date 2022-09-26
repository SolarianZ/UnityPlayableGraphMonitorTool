using UnityEditor.UIElements;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace GBG.PlayableGraphMonitor.Editor
{
    public partial class PlayableGraphMonitorWindow
    {
        private Toolbar _toolbar;

        private PopupField<PlayableGraph> _graphPopupField;

        private ToolbarToggle _inspectorToggle;


        private void CreateToolbar()
        {
            _toolbar = new Toolbar();
            rootVisualElement.Add(_toolbar);

            // playable graph popup
            _graphPopupField = new PopupField<PlayableGraph>(_graphs, 0,
                GraphPopupFieldFormatter, GraphPopupFieldFormatter);
            _toolbar.Add(_graphPopupField);
            _toolbar.Add(new ToolbarSpacer());

            // frame all button
            var frameAllButton = new ToolbarButton(OnFrameAllButtonClicked)
            {
                name = "frame-all-button",
                text = "Frame All",
            };
            _toolbar.Add(frameAllButton);
            _toolbar.Add(new ToolbarSpacer());

            // inspector toggle
            _inspectorToggle = new ToolbarToggle()
            {
                name = "inspector-toggle",
                text = "Inspector",
                value = true,
            };
            _inspectorToggle.RegisterValueChangedCallback(ToggleInspector);
            _toolbar.Add(_inspectorToggle);
        }

        private string GraphPopupFieldFormatter(PlayableGraph graph)
        {
            if (graph.IsValid())
            {
                return graph.GetEditorName();
            }

            return "No PlayableGraph";
        }

        private void UpdatePlayableGraphPopupField()
        {
            var index = _graphs.IndexOf(_graphPopupField.value);
            if (index < 1)
            {
                // _graphs[0] is an invalid graph
                index = _graphs.Count - 1;
            }

            _graphPopupField.index = index;
            _graphPopupField.value = _graphs[index];
            _graphPopupField.MarkDirtyRepaint();
        }

        private void OnFrameAllButtonClicked()
        {
            _graphView.FrameAll();
        }

        private void ToggleInspector(ChangeEvent<bool> evt)
        {
            var showInspector = evt.newValue;
            _nodeInspectorPanel.visible = showInspector;
            _nodeInspectorPanel.style.width = showInspector ? _NODE_INSPECTOR_WIDTH : 0;
        }
    }
}
