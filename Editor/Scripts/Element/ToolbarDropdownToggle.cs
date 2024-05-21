using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GBG.PlayableGraphMonitor.Editor
{
    public class ToolbarDropdownToggle : VisualElement, INotifyValueChanged<bool>
    {
        private readonly ToolbarToggle _toggle;
        private readonly ToolbarMenu _menu;

        public string text
        {
            get => _toggle.text;
            set => _toggle.text = value;
        }
        public string label
        {
            get => _toggle.label;
            set => _toggle.label = value;
        }
        public Label labelElement => _toggle.labelElement;
        public new string tooltip
        {
            get => _toggle.tooltip;
            set => _toggle.tooltip = value;
        }
        public bool value
        {
            get => _toggle.value;
            set => _toggle.value = value;
        }
        public string menuTooltip
        {
            get => _menu.tooltip;
            set => _menu.tooltip = value;
        }
        public DropdownMenu menu => _menu.menu;
        public ToolbarMenu.Variant menuVariant
        {
            get => _menu.variant;
            set => _menu.variant = value;
        }


        public ToolbarDropdownToggle()
        {
            style.flexDirection = FlexDirection.Row;
            style.flexShrink = 0;

            _toggle = new ToolbarToggle();
            _toggle.style.borderRightWidth = 0;
            _toggle.style.minWidth = 0;
            Add(_toggle);

            _menu = new ToolbarMenu();
            _menu.style.borderLeftWidth = 0;
            Add(_menu);
        }

        public void SetValueWithoutNotify(bool newValue)
        {
            _toggle.SetValueWithoutNotify(newValue);
        }
    }
}