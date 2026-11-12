using UnityEngine;
using UnityEngine.UI;

namespace Antchemy
{
    public partial class UIManager
    {
        GameObject settingsPanel;
        Button musicSetting, soundSetting;
        GameObject adPrivacyEntry;
        public bool IsSettingsVisible => settingsPanel != null && settingsPanel.activeSelf;

        void BuildSettings()
        {
            settingsPanel = new GameObject("Settings", typeof(RectTransform), typeof(Image));
            settingsPanel.transform.SetParent(root, false);
            var shell = settingsPanel.GetComponent<RectTransform>(); Stretch(shell);
            var bg = settingsPanel.GetComponent<Image>(); bg.color = Color.white; bg.raycastTarget = true;
            { var g = settingsPanel.AddComponent<VerticalGradient>(); g.top = Hex("FFF4DE"); g.bottom = Hex("F1CC8E"); }
            var paperTexture = Resources.Load<Texture2D>("UI/HomePaper");
            if (paperTexture != null)
            {
                var paper = new GameObject("Diamond paper", typeof(RectTransform), typeof(RawImage));
                paper.transform.SetParent(shell, false); Stretch(paper.GetComponent<RectTransform>());
                var raw = paper.GetComponent<RawImage>(); raw.texture = paperTexture;
                raw.uvRect = new Rect(0, 0, 6, 11); raw.color = new Color(1, 1, 1, .32f); raw.raycastTarget = false;
            }
            SparkleField.Create(shell, 22, new Color(1f,.82f,.42f,1));
            var page = ReferencePage.Create(shell, "Settings content");

            // Header
            { var burst = Image(page, "Sunburst", new Vector2(0,-130), new Vector2(620,620), new Color(.98f,.74f,.3f,.22f), new Vector2(.5f,1));
              burst.sprite = PremiumSprites.Sunburst(); burst.type = UnityEngine.UI.Image.Type.Simple; burst.gameObject.AddComponent<Spin>().degreesPerSecond = 4; }
            Label(page, "ANT COLONY COLORS", new Vector2(0,-62), new Vector2(600,30), 17, Muted, true, new Vector2(.5f,1));
            { var t = Label(page, "SETTINGS", new Vector2(0,-128), new Vector2(640,90), 56, Color.white, true, new Vector2(.5f,1));
              var g = t.gameObject.AddComponent<VerticalGradient>(); g.top = Hex("E9B949"); g.bottom = Hex("A06C12");
              var sh = t.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(.45f,.28f,.04f,.25f); sh.effectDistance = new Vector2(0,-3); }
            Image(page, "Ornament left", new Vector2(-96,-188), new Vector2(150,3), new Color(.78f,.58f,.16f,.55f), new Vector2(.5f,1));
            Image(page, "Ornament right", new Vector2(96,-188), new Vector2(150,3), new Color(.78f,.58f,.16f,.55f), new Vector2(.5f,1));
            { var d = Image(page, "Ornament gem", new Vector2(0,-188), new Vector2(14,14), Hex("C8942A"), new Vector2(.5f,1)); d.rectTransform.localRotation = Quaternion.Euler(0,0,45); }
            Label(page, "Make yourself at home.", new Vector2(0,-222), new Vector2(600,36), 22, Muted, false, new Vector2(.5f,1));

            // Audio card
            var card = Panel(page, "Audio settings", new Vector2(0,-352), new Vector2(620,210), Color.white, new Vector2(.5f,1));
            { var g = card.gameObject.AddComponent<VerticalGradient>(); g.top = Hex("FFFEFB"); g.bottom = Hex("FBEFD2"); }
            Elevate(card, .22f, 8);
            { var ol = card.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(.82f,.64f,.3f,.45f); ol.effectDistance = new Vector2(1.5f,-1.5f); }
            SettingBadge(card, new Vector2(-250,52), GlassIcon.Kind.Play);
            Label(card, "Music", new Vector2(-60,64), new Vector2(300,40), 27, Ink, true).alignment = TextAnchor.MiddleLeft;
            Label(card, "Background melodies", new Vector2(-60,28), new Vector2(300,30), 18, Muted).alignment = TextAnchor.MiddleLeft;
            Image(card, "Divider", Vector2.zero, new Vector2(540,2), new Color(.82f,.64f,.3f,.3f));
            SettingBadge(card, new Vector2(-250,-52), GlassIcon.Kind.Sound);
            Label(card, "Sound effects", new Vector2(-60,-40), new Vector2(300,40), 27, Ink, true).alignment = TextAnchor.MiddleLeft;
            Label(card, "Little sounds as you play", new Vector2(-60,-76), new Vector2(300,30), 18, Muted).alignment = TextAnchor.MiddleLeft;
            musicSetting = SettingSwitch(card, "Music setting", new Vector2(222,52), () => {
                var audio = AudioManager.Instance; audio.ToggleMusic(!audio.MusicEnabled); RefreshSettings();
            });
            soundSetting = SettingSwitch(card, "Sound effects setting", new Vector2(222,-52), () => {
                var audio = AudioManager.Instance; audio.ToggleSound(!audio.SoundEnabled); RefreshSettings();
            });

            // Share / Rate
            Pill(page, "SHARE", new Vector2(-160,-512), new Vector2(300,76), Hex("F3E6C4"), Ink, OpenShare, new Vector2(.5f,1)).gameObject.name = "Share game";
            Pill(page, "RATE US", new Vector2(160,-512), new Vector2(300,76), Green, Color.white, RateApp, new Vector2(.5f,1)).gameObject.name = "Rate us";

            // Links
            SettingsLinkRow(page, "PRIVACY POLICY", "Privacy Policy", -602, OpenPrivacyPolicy, "i");
            SettingsLinkRow(page, "TERMS OF USE", "Terms of Use", -680, OpenTerms, "\u00A7");
            SettingsLinkRow(page, "HELP & SUPPORT", "Help and support", -758, OpenSupportPage, "?");
            SettingsLinkRow(page, "CONTACT US BY EMAIL", "Contact us", -836, ContactSupport, "@");
            SettingsLinkRow(page, "OUR WEBSITE", "Website", -914, OpenWebsite, "W");
            adPrivacyEntry=Pill(page,"AD PRIVACY",new Vector2(0,-994),new Vector2(620,56),Hex("F3E6C4"),Ink,()=>AdManager.Instance?.OpenPrivacyOptions(),new Vector2(.5f,1)).gameObject;
            adPrivacyEntry.name="Ad privacy options"; RefreshAdPrivacy();
            Label(page, "ANT COLONY COLORS  /  VERSION " + Application.version, new Vector2(0,195), new Vector2(620,30), 15, Muted, true, new Vector2(.5f,0));
            var back = Pill(page, "BACK TO HOME", new Vector2(0,100), new Vector2(580,84), Green, Color.white, CloseSettings, new Vector2(.5f,0));
            back.gameObject.name = "Settings Back to Home"; back.GetComponentInChildren<Text>().fontSize = 24;
            BuildSettingsDialog(page);
            settingsPanel.SetActive(false);
        }

        void SettingBadge(RectTransform parent, Vector2 pos, GlassIcon.Kind kind)
        {
            var rim = Image(parent, "Badge", pos, Vector2.one * 58, Color.white); rim.sprite = PremiumSprites.Circle(); rim.type = UnityEngine.UI.Image.Type.Simple;
            { var g = rim.gameObject.AddComponent<VerticalGradient>(); g.top = Hex("F6D77A"); g.bottom = Hex("B9821C"); }
            { var sh = rim.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(.47f,.3f,.06f,.4f); sh.effectDistance = new Vector2(0,-3); }
            Icon(rim.transform, kind, Vector2.zero, 30);
            var gi = rim.GetComponentInChildren<GlassIcon>(); if(gi != null) gi.color = Color.white;
        }

        Button SettingSwitch(RectTransform parent, string name, Vector2 pos, System.Action action)
        {
            var track = Image(parent, name, pos, new Vector2(116,54), Color.white); track.raycastTarget = true;
            track.gameObject.AddComponent<VerticalGradient>();
            { var ol = track.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(.5f,.33f,.1f,.35f); ol.effectDistance = new Vector2(1,-1); }
            var button = track.gameObject.AddComponent<Button>(); button.targetGraphic = track; button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => action());
            var state = Label(track.rectTransform, "ON", Vector2.zero, new Vector2(60,30), 15, Color.white, true); state.name = "State";
            var knob = Image(track.rectTransform, "Knob", Vector2.zero, new Vector2(44,44), Color.white); knob.sprite = PremiumSprites.Circle(); knob.type = UnityEngine.UI.Image.Type.Simple;
            { var sh = knob.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(.3f,.18f,.02f,.4f); sh.effectDistance = new Vector2(0,-3); }
            track.gameObject.AddComponent<ButtonJuice>();
            return button;
        }

        void RefreshSettings()
        {
            RefreshAdPrivacy();
            var audio = AudioManager.Instance;
            if(audio == null) return;
            RefreshSetting(musicSetting, audio.MusicEnabled);
            RefreshSetting(soundSetting, audio.SoundEnabled);
        }

        static void RefreshSetting(Button button, bool enabled)
        {
            var track = button.GetComponent<Image>(); var g = button.GetComponent<VerticalGradient>();
            g.top = enabled ? Hex("F6D77A") : Hex("F1E6CC"); g.bottom = enabled ? Hex("B9821C") : Hex("D9C9A4");
            track.SetVerticesDirty();
            var knob = button.transform.Find("Knob") as RectTransform; if(knob != null) knob.anchoredPosition = new Vector2(enabled ? 29 : -29, 0);
            var state = button.transform.Find("State") as RectTransform;
            if(state != null) { state.anchoredPosition = new Vector2(enabled ? -22 : 22, 0); var t = state.GetComponent<Text>(); t.text = enabled ? "ON" : "OFF"; t.color = enabled ? Color.white : Hex("A17C51"); }
        }

        public void OpenSettings()
        {
            OnClickHome();
            homePanel.SetActive(false); homeHero?.SetVisible(false);
            CloseSettingsDialog();
            RefreshSettings();
            settingsPanel.SetActive(true); settingsPanel.transform.SetAsLastSibling();
        }

        public void CloseSettings() { OnClickHome(); }
        public void RefreshAdPrivacy()
        {
            if(adPrivacyEntry != null) adPrivacyEntry.SetActive(AdManager.Instance != null && AdManager.Instance.PrivacyRequired);
        }
    }
}
