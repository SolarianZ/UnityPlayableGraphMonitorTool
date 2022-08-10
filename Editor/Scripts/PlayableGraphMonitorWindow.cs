using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Assertions;
using UnityEngine.Playables;
using PlayableUtility = UnityEditor.Playables.Utility;

namespace GBG.PlayableGraphMonitor.Editor
{
    public class PlayableGraphMonitorWindow : EditorWindow
    {
        [MenuItem("Window/Analysis/PlayableGraph Monitor")]
        public static PlayableGraphMonitorWindow Open()
        {
            return GetWindow<PlayableGraphMonitorWindow>("Playable Graph Monitor");
        }


        private readonly List<PlayableGraph> _graphs = new List<PlayableGraph>();

        private string[] _graphPopupMenuItems;

        private int _selectedGraphNumber;

        private PlayableGraph? _selectedGraph;

        private Vector2 _graphListScrollPos;

        private GUIStyle _labelStyle;


        private void OnEnable()
        {
            _graphs.AddRange(PlayableUtility.GetAllGraphs());
            UpdateGraphPopupMenuItems();

            PlayableUtility.graphCreated += OnGraphCreated;
            PlayableUtility.destroyingGraph += OnDestroyingGraph;
        }

        private void OnDisable()
        {
            PlayableUtility.graphCreated -= OnGraphCreated;
            PlayableUtility.destroyingGraph -= OnDestroyingGraph;
        }

        private void OnGUI()
        {
            PrepareStyles();

            DrawGraphDropdownList();

            DrawGraphs();
        }

        private void Update()
        {
            Repaint();
        }

        private void PrepareStyles()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    richText = true
                };
            }
        }

        private void DrawGraphDropdownList()
        {
            EditorGUI.BeginChangeCheck();
            _selectedGraphNumber = EditorGUILayout.Popup(_selectedGraphNumber, _graphPopupMenuItems);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedGraph = _selectedGraphNumber == 0 ? null : _graphs[_selectedGraphNumber - 1];
            }
        }

        private void DrawGraphs()
        {
            _graphListScrollPos = GUILayout.BeginScrollView(_graphListScrollPos);
            for (int i = 0; i < _graphs.Count; i++)
            {
                // graph
                var graph = _selectedGraph ?? _graphs[i];
                if (!graph.IsValid())
                {
                    EditorGUILayout.LabelField($"<b>[Graph] <color=red>[Invalid]</color></b> {graph.GetEditorName()}",
                        _labelStyle);
                    continue;
                }
                EditorGUILayout.LabelField($"<b>[Graph]</b> {graph.GetEditorName()}", _labelStyle);

                // graph output
                EditorGUI.indentLevel += 2;
                for (int j = 0; j < graph.GetOutputCount(); j++)
                {
                    var output = graph.GetOutput(j);
                    if (!output.IsOutputValid())
                    {
                        EditorGUILayout.LabelField($"<b>[Output] <color=red>[Invalid]</color></b> {output.GetEditorName()}",
                            _labelStyle);
                        continue;
                    }
                    EditorGUILayout.LabelField($"<b>[Output]</b> {output.GetEditorName()}", _labelStyle);

                    // output source playable
                    var sourcePlayable = output.GetSourcePlayable();
                    ListPlayableRecursively(sourcePlayable);
                }
                EditorGUI.indentLevel -= 2;

                if (_selectedGraph != null)
                {
                    break;
                }
            }
            GUILayout.EndScrollView();
        }

        private void ListPlayableRecursively(Playable playable)
        {
            if (playable.IsNull())
            {
                return;
            }

            EditorGUI.indentLevel += 2;
            var sourcePlayableTypeName = playable.GetPlayableType().Name;
            if (!playable.IsValid())
            {
                EditorGUILayout.LabelField($"<b><color=red>[Invalid]</color></b> {sourcePlayableTypeName}",
                    _labelStyle);
                EditorGUI.indentLevel -= 2;
                return;
            }
            EditorGUILayout.LabelField($"{sourcePlayableTypeName}  ({GetPlayableStates(playable)})", _labelStyle);

            for (int i = 0; i < playable.GetInputCount(); i++)
            {
                var input = playable.GetInput(i);
                ListPlayableRecursively(input);
            }
            EditorGUI.indentLevel -= 2;
        }

        private string GetPlayableStates(Playable playable)
        {
            Assert.IsTrue(playable.IsValid());

            var playState = playable.GetPlayState();
            var inputCount = playable.GetInputCount();
            var outputCount = playable.GetOutputCount();
            var speed = playable.GetSpeed();
            var time = playable.GetTime();
            var duration = playable.GetDuration();
            var durationStr = duration > float.MaxValue ? "+Inf" : duration.ToString("F3");
            var isDone = playable.IsDone() ? "Done" : "NotDone";
            var clipInfo = string.Empty;
            if (playable.IsPlayableOfType<AnimationClipPlayable>())
            {
                var animClipPlayable = (AnimationClipPlayable)playable;
                var clip = animClipPlayable.GetAnimationClip();
                clipInfo = $"  C:{(clip ? clip.name : "null")}";
            }
            return $"{playState}  {speed:F2}x  T:{time:F3}(s)/{durationStr}(s)  I:{inputCount}  O:{outputCount}  {isDone}{clipInfo}";
        }

        private void UpdateGraphPopupMenuItems()
        {
            _graphPopupMenuItems = new string[_graphs.Count + 1];
            _graphPopupMenuItems[0] = "All";
            for (int i = 0; i < _graphs.Count; i++)
            {
                _graphPopupMenuItems[i + 1] = _graphs[i].GetEditorName();
            }

            if (_selectedGraph == null)
            {
                _selectedGraphNumber = 0;
                return;
            }

            _selectedGraphNumber = _graphs.IndexOf(_selectedGraph.Value) + 1;
        }

        private void OnGraphCreated(PlayableGraph graph)
        {
            if (!_graphs.Contains(graph))
            {
                _graphs.Add(graph);
                UpdateGraphPopupMenuItems();
            }

        }

        private void OnDestroyingGraph(PlayableGraph graph)
        {
            _graphs.Remove(graph);

            UpdateGraphPopupMenuItems();
        }
    }
}
