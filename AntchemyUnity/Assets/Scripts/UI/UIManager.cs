using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Antchemy
{
    public partial class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }
        static readonly Color Ink = Hex("63452A"), Green = Hex("C8942A"), Cream = Hex("FFF6E0"), Muted = Hex("A17C51");
        RectTransform root;
        Text levelText, coinsText, progressText, speedText, hintText, modalTitle, modalBody;
        Image progressFill;
        GameObject modal, levelPicker;
        Button modalPrimary;
        Text modalPrimaryText, pageText;
        RectTransform levelContent;
        int levelPage = 1;
        readonly LevelThumbnailAtlas pictureThumbnails = new LevelThumbnailAtlas();
        public const int ExtraNestCoinCost = 100;
        public int CurrentPicturePage => levelPage;
        Font font, fontBold;
        RectTransform modalBox, modalRestart;
        GameObject modalExtraNest;
        public static Font PremiumFont(bool bold) { var f = Resources.Load<Font>(bold ? "Fonts/Poppins-Bold" : "Fonts/Poppins-Medium"); return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        bool fast, paused;
        string modalKind;
        float hintUntil;
        public bool HasOpenModal => AdManager.Instance != null && AdManager.Instance.IsShowing || IsHomeVisible || IsSettingsVisible || modal != null && modal.activeSelf || levelPicker != null && levelPicker.activeSelf;

        void Awake() { Instance = this; Build(); }
        void Start() { UpdateCoins(GameManager.Instance.Coins); RefreshHome(); if (showHomeOnLaunch) OnClickHome(); }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (Input.GetKeyDown(KeyCode.R) && !HasOpenModal) OnClickRetry();
            if (Input.GetKeyDown(KeyCode.Space) && !HasOpenModal) ToggleSpeed();
            if (hintUntil > 0 && Time.unscaledTime > hintUntil) { hintUntil = 0; hintText.text = "Pick a colony. Let the little workers do the rest."; }
        }
        void OnDestroy() { pictureThumbnails.Dispose(); if(glassMaterial!=null) Destroy(glassMaterial); if(buttonGlassMaterial!=null) Destroy(buttonGlassMaterial); if (Instance == this) Instance = null; Time.timeScale = 1; }
        public void OnLevelLoaded(int number, int total)
        {
            HideHome();
            paused = false;
            Time.timeScale = fast ? 2 : 1;
            levelText.text = "LEVEL " + number.ToString("00");
            modal.SetActive(false);
            levelPicker.SetActive(false);
            UpdateProgress(total, total);
            hintText.text = number == 1 ? "Tap a numbered colony to send your ants." : "Choose colors along the outside of the picture.";
            hintUntil = Time.unscaledTime + 8;
            RefreshHome();
            if (showHomeOnLaunch) OnClickHome();
        }
        public void UpdateProgress(int remaining, int total)
        {
            float t = total <= 0 ? 0 : 1f - (float)remaining / total;
            progressFill.fillAmount = t;
            progressText.text = Mathf.RoundToInt(t * 100) + "%";
        }
        public void UpdateCoins(int value)
        {
            if (coinsText != null) { if (coinsText.text != value.ToString("N0")) Pulse(coinsText); coinsText.text = value.ToString("N0"); }
            if (homeCoinsText != null) homeCoinsText.text = value.ToString("N0");
        }
        static void Pulse(Component c) { var b = c.GetComponent<Bump>(); if (b == null) b = c.gameObject.AddComponent<Bump>(); b.Play(); }
        public void ShowWinDialog()
        {
            int bonus = GameManager.Instance != null ? GameManager.Instance.LastBonus : 0;
            string body = "The picture is clear. Your colony earned 50 coins.";
            if(bonus > 0) body += "\nDaily puzzle bonus: +" + bonus + " coins  (streak " + GameManager.Instance.DailyStreak + ")";
            ShowModal("win", "Beautiful teamwork!", body, "NEXT PICTURE");
        }
        public const int ShuffleCoinCost = 60;
        public void OnClickShuffle()
        {
            var level = LevelManager.Instance;
            if(level == null || level.IsLevelEnded) return;
            if(GameManager.Instance == null || GameManager.Instance.Coins < ShuffleCoinCost) { Toast("Shuffle costs " + ShuffleCoinCost + " coins."); return; }
            if(!level.TryShuffleQueues()) { Toast("Nothing to shuffle right now."); return; }
            GameManager.Instance.SpendCoins(ShuffleCoinCost); Toast("Colonies shuffled!");
        }
        public void OnClickDaily()
        {
            var game = GameManager.Instance; if(game == null) return;
            if(AdManager.Instance != null && AdManager.Instance.IsShowing) return;
            showHomeOnLaunch = false; Resume();
            if(game.StartDaily()) HideHome();
        }
        public void ShowLoseDialog(LoseReason reason)
        {
            string message = reason == LoseReason.LockStuck
                ? "The waiting colonies cannot move. Try a different order to open the route."
                : "There is not enough room for the next colony. Try another order or add a nest.";
            ShowModal("lose", "A little traffic jam", message, "TRY AGAIN");
        }
        public void OnClickNextLevel()
        {
            Action next = () => { Resume(); GameManager.Instance.NextLevel(); };
            if(AdManager.Instance != null) AdManager.Instance.Break(next); else next();
        }
        public void OnClickRetry()
        {
            Action again = () => { Resume(); GameManager.Instance.RestartLevel(); };
            if(AdManager.Instance != null) AdManager.Instance.Break(again); else again();
        }
        public void OnClickAddSlot()
        {
            if (LevelManager.Instance == null) return;
            var level=LevelManager.Instance;
            if(!level.CanAddSlot) { Toast("No extra nest is needed for this board."); return; }
            int generation=level.LoadGeneration;
            if(AdManager.MobilePlatform)
            {
                // A nest is only ever granted after the rewarded video is watched to its reward.
                if(AdManager.Instance == null || !AdManager.Instance.TryRewarded(() => ApplyExtraNest(level,generation)))
                    Toast("The video is still loading. Please try again in a moment.");
                return;
            }
#if UNITY_EDITOR
            // Only the Unity editor keeps the free extra-nest tool for development.
            ApplyExtraNest(level,generation);
#else
            Toast("Extra nests unlock by watching a short video on your device.");
#endif
        }
        internal void ApplyExtraNest(LevelManager level,int generation)
        {
            if(level != LevelManager.Instance || level.LoadGeneration != generation || !level.TryAddSlot()) return;
            modal.SetActive(false);
            paused = false;
            Time.timeScale = fast ? 2 : 1;
            Toast("An extra nest gives your colony room to breathe.");
        }
        void ToggleSpeed()
        {
            fast = !fast;
            speedText.text = fast ? "2x" : "1x";
            if (!paused) Time.timeScale = fast ? 2 : 1;
        }
        void TogglePause()
        {
            if(AdManager.Instance != null && AdManager.Instance.IsShowing) return;
            if (IsSettingsDialogVisible) { CloseSettingsDialog(); return; }
            if (IsSettingsVisible) { CloseSettings(); return; }
            if (levelPicker.activeSelf) { ClosePictures(); return; }
            if (IsHomeVisible) return;
            if (modal.activeSelf) { if (modalKind == "pause") Resume(); return; }
            paused = true;
            Time.timeScale = 0;
            ShowModal("pause", "Take a breather", "Your ants will be right here when you return.", "KEEP PLAYING");
        }
        void Resume() { showHomeOnLaunch = false; HideHome(); paused = false; modal.SetActive(false); levelPicker.SetActive(false); GameManager.Instance?.SetPaused(false); Time.timeScale = fast ? 2 : 1; AudioManager.Instance?.PlayMusic("music_gameplay"); }
        void ToggleSound()
        {
            var audio = AudioManager.Instance;
            bool enabled = !(audio.MusicEnabled || audio.SoundEnabled);
            audio.ToggleMusic(enabled);
            audio.ToggleSound(enabled);
            Toast(enabled ? "Sound on" : "Sound off");
        }
        public void ShowToast(string text) { Toast(text); }
        void Toast(string text) { hintText.text = text; hintUntil = Time.unscaledTime + 5; }
        void Build()
        {
            font = PremiumFont(false); fontBold = PremiumFont(true);
            var go = new GameObject("Game UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280); scaler.matchWidthOrHeight = 0;
            root = go.GetComponent<RectTransform>();
            if (FindAnyObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var safe = new GameObject("Safe Area", typeof(RectTransform)); safe.transform.SetParent(root, false);
            root = safe.GetComponent<RectTransform>(); root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.sizeDelta = Vector2.zero;
            safe.AddComponent<SafeArea>();
            gameplayHud = new GameObject("Gameplay HUD", typeof(RectTransform));
            gameplayHud.transform.SetParent(root, false);
            var hud = gameplayHud.GetComponent<RectTransform>(); Stretch(hud);
            BuildGameplayHud(hud);
            BuildModal(); BuildLevelPicker(); BuildHome(); BuildSettings();
        }
        void BuildModal()
        {
            modal = new GameObject("Dialog", typeof(RectTransform), typeof(Image)); modal.transform.SetParent(root,false);
            var rt = modal.GetComponent<RectTransform>(); Stretch(rt); modal.GetComponent<Image>().color = new Color(.17f,.11f,.05f,.62f);
            var box = Panel(rt,"Dialog card",Vector2.zero,new Vector2(596,540),Color.white); modalBox = box;
            { var g = box.gameObject.AddComponent<VerticalGradient>(); g.top = Hex("FFFBF1"); g.bottom = Hex("F8EAD0"); }
            Elevate(box,.35f,10); box.gameObject.AddComponent<PopIn>();
            { var ol = box.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(.86f,.68f,.32f,.6f); ol.effectDistance = new Vector2(1.5f,-1.5f); }
            Image(box,"Gold rule",new Vector2(0,132),new Vector2(120,4),Hex("D4A243"));
            Label(box,"ANT COLONY COLORS",new Vector2(0,160),new Vector2(500,35),17,Muted,true);
            modalTitle = Label(box,"Take a breather",new Vector2(0,90),new Vector2(530,76),36,Ink,true);
            modalBody = Label(box,"",new Vector2(0,11),new Vector2(470,82),22,Muted,false);
            modalPrimary = Pill(box,"KEEP PLAYING",new Vector2(0,-87),new Vector2(462,65),Green,Color.white,()=> { if (modalKind == "win") OnClickNextLevel(); else if (modalKind == "lose") OnClickRetry(); else Resume(); });
            modalPrimaryText = modalPrimary.GetComponentInChildren<Text>();
            modalRestart = Pill(box,"RESTART",new Vector2(-124,-166),new Vector2(222,48),Hex("F3E6C4"),Ink,OnClickRetry).GetComponent<RectTransform>();
            modalExtraNest = Pill(box,AdManager.MobilePlatform ? "WATCH AD +1" : "+ EXTRA NEST",new Vector2(124,-166),new Vector2(222,48),Hex("F3E6C4"),Ink,OnClickAddSlot).gameObject;
            Pill(box,"BACK TO HOME",new Vector2(0,-229),new Vector2(462,44),Hex("F3E6C4"),Ink,OnClickHome);
            modal.SetActive(false);
        }
        void ShowModal(string kind, string title, string body, string button)
        {
            modalKind = kind; modalTitle.text = title; modalBody.text = body; modalPrimaryText.text = button;
            bool won = kind == "win";
            if(modalExtraNest != null) modalExtraNest.SetActive(!won);
            if(modalRestart != null)
            {
                modalRestart.anchoredPosition = new Vector2(won ? 0 : -124, -166);
                modalRestart.sizeDelta = new Vector2(won ? 462 : 222, 48);
                var label = modalRestart.GetComponentInChildren<Text>(); if(label != null) label.rectTransform.sizeDelta = modalRestart.sizeDelta - Vector2.one * 8;
            }
            modal.SetActive(true); modal.transform.SetAsLastSibling();
            if(kind == "win") Confetti.Burst(root);
        }
        void BuildLevelPicker()
        {
            levelPicker = new GameObject("Pictures",typeof(RectTransform),typeof(Image)); levelPicker.transform.SetParent(root,false);
            var rt = levelPicker.GetComponent<RectTransform>(); Stretch(rt); levelPicker.GetComponent<Image>().color = Cream;
            // Rectangle clipping also works when the camera's render target has no stencil buffer.
            var scrollObj = new GameObject("Level scroll",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect)); scrollObj.transform.SetParent(rt,false);
            var scrollRT = scrollObj.GetComponent<RectTransform>(); scrollRT.anchorMin=new Vector2(.5f,0); scrollRT.anchorMax=new Vector2(.5f,1); scrollRT.sizeDelta=new Vector2(640,-410); scrollRT.anchoredPosition=new Vector2(0,-15);
            scrollObj.GetComponent<Image>().color=Color.clear;
            var content = new GameObject("Levels",typeof(RectTransform),typeof(GridLayoutGroup),typeof(ContentSizeFitter)); content.transform.SetParent(scrollRT,false);
            var cr = content.GetComponent<RectTransform>(); cr.anchorMin=new Vector2(0,1);cr.anchorMax=new Vector2(1,1); cr.pivot=new Vector2(.5f,1); cr.sizeDelta=Vector2.zero;
            var grid = content.GetComponent<GridLayoutGroup>(); grid.cellSize=new Vector2(148,180);grid.spacing=new Vector2(16,16);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=4;
            content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            levelContent=cr;
            var scroll=scrollObj.GetComponent<ScrollRect>();scroll.viewport=scrollRT;scroll.content=cr;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            // Navigation is outside the scroll hierarchy and draws above it.
            var header=Panel(rt,"Collection header",Vector2.zero,new Vector2(0,220),Cream,new Vector2(.5f,1));
            header.anchorMin=new Vector2(0,1);header.anchorMax=Vector2.one;header.pivot=new Vector2(.5f,1);header.anchoredPosition=Vector2.zero;
            Label(header,"THE PICTURE COLLECTION",new Vector2(0,-100),new Vector2(650,60),29,Ink,true,new Vector2(.5f,1));
            Label(header,"COMPLETE PICTURES OR WATCH A VIDEO TO UNLOCK",new Vector2(0,-138),new Vector2(640,24),15,Muted,false,new Vector2(.5f,1));
            pageText=Label(header,"1 / 27",new Vector2(0,-178),new Vector2(180,45),22,Muted,true,new Vector2(.5f,1));
            Pill(header,"< PREVIOUS",new Vector2(-210,-178),new Vector2(190,48),Hex("F3E6C4"),Ink,()=>ShowPicturePage(levelPage-1),new Vector2(.5f,1));
            Pill(header,"NEXT >",new Vector2(210,-178),new Vector2(190,48),Hex("F3E6C4"),Ink,()=>ShowPicturePage(levelPage+1),new Vector2(.5f,1));
            var footer=Panel(rt,"Collection footer",Vector2.zero,new Vector2(0,150),Cream,new Vector2(.5f,0));
            footer.anchorMin=Vector2.zero;footer.anchorMax=new Vector2(1,0);footer.pivot=new Vector2(.5f,0);footer.anchoredPosition=Vector2.zero;
            Pill(footer,"BACK TO COLONY",new Vector2(0,70),new Vector2(480,60),Green,Color.white,ClosePictures,new Vector2(.5f,0));
            levelPicker.SetActive(false);
        }
        public void ShowPicturePage(int page)
        {
            int count=GameManager.Instance!=null?GameManager.Instance.LevelCount:2700;
            int pages=Mathf.CeilToInt(count/100f);levelPage=Mathf.Clamp(page,1,pages);
            if(pageText!=null)pageText.text=levelPage+" / "+pages;
            foreach(Transform child in levelContent){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var atlas = pictureThumbnails.GetPage(levelPage);
            int start=(levelPage-1)*100+1;
            for(int n=start;n<start+100&&n<=count;n++)
            {
                int selected=n;
                bool unlocked = GameManager.Instance != null && GameManager.Instance.IsLevelUnlocked(n);
                bool current = GameManager.Instance != null && n == GameManager.Instance.CurrentLevel;
                var button=Pill(levelContent,n.ToString("00"),Vector2.zero,new Vector2(148,180),!unlocked?Hex("F0E9D9"):current?Hex("F3E6C4"):Color.white,unlocked?Ink:Muted,()=>PlayPicture(selected));
                button.gameObject.name="Picture "+n;
                button.interactable = true;
                var colors = button.colors; colors.disabledColor = Color.white; button.colors = colors;
                var number = button.GetComponentInChildren<Text>();
                number.rectTransform.anchoredPosition = new Vector2(0,-69);
                number.rectTransform.sizeDelta = new Vector2(136,30);
                number.fontSize = 20;
                var preview = new GameObject("Puzzle preview",typeof(RectTransform),typeof(RawImage));
                preview.transform.SetParent(button.transform,false);
                Position(preview.GetComponent<RectTransform>(),new Vector2(0,15),new Vector2(128,128),null);
                var picture = preview.GetComponent<RawImage>(); picture.texture = atlas;
                picture.uvRect = LevelThumbnailAtlas.GetUV(n); picture.raycastTarget = false;
                if(!unlocked)
                {
                    picture.color = new Color(1,1,1,.25f);
                    BuildPictureLock(button.GetComponent<RectTransform>());
                }
            }
            var scroll=levelContent.GetComponentInParent<ScrollRect>();if(scroll!=null)scroll.StopMovement();
            levelContent.anchoredPosition=Vector2.zero;
        }
        void OpenLevelPicker() { OpenPictures(); }
        static void BuildPictureLock(RectTransform card)
        {
            var badge = Panel(card,"Locked picture",new Vector2(40,-22),new Vector2(46,46),Cream);
            Image(badge,"Lock shackle",new Vector2(0,6),new Vector2(22,26),Ink);
            Image(badge,"Shackle opening",new Vector2(0,5),new Vector2(12,16),Cream);
            Image(badge,"Lock body",new Vector2(0,-6),new Vector2(30,22),Ink);
            Image(badge,"Keyhole",new Vector2(0,-5),new Vector2(4,7),Cream);
        }
        static void Stretch(RectTransform rt) { rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.sizeDelta=Vector2.zero; }
        static void Position(RectTransform rt, Vector2 pos, Vector2 size, Vector2? anchor)
        { rt.anchorMin=rt.anchorMax=anchor ?? new Vector2(.5f,.5f);rt.anchoredPosition=pos;rt.sizeDelta=size; }
        static Sprite round;
        internal static Sprite RoundedSprite() => Rounded();
        static Sprite Rounded()
        {
            if(round!=null)return round;
            const int s=64;var texture=new Texture2D(s,s,TextureFormat.RGBA32,false);var pixels=new Color[s*s];
            for(int y=0;y<s;y++)for(int x=0;x<s;x++){float dx=Mathf.Max(16-x,x-47),dy=Mathf.Max(16-y,y-47);float d=new Vector2(Mathf.Max(0,dx),Mathf.Max(0,dy)).magnitude;pixels[y*s+x]=new Color(1,1,1,Mathf.Clamp01(16.5f-d));}
            texture.SetPixels(pixels);texture.Apply();texture.wrapMode=TextureWrapMode.Clamp;
            round=Sprite.Create(texture,new Rect(0,0,s,s),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(18,18,18,18));return round;
        }
        static Image Image(RectTransform parent,string name,Vector2 pos,Vector2 size,Color color,Vector2? anchor=null)
        { var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var im=go.GetComponent<Image>();Position(im.rectTransform,pos,size,anchor);im.sprite=Rounded();im.type=UnityEngine.UI.Image.Type.Sliced;im.color=color;im.raycastTarget=false;return im; }
        static RectTransform Panel(RectTransform parent,string name,Vector2 pos,Vector2 size,Color color,Vector2? anchor=null) => Image(parent,name,pos,size,color,anchor).rectTransform;
        Text Label(RectTransform parent,string value,Vector2 pos,Vector2 size,int fontSize,Color color,bool bold=false,Vector2? anchor=null)
        { var go=new GameObject(value,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var t=go.GetComponent<Text>();Position(t.rectTransform,pos,size,anchor);t.text=value;t.font=bold?fontBold:font;t.fontSize=fontSize;t.color=color;t.fontStyle=FontStyle.Normal;if(t.font!=null&&t.font.name=="LegacyRuntime"&&bold)t.fontStyle=FontStyle.Bold;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;t.lineSpacing=.9f;return t; }
        Button Pill(RectTransform parent,string title,Vector2 pos,Vector2 size,Color bg,Color fg,Action action,Vector2? anchor=null)
        { var image=Image(parent,title,pos,size,bg,anchor);image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;colors.highlightedColor=new Color(1f,.95f,.82f);colors.pressedColor=new Color(.86f,.74f,.45f);button.colors=colors;button.onClick.AddListener(()=>action());var label=Label(image.rectTransform,title,Vector2.zero,size-Vector2.one*8,20,fg,true);StylePill(image,label,bg==Green);return button; }
        static bool SameColor(Color a,Color b) => Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)<.02f;
        void StylePill(Image image,Text label,bool primary)
        {
            var rt=image.rectTransform; var size=rt.sizeDelta; var bg=image.color;
            var grad=image.gameObject.AddComponent<VerticalGradient>();
            if(primary)
            {
                image.color=Color.white; grad.top=Hex("F1CF73"); grad.bottom=Hex("B9821C");
                var sh=image.gameObject.AddComponent<Shadow>(); sh.effectColor=new Color(.47f,.3f,.06f,.55f); sh.effectDistance=new Vector2(0,-5);
                var gloss=Image(rt,"Gloss",new Vector2(0,size.y*.21f),new Vector2(size.x-26,size.y*.34f),new Color(1,1,1,.13f));
                gloss.transform.SetSiblingIndex(0);
                ShineSweep.Attach(rt,.5f,UnityEngine.Random.value*1.5f);
                var ts=label.gameObject.AddComponent<Shadow>(); ts.effectColor=new Color(.38f,.22f,.02f,.45f); ts.effectDistance=new Vector2(0,-2);
                label.color=Color.white;
            }
            else
            {
                grad.top=Color.Lerp(Color.white,Color.white*1.02f,.5f); grad.bottom=new Color(.93f,.88f,.8f);
                var sh=image.gameObject.AddComponent<Shadow>(); sh.effectColor=new Color(.55f,.38f,.14f,.2f); sh.effectDistance=new Vector2(0,-4);
                var ol=image.gameObject.AddComponent<Outline>(); ol.effectColor=new Color(.82f,.64f,.3f,.35f); ol.effectDistance=new Vector2(1,-1);
            }
            image.gameObject.AddComponent<ButtonJuice>();
        }
        static void Elevate(RectTransform rt,float alpha=.18f,float distance=6)
        { var sh=rt.gameObject.AddComponent<Shadow>(); sh.effectColor=new Color(.5f,.33f,.1f,alpha); sh.effectDistance=new Vector2(0,-distance); }
        static RectTransform CoinIcon(RectTransform parent,Vector2 pos,float size,Vector2? anchor=null)
        {
            var rim=new GameObject("Coin",typeof(RectTransform),typeof(Image)); rim.transform.SetParent(parent,false);
            var ri=rim.GetComponent<Image>(); ri.sprite=PremiumSprites.Circle(); ri.color=Hex("A8741A"); ri.raycastTarget=false;
            Position(ri.rectTransform,pos,Vector2.one*size,anchor);
            var face=new GameObject("Face",typeof(RectTransform),typeof(Image)); face.transform.SetParent(rim.transform,false);
            var fi=face.GetComponent<Image>(); fi.sprite=PremiumSprites.Circle(); fi.raycastTarget=false;
            Position(fi.rectTransform,new Vector2(0,1),Vector2.one*(size*.8f),null);
            var g=face.AddComponent<VerticalGradient>(); g.top=Hex("FFE79A"); g.bottom=Hex("E0A93A");
            var inner=new GameObject("Inner",typeof(RectTransform),typeof(Image)); inner.transform.SetParent(face.transform,false);
            var ii=inner.GetComponent<Image>(); ii.sprite=PremiumSprites.Circle(); ii.color=new Color(.85f,.6f,.16f,.45f); ii.raycastTarget=false;
            Position(ii.rectTransform,new Vector2(0,-1),Vector2.one*(size*.46f),null);
            var glint=new GameObject("Glint",typeof(RectTransform),typeof(Image)); glint.transform.SetParent(face.transform,false);
            var gi=glint.GetComponent<Image>(); gi.sprite=PremiumSprites.SoftCircle(); gi.color=new Color(1,1,1,.85f); gi.raycastTarget=false;
            Position(gi.rectTransform,new Vector2(-size*.17f,size*.17f),Vector2.one*(size*.34f),null);
            return ri.rectTransform;
        }
        static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#"+hex,out var color);return color; }
    }
    public class SafeArea : MonoBehaviour
    {
        Rect last;Vector2Int size;
        void Update()
        {
            var r=Screen.safeArea; var s=new Vector2Int(Screen.width,Screen.height);if(last==r&&size==s)return;last=r;size=s;
            var rt=(RectTransform)transform; rt.anchorMin=r.position/new Vector2(Screen.width,Screen.height);rt.anchorMax=(r.position+r.size)/new Vector2(Screen.width,Screen.height);rt.offsetMin=rt.offsetMax=Vector2.zero;
        }
    }
}
