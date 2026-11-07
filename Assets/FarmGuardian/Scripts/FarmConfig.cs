using UnityEngine;

namespace FarmGuardian
{
    [CreateAssetMenu(menuName = "Farm Guardian/Game Configuration")]
    public sealed class FarmConfig : ScriptableObject
    {
        public const int MaxLevel=100;
        [Header("Player and interactions")]
        public float playerSpeed = 5.8f;
        public float interactionRange = 2.4f;
        public float animalCaptureSeconds = 0.65f;
        public float thiefCatchSeconds = 2f;
        public float thiefStunSeconds = 15f;
        public float plantingSeconds = 2.5f;
        [Header("Thief")]
        public float thiefBaseSpeed = 2.2f;
        public float thiefSpeedIncrease = 0.2f;
        public float thiefMaxSpeed = 3.3f;
        public float theftSeconds = 3f;
        public float escapeDeadline = 22f;
        [Tooltip("Retry interval when no secured animal is available. The thief starts moving immediately on spawn.")]
        public float thiefStartDelay = .2f;
        [Header("Progression")]
        public int levelsPerTheme = 15;
        public float[] timeLimits = { 90, 80, 70, 60 };
        public int maxAnimals = 8;
        public int maxPlantingPlots = 3;
        [Range(0, 1)] public float plantingChance = 0.45f;
        [Header("Rewards")]
        public int baseReward = 30;
        public int animalReward = 10;
        public int plantingReward = 15;
        public int upgradeBaseCost = 60;
        public int maxUpgradeLevel = 4;
        public float movementUpgradePerLevel = 0.08f;
        public float coinUpgradePerLevel = 0.15f;
        public float farmingUpgradePerLevel = 0.20f;
        public float captureUpgradePerLevel = 0.20f;
        public int goldenOutfitCost = 300;
        [Header("Main menu artwork")]
        public Sprite menuBackground;
        public Sprite menuLogo;
        public Sprite menuButton;
        [Header("Verified source models, assigned by setup tool")]
        public GameObject farmerModel;
        public GameObject thiefModel;
        public GameObject[] animalModels;
        public string[] animalNames = { "Cows", "Pigs", "Chicks", "Bunnies" };
        public Material farmerMaterial;
        public Material thiefMaterial;
        public Material animalMaterial;
        public Material environmentMaterial;
        public GameObject townModel;
        public RuntimeAnimatorController[] animalAnimators;

        public LevelSpec GetLevel(int level)
        {
            level = Mathf.Clamp(level, 1, FarmConfig.MaxLevel);
            int band = (level - 1) / Mathf.Max(1, levelsPerTheme);
            int animals = Mathf.Min(Mathf.Clamp(maxAnimals, 3, 8), 3 + level / 2);
            var random = new System.Random(level * 7919);
            int plots = level == 1 || level == 3 || level == 5 ? 0 :
                level == 2 ? 1 : level == 4 ? 2 :
                random.NextDouble() < plantingChance ? 1 + random.Next(Mathf.Clamp(maxPlantingPlots, 1, 3)) : 0;
            return new LevelSpec
            {
                number = level, theme = band % FarmMapLayout.ThemeCount, animalCount = animals,
                typeCount = Mathf.Clamp(1 + (level - 1) / 3, 1, Mathf.Min(4, animalModels.Length)),
                plantingCount = plots,
                seconds = timeLimits != null && timeLimits.Length > 0 ?
                    Mathf.Max(60, timeLimits[Mathf.Min(band, timeLimits.Length - 1)]) : 90,
                thiefSpeed = Mathf.Min(thiefMaxSpeed, thiefBaseSpeed + (level - 1) * thiefSpeedIncrease)
            };
        }
    }

    public struct LevelSpec
    {
        public int number, theme, animalCount, typeCount, plantingCount;
        public float seconds, thiefSpeed;
        public string ThemeName => new[] { "Countryside Farm", "Woodland Ranch", "Mountain Farm", "Advanced Farm" }[theme];
    }
}

