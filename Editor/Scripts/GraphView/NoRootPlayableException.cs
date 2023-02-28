using System;

namespace GBG.PlayableGraphMonitor.Editor.GraphView
{
    /// <summary>
    /// All Playable has a parent Playable, that means there must be at least one cycle in the PlayableGraph.
    /// </summary>
    public class NoRootPlayableException : Exception
    {
        public NoRootPlayableException()
        {
        }

        public NoRootPlayableException(string message) : base(message)
        {
        }
    }
}