using System.Text;
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
            var clipFieldSelector = _targetField.Q(className: "unity-object-field__selector");
            clipFieldSelector.SetEnabled(false);
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

        protected override void AppendNodeDescription(StringBuilder descBuilder)
        {
            base.AppendNodeDescription(descBuilder);

            if (!PlayableOutput.IsOutputValid())
            {
                return;
            }

            var audioPlayableOutput = (AudioPlayableOutput)PlayableOutput;
            var evaluateOnSeek = audioPlayableOutput.GetEvaluateOnSeek();
            descBuilder.AppendLine(LINE);
            descBuilder.Append("Evaluate on Seek: ").AppendLine(evaluateOnSeek.ToString());
        }
    }
}