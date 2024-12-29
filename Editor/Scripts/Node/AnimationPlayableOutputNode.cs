using UnityEditor;
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

        protected override void DrawNodeDescriptionInternal()
        {
            base.DrawNodeDescriptionInternal();

            if (!PlayableOutput.IsOutputValid())
            {
                return;
            }

            var animationPlayableOutput = (AnimationPlayableOutput)PlayableOutput;
            var target = animationPlayableOutput.GetTarget();
            GUILayout.Label(LINE);
            EditorGUILayout.ObjectField("Target:", target, typeof(Animator), true);
            EditorGUI.BeginChangeCheck();
            var animationStreamSource = (AnimationStreamSource)EditorGUILayout.EnumPopup("AnimationStreamSource:", animationPlayableOutput.GetAnimationStreamSource());
            if (EditorGUI.EndChangeCheck())
                animationPlayableOutput.SetAnimationStreamSource(animationStreamSource);
            EditorGUI.BeginChangeCheck();
            var sortingOrder = (ushort)EditorGUILayout.IntField("SortingOrder:", animationPlayableOutput.GetSortingOrder());
            if (EditorGUI.EndChangeCheck())
                animationPlayableOutput.SetSortingOrder(sortingOrder);
        }
    }
}