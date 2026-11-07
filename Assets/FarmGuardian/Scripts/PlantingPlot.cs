using UnityEngine;

namespace FarmGuardian
{
    public sealed class PlantingPlot : MonoBehaviour
    {
        public FarmSession Session;
        public Transform Crop;
        public bool Planted { get; private set; }
        float growth;
        public void Plant()
        {
            if (Planted || !Session.Playing) return;
            Planted = true; growth = 0;
            if (Crop != null) { Crop.gameObject.SetActive(true); Crop.localScale = Vector3.one * 0.05f; }
        }
        void Update()
        {
            if (!Session.Playing || !Planted || Crop == null || growth >= 1) return;
            growth = Mathf.Min(1, growth + Time.deltaTime * 0.5f);
            Crop.localScale = Vector3.one * Mathf.Lerp(0.05f, 1, growth);
        }
    }
}
