using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using PopupWindow = UnityEditor.PopupWindow;

namespace GBG.PlayableGraphMonitor.Editor
{
    public class SearchablePopupWindowContent<T> : PopupWindowContent
    {
        public static void Show(Rect activatorRect, GetChoices choicesProvider, Action<T> itemSelected,
            Func<T, string> formatListItemCallback = null, Func<T, string> formatSelectedValueCallback = null)
        {
            PopupWindow.Show(activatorRect, new SearchablePopupWindowContent<T>(choicesProvider, itemSelected,
                formatListItemCallback, formatSelectedValueCallback));
        }


        public delegate void GetChoices(out IList<T> choices, out int selectionIndex);

        private readonly GetChoices _choicesProvider;
        private readonly Action<T> _itemSelected;
        private readonly Func<T, string> _formatListItemCallback;
        private readonly Func<T, string> _formatSelectedValueCallback;

        private string _searchContent;
        private Vector2 _scrollPosition;
        private List<T> _filteredChoices;
        private ReorderableList _list;

        public Vector2 minSize { get; set; } = new Vector2(300, 200);
        public Vector2 maxSize { get; set; } = new Vector2(900, 600);


        public SearchablePopupWindowContent(GetChoices choicesProvider, Action<T> itemSelected,
            Func<T, string> formatListItemCallback = null, Func<T, string> formatSelectedValueCallback = null)
        {
            _choicesProvider = choicesProvider ?? throw new ArgumentNullException(nameof(choicesProvider));
            _itemSelected = itemSelected;
            _formatListItemCallback = formatListItemCallback;
            _formatSelectedValueCallback = formatSelectedValueCallback ?? formatListItemCallback;
        }

        public override void OnOpen()
        {
            base.OnOpen();

            IList<T> allChoices = null;
            int currentSelection = -1;
            _choicesProvider?.Invoke(out allChoices, out currentSelection);

            _filteredChoices = new List<T>(allChoices ?? Array.Empty<T>());
            _list = new ReorderableList(_filteredChoices, typeof(T))
            {
                index = currentSelection,
                displayAdd = false,
                displayRemove = false,
                headerHeight = 0,
                footerHeight = 0,
                draggable = false,
                onMouseUpCallback = OnMouseUp,
                drawElementCallback = DrawElement,
                drawElementBackgroundCallback = DrawElementBackground,
            };
        }

        public override void OnGUI(Rect rect)
        {
            const string SEARCH_CONTROL = "SearchablePopupWindowContent.ToolbarSearchField";

            EditorGUI.BeginChangeCheck();
            {
                GUI.SetNextControlName(SEARCH_CONTROL);
                _searchContent = EditorGUILayoutHelper.ToolbarSearchField(_searchContent);
                EditorGUI.FocusTextInControl(SEARCH_CONTROL);
            }
            if (EditorGUI.EndChangeCheck())
            {
                IList<T> allChoices = null;
                int currentSelection = -1;
                _choicesProvider?.Invoke(out allChoices, out currentSelection);
                allChoices = allChoices ?? Array.Empty<T>();

                _filteredChoices.Clear();
                for (int i = 0; i < allChoices.Count; i++)
                {
                    string elemDisplayName = GetElementDisplayName(allChoices, i, currentSelection == i);
                    if (elemDisplayName.IndexOf(_searchContent, StringComparison.OrdinalIgnoreCase) >= 0)
                        _filteredChoices.Add(allChoices[i]);
                }

                _list.list = _filteredChoices;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            _list.DoLayoutList();
            EditorGUILayout.EndScrollView();

            editorWindow.Repaint();
        }

        public override Vector2 GetWindowSize()
        {
            IList<T> allChoices = null;
            int currentSelection = -1;
            _choicesProvider?.Invoke(out allChoices, out currentSelection);
            allChoices = allChoices ?? Array.Empty<T>();
            T currentValue = currentSelection == -1 ? default : allChoices[currentSelection];

            GUIContent tempLabelContent = new GUIContent();
            float maxItemWidth = 0;
            foreach (T item in allChoices)
            {
                string label = item?.GetHashCode() == currentValue?.GetHashCode()
                    ? _formatSelectedValueCallback?.Invoke(item) ?? item?.ToString() ?? string.Empty
                    : _formatListItemCallback?.Invoke(item) ?? item?.ToString() ?? string.Empty;
                tempLabelContent.text = label;
                float width = EditorStyles.toolbarPopup.CalcSize(tempLabelContent).x + 12;
                if (width > maxItemWidth)
                    maxItemWidth = width;
            }

            float listHeight = _list.elementHeight * _list.count + 36;
            Vector2 size = new Vector2
            {
                x = Mathf.Clamp(maxItemWidth, minSize.x, maxSize.x),
                y = Mathf.Clamp(listHeight, minSize.y, maxSize.y),
            };

            return size;
        }


        private string GetElementDisplayName(IList<T> elements, int index, bool isActive)
        {
            T item = elements[index];
            string text;
            if (isActive)
                text = _formatSelectedValueCallback?.Invoke(item) ?? item.ToString();
            else
                text = _formatListItemCallback?.Invoke(item) ?? item.ToString();

            return text;
        }

        private void DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (isActive)
            {
                EditorGUI.DrawRect(rect, new Color(0.24f, 0.48f, 0.90f, 0.5f));
            }
            else if (isFocused)
            {
                EditorGUI.DrawRect(rect, new Color(0.24f, 0.48f, 0.90f, 0.2f));
            }
            else if (rect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(rect, new Color(0.24f, 0.48f, 0.90f, 0.1f));
            }
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            string text = GetElementDisplayName(_filteredChoices, index, isActive);
            GUI.Label(rect, text);
        }

        private void OnMouseUp(ReorderableList list)
        {
            T newSelection = _filteredChoices[list.index];
            editorWindow.Close();
            _itemSelected?.Invoke(newSelection);
        }
    }
}