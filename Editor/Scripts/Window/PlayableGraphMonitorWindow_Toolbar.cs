using System;
using UnityEditor;
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

        // Inspector
        [SerializeField]
        private bool _displayInspector = true;
        private ToolbarToggle _inspectorToggle;

        // Auto layout
        private ToolbarToggle _autoLayoutToggle;
        private TextElement _autoLayoutLabel;

        // Refresh rate
        [SerializeField]
        private RefreshRate _refreshRate = RefreshRate.Fps_20;
        private EnumField _refreshRateField;
        private TextElement _refreshRateLabel;
        private ToolbarButton _manualUpdateViewButton;


        // Common data
        private static Color NotableTextColor => Color.yellow;
        private static Color NormalTextColor => EditorGUIUtility.isProSkin
            ? Color.white
            : Color.black;


        private void CreateToolbar()
        {
            // Toolbar
            _toolbar = new Toolbar();
            rootVisualElement.Add(_toolbar);

            // Playable graph popup
            _graphPopupField = new PopupField<PlayableGraph>(_graphs, 0,
                GraphPopupFieldFormatter, GraphPopupFieldFormatter);
            _graphPopupField.RegisterValueChangedCallback(OnSelectedPlayableGraphChanged);
            _graphPopupField.Q<TextElement>(className: "unity-text-element").style.color = NormalTextColor;
            _toolbar.Add(_graphPopupField);
            _toolbar.Add(new ToolbarSpacer());

            // Inspector toggle
            _inspectorToggle = new ToolbarToggle()
            {
                text = "Inspector",
                tooltip = "Disabling this option can significantly improve performance.",
                value = _displayInspector,
            };
            _inspectorToggle.RegisterValueChangedCallback(ToggleInspector);
            _inspectorToggle.Q<TextElement>(className: "unity-text-element").style.color = NormalTextColor;
            _toolbar.Add(_inspectorToggle);

            // Clip ProgressBar toggle
            var clipProgressBarToggle = new ToolbarToggle()
            {
                text = "Clip Progress",
                tooltip = "Disabling this option can significantly improve performance.",
                value = _viewUpdateContext.ShowClipProgressBar,
            };
            clipProgressBarToggle.RegisterValueChangedCallback(ToggleDisplayClipProgressBar);
            clipProgressBarToggle.Q<TextElement>(className: "unity-text-element").style.color = NormalTextColor;
            _toolbar.Add(clipProgressBarToggle);

            // Auto layout toggle
            _autoLayoutToggle = new ToolbarToggle()
            {
                text = "Auto Layout",
                value = _viewUpdateContext.AutoLayout,
                tooltip = "If you want to drag nodes manually, disable 'Auto Layout' " +
                          $"and set the max refresh rate to '{RefreshRate.Manual}'.",
            };
            _autoLayoutToggle.RegisterValueChangedCallback(ToggleAutoLayout);
            _autoLayoutLabel = _autoLayoutToggle.Q<TextElement>(className: "unity-text-element");
            _autoLayoutLabel.style.color = _viewUpdateContext.AutoLayout ? NormalTextColor : NotableTextColor;
            _toolbar.Add(_autoLayoutToggle);

            // Refresh rate popup
            _updateNodesMovability = _refreshRate == RefreshRate.Manual;
            _refreshRateField = new EnumField(_refreshRate)
            {
                tooltip = "Max refresh rate.",
            };
            _refreshRateField.RegisterValueChangedCallback(OnRefreshRateChanged);
            _refreshRateLabel = _refreshRateField.Q<TextElement>(className: "unity-text-element");
            _refreshRateLabel.style.color = _refreshRate != RefreshRate.Manual ? NormalTextColor : NotableTextColor;
            _toolbar.Add(_refreshRateField);

            // Manual update button
            _manualUpdateViewButton = new ToolbarButton(OnManualUpdateButtonClicked)
            {
                text = "Update View",
                style =
                {
                    display = _refreshRate == RefreshRate.Manual
                        ? DisplayStyle.Flex
                        : DisplayStyle.None
                }
            };
            _toolbar.Add(_manualUpdateViewButton);

            // Frame all button
            _toolbar.Add(new ToolbarSpacer());
            var frameAllButton = new ToolbarButton(OnFrameAllButtonClicked)
            {
                text = "Frame All",
            };
            frameAllButton.Q<TextElement>(className: "unity-text-element").style.color = NormalTextColor;
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
            _refreshRateField.value = _refreshRate;
        }

        private void ToggleInspector(ChangeEvent<bool> evt)
        {
            _displayInspector = evt.newValue;
            _sidebarPanel.style.display = _displayInspector ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ToggleDisplayClipProgressBar(ChangeEvent<bool> evt)
        {
            _viewUpdateContext.ShowClipProgressBar = evt.newValue;
        }

        private void ToggleAutoLayout(ChangeEvent<bool> evt)
        {
            _viewUpdateContext.AutoLayout = evt.newValue;
            _autoLayoutLabel.style.color = _viewUpdateContext.AutoLayout
                ? NormalTextColor
                : NotableTextColor;
        }

        private void OnRefreshRateChanged(ChangeEvent<Enum> evt)
        {
            _refreshRate = (RefreshRate)evt.newValue;
            _nextUpdateViewTimeMS = _nextUpdateViewTimeMS - (long)(RefreshRate)evt.previousValue + (long)_refreshRate;

            var displayStyle = _refreshRate == RefreshRate.Manual ? DisplayStyle.Flex : DisplayStyle.None;
            _manualUpdateViewButton.style.display = displayStyle;

            _refreshRateLabel.style.color = _refreshRate == RefreshRate.Manual
                ? NotableTextColor
                : NormalTextColor;

            _updateNodesMovability = true;
        }

        private void OnManualUpdateButtonClicked()
        {
            _nextUpdateViewTimeMS = GetCurrentEditorTimeMs();
        }

        private void OnFrameAllButtonClicked()
        {
            _graphView.FrameAll();
        }
    }
}