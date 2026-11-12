using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Antchemy
{
    // Shared procedural sprites for the premium look (no imported art needed).
    public static class PremiumSprites
    {
        static Sprite softCircle, hardCircle, shineBand, sunburst, sparkle;

        public static Sprite SoftCircle()
        {
            if (softCircle != null) return softCircle;
            const int s = 128; var tex = NewTex(s, s);
            var px = new Color[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(s / 2f, s / 2f)) / (s / 2f);
                float a = Mathf.Clamp01(1 - d); a = a * a * (3 - 2 * a);
                px[y * s + x] = new Color(1, 1, 1, a);
            }
            tex.SetPixels(px); tex.Apply();
            return softCircle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(.5f, .5f), 100);
        }

        public static Sprite Circle()
        {
            if (hardCircle != null) return hardCircle;
            const int s = 128; var tex = NewTex(s, s);
            var px = new Color[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(s / 2f, s / 2f));
                px[y * s + x] = new Color(1, 1, 1, Mathf.Clamp01(s / 2f - 1 - d));
            }
            tex.SetPixels(px); tex.Apply();
            return hardCircle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(.5f, .5f), 100);
        }

        public static Sprite ShineBand()
        {
            if (shineBand != null) return shineBand;
            const int w = 64, h = 4; var tex = NewTex(w, h);
            var px = new Color[w * h];
            for (int x = 0; x < w; x++)
            {
                float t = Mathf.Abs(x / (w - 1f) * 2 - 1);
                float a = Mathf.Pow(1 - t, 2.2f);
                for (int y = 0; y < h; y++) px[y * w + x] = new Color(1, 1, 1, a);
            }
            tex.SetPixels(px); tex.Apply();
            return shineBand = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100);
        }

        public static Sprite Sunburst()
        {
            if (sunburst != null) return sunburst;
            const int s = 256; var tex = NewTex(s, s);
            var px = new Color[s * s];
            const int rays = 14;
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                var p = new Vector2(x + .5f - s / 2f, y + .5f - s / 2f);
                float r = p.magnitude / (s / 2f);
                float ang = Mathf.Atan2(p.y, p.x) / (Mathf.PI * 2) * rays;
                float ray = Mathf.Abs(ang - Mathf.Round(ang)) * 2;          // 0 centre of ray .. 1 gap
                float rayA = Mathf.SmoothStep(1, 0, (ray - .35f) / .3f);
                float fade = Mathf.Clamp01(1 - r); fade *= fade;
                px[y * s + x] = new Color(1, 1, 1, rayA * fade);
            }
            tex.SetPixels(px); tex.Apply();
            return sunburst = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(.5f, .5f), 100);
        }

        public static Sprite Sparkle()
        {
            if (sparkle != null) return sparkle;
            const int s = 64; var tex = NewTex(s, s);
            var px = new Color[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Abs(x + .5f - s / 2f) / (s / 2f), dy = Mathf.Abs(y + .5f - s / 2f) / (s / 2f);
                float star = Mathf.Clamp01(1 - (Mathf.Sqrt(dx) + Mathf.Sqrt(dy)) * .9f);   // four-point star
                float core = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy) * 3);
                px[y * s + x] = new Color(1, 1, 1, Mathf.Clamp01(star * 1.6f + core));
            }
            tex.SetPixels(px); tex.Apply();
            return sparkle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(.5f, .5f), 100);
        }

        static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; return t;
        }
    }

    // Tints a graphic's vertices from top to bottom (works with sliced rounded images).
    public sealed class VerticalGradient : BaseMeshEffect
    {
        public Color top = Color.white, bottom = Color.white;
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            var r = ((RectTransform)transform).rect; var v = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                float t = Mathf.InverseLerp(r.yMin, r.yMax, v.position.y);
                v.color = (Color32)((Color)v.color * Color.Lerp(bottom, top, t));
                vh.SetUIVertex(v, i);
            }
        }
    }

    // Springy press feedback and an optional idle "breathing" pulse. Uses unscaled time so it
    // also works on Home, where the game is paused.
    public sealed class ButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float breathe;          // 0 = off, e.g. .02 for the main call to action
        float scale = 1, velocity, target = 1, phase;
        void OnEnable() { phase = Random.value * 6f; scale = 1; velocity = 0; target = 1; }
        public void OnPointerDown(PointerEventData e) { target = .93f; }
        public void OnPointerUp(PointerEventData e) { target = 1; velocity += 2.5f; }
        public void OnPointerExit(PointerEventData e) { target = 1; }
        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1 / 30f);
            velocity += ((target - scale) * 520f - velocity * 20f) * dt;
            scale += velocity * dt;
            float pulse = breathe > 0 ? 1 + Mathf.Sin(Time.unscaledTime * 2.4f + phase) * breathe : 1;
            transform.localScale = Vector3.one * (scale * pulse);
        }
    }

    // A soft light band that sweeps across a button every few seconds.
    public sealed class ShineSweep : MonoBehaviour
    {
        public float interval = 3.2f, duration = .9f;
        RectTransform band; RectTransform area; float startAt;
        public static void Attach(RectTransform button, float alpha = .45f, float delay = 0)
        {
            var mask = new GameObject("Shine area", typeof(RectTransform), typeof(RectMask2D));
            mask.transform.SetParent(button, false);
            var mrt = mask.GetComponent<RectTransform>(); mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
            mrt.offsetMin = new Vector2(6, 4); mrt.offsetMax = new Vector2(-6, -4);
            mask.GetComponent<RectMask2D>().softness = new Vector2Int(10, 6);
            mask.transform.SetSiblingIndex(0);
            var go = new GameObject("Shine", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(mrt, false);
            var img = go.GetComponent<Image>(); img.sprite = PremiumSprites.ShineBand(); img.raycastTarget = false;
            img.color = new Color(1, 1, 1, alpha);
            var rt = img.rectTransform; rt.sizeDelta = new Vector2(70, 240); rt.localRotation = Quaternion.Euler(0, 0, -22);
            var s = mask.AddComponent<ShineSweep>(); s.band = rt; s.area = mrt; s.startAt = Time.unscaledTime + 1 + delay;
        }
        void Update()
        {
            if (band == null) return;
            float w = area.rect.width;
            float t = (Time.unscaledTime - startAt) % interval;
            if (Time.unscaledTime < startAt) t = interval;
            float k = Mathf.Clamp01(t / duration);
            band.anchoredPosition = new Vector2(Mathf.Lerp(-w / 2 - 80, w / 2 + 80, k * k * (3 - 2 * k)), 0);
            band.gameObject.SetActive(t < duration);
        }
    }

    // Slow rotation (sunburst) and gentle bobbing helpers.
    public sealed class Spin : MonoBehaviour
    {
        public float degreesPerSecond = 6;
        void Update() { transform.Rotate(0, 0, degreesPerSecond * Mathf.Min(Time.unscaledDeltaTime, .1f)); }
    }

    // Golden sparkles that drift upward and twinkle behind the Home content.
    public sealed class SparkleField : MonoBehaviour
    {
        class P { public RectTransform rt; public Image img; public float speed, phase, size, drift; }
        readonly List<P> items = new List<P>();
        RectTransform area;
        public static SparkleField Create(RectTransform parent, int count, Color tint)
        {
            var go = new GameObject("Sparkles", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            var field = go.AddComponent<SparkleField>(); field.area = rt;
            for (int i = 0; i < count; i++)
            {
                var s = new GameObject("Sparkle", typeof(RectTransform), typeof(Image)); s.transform.SetParent(rt, false);
                var img = s.GetComponent<Image>(); img.sprite = i % 3 == 0 ? PremiumSprites.Sparkle() : PremiumSprites.SoftCircle();
                img.raycastTarget = false; img.color = tint;
                var p = new P { rt = img.rectTransform, img = img, speed = Random.Range(10f, 28f), phase = Random.value * 10, size = Random.Range(8f, 22f), drift = Random.Range(-8f, 8f) };
                p.rt.anchorMin = p.rt.anchorMax = new Vector2(Random.value, Random.value);
                p.rt.sizeDelta = Vector2.one * p.size; field.items.Add(p);
            }
            return field;
        }
        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f), h = Mathf.Max(1, area.rect.height), now = Time.unscaledTime;
            foreach (var p in items)
            {
                var a = p.rt.anchorMin;
                a.y += p.speed * dt / h; a.x += p.drift * dt / Mathf.Max(1, area.rect.width) * Mathf.Sin(now * .6f + p.phase);
                if (a.y > 1.03f) { a.y = -.03f; a.x = Random.value; }
                p.rt.anchorMin = p.rt.anchorMax = a;
                var c = p.img.color; c.a = (.25f + .55f * (.5f + .5f * Mathf.Sin(now * 2.2f + p.phase))) * Mathf.Clamp01(Mathf.Min(a.y, 1 - a.y) * 8);
                p.img.color = c;
                p.rt.localRotation = Quaternion.Euler(0, 0, now * 25 + p.phase * 30);
            }
        }
    }

    // Scales a card in with a little overshoot every time it is shown.
    public sealed class PopIn : MonoBehaviour
    {
        float t;
        CanvasGroup group;
        void OnEnable() { t = 0; if (group == null) { group = gameObject.GetComponent<CanvasGroup>(); if (group == null) group = gameObject.AddComponent<CanvasGroup>(); } Apply(); }
        void Update() { if (t >= 1) return; t = Mathf.Min(1, t + Mathf.Min(Time.unscaledDeltaTime, 1 / 30f) / .38f); Apply(); }
        void Apply()
        {
            float c = 1.70158f * 1.4f, x = t - 1;
            float s = 1 + (c + 1) * x * x * x + c * x * x;        // ease-out-back
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(.82f, 1f, s);
            if (group != null) group.alpha = Mathf.Clamp01(t * 2.2f);
        }
    }

    // A celebratory burst of confetti over the whole screen.
    public sealed class Confetti : MonoBehaviour
    {
        class Bit { public RectTransform rt; public Vector2 v; public float spin, life; public Image img; }
        readonly List<Bit> bits = new List<Bit>();
        float age;
        static readonly Color[] Colors =
        {
            new Color(.93f,.74f,.27f), new Color(1f,.86f,.45f), new Color(.98f,.55f,.38f),
            new Color(.55f,.78f,.62f), new Color(.52f,.66f,.93f), new Color(.95f,.6f,.78f), Color.white
        };
        public static void Burst(RectTransform parent, int count = 90)
        {
            var go = new GameObject("Confetti", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            go.transform.SetAsLastSibling();
            var c = go.AddComponent<Confetti>();
            for (int i = 0; i < count; i++)
            {
                var b = new GameObject("Bit", typeof(RectTransform), typeof(Image)); b.transform.SetParent(rt, false);
                var img = b.GetComponent<Image>(); img.raycastTarget = false; img.color = Colors[i % Colors.Length];
                if (i % 4 == 0) img.sprite = PremiumSprites.Circle();
                var brt = img.rectTransform; brt.sizeDelta = i % 4 == 0 ? Vector2.one * 12 : new Vector2(9, 16);
                bool left = i % 2 == 0;
                brt.anchoredPosition = new Vector2(left ? -300 : 300, -420);
                float ang = (left ? Random.Range(55f, 85f) : Random.Range(95f, 125f)) * Mathf.Deg2Rad;
                float speed = Random.Range(900f, 1500f);
                c.bits.Add(new Bit { rt = brt, img = img, v = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * speed, spin = Random.Range(-540f, 540f), life = Random.Range(2.2f, 3.2f) });
            }
        }
        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1 / 30f); age += dt;
            foreach (var b in bits)
            {
                b.v.y -= 1500f * dt; b.v *= 1 - 1.6f * dt;
                b.rt.anchoredPosition += b.v * dt;
                b.rt.localRotation = Quaternion.Euler(0, 0, b.rt.localEulerAngles.z + b.spin * dt);
                b.rt.localScale = new Vector3(Mathf.Abs(Mathf.Sin(age * 6 + b.spin)), 1, 1);
                var col = b.img.color; col.a = Mathf.Clamp01((b.life - age) * 2); b.img.color = col;
            }
            if (age > 3.4f) Destroy(gameObject);
        }
    }

    // Bounces a counter when its value changes.
    public sealed class Bump : MonoBehaviour
    {
        float t = 1;
        public void Play() { t = 0; }
        void Update()
        {
            if (t >= 1) return;
            t = Mathf.Min(1, t + Mathf.Min(Time.unscaledDeltaTime, 1 / 30f) / .35f);
            transform.localScale = Vector3.one * (1 + Mathf.Sin(t * Mathf.PI) * .18f);
        }
    }
}
