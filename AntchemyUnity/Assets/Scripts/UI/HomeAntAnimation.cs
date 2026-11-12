using UnityEngine;
using UnityEngine.UI;

namespace Antchemy
{
    // Split the welcome art at the wrist so only the hand moves while Home is paused.
    public sealed class HomeAntAnimation : MonoBehaviour
    {
        // Pixel coordinates in HomeAntWelcome.png, measured from the upper-left.
        const int HandLeft = 950;
        const int HandTop = 135;
        const int HandRight = 1130;
        const int HandBottom = 325;
        const int WristX = 1000;
        const int WristY = 310;

        Texture2D bodyTexture;
        Texture2D handTexture;
        RectTransform hand;

        void Start()
        {
            var image = GetComponent<RawImage>();
            var source = image != null ? image.texture as Texture2D : null;
            if (source == null) return;

            Color32[] pixels;
            try { pixels = source.GetPixels32(); }
            catch (UnityException error)
            {
                Debug.LogWarning("Welcome ant hand could not be animated: " + error.Message);
                return;
            }

            int width = source.width, height = source.height;
            int left = Mathf.Clamp(HandLeft, 0, width), right = Mathf.Clamp(HandRight, left, width);
            int top = Mathf.Clamp(HandTop, 0, height), bottom = Mathf.Clamp(HandBottom, top, height);
            int handWidth = right - left, handHeight = bottom - top;
            if (handWidth == 0 || handHeight == 0) return;

            var bodyPixels = (Color32[])pixels.Clone();
            var handPixels = new Color32[handWidth * handHeight];
            for (int y = top; y < bottom; y++)
            {
                int sourceY = height - 1 - y;
                int handY = bottom - 1 - y;
                for (int x = left; x < right; x++)
                {
                    int sourceIndex = sourceY * width + x;
                    handPixels[handY * handWidth + x - left] = pixels[sourceIndex];
                    bodyPixels[sourceIndex].a = 0;
                }
            }

            bodyTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            bodyTexture.name = "Welcome ant still body";
            bodyTexture.SetPixels32(bodyPixels);
            bodyTexture.Apply(false, true);
            image.texture = bodyTexture;

            handTexture = new Texture2D(handWidth, handHeight, TextureFormat.RGBA32, false);
            handTexture.name = "Welcome ant waving hand";
            handTexture.SetPixels32(handPixels);
            handTexture.Apply(false, true);

            var handObject = new GameObject("Waving hand", typeof(RectTransform), typeof(RawImage));
            handObject.transform.SetParent(transform, false);
            var handImage = handObject.GetComponent<RawImage>();
            handImage.texture = handTexture;
            handImage.raycastTarget = false;
            hand = handObject.GetComponent<RectTransform>();
            var art = GetComponent<RectTransform>();
            float scaleX = art.rect.width / width, scaleY = art.rect.height / height;
            hand.anchorMin = hand.anchorMax = new Vector2(.5f, .5f);
            hand.sizeDelta = new Vector2(handWidth * scaleX, handHeight * scaleY);
            hand.pivot = new Vector2((WristX - left) / (float)handWidth, (bottom - WristY) / (float)handHeight);
            hand.anchoredPosition = new Vector2((WristX - width * .5f) * scaleX, (height * .5f - WristY) * scaleY);
        }

        void Update()
        {
            if (hand == null) return;
            hand.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 4f) * 16f);
        }

        void OnDestroy()
        {
            if (bodyTexture != null) Destroy(bodyTexture);
            if (handTexture != null) Destroy(handTexture);
        }
    }
}
