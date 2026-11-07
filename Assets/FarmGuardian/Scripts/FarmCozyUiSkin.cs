using UnityEngine;
using UnityEngine.UI;

namespace FarmGuardian
{
    // Uses unmodified PNG artwork from the locally supplied dobo_ui demo.
    // Resources copies make the same art available in builds without Editor APIs.
    public sealed class FarmCozyUiSkin
    {
        static FarmCozyUiSkin shared;
        public static FarmCozyUiSkin Shared=>shared??(shared=new FarmCozyUiSkin());
        public readonly Sprite WoodPanel,Paper,WoodButton,CreamButton,RedButton,RoundWood,RoundCream,BackArrow;
        public readonly Color Ink=new Color(.29f,.17f,.12f);
        public bool Ready=>WoodPanel!=null&&Paper!=null&&WoodButton!=null&&CreamButton!=null&&RedButton!=null&&RoundWood!=null&&RoundCream!=null&&BackArrow!=null;
        FarmCozyUiSkin()
        {
            WoodPanel=Load("WoodenContainer1",32);Paper=Load("NoteContainerSimple",new Vector4(80,160,32,160));
            WoodButton=Load("SquareButton2_wood",30);CreamButton=Load("RoundButton2_white",80);
            RedButton=Load("SquareButton2_red",30);
            RoundWood=Load("RoundButton1_wood",0);RoundCream=Load("RoundButton1_white",0);
            BackArrow=Load("ArrowBrownOuline22",0);
        }
        static Sprite Load(string name,float border)
        {return Load(name,Vector4.one*border);}
        static Sprite Load(string name,Vector4 border)
        {
            var texture=Resources.Load<Texture2D>("CozyUI/"+name);
            if(texture==null){Debug.LogWarning("Cozy UI artwork missing: "+name);return null;}
            return Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);
        }
        public void Style(FarmUiElement element,string template)
        {
            // Remove the old generated outline/gloss without touching interaction components.
            foreach(var child in element.GetComponentsInChildren<Transform>(true))
                if(child.name=="Gentle shadow"||child.name=="Pastel outline"||child.name=="Soft gradient")child.gameObject.SetActive(false);
            element.PreserveArtworkColor=true;
            if(element.Label!=null)
            {
                element.Label.color=Ink;element.Label.alignment=TMPro.TextAlignmentOptions.Midline;
                element.Label.margin=Vector4.zero;
                CenterCaption(element,template=="RibbonTitle"?6:8);
            }
            var rect=(RectTransform)element.Face.transform;
            FarmUiFactory.Stretch(rect);
            if(template=="LevelButton")Paint(element.Face,RoundCream,false);
            else if(template=="CurrencyCounter")Paint(element.Face,CreamButton,true);
            else if(template=="RibbonTitle")Paint(element.Face,WoodPanel,true);
            else if(element.Button!=null)Paint(element.Face,WoodButton,true);
            else
            {
                Paint(element.Face,WoodPanel,true);
                var paper=FarmUiFactory.Rect("Cozy paper inset",element.Face.transform,Vector2.zero,Vector2.zero);
                FarmUiFactory.Stretch(paper,12);
                var image=paper.gameObject.AddComponent<Image>();Paint(image,Paper,true);image.raycastTarget=false;
            }
        }
        public void StyleButton(FarmUiElement element,string title,Vector2 size)
        {
            Paint(element.Face,size.x<=110?RoundCream:title=="EXIT"||title=="CATCH THIEF"||title=="RETRY"||title=="RESET"||title=="RESET PROGRESS"?RedButton:
                title=="BACK"||title=="REPLAY"||title=="LEVEL SELECT"?CreamButton:WoodButton,size.x>110);
            CenterCaption(element,element.Face.sprite==CreamButton?4:element.Face.sprite==RoundCream?size.y*.055f:7);
            if(title!="<"&&title!=">")return;
            element.Label.text="";
            var r=FarmUiFactory.Rect("Cozy page arrow",element.transform,new Vector2(27,43),new Vector2(0,size.y*.055f));
            var image=r.gameObject.AddComponent<Image>();Paint(image,BackArrow,false);image.raycastTarget=false;
            if(title==">")r.localRotation=Quaternion.Euler(0,0,180);
        }
        public static void Paint(Image image,Sprite sprite,bool sliced)
        {image.sprite=sprite;image.type=sliced?Image.Type.Sliced:Image.Type.Simple;image.color=Color.white;}
        public static void CenterCaption(FarmUiElement element,float lift)
        {
            if(element.Label==null)return;
            var r=element.Label.rectTransform;FarmUiFactory.Stretch(r);
            r.offsetMin=new Vector2(12,12+lift);r.offsetMax=new Vector2(-12,-12+lift);
        }
    }
}
