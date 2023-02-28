using GBG.PlayableGraphMonitor.Editor.Node;
using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEngine.UIElements;


namespace GBG.PlayableGraphMonitor.Editor
{
    public partial class PlayableGraphMonitorWindow
    {
        private const float _NODE_INSPECTOR_WIDTH = 250f;

        private VisualElement _nodeInspectorPanel;

        private Label _nodeDescriptionLabel;


        private void CreateNodeInspector(VisualElement container)
        {
            _nodeInspectorPanel = new VisualElement
            {
                name = "node-inspector",
                style =
                {
                    width = new Length(_NODE_INSPECTOR_WIDTH, LengthUnit.Pixel),
                    height = new Length(100, LengthUnit.Percent),
                    backgroundColor = GraphTool.GetNodeInspectorBackgroundColor(),
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 4,
                    paddingBottom = 4,
                    display = _displayInspector ? DisplayStyle.Flex : DisplayStyle.None,
                }
            };

            var scrollView = new ScrollView
            {
                name = "node-desc-container"
            };
            _nodeDescriptionLabel = new Label
            {
                name = "node-desc-label",
                style =
                {
                    color = GraphTool.GetNodeInspectorTextColor(),
                    whiteSpace = WhiteSpace.Normal
                }
            };
            scrollView.Add(_nodeDescriptionLabel);
            _nodeInspectorPanel.Add(scrollView);

            container.Add(_nodeInspectorPanel);
        }

        private void DrawGraphNodeInspector()
        {
            if (!_inspectorToggle.value)
            {
                return;
            }

            if (_graphView.selection.Count == 1 &&
                _graphView.selection[0] is GraphViewNode node)
            {
                _nodeDescriptionLabel.text = node.GetNodeDescription();
                return;
            }

            _nodeDescriptionLabel.text = "Select one node to show details.";
        }
    }
}