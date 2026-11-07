using UnityEngine;

namespace FarmGuardian
{
    // Baked scenery only. The existing session owns every actor, objective and timer.
    public sealed class FarmScenerySet : ScriptableObject
    {
        public GameObject[] themes;
        public int[] sourceInstances, renderedBatches, villageHouses, interiorTrees;
        public string[] sourceNames;
    }
}
