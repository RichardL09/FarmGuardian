using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace FarmGuardian
{
    public enum SessionState { MainMenu, LevelIntroduction, Playing, Paused, Won, Lost, Shop, Skins, Settings, LevelSelect }

    public sealed class FarmSession : MonoBehaviour
    {
        public FarmConfig config;
        public SessionState State { get; private set; } = SessionState.MainMenu;
        public FarmSave Save { get; private set; }
        public LevelSpec Level { get; private set; }
        public float Remaining { get; private set; }
        public int Reward { get; private set; }
        public int EarnedStars { get; private set; }
        public string ResultReason { get; private set; }
        public float HoldProgress { get; private set; }
        public string HoldLabel { get; private set; }
        public FarmWorld World { get; private set; }
        public FarmHud Hud { get; private set; }
        public readonly List<FarmAnimal> Animals = new List<FarmAnimal>();
        public readonly List<PlantingPlot> Plots = new List<PlantingPlot>();
        public FarmPlayer Player { get; set; }
        public FarmThief Thief { get; set; }
        public Vector3 PenPosition { get; set; }
        public Vector3 EscapePosition { get; set; }
        public int SelectedLevel { get; private set; } = 1;
        public int LostCount => Animals.FindAll(a => a.State == AnimalState.Lost).Count;
        public int RemainingAnimalCount => Mathf.Max(0, Level.animalCount - LostCount);
        public const int SafeAnimalScore=100,PlantedPlotScore=150;
        public int Lives=>RemainingAnimalCount;
        // Score describes this run's current secured objectives, not the saved wallet.
        // Re-delivering a stolen animal restores its points without farming extra points.
        public int Score=>SecuredCount*SafeAnimalScore+PlantedCount*PlantedPlotScore;
        public bool PermanentLoss => LostCount > 0;
        public int OneStarLossThreshold => Mathf.Max(2, Mathf.CeilToInt(Level.animalCount * .5f));
        public int CurrentStars => LostCount >= Level.animalCount ? 0 : LostCount == 0 ? 3 : LostCount >= OneStarLossThreshold ? 1 : 2;
        public bool Playing => State == SessionState.Playing;
        public int SecuredCount => Animals.FindAll(a => a.State == AnimalState.Secured).Count;
        public FarmAnimal CarriedAnimal {get;private set;}
        public bool CaptureInProgress=>Animals.Exists(a=>a.State==AnimalState.BeingCaptured);
        public bool CanDeposit=>Playing&&CarriedAnimal!=null&&Player!=null&&
            (World.GateFor(CarriedAnimal.TypeIndex)==null||World.GateFor(CarriedAnimal.TypeIndex).Passable)&&
            Vector3.Distance(Player.transform.position,CarriedAnimal.Home)<=2.1f&&HasLineOfSight(CarriedAnimal.Home);
        public int PlantedCount => Plots.FindAll(p => p.Planted).Count;
        float holdTime;
        Object holdTarget;
        bool uiCatch, uiPlant;
        bool resumeAfterFocus;
        AudioSource audioSource;
        AudioClip feedback;
        SessionState settingsReturn=SessionState.MainMenu;
        float nextUiPurchase;

        void Awake()
        {
            if (config == null) { Debug.LogError("Farm Guardian configuration missing. Run Tools > Farm Guardian > Build Game."); enabled = false; return; }
            Application.targetFrameRate = 60;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            // Override a saved windowed resolution from older builds on startup.
            Screen.SetResolution(Display.main.systemWidth,Display.main.systemHeight,FullScreenMode.FullScreenWindow);
#endif
            FarmUiPreferences.RestoreQuality();
            Save = FarmSave.Load();
            SelectedLevel = Save.unlockedLevel;
            Level = config.GetLevel(SelectedLevel);
            World = gameObject.AddComponent<FarmWorld>();
            World.Session = this;
            Hud = gameObject.AddComponent<FarmHud>();
            Hud.Session = this;
            audioSource = gameObject.AddComponent<AudioSource>();
            // Short synthesized feedback: no external sound asset is assumed.
            feedback = AudioClip.Create("Farm Guardian feedback", 4410, 1, 44100, false);
            var samples = new float[4410];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * 2 * Mathf.PI * 660 / 44100) * 0.12f * (1f - i / 4410f);
            feedback.SetData(samples, 0);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (Playing) Pause(); else if (State == SessionState.Paused) Resume();
                else if (State == SessionState.Shop || State == SessionState.Skins || State == SessionState.Settings) CloseMenuPanel();
                else if(State==SessionState.LevelSelect||State==SessionState.LevelIntroduction) MainMenu();
            }
            if (!Playing) return;
            Remaining = Mathf.Max(0, Remaining - Time.deltaTime);
            if (Remaining <= 0) { Finish(false, "Time expired"); return; }
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame) CaptureNearest();
            UpdateHold(uiCatch || (keyboard != null && keyboard.fKey.isPressed),
                uiPlant || (keyboard != null && keyboard.pKey.isPressed), Time.deltaTime);
            CheckWin();
        }

        public void SelectLevel(int delta)
        {
            SelectedLevel = Mathf.Clamp(SelectedLevel + delta, 1, Mathf.Min(FarmConfig.MaxLevel,Save.unlockedLevel));
            Level = config.GetLevel(SelectedLevel);
            Hud.Rebuild();
        }

        public void Introduce(int level)
        {
            Time.timeScale = 1;
            SelectedLevel = Mathf.Clamp(level, 1, Mathf.Min(FarmConfig.MaxLevel,Save.unlockedLevel));
            Level = config.GetLevel(SelectedLevel);
            CarriedAnimal=null;World.Clear();
            Animals.Clear(); Plots.Clear(); Player = null; Thief = null;
            Reward = 0; EarnedStars = 0; Remaining = Level.seconds;
            ResultReason = "";
            SetState(SessionState.LevelIntroduction);
        }

        public void Begin()
        {
            if (State != SessionState.LevelIntroduction) return;
            World.Build(Level);
            SetState(SessionState.Playing);
        }

        public void MainMenu()
        {
            Time.timeScale = 1;
            CarriedAnimal=null;World.Clear(); Animals.Clear(); Plots.Clear(); Player = null; Thief = null;
            SetState(SessionState.MainMenu);
        }

        public void Pause() { if (Playing) { SetState(SessionState.Paused); Time.timeScale = 0; } }
        public void Resume() { if (State == SessionState.Paused) { Time.timeScale = 1; SetState(SessionState.Playing); } }
        public void QuitGame()
        {
            if(State!=SessionState.MainMenu)return;
            Save.Store();PlayerPrefs.Save();Application.Quit();
        }
        public void OpenShop() { if (State == SessionState.MainMenu) SetState(SessionState.Shop); }
        public void CloseShop() { if (State == SessionState.Shop) SetState(SessionState.MainMenu); }
        public void OpenSkins() { if (State == SessionState.MainMenu) SetState(SessionState.Skins); }
        public void OpenSettings() { if (State == SessionState.MainMenu||State==SessionState.Paused) {settingsReturn=State;SetState(SessionState.Settings);} }
        public bool SettingsFromPause => State==SessionState.Settings&&settingsReturn==SessionState.Paused;
        public void OpenLevelSelect()
        {
            if(Playing) return;
            Time.timeScale=1;CarriedAnimal=null;World.Clear();Animals.Clear();Plots.Clear();Player=null;Thief=null;
            Hud.PrepareLevelSelection();
            SetState(SessionState.LevelSelect);
        }
        public void ChooseLevel(int level)
        { if(State!=SessionState.LevelSelect||level<1||level>FarmConfig.MaxLevel||level>Save.unlockedLevel) return;SelectedLevel=level;Level=config.GetLevel(level); }
        public bool UiBuyUpgrade(int index)
        {
            if(State!=SessionState.Shop||Time.unscaledTime<nextUiPurchase) return false;
            nextUiPurchase=Time.unscaledTime+.4f;bool bought=Save.BuyUpgrade(index,config);if(bought) Hud.Rebuild();return bought;
        }
        public bool UiUseOutfit(bool golden)
        {return UiUseSkin(golden?FarmSkinCatalog.GoldenFarmer:FarmSkinCatalog.Classic);}
        public bool UiUseSkin(string id)
        {
            if(State!=SessionState.Shop&&State!=SessionState.Skins) return false;
            if(FarmSkinCatalog.Find(id)==null||FarmSkinCatalog.Model(config,id)==null)return false;
            if(!Save.OwnsSkin(id))
            {
                if(Time.unscaledTime<nextUiPurchase||Save.coins<FarmSkinCatalog.Cost(id,config)) return false;
                nextUiPurchase=Time.unscaledTime+.4f;
            }
            bool changed=Save.BuyOrEquipSkin(id,config);if(changed)Hud.Rebuild();return changed;
        }
        public void SetEffectsVolume(float value)
        { FarmUiPreferences.SfxVolume=value;Save.sound=value>.001f;Save.Store();if(audioSource!=null)audioSource.volume=value; }
        public void ResetProgress()
        { if(State!=SessionState.Settings) return;Save=new FarmSave();Save.Store();SelectedLevel=1;Level=config.GetLevel(1);MainMenu(); }
        public void CloseMenuPanel()
        {
            if(State==SessionState.Settings) SetState(settingsReturn);
            else if (State == SessionState.Shop || State == SessionState.Skins) SetState(SessionState.MainMenu);
        }
        public bool BuyOrToggleOutfit()
        {
            return UiUseSkin(Save.EquippedSkinId==FarmSkinCatalog.GoldenFarmer?FarmSkinCatalog.Classic:FarmSkinCatalog.GoldenFarmer);
        }
        public void ToggleSound() { Save.sound = !Save.sound; Save.Store(); Hud.Rebuild(); }
        public void SetCatchHeld(bool held) { uiCatch = held; if (!held) ResetHold(); }
        public void SetPlantHeld(bool held) { uiPlant = held; if (!held) ResetHold(); }

        void SetState(SessionState state)
        {
            State = state; uiCatch = uiPlant = false; ResetHold();
            if (Hud != null) { Hud.ResetInput(); Hud.Rebuild(); }
        }

        public FarmAnimal NearestAnimal()
        {
            if (Player == null||CarriedAnimal!=null||CaptureInProgress) return null;
            FarmAnimal result = null;
            float nearest = config.interactionRange;
            foreach (var a in Animals)
            {
                if (a.State != AnimalState.Wandering && a.State != AnimalState.Escaping) continue;
                if(!a.CanCapture)continue;
                float distance = Vector3.Distance(Player.transform.position, a.transform.position);
                if (distance <= nearest && HasLineOfSight(a.transform.position)) { result = a; nearest = distance; }
            }
            return result;
        }

        public PlantingPlot NearestPlot()
        {
            if (Player == null||CarriedAnimal!=null||CaptureInProgress) return null;
            return Plots.Find(p => !p.Planted && Vector3.Distance(Player.transform.position, p.transform.position) <= config.interactionRange && HasLineOfSight(p.transform.position));
        }

        public bool CanCatchThief => Playing && Player != null && Thief != null && Thief.Catchable &&
            Vector3.Distance(Player.transform.position, Thief.transform.position) <= config.interactionRange && HasLineOfSight(Thief.transform.position);

        bool HasLineOfSight(Vector3 target)
        {
            Vector3 start = Player.transform.position + Vector3.up * 0.8f;
            Vector3 delta = target + Vector3.up * 0.8f - start;
            return !Physics.Raycast(start, delta.normalized, delta.magnitude, 1 << FarmWorld.SolidLayer);
        }

        public void CaptureNearest()
        {
            if (!Playing) return;
            if(CarriedAnimal!=null)
            {
                if(CanDeposit&&CarriedAnimal.Deposit()){CarriedAnimal=null;CheckWin();}return;
            }
            var animal = NearestAnimal();
            if (animal != null) { animal.Capture(config.animalCaptureSeconds / (1 + Save.capture * config.captureUpgradePerLevel)); Beep(); }
        }

        public bool TryCarry(FarmAnimal animal)
        {
            if(!Playing||Player==null||CarriedAnimal!=null)return false;
            CarriedAnimal=animal;animal.AttachToPlayer(Player.transform);
            Player.WalkAnimation?.Drive(0,false,true);return true;
        }

        public void UpdateHold(bool catching, bool planting, float delta)
        {
            if (!Playing) { ResetHold(); return; }
            Object target = catching && CanCatchThief ? (Object)Thief : planting && Level.plantingCount > 0 ? NearestPlot() : null;
            if (target == null) { ResetHold(); return; }
            if (target != holdTarget) { ResetHold(); holdTarget = target; }
            bool thief = target is FarmThief;
            float duration = thief ? config.thiefCatchSeconds : config.plantingSeconds / (1 + Save.farming * config.farmingUpgradePerLevel);
            holdTime += delta;
            HoldLabel = thief ? "CATCHING THIEF" : "PLANTING";
            HoldProgress = Mathf.Clamp01(holdTime / Mathf.Max(0.05f, duration));
            if (HoldProgress < 1) return;
            if (thief) Thief.Catch(); else ((PlantingPlot)target).Plant();
            ResetHold(); Beep(); CheckWin();
        }

        void ResetHold() { holdTime = 0; holdTarget = null; HoldProgress = 0; HoldLabel = ""; }

        public void OnSecured(FarmAnimal animal)
        {
            Beep();
            if (Thief != null && Thief.State == ThiefState.Inactive) Thief.Activate();
            CheckWin();
        }

        public void AnimalLost(FarmAnimal animal = null)
        {
            if (!Playing) return;
            if (animal == null) animal = Thief != null ? Thief.StolenAnimal : null;
            if (animal == null || !Animals.Contains(animal) || !animal.MarkLost()) return;
            if (LostCount >= Level.animalCount) Finish(false, "All animals were stolen");
            else CheckWin();
        }

        bool MissionsComplete => Remaining > 0 && Animals.Count == Level.animalCount && SecuredCount > 0 &&
            SecuredCount + LostCount == Level.animalCount && PlantedCount >= Level.plantingCount &&
            (Thief == null || Thief.StolenAnimal == null);

        public void CheckWin()
        {
            if (!Playing) return;
            if (Remaining <= 0) { Finish(false, "Time expired"); return; }
            if (LostCount >= Level.animalCount) { Finish(false, "All animals were stolen"); return; }
            if (MissionsComplete) Finish(true, LostCount == 0 ? "Every animal is safe!" : "Remaining animals rescued and all missions completed!");
        }

        public void Finish(bool won, string reason)
        {
            if (!Playing) return;
            if (won && !MissionsComplete) return;
            if (!won && Remaining > 0 && LostCount < Level.animalCount) return;
            ResultReason = reason;
            EarnedStars = won ? CurrentStars : 0;
            if (won)
            {
                Reward = Mathf.RoundToInt((config.baseReward + SecuredCount * config.animalReward +
                    PlantedCount * config.plantingReward + Mathf.FloorToInt(Remaining / 5)) * (1 + Save.coinBonus * config.coinUpgradePerLevel));
                Save.coins = Mathf.Min(10000000, Save.coins + Reward);
                Save.unlockedLevel = Mathf.Max(Save.unlockedLevel, Mathf.Min(FarmConfig.MaxLevel, Level.number + 1));
                Save.RecordStars(Level.number, EarnedStars);
                Save.Store();
            }
            SetState(won ? SessionState.Won : SessionState.Lost);
            Beep();
        }

        public void Beep() { if (Save.sound && audioSource != null && feedback != null) {audioSource.volume=FarmUiPreferences.SfxVolume;audioSource.PlayOneShot(feedback);} }
        void OnApplicationFocus(bool focused)
        {
            if (!focused && Playing) { resumeAfterFocus = true; Pause(); }
            else if (focused && resumeAfterFocus) resumeAfterFocus = false; // User explicitly resumes.
        }
        void OnApplicationPause(bool paused) { if (paused && Playing) Pause(); }
        void OnDestroy() { Time.timeScale = 1; if (feedback != null) Destroy(feedback); }
    }
}

