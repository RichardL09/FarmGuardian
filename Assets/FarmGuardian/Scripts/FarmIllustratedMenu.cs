using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FarmGuardian
{
    public sealed class FarmIllustratedMenu : MonoBehaviour
    {
        public TMP_Text Balance {get;private set;}
        RectTransform root,viewport;RawImage fullBleed;FarmMenuBackdropPatch fittedScenery;Texture2D source,plate;
        public void Build(Transform parent,FarmSession session,FarmUiTheme theme)
        {
            source=Resources.Load<Texture2D>("ReferenceMenu/DesignSource");
            plate=Resources.Load<Texture2D>("ReferenceMenu/BackgroundPlate");
            if(source==null||plate==null){Debug.LogError("Reference menu artwork missing. Import the supplied design and clean plates.");return;}
            viewport=(RectTransform)parent;
            var background=FarmUiFactory.Rect("Full screen menu scenery",parent,Vector2.zero,Vector2.zero);
            FarmUiFactory.Stretch(background);fullBleed=background.gameObject.AddComponent<RawImage>();
            fullBleed.texture=plate;fullBleed.raycastTarget=false;
            root=FarmUiFactory.Rect("Reference layout - 1672 x 941",parent,new Vector2(1672,941),Vector2.zero);
            var scenery=FarmUiFactory.Rect("Original farm background",root,new Vector2(1672,941),Vector2.zero);
            fittedScenery=scenery.gameObject.AddComponent<FarmMenuBackdropPatch>();
            fittedScenery.texture=source;fittedScenery.raycastTarget=false;fittedScenery.Feather=100;
            // Keep the illustrated scenery and logo; replace baked controls with live Cozy UI.
            Rect logo=new Rect(887,96,580,310);
            Rect play=new Rect(963,430,433,135),shop=new Rect(963,572,433,120),settings=new Rect(963,701,433,117);
            Rect wallet=new Rect(1402,23,250,80);
            foreach(var r in new[]{play,shop,settings,wallet})Backdrop(r);
            Slice("FARM GUARDIAN logo",logo,source);
            var ui=new FarmUiFactory(theme){UseCozy=true};
            Action(ui,"PLAY","START",new Vector2(1179,495),session.OpenLevelSelect,FarmIcon.Play);
            Action(ui,"SHOP","UPGRADES",new Vector2(1179,638),session.OpenShop,FarmIcon.Cart);
            Action(ui,"SETTINGS","SETTINGS",new Vector2(1179,781),session.OpenSettings,FarmIcon.Gear);
            var exit=ui.Button("EXIT",root,new Vector2(280,70),Pos(new Vector2(1179,886)),session.QuitGame);
            var exitLabel=exit.GetComponent<FarmUiElement>().Label;exitLabel.fontSizeMax=30;exitLabel.fontSizeMin=24;
            var counter=ui.Instantiate(theme.currencyCounter,"Main menu coins",root,new Vector2(280,72),Pos(new Vector2(1512,63)));
            counter.Bind("",Color.white);
            ui.Symbol(FarmIcon.Coin,counter.transform,new Vector2(34,34),new Vector2(-105,4),FarmCozyUiSkin.Shared.Ink);
            Balance=ui.Text(session.Save.coins.ToString("N0"),counter.transform,new Vector2(140,48),new Vector2(-6,4),30);
            Balance.name="Actual saved coins";Balance.enableAutoSizing=true;Balance.fontSizeMax=30;Balance.fontSizeMin=18;
            var plus=ui.Button("+",counter.transform,new Vector2(72,72),new Vector2(102,0),session.OpenShop);
            plus.name="COINS";
            RefreshLayout();
        }
        void LateUpdate()=>RefreshLayout();
        public void RefreshLayout()
        {
            if(viewport==null||root==null||viewport.rect.width<=0||viewport.rect.height<=0)return;
            var bounds=viewport.rect;
            var safe=Screen.safeArea;
            var normalized=Screen.width>0&&Screen.height>0?
                new Rect(safe.x/Screen.width,safe.y/Screen.height,safe.width/Screen.width,safe.height/Screen.height):new Rect(0,0,1,1);
            root.anchoredPosition=new Vector2(bounds.width*(normalized.center.x-.5f),bounds.height*(normalized.center.y-.5f));
            root.localScale=Vector3.one*Mathf.Min(bounds.width*normalized.width/1672,bounds.height*normalized.height/941);
            var fitted=root.sizeDelta*root.localScale.x;
            var featherAxes=new Vector2(bounds.width-fitted.x>2?1:0,bounds.height-fitted.y>2?1:0);
            if(fittedScenery.FeatherAxes!=featherAxes){fittedScenery.FeatherAxes=featherAxes;fittedScenery.SetVerticesDirty();}
            float imageAspect=plate.width/(float)plate.height,screenAspect=bounds.width/bounds.height;
            var uv=new Rect(0,0,1,1);
            if(screenAspect>imageAspect){uv.height=imageAspect/screenAspect;uv.y=(1-uv.height)*.5f;}
            else {uv.width=screenAspect/imageAspect;uv.x=(1-uv.width)*.5f;}
            fullBleed.uvRect=uv;
        }
        Vector2 Pos(Vector2 p)=>new Vector2(p.x-836,470.5f-p.y);
        RawImage Slice(string name,Rect pixels,Texture2D texture)
        {
            var r=FarmUiFactory.Rect(name,root,pixels.size,Pos(pixels.center));
            var image=r.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
            image.uvRect=new Rect(pixels.x/1672,1-pixels.yMax/941,pixels.width/1672,pixels.height/941);return image;
        }
        void Backdrop(Rect original)
        {
            var pixels=Rect.MinMaxRect(Mathf.Max(0,original.xMin-24),Mathf.Max(0,original.yMin-24),Mathf.Min(1672,original.xMax+24),Mathf.Min(941,original.yMax+24));
            var r=FarmUiFactory.Rect("Clean backdrop patch",root,pixels.size,Pos(pixels.center));
            var image=r.gameObject.AddComponent<FarmMenuBackdropPatch>();image.texture=plate;image.raycastTarget=false;
            image.uvRect=new Rect(pixels.x/1672,1-pixels.yMax/941,pixels.width/1672,pixels.height/941);
        }
        void Action(FarmUiFactory ui,string name,string caption,Vector2 pixels,UnityAction action,FarmIcon icon)
        {
            var button=ui.Button(caption,root,new Vector2(420,110),Pos(pixels),action,icon:icon);
            button.name=name;
            var label=button.GetComponent<FarmUiElement>().Label;label.fontSizeMax=36;label.fontSizeMin=28;
        }
    }
}

