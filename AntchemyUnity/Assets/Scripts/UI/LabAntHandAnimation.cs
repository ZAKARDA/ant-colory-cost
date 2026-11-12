using UnityEngine;
using UnityEngine.UI;

namespace Antchemy
{
    // The scientist and cup stay planted while her free hand stirs the liquid.
    public sealed class LabAntHandAnimation : MonoBehaviour
    {
        // Coordinates in LabAntMixing.png, measured from the top-left.
        const int Left = 680, Top = 480, Right = 845, Bottom = 685;
        const int WristX = 692, WristY = 561;
        static readonly Vector2[] ArmEdge =
        {
            new Vector2(755, 490), new Vector2(802, 490), new Vector2(825, 530),
            new Vector2(814, 575), new Vector2(826, 658), new Vector2(808, 660),
            new Vector2(797, 603), new Vector2(757, 600), new Vector2(700, 582),
            new Vector2(691, 558), new Vector2(714, 526)
        };

        Texture2D bodyTexture;
        Texture2D handTexture;
        RectTransform hand;
        float startedAt;

        void Start()
        {
            startedAt = Time.unscaledTime;
            var image = GetComponent<RawImage>();
            var source = image != null ? image.texture as Texture2D : null;
            if (source == null) return;
            Color32[] pixels;
            try { pixels = source.GetPixels32(); }
            catch (UnityException error)
            {
                Debug.LogWarning("Lab ant hand could not be animated: " + error.Message);
                return;
            }

            int width = source.width, height = source.height;
            int left = Mathf.Clamp(Left, 0, width), right = Mathf.Clamp(Right, left, width);
            int top = Mathf.Clamp(Top, 0, height), bottom = Mathf.Clamp(Bottom, top, height);
            if (left == right || top == bottom) return;
            var bodyPixels = (Color32[])pixels.Clone();
            var handPixels = new Color32[(right - left) * (bottom - top)];
            for (int y = top; y < bottom; y++)
            {
                int sourceY = height - 1 - y;
                int handY = bottom - 1 - y;
                for (int x = left; x < right; x++)
                {
                    if (!InsideArm(x, y)) continue;
                    int sourceIndex = sourceY * width + x;
                    handPixels[handY * (right - left) + x - left] = pixels[sourceIndex];
                    bodyPixels[sourceIndex].a = 0;
                }
            }

            bodyTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            bodyTexture.name = "Lab ant still body";
            bodyTexture.SetPixels32(bodyPixels);
            bodyTexture.Apply(false, true);
            image.texture = bodyTexture;

            handTexture = new Texture2D(right - left, bottom - top, TextureFormat.RGBA32, false);
            handTexture.name = "Lab ant stirring hand";
            handTexture.SetPixels32(handPixels);
            handTexture.Apply(false, true);
            var handObject = new GameObject("Mixing hand", typeof(RectTransform), typeof(RawImage));
            handObject.transform.SetParent(transform, false);
            var handImage = handObject.GetComponent<RawImage>();
            handImage.texture = handTexture;
            handImage.raycastTarget = false;
            hand = handObject.GetComponent<RectTransform>();
            var art = GetComponent<RectTransform>();
            float scaleX = art.rect.width / width, scaleY = art.rect.height / height;
            hand.anchorMin = hand.anchorMax = new Vector2(.5f, .5f);
            hand.sizeDelta = new Vector2((right - left) * scaleX, (bottom - top) * scaleY);
            hand.pivot = new Vector2((WristX - left) / (float)(right - left), (bottom - WristY) / (float)(bottom - top));
            hand.anchoredPosition = new Vector2((WristX - width * .5f) * scaleX, (height * .5f - WristY) * scaleY);
        }

        static bool InsideArm(int x, int y)
        {
            bool inside = false;
            for (int i = 0, j = ArmEdge.Length - 1; i < ArmEdge.Length; j = i++)
            {
                Vector2 a = ArmEdge[i], b = ArmEdge[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        void Update()
        {
            if (hand == null) return;
            float elapsed = Time.unscaledTime - startedAt;
            float prepare = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - .15f) / .4f));
            hand.localRotation = Quaternion.Euler(0f, 0f, prepare * Mathf.Sin(elapsed * 9f) * 15f);
        }

        void OnDestroy()
        {
            if (bodyTexture != null) Destroy(bodyTexture);
            if (handTexture != null) Destroy(handTexture);
        }
    }
}
