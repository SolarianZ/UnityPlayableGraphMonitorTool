using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Node;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Pool
{
    public interface IPlayableNodePool
    {
        PlayableNode GetActiveNode(Playable playable);
        IEnumerable<PlayableNode> GetActiveNodes();
        PlayableNode Alloc(Playable playable);
        void Recycle(PlayableNode node);
        void RecycleAllActiveNodes();
        void RemoveDormantNodesFromView();
    }
}