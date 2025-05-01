using GBG.PlayableGraphMonitor.Editor.GraphView;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class AnimationScriptPlayableNode : PlayableNode
    {
        private readonly Label _jobTypeLabel;

        private MethodInfo _getJobTypeMethod;

        private Func<Type> _getJobTypeFunc;


        public AnimationScriptPlayableNode()
        {
            _jobTypeLabel = new Label
            {
                style =
                {
                    marginTop = 1,
                    marginBottom = 1,
                    marginLeft = 6,
                    color = Color.white,
                    fontSize = 12,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    unityFontStyleAndWeight = FontStyle.Bold,
                }
            };
            var banner = mainContainer.Q("divider");
            banner.style.height = StyleKeyword.Auto;
            banner.Add(_jobTypeLabel);
        }


        protected override void OnUpdate(PlayableGraphViewUpdateContext updateContext, bool playableChanged)
        {
            base.OnUpdate(updateContext, playableChanged);

            if (playableChanged)
            {
                _getJobTypeFunc = null;
                _jobTypeLabel.text = GetJobType()?.Name;
            }
        }

        protected override void AppendPlayableTypeDescription()
        {
            base.AppendPlayableTypeDescription();

            // Job
            var jobType = GetJobType();
            GUILayout.Label($"Job: {jobType?.Name ?? "?"}");
        }

        protected override void DrawNodeDescriptionInternal()
        {
            base.DrawNodeDescriptionInternal();

            if (!Playable.IsValid())
            {
                return;
            }

            var animScriptPlayable = (AnimationScriptPlayable)Playable;
            GUILayout.Label(LINE);
            GUILayout.Label($"ProcessInputs: {animScriptPlayable.GetProcessInputs()}");
        }

        public Type GetJobType()
        {
            if (_getJobTypeFunc != null)
            {
                return _getJobTypeFunc();
            }

            var playableHandle = Playable.GetHandle();
            if (_getJobTypeMethod == null)
            {
                _getJobTypeMethod = playableHandle.GetType().GetMethod("GetJobType",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (_getJobTypeMethod == null)
                {
                    Debug.LogError("Failed to get method 'PlayableHandle.GetJobType()'.");
                    return null;
                }
            }

            _getJobTypeFunc = (Func<Type>)_getJobTypeMethod.CreateDelegate(typeof(Func<Type>), playableHandle);

            return _getJobTypeFunc();
        }
    }
}