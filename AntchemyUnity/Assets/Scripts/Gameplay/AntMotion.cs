using UnityEngine;

namespace Antchemy
{
    // A small anatomical ant with an alternating tripod gait. The body keeps its colony color.
    public sealed class AntMotion : MonoBehaviour
    {
        static int nextPhase;
        readonly Transform[] upperLegs = new Transform[6];
        readonly Transform[] lowerLegs = new Transform[6];
        readonly Transform[] knees = new Transform[6];
        readonly Vector3[] hips = new Vector3[6];
        readonly Vector3[] restingKnees = new Vector3[6];
        readonly Vector3[] restingFeet = new Vector3[6];
        readonly Transform[] antennae = new Transform[2];
        Transform body, abdomen;
        float phaseOffset;
        public bool Walking { get; set; }

        public void Initialize(int colorIndex)
        {
            var shell = ColorPalette.GetAntColor(colorIndex);
            var darkShell = Color.Lerp(shell, Color.black, .27f);
            var legColor = Color.Lerp(shell, Color.black, .36f);
            phaseOffset = (++nextPhase % 37) * .23f;

            var bodyObject = new GameObject("Segmented body");
            bodyObject.transform.SetParent(transform, false);
            body = bodyObject.transform;
            abdomen = GameplayVisuals.Shape(body, "Oval abdomen", PrimitiveType.Sphere,
                new Vector3(0, -.29f, -.13f), new Vector3(.35f, .42f, .24f), darkShell).transform;
            GameplayVisuals.Shape(body, "Abdomen segment", PrimitiveType.Sphere,
                new Vector3(0, -.20f, -.26f), new Vector3(.27f, .035f, .025f), Color.Lerp(shell, Color.black, .43f));
            GameplayVisuals.Shape(body, "Narrow waist", PrimitiveType.Capsule,
                new Vector3(0, -.12f, -.13f), new Vector3(.07f, .11f, .08f), legColor);
            GameplayVisuals.Shape(body, "Thorax", PrimitiveType.Sphere,
                new Vector3(0, .035f, -.14f), new Vector3(.22f, .23f, .20f), shell);
            GameplayVisuals.Shape(body, "Head", PrimitiveType.Sphere,
                new Vector3(0, .27f, -.18f), new Vector3(.31f, .27f, .25f), shell);

            for (int side = -1; side <= 1; side += 2)
            {
                GameplayVisuals.Shape(body, "Compound eye", PrimitiveType.Sphere,
                    new Vector3(side * .115f, .29f, -.30f), new Vector3(.066f, .081f, .045f),
                    new Color(.09f, .10f, .12f));
                MakeRod(body, "Mandible", new Vector3(side * .08f, .40f, -.25f),
                    new Vector3(side * .025f, .46f, -.26f), .025f, darkShell);

                var feeler = new GameObject("Jointed antenna").transform;
                feeler.SetParent(body, false);
                feeler.localPosition = new Vector3(side * .08f, .38f, -.17f);
                MakeRod(feeler, "Antenna stem", Vector3.zero,
                    new Vector3(side * .075f, .075f, 0), .022f, darkShell);
                MakeRod(feeler, "Antenna tip", new Vector3(side * .075f, .075f, 0),
                    new Vector3(side * .17f, .145f, 0), .019f, darkShell);
                antennae[side < 0 ? 0 : 1] = feeler;

                for (int pair = 0; pair < 3; pair++)
                {
                    int index = (side < 0 ? 0 : 3) + pair;
                    float hipY = .12f - pair * .115f;
                    hips[index] = new Vector3(side * .105f, hipY, -.10f);
                    restingKnees[index] = new Vector3(side * (.29f + (pair == 1 ? .035f : 0)),
                        hipY + (pair == 0 ? .075f : pair == 2 ? -.09f : -.015f), -.105f);
                    restingFeet[index] = new Vector3(side * .43f,
                        pair == 0 ? .12f : pair == 1 ? -.12f : -.36f, -.10f);
                    upperLegs[index] = MakeRod(transform, "Upper leg", hips[index], restingKnees[index], .036f, legColor);
                    lowerLegs[index] = MakeRod(transform, "Lower leg", restingKnees[index], restingFeet[index], .027f, legColor);
                    knees[index] = GameplayVisuals.Shape(transform, "Leg joint", PrimitiveType.Sphere,
                        restingKnees[index], Vector3.one * .045f, darkShell).transform;
                }
            }
        }

        static Transform MakeRod(Transform parent, string name, Vector3 start, Vector3 end, float width, Color color)
        {
            var rod = GameplayVisuals.Shape(parent, name, PrimitiveType.Capsule, Vector3.zero, Vector3.one, color).transform;
            PlaceRod(rod, start, end, width);
            return rod;
        }

        static void PlaceRod(Transform rod, Vector3 start, Vector3 end, float width)
        {
            var direction = end - start;
            rod.localPosition = (start + end) * .5f;
            rod.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            rod.localScale = new Vector3(width, direction.magnitude * .5f, width);
        }

        void Update()
        {
            if (body == null) return;
            float time = Time.time;
            float stride = time * 15f + phaseOffset;
            body.localPosition = new Vector3(0, Walking ? Mathf.Abs(Mathf.Sin(stride)) * .014f : Mathf.Sin(time * 2f + phaseOffset) * .004f, 0);
            abdomen.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * (Walking ? 9f : 1.6f) + phaseOffset) * (Walking ? 2.5f : .8f));
            antennae[0].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * 4f + phaseOffset) * 7f);
            antennae[1].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * 4.7f + phaseOffset + 1f) * 7f);
            for (int i = 0; i < 6; i++)
            {
                int side = i < 3 ? -1 : 1, pair = i % 3;
                float tripod = ((side < 0) == (pair != 1)) ? 0f : Mathf.PI;
                float step = Walking ? Mathf.Sin(stride + tripod) : 0f;
                float lift = Walking ? Mathf.Max(0, Mathf.Cos(stride + tripod)) : 0f;
                var knee = restingKnees[i] + new Vector3(side * lift * .018f, step * .027f, -lift * .015f);
                var foot = restingFeet[i] + new Vector3(0, step * .075f, -lift * .043f);
                PlaceRod(upperLegs[i], hips[i], knee, .036f);
                PlaceRod(lowerLegs[i], knee, foot, .027f);
                knees[i].localPosition = knee;
            }
        }
    }
}
