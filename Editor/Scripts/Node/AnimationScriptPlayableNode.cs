using System;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class AnimationScriptPlayableNode : PlayableNode
    {
        private MethodInfo _getJobTypeMethod;

        private Func<Type> _getJobTypeFunc;


        protected override void OnUpdate(bool playableChanged)
        {
            base.OnUpdate(playableChanged);

            if (playableChanged)
            {
                _getJobTypeFunc = null;
            }
        }

        protected override void AppendPlayableTypeDescription(StringBuilder descBuilder)
        {
            descBuilder.Append("Type: ").AppendLine(Playable.GetPlayableType()?.Name ?? "");

            // Job
            var jobType = GetJobType();
            descBuilder.Append("Job: ").AppendLine(jobType?.Name ?? "");
        }


        private Type GetJobType()
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