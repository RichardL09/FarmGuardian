using UnityEngine;

namespace FarmGuardian
{
    // Saved geometry makes the first scene inspectable in Edit Mode. Runtime FarmWorld uses
    // the exact same scenery prefabs for the selected theme, so this preview must not duplicate it.
    [DefaultExecutionOrder(-10000)]
    public sealed class FarmScenePreview : MonoBehaviour
    {
        void Awake(){if(Application.isPlaying)gameObject.SetActive(false);}
    }
}
