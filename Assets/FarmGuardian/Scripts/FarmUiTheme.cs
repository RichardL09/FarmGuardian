using TMPro;
using UnityEngine;

namespace FarmGuardian
{
    [CreateAssetMenu(menuName="Farm Guardian/UI Theme")]
    public sealed class FarmUiTheme : ScriptableObject
    {
        public TMP_FontAsset font;
        public Sprite rounded, circle, gloss;
        public Sprite[] icons;
        public GameObject primaryButton, secondaryButton, iconButton, creamPanel, ribbonTitle, currencyCounter,
            itemCard, skinCard, upgradeCard, levelButton, progressBar, popupPanel, pausePanel, resultPanel;
        public Color yellow, cyan, green, pink, purple, cream, brown, ink, red, lavenderLight, orange;
        public void ApplyPastelPalette()
        {
            yellow=Hex("FFD56B");cyan=Hex("72C6E8");green=Hex("91D66F");pink=Hex("F28BA8");purple=Hex("B9A4D5");
            cream=Hex("FFF2D8");brown=Hex("8C725F");ink=Hex("59483F");red=Hex("E98989");lavenderLight=Hex("D7C8E8");orange=Hex("FFAD66");
        }
        static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var color);return color;}
    }
}
