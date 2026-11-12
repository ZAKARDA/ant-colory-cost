using System;
using UnityEngine;
using UnityEngine.UI;

namespace Antchemy
{
    public partial class UIManager
    {
        RectTransform hudRoot,nestsCaption,queueCaption,speedControl;
        Material glassMaterial,buttonGlassMaterial;
        Text toolbarSound;
        static readonly Color GlassInk=new Color(.33f,.23f,.12f);

        void BuildGameplayHud(RectTransform hud)
        {
            hudRoot=hud;
            glassMaterial=new Material(Resources.Load<Shader>("UI/GlassUI"));
            glassMaterial.SetFloat("_Frost",.66f);
            buttonGlassMaterial=new Material(glassMaterial); buttonGlassMaterial.SetFloat("_Frost",.88f);
            var header=new GameObject("Game header",typeof(RectTransform)).GetComponent<RectTransform>(); header.SetParent(hud,false);
            header.anchorMin=new Vector2(0,1); header.anchorMax=Vector2.one;
            header.sizeDelta=new Vector2(0,GameplayViewport.HeaderHeight); header.anchoredPosition=new Vector2(0,-GameplayViewport.HeaderHeight/2);
            Label(header,"ANT COLONY COLORS",new Vector2(0,46),new Vector2(300,18),12,Hex("B98A2E"),true);
            levelText=Label(header,"LEVEL 01",new Vector2(0,12),new Vector2(278,40),32,Ink,true);
            var pause=GlassButton(header,"Pause game",new Vector2(43,12),new Vector2(52,52),TogglePause,new Vector2(0,.5f));
            Icon(pause.transform,GlassIcon.Kind.Pause,Vector2.zero,26);
            var home=GlassButton(header,"Return Home",new Vector2(105,12),new Vector2(52,52),OnClickHome,new Vector2(0,.5f));
            Icon(home.transform,GlassIcon.Kind.Home,Vector2.zero,28);
            var coin=GlassPanel(header,"Coin purse",new Vector2(-91,12),new Vector2(146,52),new Vector2(1,.5f));
            CoinIcon(coin,new Vector2(-44,0),30);
            coinsText=Label(coin,"250",new Vector2(16,0),new Vector2(92,34),22,GlassInk,true);
            var rail=Panel(header,"Progress",new Vector2(-18,-40),new Vector2(492,7),new Color(.55f,.42f,.16f,.20f));
            progressFill=Image(rail,"Filled",Vector2.zero,new Vector2(492,7),Color.white);
            { var g=progressFill.gameObject.AddComponent<VerticalGradient>(); g.top=Hex("F3D27A"); g.bottom=Hex("C08A22"); }
            progressFill.type=UnityEngine.UI.Image.Type.Filled; progressFill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
            progressText=Label(header,"0%",new Vector2(270,-40),new Vector2(65,28),16,GlassInk,true);

            nestsCaption=Label(hud,"WORKING NESTS",Vector2.zero,new Vector2(270,24),14,Muted,true).rectTransform;
            queueCaption=Label(hud,"NEXT COLONIES",Vector2.zero,new Vector2(260,22),13,Muted,true).rectTransform;
            var speed=GlassButton(hud,"Playback speed",Vector2.zero,new Vector2(58,30),ToggleSpeed);
            speedControl=speed.GetComponent<RectTransform>();
            speedText=Label(speedControl,"1x",Vector2.zero,new Vector2(50,26),16,GlassInk,true);
            hintText=Label(hud,"Tap a numbered colony to send your ants.",new Vector2(0,150),new Vector2(650,32),16,Ink,false,new Vector2(.5f,0));
            hintText.gameObject.name="Game hint";

            var shadow=Panel(hud,"Toolbar soft shadow",new Vector2(0,65),new Vector2(672,114),new Color(.32f,.26f,.16f,.11f),new Vector2(.5f,0));
            HorizontalInset(shadow,22,114,65);
            var footer=GlassPanel(hud,"Glass toolbar",new Vector2(0,70),new Vector2(672,112),new Vector2(.5f,0));
            HorizontalInset(footer,24,112,70);
            footer.GetComponent<Image>().raycastTarget=true;
            GlassTool(footer,GlassIcon.Kind.Nest,AdManager.MobilePlatform?"WATCH AD +1":"EXTRA NEST",.1f,OnClickAddSlot);
            GlassTool(footer,GlassIcon.Kind.Restart,"RESTART",.3f,OnClickRetry);
            GlassTool(footer,GlassIcon.Kind.Shuffle,"SHUFFLE 60",.5f,OnClickShuffle);
            toolbarSound=GlassTool(footer,GlassIcon.Kind.Sound,"SOUND",.7f,ToggleSound);
            GlassTool(footer,GlassIcon.Kind.Pictures,"PICTURES",.9f,OpenLevelPicker);
        }

        static void HorizontalInset(RectTransform rect,float inset,float height,float y)
        {
            rect.anchorMin=new Vector2(0,0); rect.anchorMax=new Vector2(1,0);
            rect.sizeDelta=new Vector2(-inset*2,height); rect.anchoredPosition=new Vector2(0,y);
        }
        RectTransform GlassPanel(RectTransform parent,string name,Vector2 position,Vector2 size,Vector2? anchor=null)
        {
            var image=Image(parent,name,position,size,Color.white,anchor);
            image.material=glassMaterial; image.pixelsPerUnitMultiplier=.75f;
            var outline=image.gameObject.AddComponent<Outline>(); outline.effectColor=new Color(1,1,1,.65f); outline.effectDistance=new Vector2(1,-1);
            return image.rectTransform;
        }
        Button GlassButton(RectTransform parent,string name,Vector2 position,Vector2 size,Action action,Vector2? anchor=null)
        {
            var rect=GlassPanel(parent,name,position,size,anchor); var image=rect.GetComponent<Image>(); image.raycastTarget=true;
            image.material=buttonGlassMaterial;
            var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            var colors=button.colors; colors.highlightedColor=new Color(1f,.98f,.92f); colors.pressedColor=new Color(.95f,.88f,.72f); colors.fadeDuration=.12f;
            button.colors=colors; button.onClick.AddListener(()=>action()); rect.gameObject.AddComponent<ButtonJuice>(); return button;
        }
        Text GlassTool(RectTransform parent,GlassIcon.Kind kind,string title,float anchor,Action action)
        {
            var button=GlassButton(parent,title,Vector2.zero,new Vector2(118,88),action,new Vector2(anchor,.5f));
            Icon(button.transform,kind,new Vector2(0,13),31);
            return Label(button.GetComponent<RectTransform>(),title,new Vector2(0,-25),new Vector2(112,24),11,GlassInk,true);
        }
        static void Icon(Transform parent,GlassIcon.Kind kind,Vector2 position,float size)
        {
            var go=new GameObject("Icon",typeof(RectTransform),typeof(CanvasRenderer),typeof(GlassIcon)); go.transform.SetParent(parent,false);
            Position(go.GetComponent<RectTransform>(),position,Vector2.one*size,null);
            var icon=go.GetComponent<GlassIcon>(); icon.icon=kind; icon.color=new Color(.62f,.42f,.1f); icon.raycastTarget=false;
        }
        void LateUpdate()
        {
            if(hudRoot==null || !gameplayHud.activeSelf || Camera.main==null) return;
            AnchorToWorld(nestsCaption,new Vector3(-2.1f,-1.12f,0));
            AnchorToWorld(queueCaption,new Vector3(0,-2.79f,0));
            AnchorToWorld(speedControl,new Vector3(3.1f,-1.3f,0));
            var audio=AudioManager.Instance;
            if(toolbarSound!=null && audio!=null) toolbarSound.text=audio.MusicEnabled||audio.SoundEnabled?"SOUND ON":"SOUND OFF";
        }
        void AnchorToWorld(RectTransform label,Vector3 world)
        {
            var point=Camera.main.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hudRoot,point,null,out var local);
            label.anchoredPosition=local;
        }
    }
}
