using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FarmGuardian
{
    public sealed class FarmUiFactory
    {
        public readonly FarmUiTheme Theme;
        public bool UseCozy;
        public bool CozyEnabled=>UseCozy&&FarmCozyUiSkin.Shared.Ready;
        public FarmUiFactory(FarmUiTheme theme) { Theme = theme; }
        public static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position,Vector2? anchor=null)
        {
            var go = new GameObject(name,typeof(RectTransform)); var r=go.GetComponent<RectTransform>();
            r.SetParent(parent,false); r.anchorMin=r.anchorMax=anchor??Vector2.one*.5f;
            r.sizeDelta=size; r.anchoredPosition=position; return r;
        }
        public static void Stretch(RectTransform r,float inset=0)
        { r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=Vector2.one*inset; r.offsetMax=Vector2.one*-inset; }
        public Image Image(string name,Transform parent,Vector2 size,Vector2 position,Color color,Sprite sprite=null,Vector2? anchor=null,bool blocks=false)
        {
            var r=Rect(name,parent,size,position,anchor); var image=r.gameObject.AddComponent<Image>();
            image.sprite=sprite??Theme.rounded; image.type=UnityEngine.UI.Image.Type.Sliced; image.color=color; image.raycastTarget=blocks; return image;
        }
        public TMP_Text Text(string value,Transform parent,Vector2 size,Vector2 position,float fontSize=28,Color? color=null,TextAlignmentOptions alignment=TextAlignmentOptions.Center,Vector2? anchor=null)
        {
            var r=Rect("Text",parent,size,position,anchor); var t=r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font=FarmUiTypography.ForText(fontSize>=28,Theme.font); t.text=value; t.fontSize=fontSize; t.color=color??(CozyEnabled?FarmCozyUiSkin.Shared.Ink:Theme.ink);
            t.fontStyle=t.font!=Theme.font?FontStyles.Normal:fontSize>=28?FontStyles.Bold:FontStyles.Normal;
            // Coiny's font metrics include generous descender space. Center visible glyphs,
            // not that invisible space, so numbers and captions sit on their artwork.
            t.alignment=CozyEnabled&&fontSize>=28&&alignment==TextAlignmentOptions.Center?TextAlignmentOptions.Midline:alignment;
            t.raycastTarget=false; t.overflowMode=TextOverflowModes.Ellipsis; t.margin=new Vector4(4,2,4,2); return t;
        }
        public FarmUiElement Instantiate(GameObject prefab,string name,Transform parent,Vector2 size,Vector2 position,Vector2? anchor=null)
        {
            if(prefab==null) throw new System.InvalidOperationException("UI prefab missing. Run Tools > Farm Guardian > Build UI Kit.");
            var instance=Object.Instantiate(prefab,parent,false); instance.name=name;
            var r=(RectTransform)instance.transform; r.anchorMin=r.anchorMax=anchor??Vector2.one*.5f; r.sizeDelta=size; r.anchoredPosition=position;
            var element=instance.GetComponent<FarmUiElement>();
            if(element.Label!=null)
            {
                element.Label.font=FarmUiTypography.ForText(true,Theme.font);
                if(element.Label.font!=Theme.font)element.Label.fontStyle=FontStyles.Normal;
            }
            if(CozyEnabled)FarmCozyUiSkin.Shared.Style(element,prefab.name);
            return element;
        }
        public RectTransform Panel(string name,Transform parent,Vector2 size,Vector2 position,GameObject prefab=null)
        { return (RectTransform)Instantiate(prefab??Theme.creamPanel,name,parent,size,position).transform; }
        public Button Button(string title,Transform parent,Vector2 size,Vector2 position,UnityAction action,Color? tint=null,FarmIcon? icon=null,Vector2? anchor=null)
        {
            size.y=Mathf.Max(72,size.y);
            var element=Instantiate(Theme.primaryButton,title,parent,size,position,anchor); element.Bind(title,tint??Theme.green);
            if(CozyEnabled)FarmCozyUiSkin.Shared.StyleButton(element,title,size);
            element.Label.fontSize=size.y>=80?34:26;
            element.Label.enableAutoSizing=true;element.Label.fontSizeMax=element.Label.fontSize;element.Label.fontSizeMin=24;
            if(CozyEnabled)
            {
                element.Label.textWrappingMode=TextWrappingModes.NoWrap;
                element.Label.fontSizeMax=size.x<400?28:32;element.Label.fontSizeMin=20;
            }
            if(icon.HasValue)
            {
                Symbol(icon.Value,element.transform,new Vector2(38,38),new Vector2(-size.x*.5f+43,CozyEnabled?7:0),Theme.ink);
                element.Label.rectTransform.offsetMin=new Vector2(66,CozyEnabled?22:3); element.Label.rectTransform.offsetMax=new Vector2(-8,CozyEnabled?-8:-3);
            }
            if(action!=null) element.Button.onClick.AddListener(action);
            return element.Button;
        }
        public void Symbol(FarmIcon icon,Transform parent,Vector2 size,Vector2 position,Color? color=null)
        {
            if(icon==FarmIcon.Coin&&FarmCoinArtwork.Sprite!=null)
            {
                var coin=Image("Icon - Coin",parent,size,position,Color.white,FarmCoinArtwork.Sprite);
                coin.type=UnityEngine.UI.Image.Type.Simple;coin.useSpriteMesh=true;coin.preserveAspect=true;return;
            }
            if(Theme.icons!=null&&(int)icon<Theme.icons.Length&&Theme.icons[(int)icon]!=null)
            {
                var image=Image("Icon - "+icon,parent,size,position,color??Theme.ink,Theme.icons[(int)icon]); image.type=UnityEngine.UI.Image.Type.Simple;
                if(CozyEnabled&&icon==FarmIcon.Star)
                {
                    var outline=image.gameObject.AddComponent<Outline>();outline.effectColor=FarmCozyUiSkin.Shared.Ink;outline.effectDistance=new Vector2(1.4f,-1.4f);
                }
                return;
            }
            var r=Rect(icon.ToString(),parent,size,position); var g=r.gameObject.AddComponent<FarmUiIcon>(); g.Icon=icon; g.color=color??Theme.ink; g.raycastTarget=false;
        }
        public void Ribbon(string title,Transform parent,float width,float y)
        {
            var ribbon=Instantiate(Theme.ribbonTitle,"Ribbon - "+title,parent,new Vector2(width,90),new Vector2(0,y));
            ribbon.Bind(title,Theme.lavenderLight); ribbon.Label.fontSize=38;
        }
        public static FarmUiElement BuildTemplate(FarmUiTheme theme,string name,bool button,bool ribbon=false)
        {
            var f=new FarmUiFactory(theme); var r=Rect(name,null,new Vector2(400,90),Vector2.zero);
            var element=r.gameObject.AddComponent<FarmUiElement>();
            var shadow=f.Image("Gentle shadow",r,Vector2.zero,new Vector2(0,-4),new Color(.35f,.28f,.32f,.13f)); Stretch(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition=Vector2.down*4;
            var border=f.Image("Pastel outline",r,Vector2.zero,Vector2.zero,Color.Lerp(theme.pink,theme.cream,.55f),blocks:!button); Stretch(border.rectTransform);
            var face=f.Image("Face",r,Vector2.zero,Vector2.zero,button?theme.green:theme.cream,blocks:button); Stretch(face.rectTransform,2);
            element.Face=face;
            var shine=f.Image("Soft gradient",face.transform,Vector2.zero,Vector2.zero,new Color(1,1,1,button?.16f:.08f),theme.gloss); Stretch(shine.rectTransform,2);
            if(button||ribbon)
            {
                element.Label=f.Text(name,r,new Vector2(380,70),Vector2.zero,32);
                Stretch(element.Label.rectTransform,6); element.Label.outlineWidth=0;
            }
            if(button)
            {
                element.Button=r.gameObject.AddComponent<Button>(); element.Button.targetGraphic=face;
                var colors=element.Button.colors; colors.highlightedColor=new Color(1,.99f,.98f); colors.pressedColor=new Color(.9f,.91f,.93f);
                colors.disabledColor=new Color(.80f,.78f,.80f,1);colors.fadeDuration=.1f; element.Button.colors=colors;
                var nav=element.Button.navigation; nav.mode=Navigation.Mode.None; element.Button.navigation=nav;
                r.gameObject.AddComponent<FarmUiMotion>();
            }
            return element;
        }
    }
}
