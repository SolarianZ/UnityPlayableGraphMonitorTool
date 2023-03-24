using System.Text;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class AnimationPlayableOutputNode : PlayableOutputNode
    {
        private readonly ObjectField _targetField;


        public AnimationPlayableOutputNode()
        {
            var banner = mainContainer.Q("divider");
            banner.style.height = StyleKeyword.Auto;

            _targetField = new ObjectField
            {
                objectType = typeof(Animator),
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

            var animationPlayableOutput = (AnimationPlayableOutput)PlayableOutput;
            var target = animationPlayableOutput.GetTarget();
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

            var animationPlayableOutput = (AnimationPlayableOutput)PlayableOutput;
            var target = animationPlayableOutput.GetTarget();
            var animationStreamSource = animationPlayableOutput.GetAnimationStreamSource();
            var sortingOrder = animationPlayableOutput.GetSortingOrder();
            descBuilder.AppendLine(LINE);
            descBuilder.Append("Target: ").AppendLine(target?.name ?? "Null")
                .Append("AnimationStreamSource: ").AppendLine(animationStreamSource.ToString())
                .Append("SortingOrder: ").AppendLine(sortingOrder.ToString());
        }
    }
}