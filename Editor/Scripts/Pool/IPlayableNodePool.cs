using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Node;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Pool
{
    public interface IPlayableNodePool
    {
        bool IsNodeActive(Playable playable);
        PlayableNode GetActiveNode(Playable playable);
        bool TryGetActiveNode(Playable playable, out PlayableNode node);
        IEnumerable<PlayableNode> GetActiveNodes();
        PlayableNode Alloc(Playable playable);
        void Recycle(PlayableNode node);
        void RecycleAllActiveNodes();
        void RemoveDormantNodesFromView();
    }
}