using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor.UIElements;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace GBG.PlayableGraphMonitor.Editor
{
    public partial class PlayableGraphMonitorWindow
    {
        private Toolbar _toolbar;

        private PopupField<PlayableGraph> _graphPopupField;

        private Button _showInspectorButton;


        private void CreateToolbar()
        {
            _toolbar = new Toolbar();
            rootVisualElement.Add(_toolbar);

            // playable graph popup
            _graphPopupField = new PopupField<PlayableGraph>(_graphs, 0,
                GraphPopupFieldFormatter, GraphPopupFieldFormatter);
            _toolbar.Add(_graphPopupField);

            // frame all button
            var frameAllButton = new Button(OnFrameAllButtonClicked)
            {
                name = "frame-all-button",
                text = "Frame All",
            };
            _toolbar.Add(frameAllButton);

            // inspector button
            _showInspectorButton = new Button(OnInspectorButtonClicked)
            {
                name = "inspector-button",
                text = "Show Inspector",
                style =
            {
                backgroundColor = GraphTool.GetButtonBackgroundColor(true)
            },
                userData = true
            };
            _toolbar.Add(_showInspectorButton);
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

        private void OnInspectorButtonClicked()
        {
            var showInspector = !(bool)_showInspectorButton.userData;
            _showInspectorButton.userData = showInspector;
            _showInspectorButton.style.backgroundColor = GraphTool.GetButtonBackgroundColor(showInspector);

            _nodeInspectorPanel.visible = showInspector;
            _nodeInspectorPanel.style.width = showInspector ? _nodeInspectorWidth : 0;
        }
    }
}