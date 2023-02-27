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
        // Toolbar
        private Toolbar _toolbar;

        // PlayableGraph popup
        private PopupField<PlayableGraph> _graphPopupField;

        // Refresh rate
        private static readonly RefreshRate _defaultRefreshRate = RefreshRate.Fps_20;
        private EnumField _refreshRateField;
        private TextElement _refreshRateLabel;
        private StyleColor _refreshRateTextNormalColor;
        private ToolbarButton _manualUpdateViewButton;

        // Inspector
        private ToolbarToggle _inspectorToggle;

        // Auto layout
        private ToolbarToggle _autoLayoutToggle;
        private TextElement _autoLayoutLabel;
        private StyleColor _autoLayoutTextNormalColor;

        // Error
        private Label _errorTipLabel;

        // Common data
        private static readonly Color _notableTextColor = Color.yellow;


        private void CreateToolbar()
        {
            // Toolbar
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

            // Clip ProgressBar toggle
            var clipProgressBarToggle = new ToolbarToggle()
            {
                name = "clip-progress-bar-toggle",
                text = "Clip Progress",
                value = _viewUpdateContext.ShowClipProgressBar,
            };
            clipProgressBarToggle.RegisterValueChangedCallback(ToggleDisplayClipProgressBar);
            _toolbar.Add(clipProgressBarToggle);

            // Auto layout toggle
            _autoLayoutToggle = new ToolbarToggle()
            {
                name = "auto-layout-toggle",
                text = "Auto Layout",
                value = true,
                tooltip = $"Disable auto layout and set refresh rate to {RefreshRate.Manual}, " +
                          "then you can drag node layout manually."
            };
            _autoLayoutToggle.RegisterValueChangedCallback(ToggleAutoLayout);
            _autoLayoutLabel = _autoLayoutToggle.Q<TextElement>(className: "unity-text-element");
            _autoLayoutTextNormalColor = _autoLayoutLabel.style.color;
            _toolbar.Add(_autoLayoutToggle);

            // Refresh rate popup
            _refreshRateField = new EnumField(_defaultRefreshRate)
            {
                name = "refresh-rate-popup"
            };
            _refreshRateField.RegisterValueChangedCallback(OnRefreshRateChanged);
            _refreshRateLabel = _refreshRateField.Q<TextElement>(className: "unity-text-element");
            _refreshRateTextNormalColor = _refreshRateLabel.style.color;
            _toolbar.Add(_refreshRateField);

            // Manual update button
            _manualUpdateViewButton = new ToolbarButton(OnManualUpdateButtonClicked)
            {
                name = "manual-update-button",
                text = "Update View",
                style =
                {
                    display = (RefreshRate)_refreshRateField.value == RefreshRate.Manual
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
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

            // Error label
            _toolbar.Add(new ToolbarSpacer());
            _errorTipLabel = new Label
            {
                name = "error-label",
                text = "Please check the console logs.",
                style =
                {
                    color = Color.red,
                    alignSelf = Align.Center,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    display = DisplayStyle.None
                }
            };
            _toolbar.Add(_errorTipLabel);
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
            _refreshRateField.value = _defaultRefreshRate;
        }

        private void OnManualUpdateButtonClicked()
        {
            _nextUpdateViewTimeMS = GetCurrentEditorTimeMs();
        }

        private void OnRefreshRateChanged(ChangeEvent<Enum> evt)
        {
            var refreshRate = (RefreshRate)evt.newValue;
            _nextUpdateViewTimeMS = _nextUpdateViewTimeMS - (long)(RefreshRate)evt.previousValue + (long)refreshRate;

            var displayStyle = refreshRate == RefreshRate.Manual ? DisplayStyle.Flex : DisplayStyle.None;
            _manualUpdateViewButton.style.display = displayStyle;

            _refreshRateLabel.style.color = refreshRate == RefreshRate.Manual
                ? _notableTextColor
                : _refreshRateTextNormalColor;

            _graphView.SetNodesMovability(refreshRate == RefreshRate.Manual);
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

        private void ToggleDisplayClipProgressBar(ChangeEvent<bool> evt)
        {
            _viewUpdateContext.ShowClipProgressBar = evt.newValue;
        }

        private void ToggleAutoLayout(ChangeEvent<bool> evt)
        {
            _autoLayoutLabel.style.color = evt.newValue
                ? _autoLayoutTextNormalColor
                : _notableTextColor;
        }
    }
}