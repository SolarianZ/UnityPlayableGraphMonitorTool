using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace GBG.PlayableGraphMonitor.Editor
{
    public enum RefreshRate
    {
        [InspectorName("Max FPS")]
        Fps_Max = 0,

        [InspectorName("50 FPS")]
        Fps_50 = 20,

        [InspectorName("20 FPS")]
        Fps_20 = 50,

        [InspectorName("10 FPS")]
        Fps_10 = 100,

        [InspectorName("1 FPS")]
        Fps_1 = 1000,

        [InspectorName("Manual")]
        Manual = int.MaxValue
    }

    public partial class PlayableGraphMonitorWindow
    {
        private Toolbar _toolbar;

        private PopupField<PlayableGraph> _graphPopupField;

        private EnumField _refreshRateField;

        private ToolbarButton _manualUpdateViewButton;

        private ToolbarToggle _inspectorToggle;


        private void CreateToolbar()
        {
            _refreshRate = RefreshRate.Fps_20;
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

            // Refresh rate popup
            _refreshRateField = new EnumField(_refreshRate)
            {
                name = "refresh-rate-popup"
            };
            _refreshRateField.RegisterValueChangedCallback(OnRefreshRateChanged);
            _toolbar.Add(_refreshRateField);

            // Manual update button
            _manualUpdateViewButton = new ToolbarButton(OnManualUpdateButtonClicked)
            {
                name = "manual-update-button",
                text = "Update View",
                style =
                {
                    display = _refreshRate == RefreshRate.Manual ? DisplayStyle.Flex : DisplayStyle.None
                }
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
            _nextUpdateViewTimeMS = GetCurrentEditorTimeMs();
        }

        private void OnManualUpdateButtonClicked()
        {
            _nextUpdateViewTimeMS = GetCurrentEditorTimeMs();
        }

        private void OnRefreshRateChanged(ChangeEvent<Enum> evt)
        {
            _refreshRate = (RefreshRate)evt.newValue;
            _nextUpdateViewTimeMS = _nextUpdateViewTimeMS - (long)(RefreshRate)evt.previousValue + (long)_refreshRate;

            var displayStyle = _refreshRate == RefreshRate.Manual ? DisplayStyle.Flex : DisplayStyle.None;
            _manualUpdateViewButton.style.display = displayStyle;
        }

        private void OnFrameAllButtonClicked()
        {
            _graphView.FrameAll();
        }

        private void ToggleInspector(ChangeEvent<bool> evt)
        {
            var showInspector = evt.newValue;
            _nodeInspectorPanel.style.display = showInspector ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}