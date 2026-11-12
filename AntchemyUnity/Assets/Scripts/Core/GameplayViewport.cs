using UnityEngine;

namespace Antchemy
{
    public static class GameplayViewport
    {
        public const float ReferenceWidth=720, HeaderHeight=128, ControlsHeight=180;
        public static Rect Calculate(Rect safe,float screenWidth)
        {
            float scale=screenWidth/ReferenceWidth;
            return new Rect(safe.x+12*scale,safe.y+ControlsHeight*scale,
                Mathf.Max(1,safe.width-24*scale),Mathf.Max(1,safe.height-(HeaderHeight+ControlsHeight)*scale));
        }
        public static float CameraSize(float aspect) => Mathf.Max(7.35f,4.4f/Mathf.Max(.1f,aspect));
    }
}
