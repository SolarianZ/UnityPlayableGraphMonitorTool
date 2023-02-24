using UnityEditor.UIElements;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace GBG.PlayableGraphMonitor.Editor
{
    public partial class PlayableGraphMonitorWindow
    {
        private Toolbar _toolbar;

        private PopupField<PlayableGraph> _graphPopupField;

        private ToolbarToggle _autoUpdateViewToggle;

        private ToolbarButton _manualUpdateViewButton;

        private ToolbarToggle _inspectorToggle;


        private void CreateToolbar()
        {
            _autoUpdateView = true;
            _toolbar = new Toolbar();
            rootVisualElement.Add(_toolbar);

            // Playable graph popup
            _graphPopupField = new PopupField<PlayableGraph>(_graphs, 0,
                GraphPopupFieldFormatter, GraphPopupFieldFormatter);
            _graphPopupField.RegisterValueChangedCallback(OnSelectedPlayableGraphChanged);
            _toolbar.Add(_graphPopupField);
            _toolbar.Add(new ToolbarSpacer());

            // Inspector toggle
            _inspectorToggle = new ToolbarToggle()
            {
                name = "inspector-toggle",
                text = "Inspector",
                value = true,
            };
            _inspectorToggle.RegisterValueChangedCallback(ToggleInspector);
            _toolbar.Add(_inspectorToggle);

            // Auto update toggle
            _autoUpdateViewToggle = new ToolbarToggle()
            {
                name = "auto-update-toggle",
                text = "Auto Update",
                value = _autoUpdateView,
            };
            _autoUpdateViewToggle.RegisterValueChangedCallback(ToggleAutoUpdate);
            _toolbar.Add(_autoUpdateViewToggle);

            // Manual update button
            _manualUpdateViewButton = new ToolbarButton(OnManualUpdateButtonClicked)
            {
                name = "manual-update-button",
                text = "Update View",
                style = { display = _autoUpdateView ? DisplayStyle.None : DisplayStyle.Flex }
            };
            _toolbar.Add(_manualUpdateViewButton);

            // Frame all button
            _toolbar.Add(new ToolbarSpacer());
            var frameAllButton = new ToolbarButton(OnFrameAllButtonClicked)
            {
                name = "frame-all-button",
                text = "Frame All",
            };
            _toolbar.Add(frameAllButton);
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

        private void OnSelectedPlayableGraphChanged(ChangeEvent<PlayableGraph> evt)
        {
            _updateViewOnce = true;
        }

        private void OnManualUpdateButtonClicked()
        {
            _updateViewOnce = true;
        }

        private void OnFrameAllButtonClicked()
        {
            _graphView.FrameAll();
        }

        private void ToggleAutoUpdate(ChangeEvent<bool> evt)
        {
            _autoUpdateView = evt.newValue;
            _manualUpdateViewButton.style.display = _autoUpdateView ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void ToggleInspector(ChangeEvent<bool> evt)
        {
            var showInspector = evt.newValue;
            _nodeInspectorPanel.style.display = showInspector ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}