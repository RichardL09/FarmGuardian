using UnityEngine;

namespace FarmGuardian
{
    // Presentation preferences only; game progress continues to use FarmSave.
    public static class FarmUiPreferences
    {
        public static float SfxVolume { get => Mathf.Clamp01(PlayerPrefs.GetFloat("FarmGuardian.UI.Sfx",1)); set { PlayerPrefs.SetFloat("FarmGuardian.UI.Sfx",Mathf.Clamp01(value)); PlayerPrefs.Save(); } }
        public static void ApplyQuality(int value)
        { value=Mathf.Clamp(value,0,QualitySettings.names.Length-1); QualitySettings.SetQualityLevel(value,true); PlayerPrefs.SetInt("FarmGuardian.UI.Quality",value); PlayerPrefs.Save(); }
        public static void RestoreQuality()
        { if(PlayerPrefs.HasKey("FarmGuardian.UI.Quality")) QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt("FarmGuardian.UI.Quality"),0,QualitySettings.names.Length-1),true); }
    }
}
