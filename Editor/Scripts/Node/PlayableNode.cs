using GBG.PlayableGraphMonitor.Editor.GraphView;
using GBG.PlayableGraphMonitor.Editor.Utility;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class PlayableNode : GraphViewNode
    {
        public Playable Playable { get; private set; }

        public bool IsRootPlayable { get; set; }

        public Vector2 Position { get; private set; }

        public PlayableNode ParentNode { get; set; }

        public string ExtraLabel { get; private set; }


        public void Update(PlayableGraphViewUpdateContext updateContext, Playable playable)
        {
            var playableChanged = false;
            if (Playable.GetHandle() != playable.GetHandle())
            {
                Playable = playable;
                playableChanged = true;

                this.SetNodeStyle(playable.GetPlayableNodeColor());
            }

            var extraLabel = GetExtraNodeLabel(updateContext.NodeExtraLabelTable);
            if (playableChanged || ExtraLabel != extraLabel)
            {
                ExtraLabel = extraLabel;

                var playableTypeName = Playable.GetPlayableType().Name;
                var nodeTitle = string.IsNullOrEmpty(ExtraLabel)
                    ? playableTypeName
                    : $"[{ExtraLabel}]\n{playableTypeName}";

                // Expensive operation
                title = nodeTitle;
            }

            if (!Playable.IsValid())
            {
                style.backgroundColor = GraphTool.GetNodeInvalidColor();
                return;
            }

            SyncPorts(out var portChanged);
            // RefreshExpandedState(); // Expensive
            if (portChanged)
            {
                RefreshPorts();
            }

            OnUpdate(updateContext, playableChanged);
        }

        protected virtual void OnUpdate(PlayableGraphViewUpdateContext updateContext, bool playableChanged)
        {
        }

        public PlayableNode FindRootPlayableNode()
        {
            var node = this;
            while (node != null)
            {
                if (node.IsRootPlayable)
                {
                    return node;
                }

                node = node.ParentNode;
            }

            throw new Exception("This should not happen!");
        }

        public override void Release()
        {
            base.Release();
            IsRootPlayable = false;
            Position = default;
            ParentNode = null;
        }

        public override void SetPosition(Rect newPos)
        {
            Position = newPos.position;
            base.SetPosition(newPos);
        }


        #region Description

        // For debugging
        public override string ToString()
        {
            if (Playable.IsValid())
            {
                if (string.IsNullOrEmpty(ExtraLabel))
                {
                    return Playable.GetPlayableType().Name;
                }

                return $"[{ExtraLabel}] {Playable.GetPlayableType().Name}";
            }

            return GetType().Name;
        }

        public string GetExtraNodeLabel(IReadOnlyDictionary<PlayableHandle, string> nodeExtraLabelTable)
        {
            if (nodeExtraLabelTable == null || !Playable.IsValid())
            {
                return null;
            }

            var playableHandle = Playable.GetHandle();
            nodeExtraLabelTable.TryGetValue(playableHandle, out var label);

            return label;
        }


        protected override void DrawNodeDescriptionInternal()
        {
            if (!Playable.IsValid())
            {
                GUILayout.Label("Invalid Playable");
                return;
            }

            // Extra label
            if (!string.IsNullOrEmpty(ExtraLabel))
            {
                GUILayout.Label($"ExtraLabel: {ExtraLabel}");
                GUILayout.Label(LINE);
            }

            // Type
            AppendPlayableTypeDescription();

            // Playable
            GUILayout.Label(LINE);
            GUILayout.Label("IsValid: True");
            GUILayout.Label($"IsNull: {Playable.IsNull()}");
            EditorGUI.BeginChangeCheck();
            var done = EditorGUILayout.Toggle("IsDone:", Playable.IsDone());
            if (EditorGUI.EndChangeCheck())
                Playable.SetDone(done);
            EditorGUI.BeginChangeCheck();
            var playState = (PlayState)EditorGUILayout.EnumPopup("PlayState:", Playable.GetPlayState());
            if (EditorGUI.EndChangeCheck())
                if (playState == PlayState.Playing)
                    Playable.Play();
                else if (playState == PlayState.Paused)
                    Playable.Pause();
            EditorGUI.BeginChangeCheck();
            var speed = EditorGUILayout.DoubleField("Speed:", Playable.GetSpeed());
            if (EditorGUI.EndChangeCheck())
                Playable.SetSpeed(speed);
            EditorGUI.BeginChangeCheck();
            var duration = EditorGUILayout.DoubleField("Duration (s):", Playable.GetDuration());
            if (EditorGUI.EndChangeCheck())
                Playable.SetDuration(duration);
            GUILayout.Label($"PreviousTime: {Playable.GetPreviousTime().ToString("F3")}(s)");
            EditorGUI.BeginChangeCheck();
            var time = EditorGUILayout.DoubleField("Time (s):", Playable.GetTime());
            if (EditorGUI.EndChangeCheck())
                Playable.SetTime(time);
            EditorGUI.BeginChangeCheck();
            var leadTime = EditorGUILayout.FloatField("LeadTime (s):", Playable.GetLeadTime());
            if (EditorGUI.EndChangeCheck())
                Playable.SetLeadTime(leadTime);
            EditorGUI.BeginChangeCheck();
            var propagateSetTime = EditorGUILayout.Toggle($"PropagateSetTime:", Playable.GetPropagateSetTime());
            if (EditorGUI.EndChangeCheck())
                Playable.SetPropagateSetTime(propagateSetTime);
            EditorGUI.BeginChangeCheck();
            var traversalMode = (PlayableTraversalMode)EditorGUILayout.EnumPopup("TraversalMode:", Playable.GetTraversalMode());
            if (EditorGUI.EndChangeCheck())
                Playable.SetTraversalMode(traversalMode);

            // Inputs
            GUILayout.Label(LINE);
            var inputCount = Playable.GetInputCount();
            GUILayout.Label(
                inputCount == 0
                    ? "No Input"
                    : (inputCount == 1 ? "1 Input:" : $"{inputCount} Inputs:")
            );
            AppendInputPortDescription();

            // Outputs
            GUILayout.Label(LINE);
            var playableOutputCount = Playable.GetOutputCount();
            GUILayout.Label(
                playableOutputCount == 0
                    ? "No Output"
                    : (playableOutputCount == 1 ? "1 Output" : $"{playableOutputCount} Outputs")
            );
        }

        protected virtual void AppendPlayableTypeDescription()
        {
            GUILayout.Label($"Type: {Playable.GetPlayableType()?.Name ?? "?"}");
            GUILayout.Label($"HandleHashCode: {Playable.GetHandle().GetHashCode()}");
        }

        protected virtual void AppendInputPortDescription()
        {
            var playableInputCount = Playable.GetInputCount();
            for (int i = 0; i < playableInputCount; i++)
            {
                EditorGUI.BeginChangeCheck();
                var weight = EditorGUILayout.Slider($"  #{i} Weight: {Playable.GetInputWeight(i).ToString("F3")}", Playable.GetInputWeight(i), 0, 1);
                if (EditorGUI.EndChangeCheck())
                    Playable.SetInputWeight(i, weight);
            }
        }

        #endregion


        #region Port

        /// <summary>
        /// Find the output port that is connected to <see cref="outputPlayable"/>.
        /// </summary>
        /// <param name="outputPlayable"></param>
        /// <returns></returns>
        public Port FindOutputPort(Playable outputPlayable)
        {
            // If the output port of the Playable at index i is connected to a PlayableOutput,
            // Playable.GetOutput(i) will return an invalid Playable.

            // TODO FIXME: If multiple output ports of `outputPlayable` connect to different input ports of the same `Playable`,
            // this method cannot distinguish between these output ports.

            for (int i = 0; i < Playable.GetOutputCount(); i++)
            {
                var output = Playable.GetOutput(i);
                if (output.GetHandle() == outputPlayable.GetHandle())
                {
                    return OutputPorts[i];
                }
            }

            return null;
        }

        private void SyncPorts(out bool portChanged)
        {
            portChanged = false;
            var isPlayableValid = Playable.IsValid();

            // Input ports
            var inputCount = isPlayableValid ? Playable.GetInputCount() : 0;
            for (int i = InputPorts.Count - 1; i >= inputCount; i--)
            {
                // Port won't change frequently, so there's no PortPool
                inputContainer.Remove(InputPorts[i]);
                InputPorts.RemoveAt(i);
                portChanged = true;
            }

            var missingInputPortCount = inputCount - InputPorts.Count;
            for (int i = 0; i < missingInputPortCount; i++)
            {
                var inputPort = InstantiatePort<Playable>(Direction.Input);
                inputPort.portName = $"Input {InputPorts.Count}";
                inputPort.portColor = GraphTool.GetPortColor(Playable.GetInputWeight(i));

                inputContainer.Add(inputPort);
                InputPorts.Add(inputPort);
                portChanged = true;
            }

            // Output ports
            var outputCount = isPlayableValid ? Playable.GetOutputCount() : 0;
            for (int i = OutputPorts.Count - 1; i >= outputCount; i--)
            {
                // Port won't change frequently, so there's no PortPool
                outputContainer.Remove(OutputPorts[i]);
                OutputPorts.RemoveAt(i);
                portChanged = true;
            }

            var missingOutputPortCount = outputCount - OutputPorts.Count;
            for (int i = 0; i < missingOutputPortCount; i++)
            {
                var outputPort = InstantiatePort<Playable>(Direction.Output);
                outputPort.portName = $"Output {OutputPorts.Count}";
                outputPort.portColor = GraphTool.GetPortColor(1);

                outputContainer.Add(outputPort);
                OutputPorts.Add(outputPort);
                portChanged = true;
            }
        }

        #endregion
    }
}