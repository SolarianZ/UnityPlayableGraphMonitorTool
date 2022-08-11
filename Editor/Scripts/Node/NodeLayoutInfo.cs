using UnityEngine;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public struct NodeLayoutInfo
    {
        public const int HorizontalSpace = 80;

        public const int VerticalSpace = 80;

        public static readonly Vector2 StandardNodeSize = new Vector2(400, 150);

        public int TreeWidth;

        public Vector2 Origin;


        public void Reset()
        {
            TreeWidth = 0;

            Origin = Vector2.zero;
        }
    }
}