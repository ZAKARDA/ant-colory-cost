using UnityEngine;
using UnityEngine.UI;

namespace Antchemy
{
    public partial class UIManager
    {
        GameObject homePanel, gameplayHud;
        Text homeCoinsText, homeLevelText, homePlayText, homeDailyText, homeCountText;
        HomeHero homeHero;
        bool showHomeOnLaunch = true, pickerFromHome;
        public bool IsHomeVisible => homePanel != null && homePanel.activeSelf;
        public bool IsPicturePickerVisible => levelPicker != null && levelPicker.activeSelf;

        void BuildHome()
        {
            homePanel = new GameObject("Home", typeof(RectTransform), typeof(Image));
            homePanel.transform.SetParent(root, false);
            var home = homePanel.GetComponent<RectTransform>(); Stretch(home);
            var background = homePanel.GetComponent<Image>();
            background.color = Color.white; background.raycastTarget = true;
            { var g = homePanel.AddComponent<VerticalGradient>(); g.top = Hex("FFF4DE"); g.bottom = Hex("F1CC8E"); }
            // Reuse the game's generated diamond paper, with a softer home-screen tint.
            var paperTexture = Resources.Load<Texture2D>("UI/HomePaper");
            if (paperTexture != null)
            {
                var paper = new GameObject("Diamond paper", typeof(RectTransform), typeof(RawImage));
                paper.transform.SetParent(home, false); Stretch(paper.GetComponent<RectTransform>());
                var raw = paper.GetComponent<RawImage>(); raw.texture = paperTexture;
                raw.uvRect = new Rect(0, 0, 6, 11); raw.color = new Color(1, 1, 1, .32f); raw.raycastTarget = false;
            }
            SparkleField.Create(home, 28, new Color(1f,.82f,.42f,1));
            home=ReferencePage.Create(home,"Home content");
            var settings = Pill(home, "SETTINGS", new Vector2(116,-77), new Vector2(166,56), Cream, Ink, OpenSettings, new Vector2(0,1));
            settings.gameObject.name = "Open Settings";
            settings.GetComponentInChildren<Text>().fontSize = 17;
            var coin = Panel(home, "Home coins", new Vector2(-110,-77), new Vector2(160,56), Color.white, new Vector2(1,1));
            { var g = coin.gameObject.AddComponent<VerticalGradient>(); g.top = Hex("FFFBF2"); g.bottom = Hex("F6E6C6"); }
            Elevate(coin); CoinIcon(coin, new Vector2(-48,0), 34);
            homeCoinsText = Label(coin, "250", new Vector2(18,0), new Vector2(100,40), 24, Ink, true);
            Label(home, "YOUR LITTLE HAPPY PLACE", new Vector2(0, -173), new Vector2(600,32), 17, Muted, true, new Vector2(.5f,1));
            { var t = Label(home, "ANT COLONY", new Vector2(0, -247), new Vector2(660,110), 62, Ink, true, new Vector2(.5f,1));
              var sh = t.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(1,1,1,.7f); sh.effectDistance = new Vector2(0,-3); }
            { var t = Label(home, "COLORS", new Vector2(0, -330), new Vector2(640,105), 60, Color.white, true, new Vector2(.5f,1));
              var g = t.gameObject.AddComponent<VerticalGradient>(); g.top = Hex("E9B949"); g.bottom = Hex("A06C12");
              var sh = t.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(.45f,.28f,.04f,.25f); sh.effectDistance = new Vector2(0,-3); }
            Label(home, "Paint the picture, one ant at a time", new Vector2(0, -415), new Vector2(600,42), 23, Muted, false, new Vector2(.5f,1));
            var heroObject = new GameObject("Character showcase", typeof(RectTransform));
            heroObject.transform.SetParent(home, false);
            var heroCard = heroObject.GetComponent<RectTransform>();
            Position(heroCard, new Vector2(0,10), new Vector2(550,330), null);
            Label(heroCard, "WELCOME TO YOUR COLONY!", new Vector2(0,-137), new Vector2(510,28), 14, Muted, true);
            { var burst = Image(heroCard, "Sunburst", new Vector2(0,25), new Vector2(640,640), new Color(.98f,.74f,.3f,.30f));
              burst.sprite = PremiumSprites.Sunburst(); burst.type = UnityEngine.UI.Image.Type.Simple; burst.gameObject.AddComponent<Spin>().degreesPerSecond = 5;
              var glow = Image(heroCard, "Glow", new Vector2(0,25), new Vector2(400,400), new Color(1f,.96f,.82f,.9f));
              glow.sprite = PremiumSprites.SoftCircle(); glow.type = UnityEngine.UI.Image.Type.Simple; }
            var heroImage = new GameObject("Colony character", typeof(RectTransform), typeof(RawImage));
            heroImage.transform.SetParent(heroCard, false);
            Position(heroImage.GetComponent<RectTransform>(),new Vector2(0,25),new Vector2(420,280),null);
            var preview = heroImage.GetComponent<RawImage>(); preview.raycastTarget = false;
            var antArt = Resources.Load<Texture2D>("UI/HomeAntWelcome");
            if (antArt != null)
            {
                preview.texture = antArt;
                heroImage.AddComponent<HomeAntAnimation>();
            }
            else homeHero = HomeHero.Create(preview, transform);
            var level = Panel(home, "Current picture", new Vector2(0,-186), new Vector2(340,42), Hex("FFF8E8"));
            { var ol = level.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(.83f,.64f,.26f,.5f); ol.effectDistance = new Vector2(1,-1); }
            homeLevelText = Label(level, "LEVEL 01", Vector2.zero, new Vector2(310,38), 18, Ink, true);
            var play = Pill(home, "PLAY", new Vector2(0,-265), new Vector2(550,92), Green, Color.white, OnClickPlay);
            play.gameObject.name = "Play"; homePlayText = play.GetComponentInChildren<Text>(); homePlayText.fontSize = 32;
            play.GetComponent<ButtonJuice>().breathe = .018f;
            var pictures = Pill(home, "PICTURE COLLECTION", new Vector2(0,-367), new Vector2(550,72), Cream, Ink, OpenPictures);
            pictures.gameObject.name = "Picture Collection"; pictures.GetComponentInChildren<Text>().fontSize = 20;
            var daily = Pill(home, "DAILY PUZZLE", new Vector2(0,-445), new Vector2(550,64), Hex("F2D58A"), Ink, OnClickDaily);
            daily.gameObject.name = "Daily Puzzle"; homeDailyText = daily.GetComponentInChildren<Text>(); homeDailyText.fontSize = 20;
            homeCountText = Label(home, "colorful puzzles to discover", new Vector2(0,-510), new Vector2(590,34), 18, Muted);
            Label(home, "TAKE A BREATH. FIND YOUR FLOW.", new Vector2(0,57), new Vector2(620,28), 14, Muted, false, new Vector2(.5f,0));
            homePanel.SetActive(false); homeHero?.SetVisible(false);
        }
        void RefreshHome()
        {
            var game = GameManager.Instance;
            if(game == null || homeLevelText == null) return;
            int number = game.CurrentLevel;
            var level = LevelManager.Instance;
            if(level != null && level.IsLevelEnded && PictureGrid.Instance != null && PictureGrid.Instance.TotalRemainingCubes == 0)
                number = number % game.LevelCount + 1;
            homeLevelText.text = "YOUR NEXT PICTURE  /  LEVEL " + number.ToString("00");
            homeCoinsText.text = game.Coins.ToString("N0");
            if(homeCountText != null) homeCountText.text = game.LevelCount.ToString("N0") + " hand-made puzzles to discover";
            if(homeDailyText != null) homeDailyText.text = game.DailyAvailable ? "DAILY PUZZLE  +" + GameManager.DailyBonusBase + " COINS" : "DAILY DONE  /  STREAK " + game.DailyStreak;
            homePlayText.text = number == 1 ? "LET'S PLAY" : "CONTINUE";
        }
        public void OnClickHome()
        {
            if(homePanel == null) return;
            if(settingsPanel != null) settingsPanel.SetActive(false);
            modal.SetActive(false); levelPicker.SetActive(false);
            gameplayHud.SetActive(false); homePanel.SetActive(true); homePanel.transform.SetAsLastSibling();
            paused = true; Time.timeScale = 0;
            GameManager.Instance?.EnterMainMenu();
            RefreshHome(); homeHero?.SetVisible(true);
            AudioManager.Instance?.PlayMusic("music_home");
        }
        void HideHome()
        {
            if(settingsPanel != null) settingsPanel.SetActive(false);
            if(homePanel != null) homePanel.SetActive(false);
            if(gameplayHud != null) gameplayHud.SetActive(true);
            if(homeHero != null) homeHero.SetVisible(false);
        }
        public void OnClickPlay()
        {
            if(AdManager.Instance != null && AdManager.Instance.IsShowing) return;
            showHomeOnLaunch = false;
            var level = LevelManager.Instance;
            bool completed = level != null && level.IsLevelEnded && PictureGrid.Instance.TotalRemainingCubes == 0;
            bool failed = level != null && level.IsLevelEnded && !completed;
            if(completed) { OnClickNextLevel(); return; }
            Resume();
            if(failed) GameManager.Instance.RestartLevel();
        }
        public void OpenPictures()
        {
            pickerFromHome = IsHomeVisible;
            ShowPicturePage((GameManager.Instance.CurrentLevel-1)/100+1);
            paused = true; Time.timeScale = 0;
            homeHero?.SetVisible(false);
            levelPicker.SetActive(true); levelPicker.transform.SetAsLastSibling();
        }
        public void ClosePictures()
        {
            levelPicker.SetActive(false);
            if(pickerFromHome) OnClickHome(); else Resume();
        }
        GameObject unlockDialog; Text unlockTitle, unlockBody; int unlockLevel;
        const string UnlockBodyText = "Watch a short video to unlock this picture right now.";
        void OpenUnlockDialog(int number)
        {
            unlockLevel = number;
            if(unlockDialog == null)
            {
                unlockDialog = new GameObject("Unlock dialog", typeof(RectTransform), typeof(Image)); unlockDialog.transform.SetParent(root, false);
                var rt = unlockDialog.GetComponent<RectTransform>(); Stretch(rt);
                var shade = unlockDialog.GetComponent<Image>(); shade.color = new Color(.17f,.11f,.05f,.62f); shade.raycastTarget = true;
                var box = Panel(rt, "Unlock card", Vector2.zero, new Vector2(560,420), Color.white);
                { var g = box.gameObject.AddComponent<VerticalGradient>(); g.top = Hex("FFFBF1"); g.bottom = Hex("F8EAD0"); }
                Elevate(box,.35f,10); box.gameObject.AddComponent<PopIn>();
                { var ol = box.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(.86f,.68f,.32f,.6f); ol.effectDistance = new Vector2(1.5f,-1.5f); }
                var badge = Image(box, "Badge", new Vector2(0,148), new Vector2(84,84), Color.white); badge.sprite = PremiumSprites.Circle(); badge.type = UnityEngine.UI.Image.Type.Simple;
                { var g = badge.gameObject.AddComponent<VerticalGradient>(); g.top = Hex("F1CF73"); g.bottom = Hex("B9821C"); }
                { var ic = new GameObject("Play icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(GlassIcon)); ic.transform.SetParent(badge.transform, false);
                  Position(ic.GetComponent<RectTransform>(), new Vector2(2,0), Vector2.one*46, null);
                  var gi = ic.GetComponent<GlassIcon>(); gi.icon = GlassIcon.Kind.Play; gi.color = Color.white; gi.raycastTarget = false; }
                unlockTitle = Label(box, "Unlock picture", new Vector2(0,72), new Vector2(500,56), 32, Ink, true);
                unlockBody = Label(box, UnlockBodyText, new Vector2(0,12), new Vector2(460,70), 20, Muted);
                Pill(box, "WATCH VIDEO TO UNLOCK", new Vector2(0,-70), new Vector2(460,68), Green, Color.white, OnClickUnlockWithVideo);
                Pill(box, "NOT NOW", new Vector2(0,-150), new Vector2(460,48), Hex("F3E6C4"), Ink, () => unlockDialog.SetActive(false));
            }
            unlockTitle.text = "Unlock picture " + number.ToString("00") + "?";
            unlockBody.text = UnlockBodyText;
            unlockDialog.SetActive(true); unlockDialog.transform.SetAsLastSibling();
        }
        void GrantUnlock(int number)
        {
            if(GameManager.Instance == null) return;
            GameManager.Instance.UnlockLevel(number);
            if(unlockDialog != null) unlockDialog.SetActive(false);
            if(IsPicturePickerVisible) ShowPicturePage(levelPage);
            Confetti.Burst(root, 60);
            Toast("Picture " + number.ToString("00") + " unlocked!");
        }
        void OnClickUnlockWithVideo()
        {
            int number = unlockLevel;
            if(!AdManager.MobilePlatform) { GrantUnlock(number); return; }   // editor/desktop: free for testing
            if(AdManager.Instance == null || !AdManager.Instance.TryRewarded(() => GrantUnlock(number)))
                unlockBody.text = "No video is ready yet. Please try again in a moment.";
        }
        public void PlayPicture(int number)
        {
            if(!GameManager.Instance.IsLevelUnlocked(number)) { OpenUnlockDialog(number); return; }
            showHomeOnLaunch = false;
            Resume(); GameManager.Instance.SelectLevel(number);
        }
    }
}
