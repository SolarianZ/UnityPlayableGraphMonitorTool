using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Node;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Pool
{
    public interface IPlayableOutputNodePool
    {
        bool IsNodeActive(PlayableOutput playableOutput);
        PlayableOutputNode GetActiveNode(PlayableOutput playableOutput);
        bool TryGetActiveNode(PlayableOutput playableOutput, out PlayableOutputNode node);
        IEnumerable<PlayableOutputNode> GetActiveNodes();
        PlayableOutputNode Alloc(PlayableOutput playableOutput);
        void Recycle(PlayableOutputNode node);
        void RecycleAllActiveNodes();
        void RemoveDormantNodesFromView();
    }
}