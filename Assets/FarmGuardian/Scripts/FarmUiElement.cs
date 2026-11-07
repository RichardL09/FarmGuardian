using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGuardian
{
    // Shared prefab bindings. Screens provide data/callbacks, never their own currency or progression.
    public sealed class FarmUiElement : MonoBehaviour
    {
        public Image Face;
        public TMP_Text Label;
        public Button Button;
        [System.NonSerialized] public bool PreserveArtworkColor;
        public void Bind(string text, Color tint)
        { if (Face != null && !PreserveArtworkColor) Face.color = tint; if (Label != null) Label.text = text; }
    }
}
