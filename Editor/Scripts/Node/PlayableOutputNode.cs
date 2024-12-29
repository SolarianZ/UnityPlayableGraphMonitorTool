using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Playables;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using UObject = UnityEngine.Object;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class PlayableOutputNode : GraphViewNode
    {
        public PlayableOutput PlayableOutput { get; private set; }

        private readonly ObjectField _refObjectField;

        private readonly ObjectField _userDataField;

        private int _outputIndex = -1;


        public PlayableOutputNode()
        {
            var banner = mainContainer.Q("divider");
            banner.style.height = StyleKeyword.Auto;

            _refObjectField = new ObjectField
            {
                objectType = typeof(UObject),
                tooltip = "Reference Object",
            };
            var refObjectFieldSelector = _refObjectField.Q(className: "unity-object-field__selector");
            refObjectFieldSelector.style.display = DisplayStyle.None;
            refObjectFieldSelector.SetEnabled(false);
            banner.Add(_refObjectField);

            _userDataField = new ObjectField
            {
                objectType = typeof(UObject),
                tooltip = "User Data",
            };
            var userDataFieldSelector = _userDataField.Q(className: "unity-object-field__selector");
            userDataFieldSelector.style.display = DisplayStyle.None;
            userDataFieldSelector.SetEnabled(false);
            banner.Add(_userDataField);

            CreatePorts();
            // RefreshExpandedState(); // Expensive
            RefreshPorts();
        }

        public void Update(PlayableOutput playableOutput, int outputIndex)
        {
            var playableOutputChanged = false;
            if (PlayableOutput.GetHandle() != playableOutput.GetHandle() || _outputIndex != outputIndex)
            {
                PlayableOutput = playableOutput;
                _outputIndex = outputIndex;
                playableOutputChanged = true;

                // Expensive operations
                var playableOutputTypeName = playableOutput.GetPlayableOutputType().Name;
                var playableOutputEditorName = playableOutput.GetEditorName();
                title = $"#{_outputIndex} [{playableOutputEditorName}]\n{playableOutputTypeName}";

                this.SetNodeStyle(playableOutput.GetPlayableOutputNodeColor());
            }

            OnUpdate(playableOutputChanged);
        }

        protected virtual void OnUpdate(bool playableOutputChanged)
        {
            var refObject = PlayableOutput.GetReferenceObject();
            _refObjectField.style.display = refObject ? DisplayStyle.Flex : DisplayStyle.None;
            _refObjectField.SetValueWithoutNotify(refObject);

            var playableOutputUserData = PlayableOutput.GetUserData();
            _userDataField.style.display = playableOutputUserData ? DisplayStyle.Flex : DisplayStyle.None;
            _userDataField.SetValueWithoutNotify(playableOutputUserData);
        }


        #region Description

        // For debugging
        public override string ToString()
        {
            if (PlayableOutput.IsOutputValid())
            {
                return PlayableOutput.GetPlayableOutputType().Name;
            }

            return GetType().Name;
        }

        protected override void DrawNodeDescriptionInternal()
        {
            if (!PlayableOutput.IsOutputValid())
            {
                GUILayout.Label("Invalid PlayableOutput");
                return;
            }

            GUILayout.Label($"#{_outputIndex}");
            GUILayout.Label($"Type: {PlayableOutput.GetPlayableOutputType().Name}");
            GUILayout.Label($"HandleHashCode: {PlayableOutput.GetHandle().GetHashCode()}");
            GUILayout.Label(LINE);
            GUILayout.Label($"Name: {PlayableOutput.GetEditorName()}");
            GUILayout.Label(LINE);
            GUILayout.Label("IsValid: True");
            GUILayout.Label($"IsNull: {PlayableOutput.IsOutputNull()}");
            GUILayout.Label($"ReferenceObject: {PlayableOutput.GetReferenceObject()?.name ?? "Null"}");
            GUILayout.Label($"UserData: {PlayableOutput.GetUserData()?.name ?? "Null"}");
            GUILayout.Label(LINE);
            GUILayout.Label("Source Input:");
            GUILayout.Label($"  SourceOutputPort: {PlayableOutput.GetSourceOutputPort()}");
            EditorGUI.BeginChangeCheck();
            var weight = EditorGUILayout.Slider("  Weight:", PlayableOutput.GetWeight(), 0, 1);
            if (EditorGUI.EndChangeCheck())
                PlayableOutput.SetWeight(weight);
        }

        #endregion


        #region Port

        public Port InputPort { get; private set; }


        private void CreatePorts()
        {
            InputPort = InstantiatePort<Playable>(Direction.Input);
            InputPort.portName = "Source";
            InputPort.portColor = Color.white;

            inputContainer.Add(InputPort);
            InputPorts.Add(InputPort);
        }

        #endregion
    }
}