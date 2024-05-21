using GBG.PlayableGraphMonitor.Editor.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Playables;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using UNode = UnityEditor.Experimental.GraphView.Node;


namespace GBG.PlayableGraphMonitor.Editor
{
    public enum RefreshRate
    {
        [InspectorName("Max FPS")]
        Fps_Max = 0,

        [InspectorName("30 FPS")]
        Fps_50 = 33,

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

        // Node selection
        private ToolbarMenu _selectOutputNodeMenu;
        private ToolbarMenu _selectRootNodeMenu;

        // Common data
        private static Color NotableTextColor => Color.red;
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
            var clipProgressDropdownToggle = new ToolbarDropdownToggle()
            {
                text = "Clip Progress",
                tooltip = "Disabling this option can significantly improve performance.",
                value = _viewUpdateContext.ShowClipProgressBar,
            };
            clipProgressDropdownToggle.RegisterValueChangedCallback(ToggleDisplayClipProgressBar);
            clipProgressDropdownToggle.Q<TextElement>(className: "unity-text-element").style.color = NormalTextColor;
            clipProgressDropdownToggle.menu.AppendAction("Progress Text (Low-Performance)",
                ToggleDisplayClipProgressBarText,
                (_) =>
                {
                    var @checked = _viewUpdateContext.ShowClipProgressBarTitle
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal;
                    var disabled = _viewUpdateContext.ShowClipProgressBar
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled;
                    return @checked | disabled;
                });
            _toolbar.Add(clipProgressDropdownToggle);

            // Update edge toggle
            var updateEdgeToggle = new ToolbarToggle()
            {
                text = "Always Update Edges",
                tooltip = "Keep updating edges when mouse leave GraphView (will degrade performance).",
                value = _viewUpdateContext.KeepUpdatingEdges,
            };
            updateEdgeToggle.RegisterValueChangedCallback(ToggleKeepUpdatingEdges);
            updateEdgeToggle.Q<TextElement>(className: "unity-text-element").style.color = NormalTextColor;
            _toolbar.Add(updateEdgeToggle);

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
            _updateNodesMovability = true;
            _toolbar.Add(_autoLayoutToggle);

            // Refresh rate popup
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
                style = { flexShrink = 0 },
            };
            frameAllButton.Q<TextElement>(className: "unity-text-element").style.color = NormalTextColor;
            _toolbar.Add(frameAllButton);

            // Select output node
            _toolbar.Add(new ToolbarSpacer());
            _selectOutputNodeMenu = new ToolbarMenu
            {
                text = "Select Output Node",
                style = { flexShrink = 0 },
            };
            _selectOutputNodeMenu.RegisterCallback<PointerEnterEvent>(OnHoverSelectOutputNodeMenu);
            _toolbar.Add(_selectOutputNodeMenu);

            // Select root node
            _selectRootNodeMenu = new ToolbarMenu
            {
                text = "Select Root Node",
                style = { flexShrink = 0 },
            };
            _selectRootNodeMenu.RegisterCallback<PointerEnterEvent>(OnHoverSelectRootNodeMenu);
            _toolbar.Add(_selectRootNodeMenu);
        }

        private string GraphPopupFieldFormatter(PlayableGraph graph)
        {
            if (graph.IsValid())
            {
                // Popup cannot display empty names and items with the same name properly, add HashCode to display them
                return $"{graph.GetEditorName()} <{graph.GetHashCode()}>";
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

        private void ToggleDisplayClipProgressBarText(DropdownMenuAction _)
        {
            _viewUpdateContext.ShowClipProgressBarTitle = !_viewUpdateContext.ShowClipProgressBarTitle;
        }

        private void ToggleKeepUpdatingEdges(ChangeEvent<bool> evt)
        {
            _viewUpdateContext.KeepUpdatingEdges = evt.newValue;
        }

        private void ToggleAutoLayout(ChangeEvent<bool> evt)
        {
            _viewUpdateContext.AutoLayout = evt.newValue;
            _autoLayoutLabel.style.color = _viewUpdateContext.AutoLayout
                ? NormalTextColor
                : NotableTextColor;

            _updateNodesMovability = true;
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
        }

        private void OnManualUpdateButtonClicked()
        {
            _nextUpdateViewTimeMS = GetCurrentEditorTimeMs();
        }

        private void OnFrameAllButtonClicked()
        {
            _graphView.FrameAll();
        }

        private void OnHoverSelectOutputNodeMenu(PointerEnterEvent evt)
        {
            _selectOutputNodeMenu.menu.MenuItems().Clear();

            var playableGraph = _graphPopupField.value;
            if (!playableGraph.IsValid())
            {
                _selectOutputNodeMenu.menu.AppendAction("No PlayableOuput node", null,
                    DropdownMenuAction.Status.Disabled);
                return;
            }

            var nodeList = new List<UNode>();
            _graphView.nodes.ToList(nodeList);
            // _graphView.nodes.ToList() method returns nodes in random order,
            // ensure that the menu items are ordered
            var outputCount = playableGraph.GetOutputCount();
            for (int i = 0; i < outputCount; i++)
            {
                var playableOutput = playableGraph.GetOutput(i);
                var outputNode = nodeList.First(node =>
                {
                    if (node is PlayableOutputNode oNode)
                    {
                        return oNode.PlayableOutput.GetHandle() == playableOutput.GetHandle();
                    }

                    return false;
                }) as PlayableOutputNode;

                if (!outputNode.PlayableOutput.IsOutputValid())
                {
                    continue;
                }

                var nodeName = $"#{i} [{outputNode.PlayableOutput.GetEditorName()}]" +
                    $" {outputNode.PlayableOutput.GetPlayableOutputType().Name}";
                _selectOutputNodeMenu.menu.AppendAction(nodeName, _ =>
                {
                    _graphView.ClearSelection();
                    _graphView.AddToSelection(outputNode);
                    _graphView.FrameSelection();
                });
            }

            if (outputCount == 0)
            {
                _selectOutputNodeMenu.menu.AppendAction("No PlayableOuput node", null,
                    DropdownMenuAction.Status.Disabled);
            }
        }

        private void OnHoverSelectRootNodeMenu(PointerEnterEvent evt)
        {
            _selectRootNodeMenu.menu.MenuItems().Clear();

            var playableGraph = _graphPopupField.value;
            if (!playableGraph.IsValid())
            {
                _selectRootNodeMenu.menu.AppendAction("No root Playable node", null,
                    DropdownMenuAction.Status.Disabled);
                return;
            }

            var nodeList = new List<UNode>();
            _graphView.nodes.ToList(nodeList);
            // _graphView.nodes.ToList() method returns nodes in random order,
            // ensure that the menu items are ordered
            var rootPlayableCount = playableGraph.GetRootPlayableCount();
            for (int i = 0; i < rootPlayableCount; i++)
            {
                var playable = playableGraph.GetRootPlayable(i);
                var playableNode = nodeList.First(node =>
                {
                    if (node is PlayableNode pNode)
                    {
                        return pNode.Playable.GetHandle() == playable.GetHandle();
                    }

                    return false;
                }) as PlayableNode;

                if (!playableNode.Playable.IsValid())
                {
                    continue;
                }

                var nodeName = !string.IsNullOrEmpty(playableNode.ExtraLabel)
                    ? $"#{i} [{playableNode.ExtraLabel}] {playableNode.Playable.GetPlayableType().Name}"
                    : $"#{i} {playableNode.Playable.GetPlayableType().Name}";

                _selectRootNodeMenu.menu.AppendAction(nodeName, _ =>
                {
                    _graphView.ClearSelection();
                    _graphView.AddToSelection(playableNode);
                    _graphView.FrameSelection();
                });
            }

            if (rootPlayableCount == 0)
            {
                _selectRootNodeMenu.menu.AppendAction("No root Playable node", null,
                    DropdownMenuAction.Status.Disabled);
            }
        }
    }
}
