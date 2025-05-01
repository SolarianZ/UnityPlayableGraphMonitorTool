using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace GBG.PlayableGraphMonitor.Editor
{
    public static class EditorGUILayoutHelper
    {
        #region Reflection

        private static Func<string, GUILayoutOption[], string> _toolbarSearchFieldCache;


        public static string ToolbarSearchField(string searchText)
        {
            // string EditorGUILayout.ToolbarSearchField(string);
            if (_toolbarSearchFieldCache == null)
            {
                MethodInfo toolbarSearchFieldMethod = typeof(EditorGUILayout).GetMethod("ToolbarSearchField", BindingFlags.Static | BindingFlags.NonPublic,
                    null, new Type[] { typeof(string), typeof(GUILayoutOption[]) }, null);
                Assert.IsNotNull(toolbarSearchFieldMethod);
                _toolbarSearchFieldCache = (Func<string, GUILayoutOption[], string>)Delegate.CreateDelegate(typeof(Func<string, GUILayoutOption[], string>), toolbarSearchFieldMethod);
            }

            searchText = _toolbarSearchFieldCache(searchText, null);
            return searchText;
        }

        #endregion
    }
}