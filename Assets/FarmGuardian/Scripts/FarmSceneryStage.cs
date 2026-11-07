using UnityEngine;

namespace FarmGuardian
{
    public sealed class FarmSceneryStage : MonoBehaviour
    {
        public GameObject[] restingGardens;
        public void Configure(int missionPlots)
        {for(int i=0;i<restingGardens.Length;i++)if(restingGardens[i]!=null)restingGardens[i].SetActive(i>=missionPlots);}
    }
}
