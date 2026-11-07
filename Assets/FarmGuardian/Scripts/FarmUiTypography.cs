using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace FarmGuardian
{
    public static class FarmUiTypography
    {
        static TMP_FontAsset bubble,body;
        public static Font BubbleSource=>Resources.Load<Font>("CozyUI/Fonts/Coiny-Regular");
        public static TMP_FontAsset ForText(bool important,TMP_FontAsset fallback)
        {
            // Shared prefab generation must keep persistent font references.
            if(!Application.isPlaying)return fallback;
            if(important)
            {
                if(bubble==null)bubble=Create(BubbleSource,"Coiny Bubble SDF",fallback);
                return bubble!=null?bubble:fallback;
            }
            if(body==null)body=Create(Resources.Load<Font>("CozyUI/Fonts/BalsamiqSans-Regular"),"Balsamiq Sans Body SDF",fallback);
            return body!=null?body:fallback;
        }
        static TMP_FontAsset Create(Font source,string name,TMP_FontAsset fallback)
        {
            if(source==null)return null;
            var font=TMP_FontAsset.CreateFontAsset(source,64,8,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
            if(font==null)return null;
            font.name=name;
            font.fallbackFontAssetTable=new List<TMP_FontAsset>();
            if(fallback!=null)font.fallbackFontAssetTable.Add(fallback);
            string characters="";for(char c=' ';c<='~';c++)characters+=c;
            font.TryAddCharacters(characters+"•–—",out string missing);
            return font;
        }
    }
}
