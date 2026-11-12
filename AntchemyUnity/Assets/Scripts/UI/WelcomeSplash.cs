using System;
using UnityEngine;
using UnityEngine.UI;

namespace Antchemy
{
    // The cold-start greeting is timed independently of consent and ad availability.
    public sealed class WelcomeSplash : MonoBehaviour
    {
        public const float DurationSeconds = 5f;

        Text prompt;
        RectTransform walker, fill;
        AntRig rig;
        float walkPhase, lookAmount;
        float railWidth;
        Action finished;
        float startedAt;
        Func<int> shouldWait;
        float extraWait;
        const float MaxExtraWait = 10f;

        // shouldWait: 0 = ready to finish, 1 = ads still loading (bounded wait), 2 = a consent dialog is open (waits without limit).
        public static WelcomeSplash Create(Transform parent, Action onFinished = null, Func<int> shouldWait = null)
        {
            var root = new GameObject("Welcome splash", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960);
            scaler.matchWidthOrHeight = 0;

            var background = new GameObject("Warm background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root.transform, false);
            Stretch(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = new Color(.98f, .894f, .749f);

            var paper = Resources.Load<Texture2D>("UI/HomePaper");
            if (paper != null)
            {
                var pattern = new GameObject("Diamond paper", typeof(RectTransform), typeof(RawImage));
                pattern.transform.SetParent(background.transform, false);
                Stretch(pattern.GetComponent<RectTransform>());
                var image = pattern.GetComponent<RawImage>();
                image.texture = paper;
                image.uvRect = new Rect(0, 0, 6, 11);
                image.color = new Color(1, 1, 1, .32f);
                image.raycastTarget = false;
            }

            Label(background.transform, "ANT COLONY COLORS", 259, 36, new Color(.39f, .27f, .16f), true);
            Label(background.transform, "PAINT WITH ANTS", 194, 25, new Color(.72f, .52f, .12f), true);
            var antObject = new GameObject("Welcome ant", typeof(RectTransform), typeof(RawImage));
            antObject.transform.SetParent(background.transform, false);
            var antRect = antObject.GetComponent<RectTransform>();
            antRect.anchorMin = antRect.anchorMax = new Vector2(.5f, .5f);
            antRect.sizeDelta = new Vector2(330, 361);
            antRect.anchoredPosition = new Vector2(0, -12);
            var antImage = antObject.GetComponent<RawImage>();
            antImage.texture = Resources.Load<Texture2D>("UI/LabAntMixing");
            antImage.raycastTarget = false;
            if (antImage.texture != null) antObject.AddComponent<LabAntHandAnimation>();

            // Loading rail: an ant walks along it from 0% to 100% while the splash is shown.
            const float RailWidth = 360;
            var rail = Bar(background.transform, "Loading rail", new Vector2(0, -232), new Vector2(RailWidth, 14), new Color(.55f, .4f, .18f, .18f));
            var fillRect = Bar(rail, "Loading fill", Vector2.zero, new Vector2(0, 14), Color.white);
            fillRect.anchorMin = fillRect.anchorMax = fillRect.pivot = new Vector2(0, .5f);
            { var g = fillRect.gameObject.AddComponent<VerticalGradient>(); g.top = new Color(.95f, .82f, .45f); g.bottom = new Color(.74f, .52f, .12f); }
            var rig = AntRig.Build(rail);
            var ant = rig.root;
            ant.anchorMin = ant.anchorMax = new Vector2(0, .5f);

            var prompt = Label(background.transform, "MIXING THE COLORS...  0%", -268, 22, new Color(.39f, .27f, .16f), true);
            Label(background.transform, "A LITTLE COLOR MAGIC IS ON ITS WAY", -300, 16, new Color(.63f, .49f, .32f), false);

            var splash = root.AddComponent<WelcomeSplash>();
            splash.prompt = prompt;
            splash.walker = ant; splash.fill = fillRect; splash.rig = rig; splash.railWidth = RailWidth;
            splash.finished = onFinished;
            splash.shouldWait = shouldWait;
            splash.startedAt = Time.unscaledTime;
            return splash;
        }

        static RectTransform Bar(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.color = color; img.raycastTarget = false;
            img.sprite = UIManager.RoundedSprite(); img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 2.5f;
            var rt = img.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.sizeDelta = size; rt.anchoredPosition = pos;
            return rt;
        }

        static RectTransform Dot(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.sprite = PremiumSprites.Circle(); img.color = color; img.raycastTarget = false;
            var rt = img.rectTransform; rt.sizeDelta = size; rt.anchoredPosition = pos;
            return rt;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static Text Label(Transform parent, string message, float y, int size, Color color, bool bold)
        {
            var item = new GameObject(message, typeof(RectTransform), typeof(Text));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(640, 60);
            rect.anchoredPosition = new Vector2(0, y);
            var text = item.GetComponent<Text>();
            text.font = UIManager.PremiumFont(true);
            text.text = message;
            text.fontSize = size;
            text.fontStyle = FontStyle.Normal; if (!bold) text.font = UIManager.PremiumFont(false);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        // Timeline: run to 60%, stop and look at the viewer, then run on to 100%.
        const float RunA = 2.0f, Pause = 1.3f, RunB = DurationSeconds - RunA - Pause;
        const float StopAt = .6f;

        void Update()
        {
            float elapsed = Time.unscaledTime - startedAt;
            bool holding = false;
            if (elapsed >= DurationSeconds && shouldWait != null)
            {
                int wait = shouldWait();
                if (wait == 2) holding = true;
                else if (wait == 1)
                {
                    extraWait += Time.unscaledDeltaTime;
                    holding = extraWait < MaxExtraWait;
                }
                if (holding) elapsed = DurationSeconds;
            }
            if (elapsed >= DurationSeconds && !holding)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                var callback = finished;
                finished = null;
                callback?.Invoke();
                return;
            }

            float t, pausePos = -1f;
            if (elapsed < RunA) t = StopAt * Mathf.Clamp01(elapsed / RunA);
            else if (elapsed < RunA + Pause) { t = StopAt; pausePos = (elapsed - RunA) / Pause; }
            else t = StopAt + (1f - StopAt) * Mathf.Clamp01((elapsed - RunA - Pause) / RunB);

            // Look amount eases in over the first 20% of the pause and out over the last 20%.
            float look = pausePos < 0 ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.Min(pausePos / .22f, (1f - pausePos) / .22f, 1f));
            lookAmount = Mathf.MoveTowards(lookAmount, look, Time.unscaledDeltaTime * 8f);
            look = lookAmount;

            int percent = Mathf.RoundToInt(t * 100);
            prompt.text = holding ? "OPENING YOUR COLONY...  100%" : (pausePos >= 0 ? "HELLO!  " : "MIXING THE COLORS...  ") + percent + "%";
            if (fill != null) fill.sizeDelta = new Vector2(railWidth * t, fill.sizeDelta.y);
            if (walker != null)
            {
                float moving = 1f - look;
                walkPhase += Time.unscaledDeltaTime * 15f * moving;
                float bob = Mathf.Abs(Mathf.Sin(walkPhase)) * 1.6f * moving;
                walker.anchoredPosition = new Vector2(railWidth * t - 4, bob);
                if (rig != null) rig.Pose(walkPhase, look, Time.unscaledTime, pausePos);
            }
        }
    }

    // A side-view ant with six jointed legs, a segmented body and expressive eyes and antennae.
    sealed class AntRig
    {
        public RectTransform root, head, nearEye, farEye, nearPupil, farPupil, antA, antATip, antB, antBTip, shadow;
        readonly RectTransform[] upper = new RectTransform[6], lower = new RectTransform[6];
        static readonly float[] legPhase = { 0f, Mathf.PI, 0f, Mathf.PI, 0f, Mathf.PI };

        static RectTransform Ellipse(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.sprite = PremiumSprites.Circle(); img.color = color; img.raycastTarget = false;
            var rt = img.rectTransform; rt.sizeDelta = size; rt.anchoredPosition = pos;
            return rt;
        }

        static RectTransform Limb(Transform parent, string name, Vector2 pos, float length, float thickness, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.sprite = PremiumSprites.Circle(); img.color = color; img.raycastTarget = false;
            var rt = img.rectTransform; rt.pivot = new Vector2(.5f, 1f); rt.sizeDelta = new Vector2(thickness, length); rt.anchoredPosition = pos;
            return rt;
        }

        static void MakeLeg(AntRig r, int i, Transform parent, float hipX, Color color)
        {
            r.upper[i] = Limb(parent, "Leg upper", new Vector2(hipX, 22), 11, 2.6f, color);
            r.lower[i] = Limb(r.upper[i], "Leg lower", new Vector2(0, -10), 12, 2.2f, color);
        }

        static RectTransform Feeler(Transform parent, Vector2 pos, Color color, out RectTransform tip)
        {
            var baseSeg = Limb(parent, "Antenna", pos, 9, 1.8f, color); baseSeg.pivot = new Vector2(.5f, 0f);
            tip = Limb(baseSeg, "Antenna tip", new Vector2(0, 8), 8, 1.6f, color); tip.pivot = new Vector2(.5f, 0f);
            return baseSeg;
        }

        public static AntRig Build(Transform parent)
        {
            var r = new AntRig();
            var dark = new Color(.30f, .16f, .07f); var mid = new Color(.55f, .30f, .12f); var light = new Color(.74f, .46f, .20f);
            var far = new Color(.40f, .23f, .10f);
            var rootGo = new GameObject("Walking ant", typeof(RectTransform));
            r.root = rootGo.GetComponent<RectTransform>();
            r.root.SetParent(parent, false); r.root.sizeDelta = new Vector2(60, 40); r.root.localScale = Vector3.one * 1.25f;
            r.shadow = Ellipse(r.root, "Shadow", new Vector2(-2, 4), new Vector2(54, 7), new Color(.2f, .1f, .02f, .22f));
            // Far-side legs sit behind the body.
            MakeLeg(r, 3, r.root, 11, far); MakeLeg(r, 4, r.root, 5, far); MakeLeg(r, 5, r.root, -1, far);
            // Body: abdomen, narrow waist, thorax.
            Ellipse(r.root, "Abdomen", new Vector2(-18, 25), new Vector2(27, 21), mid);
            Ellipse(r.root, "Abdomen shine", new Vector2(-20, 29), new Vector2(15, 8), light);
            Ellipse(r.root, "Waist", new Vector2(-6, 24), new Vector2(8, 7), dark);
            Ellipse(r.root, "Thorax", new Vector2(3, 25), new Vector2(16, 13), mid);
            Ellipse(r.root, "Thorax shine", new Vector2(3, 28), new Vector2(9, 5), light);
            // Near-side legs in front of the body.
            MakeLeg(r, 0, r.root, 9, dark); MakeLeg(r, 1, r.root, 3, dark); MakeLeg(r, 2, r.root, -3, dark);
            // Head group.
            var headGo = new GameObject("Head", typeof(RectTransform));
            r.head = headGo.GetComponent<RectTransform>(); r.head.SetParent(r.root, false); r.head.anchoredPosition = new Vector2(17, 27);
            r.antB = Feeler(r.head, new Vector2(-1, 5), dark, out r.antBTip);
            Ellipse(r.head, "Mandible", new Vector2(8.5f, -3.5f), new Vector2(5, 3), dark);
            Ellipse(r.head, "Skull", Vector2.zero, new Vector2(17, 15), mid);
            Ellipse(r.head, "Skull shine", new Vector2(-1, 3), new Vector2(9, 4), light);
            r.farEye = Ellipse(r.head, "Far eye", new Vector2(-3, 2), new Vector2(6.5f, 6.5f), new Color(1, 1, 1, 0));
            r.farPupil = Ellipse(r.farEye, "Far pupil", Vector2.zero, new Vector2(3.2f, 3.2f), new Color(.1f, .05f, .02f, 0));
            r.nearEye = Ellipse(r.head, "Near eye", new Vector2(3, 2), new Vector2(7, 7), Color.white);
            r.nearPupil = Ellipse(r.nearEye, "Near pupil", new Vector2(1.2f, -.3f), new Vector2(3.6f, 3.6f), new Color(.1f, .05f, .02f));
            Ellipse(r.nearPupil, "Glint", new Vector2(.7f, .8f), new Vector2(1.3f, 1.3f), Color.white);
            r.antA = Feeler(r.head, new Vector2(3, 6), dark, out r.antATip);
            return r;
        }

        public void Pose(float phase, float look, float time, float pausePos)
        {
            // Walking cycle with alternating tripods; the legs relax as the ant stops.
            float amp = 1f - look;
            for (int i = 0; i < 6; i++)
            {
                float p = phase + legPhase[i];
                float swing = Mathf.Sin(p) * 30f * amp;
                float lift = Mathf.Max(0f, Mathf.Cos(p)) * 38f * amp;
                float rest = (i % 3 == 0 ? 18f : i % 3 == 1 ? 2f : -16f);
                upper[i].localRotation = Quaternion.Euler(0, 0, rest + swing);
                lower[i].localRotation = Quaternion.Euler(0, 0, -34f - lift * .5f + (1f - amp) * 8f);
            }
            // Greeting wave with the near front leg while looking at the viewer.
            if (look > .01f)
            {
                float wave = 62f + Mathf.Sin(time * 11f) * 18f;
                upper[0].localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(upper[0].localEulerAngles.z > 180f ? upper[0].localEulerAngles.z - 360f : upper[0].localEulerAngles.z, wave, look));
                lower[0].localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(lower[0].localEulerAngles.z > 180f ? lower[0].localEulerAngles.z - 360f : lower[0].localEulerAngles.z, -28f, look));
            }
            // Rear the front up a little and turn the face toward the viewer.
            root.localRotation = Quaternion.Euler(0, 0, look * 14f + Mathf.Sin(phase * .5f) * 1.2f * (1f - look));
            head.localRotation = Quaternion.Euler(0, 0, -look * 8f + Mathf.Sin(time * 5f) * 5f * look + Mathf.Sin(phase) * 2f * (1f - look));
            head.localScale = Vector3.one * (1f + .6f * look);
            head.anchoredPosition = new Vector2(17 + 2.5f * look, 27 + 4.5f * look);
            shadow.localRotation = Quaternion.Inverse(root.localRotation);
            // Second eye appears as the face turns forward, and both pupils meet the viewer's gaze.
            farEye.GetComponent<Image>().color = new Color(1, 1, 1, look);
            farPupil.GetComponent<Image>().color = new Color(.1f, .05f, .02f, look);
            nearEye.anchoredPosition = new Vector2(Mathf.Lerp(3f, 2.5f, look), 2f);
            farEye.anchoredPosition = new Vector2(Mathf.Lerp(-1f, -3.2f, look), 2f);
            nearPupil.anchoredPosition = Vector2.Lerp(new Vector2(1.2f, -.3f), new Vector2(0f, -.4f), look);
            farPupil.anchoredPosition = Vector2.zero;
            // A quick blink in the middle of the pause.
            float blink = (pausePos > .5f && pausePos < .6f) ? .1f : 1f;
            nearEye.localScale = new Vector3(1, blink, 1); farEye.localScale = new Vector3(1, blink, 1);
            // Antennae feel the air; they flick more when the ant is excited.
            float wig = 5f + 11f * look;
            antA.localRotation = Quaternion.Euler(0, 0, -52f + Mathf.Sin(time * 9f) * wig);
            antATip.localRotation = Quaternion.Euler(0, 0, 38f + Mathf.Sin(time * 12f + 1f) * wig);
            antB.localRotation = Quaternion.Euler(0, 0, -28f + Mathf.Sin(time * 8f + 2f) * wig);
            antBTip.localRotation = Quaternion.Euler(0, 0, 30f + Mathf.Sin(time * 11f + 3f) * wig);
        }
    }
}
