using System;
using UnityEngine;
using UnityEngine.UI;

namespace Antchemy
{
    public partial class UIManager
    {
        GameObject settingsDialog;
        Text settingsDialogTitle, settingsDialogBody, settingsDialogActionText;
        Button settingsDialogAction, settingsDialogBack;
        Action settingsDialogCallback;
        public bool IsSettingsDialogVisible => settingsDialog != null && settingsDialog.activeInHierarchy;

        void SettingsLinkRow(RectTransform parent, string title, string name, float y, Action action, string glyph = "")
        {
            var button = Pill(parent, title, new Vector2(0,y), new Vector2(620,66), Color.white, Ink, action, new Vector2(.5f,1));
            button.gameObject.name = name;
            var rt = button.GetComponent<RectTransform>();
            var text = button.GetComponentInChildren<Text>();
            text.rectTransform.sizeDelta = new Vector2(420,56); text.rectTransform.anchoredPosition = new Vector2(-14,0);
            text.alignment = TextAnchor.MiddleLeft; text.fontSize = 20;
            var badge = Image(rt, "Badge", new Vector2(-268,0), Vector2.one * 42, Color.white); badge.sprite = PremiumSprites.Circle(); badge.type = UnityEngine.UI.Image.Type.Simple;
            { var g = badge.gameObject.AddComponent<VerticalGradient>(); g.top = Hex("F6D77A"); g.bottom = Hex("B9821C"); }
            Label(badge.rectTransform, glyph, Vector2.zero, new Vector2(40,40), 22, Color.white, true);
            Label(rt, ">", new Vector2(270,0), new Vector2(32,40), 30, Hex("C8942A"), true);
        }

        void BuildSettingsDialog(RectTransform page)
        {
            settingsDialog = new GameObject("Settings message", typeof(RectTransform), typeof(Image));
            settingsDialog.transform.SetParent(page, false);
            var overlay = settingsDialog.GetComponent<RectTransform>(); Stretch(overlay);
            settingsDialog.GetComponent<Image>().color = new Color(.17f,.11f,.05f,.62f);
            var box = Panel(overlay, "Message card", Vector2.zero, new Vector2(600,470), Cream);
            settingsDialogTitle = Label(box, "SHARE THE GAME", new Vector2(0,153), new Vector2(540,55), 30, Ink, true);
            settingsDialogBody = Label(box, "", new Vector2(0,23), new Vector2(510,170), 21, Ink);
            settingsDialogAction = Pill(box, "COPY MESSAGE", new Vector2(0,-105), new Vector2(510,66), Green, Color.white, () => settingsDialogCallback?.Invoke());
            settingsDialogAction.gameObject.name = "Settings message action";
            settingsDialogActionText = settingsDialogAction.GetComponentInChildren<Text>();
            settingsDialogBack = Pill(box, "BACK TO SETTINGS", new Vector2(0,-184), new Vector2(510,44), Hex("F3E6C4"), Ink, CloseSettingsDialog);
            settingsDialogBack.gameObject.name = "Settings message Back";
            settingsDialog.SetActive(false);
        }

        void ShowSettingsMessage(string title, string body, bool share = false)
        {
            settingsDialogTitle.text = title; settingsDialogBody.text = body;
            settingsDialogActionText.text = share ? "COPY MESSAGE" : "BACK TO SETTINGS";
            settingsDialogBack.gameObject.SetActive(share);
            settingsDialogCallback = share ? (Action)CopyShareMessage : CloseSettingsDialog;
            settingsDialog.SetActive(true); settingsDialog.transform.SetAsLastSibling();
        }

        void CopyShareMessage()
        {
            GUIUtility.systemCopyBuffer = AppLinks.ShareText();
            settingsDialogActionText.text = "COPIED!";
            settingsDialogBody.text = "Your message is copied. Paste it into your favorite app to share it with a friend.";
        }

        void CloseSettingsDialog()
        {
            if(settingsDialog != null) settingsDialog.SetActive(false);
            settingsDialogCallback = null;
        }

        public void OpenShare() => ShowSettingsMessage("SHARE THE GAME", AppLinks.ShareText(), true);

        void RateApp()
        {
            if(AppLinks.TryWebUrl(AppLinks.Settings.storeUrl, out string url)) Application.OpenURL(url);
            else ShowSettingsMessage("RATE US", "The store page is not available yet.");
        }

        void OpenPrivacyPolicy()
        {
            if(AppLinks.TryWebUrl(AppLinks.Settings.privacyPolicyUrl, out string url)) Application.OpenURL(url);
            else ShowSettingsMessage("PRIVACY POLICY", "The privacy policy link is not available yet.");
        }

        void OpenTerms()
        {
            if(AppLinks.TryWebUrl(AppLinks.Settings.termsUrl, out string url)) Application.OpenURL(url);
            else ShowSettingsMessage("TERMS OF USE", "The terms link is not available yet.");
        }

        void OpenSupportPage()
        {
            if(AppLinks.TryWebUrl(AppLinks.Settings.supportUrl, out string url)) Application.OpenURL(url);
            else ShowSettingsMessage("HELP & SUPPORT", "The support page is not available yet.");
        }

        void OpenWebsite()
        {
            if(AppLinks.TryWebUrl(AppLinks.Settings.websiteUrl, out string url)) Application.OpenURL(url);
            else ShowSettingsMessage("OUR WEBSITE", "The website link is not available yet.");
        }

        void ContactSupport()
        {
            if(AppLinks.TryContactUrl(AppLinks.Settings.supportEmail, out string url)) Application.OpenURL(url);
            else ShowSettingsMessage("CONTACT US", "Support contact details are not available yet.");
        }
    }
}
