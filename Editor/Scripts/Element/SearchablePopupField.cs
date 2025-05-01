using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using PopupWindow = UnityEditor.PopupWindow;

namespace GBG.PlayableGraphMonitor.Editor
{
    public class SearchablePopupField<T> : PopupField<T>
    {
        #region ctor

        public SearchablePopupField(string label = null) : base(label)
        {
        }

        public SearchablePopupField(string label, List<T> choices, T defaultValue,
            Func<T, string> formatSelectedValueCallback = null,
            Func<T, string> formatListItemCallback = null)
            : base(label, choices, defaultValue, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        public SearchablePopupField(List<T> choices, T defaultValue,
            Func<T, string> formatSelectedValueCallback = null,
            Func<T, string> formatListItemCallback = null)
            : base(choices, defaultValue, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        public SearchablePopupField(List<T> choices, int defaultIndex,
            Func<T, string> formatSelectedValueCallback = null,
            Func<T, string> formatListItemCallback = null)
            : base(choices, defaultIndex, formatSelectedValueCallback, formatListItemCallback)
        {
        }

        #endregion


        #region Reflection

        private List<T> _choicesCache;
        private VisualElement _visualInputCache;


        private List<T> GetChoices()
        {
#if UNITY_2020_3_OR_NEWER
            return choices;
#else
            if (_choicesCache == null)
            {
                PropertyInfo choicesProp = typeof(BasePopupField<T, T>).GetProperty("choices", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(choicesProp);
                _choicesCache = (List<T>)choicesProp.GetValue(this);
            }

            return _choicesCache;
#endif
        }

        private VisualElement GetVisualInput()
        {
            if (_visualInputCache == null)
            {
                PropertyInfo visualInputProp = typeof(BaseField<T>).GetProperty("visualInput", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(visualInputProp);
                _visualInputCache = (VisualElement)visualInputProp.GetValue(this);
            }

            return _visualInputCache;
        }

        #endregion


        #region Block the default GenericMenu

#if !UNITY_2021_1_OR_NEWER
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
        }
#endif

#if UNITY_2023_1_OR_NEWER
        [Obsolete]
        protected override void ExecuteDefaultAction(EventBase evt)
        {
            if (TryHandleEventInternal(evt))
                return;

            base.ExecuteDefaultAction(evt);
        }
#endif

#if !UNITY_2023_1_OR_NEWER
#if UNITY_2022_1_OR_NEWER
        [Obsolete]
#endif
        public override void HandleEvent(EventBase evt)
        {
            if (TryHandleEventInternal(evt))
                return;

            base.HandleEvent(evt);
        }
#endif

        private bool TryHandleEventInternal(EventBase evt)
        {
            if (evt is PointerDownEvent pointerDownEvent)
            {
                if (pointerDownEvent.button == 0 && GetVisualInput().ContainsPoint(GetVisualInput().WorldToLocal(pointerDownEvent.originalMousePosition)))
                {
                    pointerDownEvent.StopImmediatePropagation();
                    Rect popupRect = GetVisualInput().worldBound;
                    PopupWindow.Show(popupRect, new Content(this));
                    return true;
                }
            }

            return false;
        }

        #endregion


        class Content : PopupWindowContent
        {
            private readonly SearchablePopupField<T> _popup;
            private string _searchContent;
            private Vector2 _scrollPosition;
            private List<T> _filteredChoices;
            private ReorderableList _list;


            public Content(SearchablePopupField<T> popup)
            {
                _popup = popup;
            }

            public override void OnOpen()
            {
                base.OnOpen();

                _filteredChoices = new List<T>(_popup.GetChoices());
                _list = new ReorderableList(_filteredChoices, typeof(T))
                {
                    index = _popup.index,
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
                const string SEARCH_CONTROL = "SearchablePopupField.PopupWindowContent.ToolbarSearchField";

                EditorGUI.BeginChangeCheck();
                {
                    GUI.SetNextControlName(SEARCH_CONTROL);
                    _searchContent = EditorGUILayoutHelper.ToolbarSearchField(_searchContent);
                    EditorGUI.FocusTextInControl(SEARCH_CONTROL);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    _filteredChoices.Clear();
                    for (int i = 0; i < _popup.GetChoices().Count; i++)
                    {
                        string elemDisplayName = GetElementDisplayName(_popup.GetChoices(), i, _popup.index == i);
                        if (elemDisplayName.IndexOf(_searchContent, StringComparison.OrdinalIgnoreCase) >= 0)
                            _filteredChoices.Add(_popup.GetChoices()[i]);
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
                GUIContent tempLabelContent = new GUIContent();
                float maxWidth = 0;
                foreach (T item in _popup.GetChoices())
                {
                    string label = item?.GetHashCode() == _popup.value?.GetHashCode()
                        ? _popup.formatSelectedValueCallback?.Invoke(item) ?? item?.ToString() ?? string.Empty
                        : _popup.formatListItemCallback?.Invoke(item) ?? item?.ToString() ?? string.Empty;
                    tempLabelContent.text = label;
                    float width = EditorStyles.toolbarPopup.CalcSize(tempLabelContent).x;
                    if (width > maxWidth)
                        maxWidth = width;
                }

                Vector2 size = new Vector2
                {
                    x = Mathf.Max(300, _popup.GetVisualInput().resolvedStyle.width, maxWidth),
                    y = Mathf.Min(400, Mathf.Max(_list.elementHeight * _list.count + 36, 48)),
                };

                return size;
            }


            private string GetElementDisplayName(IList<T> elements, int index, bool isActive)
            {
                T item = elements[index];
                string text;
                if (isActive)
                    text = _popup.formatSelectedValueCallback?.Invoke(item) ?? item.ToString();
                else
                    text = _popup.formatListItemCallback?.Invoke(item) ?? item.ToString();

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
                _popup.value = _filteredChoices[list.index];
                editorWindow.Close();
            }
        }
    }
}