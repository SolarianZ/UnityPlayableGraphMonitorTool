using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class AudioPlayableOutputNode : PlayableOutputNode
    {
        private readonly ObjectField _targetField;


        public AudioPlayableOutputNode()
        {
            var banner = mainContainer.Q("divider");
            banner.style.height = StyleKeyword.Auto;

            _targetField = new ObjectField
            {
                objectType = typeof(AudioSource),
                tooltip = "Target",
            };
            var targetFieldSelector = _targetField.Q(className: "unity-object-field__selector");
            targetFieldSelector.style.display = DisplayStyle.None;
            targetFieldSelector.SetEnabled(false);
            banner.Add(_targetField);
        }

        protected override void OnUpdate(bool playableOutputChanged)
        {
            base.OnUpdate(playableOutputChanged);

            if (!PlayableOutput.IsOutputValid())
            {
                return;
            }

            var audioPlayableOutput = (AudioPlayableOutput)PlayableOutput;
            var target = audioPlayableOutput.GetTarget();
            _targetField.style.display = target ? DisplayStyle.Flex : DisplayStyle.None;
            _targetField.SetValueWithoutNotify(target);
        }

        protected override void DrawNodeDescriptionInternal()
        {
            base.DrawNodeDescriptionInternal();

            if (!PlayableOutput.IsOutputValid())
            {
                return;
            }

            var audioPlayableOutput = (AudioPlayableOutput)PlayableOutput;
            var target = audioPlayableOutput.GetTarget();
            var evaluateOnSeek = audioPlayableOutput.GetEvaluateOnSeek();
            GUILayout.Label(LINE);
            EditorGUILayout.ObjectField("Target:", target, typeof(AudioSource), true);
            EditorGUILayout.Toggle("EvaluateOnSeek:", evaluateOnSeek);
        }
    }
}