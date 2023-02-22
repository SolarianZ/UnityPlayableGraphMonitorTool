using UnityEditor.Experimental.GraphView;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class PlayableNode_New : GraphViewNode_New
    {
        public Playable Playable { get; private set; }

        public void Setup(Playable playable)
        {
            Playable = playable;
        }

        public Port FindConnectedOutputPort(Playable connectedOutputPlayable)
        {
            for (int i = 0; i < Playable.GetOutputCount(); i++)
            {
                var output = Playable.GetOutput(i);
                if (output.GetHandle() == connectedOutputPlayable.GetHandle())
                {
                    return OutputPorts[i];
                }
            }

            return null;
        }
    }
}