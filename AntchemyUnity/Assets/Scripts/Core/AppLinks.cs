using System;
using System.Net.Mail;
using UnityEngine;

namespace Antchemy
{
    [Serializable]
    public sealed class AppLinkSettings
    {
        public string storeUrl = "";
        public string privacyPolicyUrl = "";
        public string termsUrl = "";
        public string websiteUrl = "";
        public string supportUrl = "";
        public string supportEmail = "";
        public string shareMessage = "Play Ant Colony Colors! Guide your ants and paint the picture.";
    }

    // Release destinations live in one resource rather than being hardcoded into buttons.
    public static class AppLinks
    {
        static AppLinkSettings settings;
        public static AppLinkSettings Settings
        {
            get
            {
                if(settings == null)
                {
                    var asset = Resources.Load<TextAsset>("AppLinks");
                    settings = asset != null ? JsonUtility.FromJson<AppLinkSettings>(asset.text) : new AppLinkSettings();
                    if(settings == null) settings = new AppLinkSettings();
                }
                return settings;
            }
        }

        public static bool TryWebUrl(string value, out string url)
        {
            url = null;
            if(!Uri.TryCreate(value == null ? "" : value.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
                string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo)) return false;
            url = uri.AbsoluteUri;
            return true;
        }

        public static string ShareText()
        {
            string message = string.IsNullOrWhiteSpace(Settings.shareMessage) ? "Come play Ant Colony Colors!" : Settings.shareMessage.Trim();
            return TryWebUrl(Settings.storeUrl, out string url) ? message + "\n\n" + url : message;
        }

        public static bool TryContactUrl(string email, out string url)
        {
            url = null;
            if(string.IsNullOrWhiteSpace(email) || email.Contains("\r") || email.Contains("\n")) return false;
            email = email.Trim();
            try { if(new MailAddress(email).Address != email || !email.Contains("@")) return false; }
            catch(FormatException) { return false; }
            string subject = "Ant Colony Colors support";
            string body = "Hello Ant Colony Colors team,\n\n\n\nApp version: " + Application.version + "\nPlatform: " + Application.platform;
            url = "mailto:" + Uri.EscapeDataString(email) + "?subject=" + Uri.EscapeDataString(subject) + "&body=" + Uri.EscapeDataString(body);
            return true;
        }
    }
}
