using UnityEngine;

namespace Antchemy
{
    public class Nest : MonoBehaviour
    {
        public int SlotIndex { get; private set; }
        public Colony OccupyingColony { get; private set; }
        public bool IsFree => OccupyingColony==null;
        Renderer nestRim, nestInterior;
        public void Initialize(int index)
        {
            SlotIndex=index;
            nestRim=GameplayVisuals.Shape(transform,"ColonyNest",PrimitiveType.Cube,new Vector3(0,0,.24f),new Vector3(1.12f,.82f,.22f),new Color(.98f,.94f,.86f)).GetComponent<Renderer>();
            nestInterior=GameplayVisuals.Shape(transform,"NestInterior",PrimitiveType.Cube,new Vector3(0,0,.10f),new Vector3(.96f,.66f,.10f),new Color(.88f,.81f,.68f)).GetComponent<Renderer>();
        }
        public void AssignColony(Colony colony) { OccupyingColony=colony; nestRim.enabled=nestInterior.enabled=false; }
        public void ClearSlot() { OccupyingColony=null; nestRim.enabled=nestInterior.enabled=true; }
    }
}
