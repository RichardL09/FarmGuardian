using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FarmGuardian.Tests
{
    public sealed class FarmGameplayTests
    {
        FarmSession session;
        FarmConfig testConfig;
        string previousSave;
        const string SaveKey = "FarmGuardian.Save.v1";

        [UnitySetUp]
        public IEnumerator Setup()
        {
            previousSave = PlayerPrefs.GetString(SaveKey, "");
            yield return SceneManager.LoadSceneAsync("FarmGuardian");
            yield return null;
            session = Object.FindFirstObjectByType<FarmSession>();
            Assert.That(session, Is.Not.Null);
            testConfig = Object.Instantiate(session.config);
            session.config = testConfig;
            session.Save.unlockedLevel = 100; session.Save.sound = false;
            session.Save.speed = session.Save.capture = session.Save.coinBonus = session.Save.farming = 0;
            session.Save.coins = 0;
            session.Save.levelStars = new int[FarmConfig.MaxLevel];
            session.Introduce(1); session.Begin();
            yield return null;
            Assert.That(session.Animals.Count, Is.EqualTo(3));
            Assert.That(session.Thief.State, Is.EqualTo(ThiefState.Inactive));
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Time.timeScale = 1;
            if (session != null) session.MainMenu();
            if (testConfig != null) Object.Destroy(testConfig);
            if (string.IsNullOrEmpty(previousSave)) PlayerPrefs.DeleteKey(SaveKey); else PlayerPrefs.SetString(SaveKey, previousSave);
            PlayerPrefs.Save();
            yield return null;
        }

        IEnumerator Secure(FarmAnimal animal)
        {
            MovePlayer(animal.transform.position+Vector3.back*.6f);
            float timeout=Time.realtimeSinceStartup+4;
            while(!animal.CanCapture&&Time.realtimeSinceStartup<timeout)yield return null;
            animal.Capture(.01f);
            while(animal.State!=AnimalState.Carried&&Time.realtimeSinceStartup<timeout)yield return null;
            Assert.That(animal.State,Is.EqualTo(AnimalState.Carried));
            Assert.That(animal.GetComponent<NavMeshAgent>().enabled,Is.False);
            MovePlayer(animal.Home);float gateEnd=Time.realtimeSinceStartup+2;while(!session.CanDeposit&&Time.realtimeSinceStartup<gateEnd)yield return null;session.CaptureNearest();
            Assert.That(animal.State,Is.EqualTo(AnimalState.Secured),"Only delivery by the player secures the animal.");
        }
        [UnityTest]
        public IEnumerator CarryRequiresDeliveryAndSpeciesHaveDifferentDifficulty()
        {
            session.Introduce(10);session.Begin();yield return null;session.Thief.enabled=false;
            var cow=session.Animals.First(a=>a.TypeIndex==0);var pig=session.Animals.First(a=>a.TypeIndex==1);var chick=session.Animals.First(a=>a.TypeIndex==2);
            Assert.That(cow.CaptureMultiplier,Is.GreaterThan(pig.CaptureMultiplier));
            Assert.That(cow.CarrySpeedMultiplier,Is.LessThan(pig.CarrySpeedMultiplier));
            MovePlayer(chick.transform.position+Vector3.back*.6f);
            float end=Time.realtimeSinceStartup+7;
            while(!chick.Airborne&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(chick.Airborne,Is.True);Assert.That(chick.CanCapture,Is.False);
            chick.Capture(.01f);Assert.That(chick.State,Is.EqualTo(AnimalState.Wandering));
            MovePlayer(cow.transform.position+Vector3.back*.6f);session.CaptureNearest();
            Assert.That(cow.State,Is.EqualTo(AnimalState.BeingCaptured));
            yield return new WaitForSeconds(.8f);Assert.That(cow.State,Is.EqualTo(AnimalState.BeingCaptured));
            end=Time.realtimeSinceStartup+4;while(cow.State!=AnimalState.Carried&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(session.CarriedAnimal,Is.SameAs(cow));Assert.That(session.SecuredCount,Is.Zero);
            Assert.That(cow.transform.parent,Is.SameAs(session.Player.transform));
            session.CaptureNearest();Assert.That(session.SecuredCount,Is.Zero,"No delivery from outside the pen.");
            pig.Capture(.01f);Assert.That(pig.State,Is.EqualTo(AnimalState.Wandering),"Carry only one animal.");
            session.Pause();var carriedPosition=cow.transform.localPosition;yield return new WaitForSecondsRealtime(.15f);
            Assert.That(cow.transform.localPosition,Is.EqualTo(carriedPosition));session.Resume();yield return new WaitForSecondsRealtime(.3f);
            FarmPresentationTests.Capture("FarmGuardian-carry-gameplay.png",1920,1080);
            MovePlayer(cow.Home);float gateEnd=Time.realtimeSinceStartup+2;while(!session.CanDeposit&&Time.realtimeSinceStartup<gateEnd)yield return null;session.CaptureNearest();Assert.That(cow.State,Is.EqualTo(AnimalState.Secured));
            Assert.That(session.CarriedAnimal,Is.Null);Assert.That(session.SecuredCount,Is.EqualTo(1));
            session.CaptureNearest();Assert.That(session.SecuredCount,Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator LevelCapMigratesOldSaveAndRejectsLevel101()
        {
            PlayerPrefs.SetString(SaveKey,"{\"version\":1,\"unlockedLevel\":9000,\"coins\":123,\"speed\":2}");
            var migrated=FarmSave.Load();Assert.That(migrated.unlockedLevel,Is.EqualTo(100));
            Assert.That(migrated.coins,Is.EqualTo(123));Assert.That(migrated.speed,Is.EqualTo(2));
            Assert.That(testConfig.GetLevel(101).number,Is.EqualTo(100));
            session.Save.unlockedLevel=100;session.OpenLevelSelect();session.ChooseLevel(101);
            Assert.That(session.SelectedLevel,Is.LessThanOrEqualTo(100));
            session.Introduce(101);Assert.That(session.Level.number,Is.EqualTo(100));yield return null;
        }

        [UnityTest]
        public IEnumerator PenGateOpensForDeliveryClosesSafelyAndHasWoodenLabel()
        {
            Assert.That(session.World.Gates.Count,Is.EqualTo(4));
            Assert.That(GameObject.Find("Guardian crossroads"),Is.Null);
            Assert.That(GameObject.Find("Player identity marker"),Is.Null);
            var gate=session.World.Gates[0];Assert.That(gate.Barrier.enabled,Is.True);
            Assert.That(GameObject.Find("Wooden sign - COWS"),Is.Not.Null);
            yield return Secure(session.Animals[0]);Assert.That(gate.Passable,Is.True);
            session.Thief.gameObject.SetActive(false);MovePlayer(session.World.Layout.PlayerSpawn);
            yield return new WaitForSeconds(2.1f);
            Assert.That(gate.Openness,Is.LessThan(.02f));Assert.That(gate.Barrier.enabled,Is.True);
        }

        [UnityTest]
        public IEnumerator SecuredAnimalsRoamInsidePensWithoutOpeningGates()
        {
            session.Introduce(10);session.Begin();yield return null;session.Thief.enabled=false;
            foreach(var animal in session.Animals.GroupBy(a=>a.TypeIndex).Select(g=>g.First()).ToArray())yield return Secure(animal);
            session.Thief.gameObject.SetActive(false);MovePlayer(session.World.Layout.PlayerSpawn);
            yield return new WaitForSeconds(2.2f);
            var secured=session.Animals.Where(a=>a.State==AnimalState.Secured).ToArray();
            var origins=secured.Select(a=>a.transform.position).ToArray();var moved=new bool[secured.Length];
            int count=session.SecuredCount;Time.timeScale=3;
            for(int frame=0;frame<240;frame++)
            {
                for(int i=0;i<secured.Length;i++)
                {
                    Assert.That(secured[i].InsidePen(secured[i].transform.position,.05f),Is.True);
                    moved[i]|=Vector3.Distance(origins[i],secured[i].transform.position)>.15f;
                    Assert.That(session.World.GateFor(secured[i].TypeIndex).Passable,Is.False,"Secured animals cannot open their gate.");
                }
                Assert.That(session.SecuredCount,Is.EqualTo(count));yield return null;
            }
            Assert.That(moved.All(m=>m),Is.True,"Every secured species should move inside its pen.");
            var pen=secured[0];MovePlayer(pen.Home);yield return new WaitForSeconds(1);
            Assert.That(session.World.GateFor(pen.TypeIndex).Passable,Is.True);
            for(int frame=0;frame<120;frame++){Assert.That(pen.InsidePen(pen.transform.position,.05f),Is.True);yield return null;}
            Time.timeScale=1;
        }

        [UnityTest]
        public IEnumerator StunRecoveryKeepsTheLastPosition()
        {
            testConfig.thiefStunSeconds=.2f;
            session.Thief.Activate();session.Thief.Catch();var position=session.Thief.StunPosition;
            yield return new WaitForSeconds(.3f);
            Assert.That(Vector3.Distance(session.Thief.transform.position,position),Is.LessThan(.08f));
            Assert.That(session.Thief.State,Is.EqualTo(ThiefState.ReturningAfterStun));
        }

        [UnityTest]
        public IEnumerator TheftDeadlineCannotLoseOutsideTheVisibleExit()
        {
            testConfig.theftSeconds=.01f;testConfig.thiefStartDelay=0;testConfig.escapeDeadline=.1f;
            var animal=session.Animals[0];yield return Secure(animal);
            session.Thief.GetComponent<NavMeshAgent>().Warp(animal.Home);
            float until=Time.realtimeSinceStartup+4;
            while(session.Thief.State!=ThiefState.Escaping&&Time.realtimeSinceStartup<until)yield return null;
            Assert.That(session.Thief.State,Is.EqualTo(ThiefState.Escaping));
            var agent=session.Thief.GetComponent<NavMeshAgent>();agent.speed=0;
            var outside=session.EscapePosition+Vector3.left*1.25f;agent.Warp(outside);
            yield return new WaitForSeconds(.3f);
            Assert.That(session.Playing,Is.True);Assert.That(session.World.ExitZone.Contains(outside),Is.False);
            agent.Warp(session.EscapePosition);yield return null;yield return null;
            Assert.That(session.State,Is.EqualTo(SessionState.Playing));
            Assert.That(session.LostCount,Is.EqualTo(1));Assert.That(animal.State,Is.EqualTo(AnimalState.Lost));
            Assert.That(session.Thief.StolenAnimal,Is.Null);Assert.That(session.Thief.State,Is.EqualTo(ThiefState.Searching));
            session.AnimalLost(animal);Assert.That(session.LostCount,Is.EqualTo(1),"The same escaped animal cannot count twice.");
        }

        [UnityTest]
        public IEnumerator BurglarAndFarmerShareCarryPoseAndAnimalLift()
        {
            session.Save.equippedSkin=FarmSkinCatalog.Classic;session.Introduce(1);session.Begin();yield return new WaitForSecondsRealtime(.3f);
            testConfig.theftSeconds=.05f;var stolen=session.Animals[0];yield return Secure(stolen);
            var thief=session.Thief;var agent=thief.GetComponent<NavMeshAgent>();agent.Warp(stolen.Home+Vector3.forward*.7f);
            float deadline=Time.realtimeSinceStartup+4;while(thief.State!=ThiefState.Escaping&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(thief.StolenAnimal,Is.SameAs(stolen));Assert.That(stolen.transform.parent,Is.SameAs(thief.transform));
            thief.enabled=false;agent.isStopped=true;agent.velocity=Vector3.zero;thief.WalkAnimation.Drive(0,false,true);
            var carried=session.Animals[1];Assert.That(session.TryCarry(carried),Is.True);
            MovePlayer(session.World.Layout.ResponseHub+Vector3.left*1.3f);
            agent.Warp(session.World.Layout.ResponseHub+Vector3.right*1.3f);thief.transform.rotation=session.Player.transform.rotation=Quaternion.identity;
            yield return new WaitForSeconds(.4f);
            Assert.That(stolen.State,Is.EqualTo(AnimalState.Stolen));Assert.That(carried.State,Is.EqualTo(AnimalState.Carried));
            Assert.That(Vector3.Distance(stolen.transform.localPosition,carried.transform.localPosition),Is.LessThan(.025f));
            Assert.That(stolen.transform.localPosition.z,Is.EqualTo(.75f).Within(.01f));
            Assert.That(stolen.transform.localPosition.y,Is.EqualTo(.75f).Within(.02f));
            Assert.That(Quaternion.Angle(stolen.transform.localRotation,carried.transform.localRotation),Is.LessThan(1.5f));
            Assert.That(stolen.Animator.speed,Is.Zero);Assert.That(thief.WalkAnimation.Animator.GetBool("Carrying"),Is.True);
            Assert.That(thief.WalkAnimation.Animator.GetCurrentAnimatorStateInfo(0).IsName("Carrying"),Is.True);
            Assert.That(Quaternion.Angle(thief.WalkAnimation.leftArm.localRotation,session.Player.WalkAnimation.leftArm.localRotation),Is.LessThan(1));
            Assert.That(Quaternion.Angle(thief.WalkAnimation.rightArm.localRotation,session.Player.WalkAnimation.rightArm.localRotation),Is.LessThan(1));
            var camera=Camera.main;var follow=camera.GetComponent<FarmCamera>();follow.enabled=false;
            var center=(thief.transform.position+session.Player.transform.position)*.5f+Vector3.up;
            camera.transform.position=center+new Vector3(0,1.2f,5.3f);camera.transform.LookAt(center);camera.fieldOfView=40;
            var canvas=Object.FindFirstObjectByType<Canvas>();canvas.enabled=false;
            FarmPresentationTests.Capture("FarmGuardian-matching-carry.png",1920,1080);canvas.enabled=true;follow.enabled=true;
            var position=stolen.transform.localPosition;session.Pause();yield return new WaitForSecondsRealtime(.15f);
            Assert.That(stolen.transform.localPosition,Is.EqualTo(position));session.Resume();thief.Catch();yield return null;
            Assert.That(thief.StolenAnimal,Is.Null);Assert.That(stolen.transform.parent,Is.Not.SameAs(thief.transform));
            Assert.That(thief.WalkAnimation.Carrying,Is.False);Assert.That(thief.WalkAnimation.Stunned,Is.True);
        }
        [UnityTest]
        public IEnumerator CarryPoseLiftsBothArmsAndEnvironmentMotionPauses()
        {
            var animal=session.Animals[0];MovePlayer(animal.transform.position+Vector3.back*.5f);animal.Capture(.01f);
            float end=Time.realtimeSinceStartup+3;while(animal.State!=AnimalState.Carried&&Time.realtimeSinceStartup<end)yield return null;
            session.Player.WalkAnimation.Drive(2,false,true);yield return new WaitForSeconds(.4f);
            Assert.That(session.Player.WalkAnimation.Carrying,Is.True);
            Assert.That(Quaternion.Angle(session.Player.WalkAnimation.leftArm.localRotation,Quaternion.identity),Is.GreaterThan(45));
            Assert.That(Quaternion.Angle(session.Player.WalkAnimation.rightArm.localRotation,Quaternion.identity),Is.GreaterThan(45));
            var rotor=Object.FindFirstObjectByType<FarmWindmillRotor>();Assert.That(rotor,Is.Not.Null);
            var wind=Object.FindFirstObjectByType<FarmEnvironmentMotion>();Assert.That(wind,Is.Not.Null);
            var shader=Shader.Find("FarmGuardian/FoliageWind");Assert.That(shader.isSupported,Is.True);
            var treeRenderers=session.World.Root.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader==shader).ToArray();
            Assert.That(treeRenderers.Length,Is.GreaterThan(0));
            float angle=rotor.Angle,time=wind.WindTime;yield return new WaitForSeconds(.25f);
            Assert.That(rotor.Angle,Is.GreaterThan(angle));Assert.That(wind.WindTime,Is.GreaterThan(time));
            session.Pause();angle=rotor.Angle;time=wind.WindTime;yield return new WaitForSecondsRealtime(.15f);
            Assert.That(rotor.Angle,Is.EqualTo(angle));Assert.That(wind.WindTime,Is.EqualTo(time));session.Resume();
            var camera=Camera.main;var follow=camera.GetComponent<FarmCamera>();follow.enabled=false;
            camera.transform.position=session.Player.transform.position+new Vector3(3,2.4f,-3.2f);camera.transform.LookAt(session.Player.transform.position+Vector3.up);
            var canvas=Object.FindFirstObjectByType<Canvas>();canvas.enabled=false;
            FarmPresentationTests.Capture("FarmGuardian-carry-pose.png",1600,900);
            var bounds=rotor.GetComponent<Renderer>().bounds;
            camera.fieldOfView=40;camera.transform.position=bounds.center+new Vector3(0,1,6);camera.transform.LookAt(bounds.center);
            FarmPresentationTests.Capture("FarmGuardian-windmill-front.png",1600,900);
            yield return new WaitForSeconds(.6f);FarmPresentationTests.Capture("FarmGuardian-windmill-turn.png",1600,900);
            canvas.enabled=true;follow.enabled=true;
        }

        void MovePlayer(Vector3 position)
        {
            var controller = session.Player.GetComponent<CharacterController>();
            controller.enabled = false; session.Player.transform.position = position + Vector3.up * 0.05f; controller.enabled = true;
            Physics.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator ScenicMenuButtonsNavigateAndSkinsCannotChargeTwice()
        {
            session.Save.coins = 0; session.Save.goldenOutfit = false; session.Save.wearGoldenOutfit = false;
            session.MainMenu(); yield return null;
            Assert.That(session.config.menuBackground, Is.Not.Null);
            Assert.That(session.config.menuLogo, Is.Not.Null);
            Assert.That(session.config.menuButton, Is.Not.Null);
            foreach (string name in new[] { "PLAY", "SHOP", "SETTINGS" })
                Assert.That(GameObject.Find(name).GetComponent<UnityEngine.UI.Button>(), Is.Not.Null);
            GameObject.Find("SHOP").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(session.State, Is.EqualTo(SessionState.Shop)); session.CloseShop(); yield return null;
            GameObject.Find("SETTINGS").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(session.State, Is.EqualTo(SessionState.Settings));
            bool before = session.Save.sound; session.ToggleSound(); Assert.That(session.Save.sound, Is.Not.EqualTo(before));
            session.CloseMenuPanel(); yield return null;
            GameObject.Find("SHOP").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;
            GameObject.Find("SKINS").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return null;
            Assert.That(session.State, Is.EqualTo(SessionState.Shop));
            Assert.That(session.BuyOrToggleOutfit(), Is.False);
            session.Save.coins = session.config.goldenOutfitCost + 10;
            Assert.That(session.BuyOrToggleOutfit(), Is.True);
            Assert.That(session.Save.coins, Is.EqualTo(10)); Assert.That(session.Save.wearGoldenOutfit, Is.True);
            Assert.That(session.BuyOrToggleOutfit(), Is.True);
            Assert.That(session.Save.coins, Is.EqualTo(10)); Assert.That(session.Save.wearGoldenOutfit, Is.False);
            session.CloseShop(); yield return null;
            GameObject.Find("PLAY").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(session.State, Is.EqualTo(SessionState.LevelSelect));
            GameObject.Find("Level 1").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(session.State, Is.EqualTo(SessionState.LevelIntroduction));
        }

        [UnityTest]
        public IEnumerator CowAnimatorFollowsAgentMovement()
        {
            session.Player.enabled=false;
            var cow=session.Animals.First(a=>a.TypeIndex==0);
            var animator=cow.Animator;var agent=cow.GetComponent<NavMeshAgent>();
            Assert.That(animator.runtimeAnimatorController,Is.SameAs(session.config.animalAnimators[0]));
            var destination=cow.WanderOrigin+Vector3.right*1.6f;
            Assert.That(NavMesh.SamplePosition(destination,out var hit,.3f,NavMesh.AllAreas),Is.True);
            agent.SetDestination(hit.position);
            float end=Time.realtimeSinceStartup+5;
            while((!animator.GetCurrentAnimatorStateInfo(0).IsName("Walk")||agent.velocity.magnitude<.2f)&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(agent.velocity.magnitude,Is.GreaterThan(.2f));
            Assert.That(animator.GetBool("Moving"),Is.True);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"),Is.True,"Live cow Animator must enter Walk while NavMeshAgent moves.");
            float time=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            yield return new WaitForSeconds(.2f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,Is.GreaterThan(time));
            agent.isStopped=true;agent.ResetPath();agent.velocity=Vector3.zero;
            // Capture holds the agent stationary while the normal animal driver continues updating Moving.
            MovePlayer(cow.transform.position+Vector3.back*.5f);cow.Capture(10);
            yield return new WaitForSeconds(.3f);
            Assert.That(animator.GetBool("Moving"),Is.False);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),Is.True);
        }

        [UnityTest]
        public IEnumerator CharacterRigsSwingLimbsAndDeformTheSuppliedMeshes()
        {
            session.Player.enabled = false;
            session.Thief.enabled = false;
            session.Thief.Visual.gameObject.SetActive(true);
            foreach (var rig in new[] { session.Player.WalkAnimation, session.Thief.WalkAnimation })
            {
                Assert.That(rig, Is.Not.Null);
                Assert.That(rig.Animator,Is.Not.Null);
                Assert.That(rig.Animator.runtimeAnimatorController,Is.Not.Null);
                Assert.That(rig.Animator.runtimeAnimatorController.animationClips.Length,Is.EqualTo(5));
                Assert.That(rig.Animator.applyRootMotion,Is.False);
                var skin = rig.GetComponentInChildren<SkinnedMeshRenderer>();
                Assert.That(skin, Is.Not.Null);
                Assert.That(skin.bones.Length, Is.EqualTo(7));
                Assert.That(skin.sharedMesh.boneWeights.Length, Is.EqualTo(skin.sharedMesh.vertexCount));
                var baked = new Mesh(); skin.BakeMesh(baked); var rest = baked.vertices;
                rig.Drive(2.8f);
                yield return new WaitForSeconds(.14f);
                Assert.That(Quaternion.Angle(rig.leftLeg.localRotation, rig.rightLeg.localRotation), Is.GreaterThan(5));
                Assert.That(Quaternion.Angle(rig.leftArm.localRotation, rig.rightArm.localRotation), Is.GreaterThan(5));
                skin.BakeMesh(baked); var pose = baked.vertices;
                Assert.That(pose.Where((point, index) => Vector3.Distance(point, rest[index]) > .005f).Count(), Is.GreaterThan(20));
                rig.Drive(0);
                yield return new WaitForSeconds(.3f);
                Assert.That(Quaternion.Angle(rig.leftLeg.localRotation, Quaternion.identity), Is.LessThan(1));
                rig.Drive(2,true,false);yield return new WaitForSeconds(.3f);
                Assert.That(rig.Animator.GetCurrentAnimatorStateInfo(0).IsName("Stunned"),Is.True);
                rig.Drive(0,false,true);yield return new WaitForSeconds(.3f);
                Assert.That(rig.Animator.GetCurrentAnimatorStateInfo(0).IsName("Carrying"),Is.True);
                Assert.That(Quaternion.Angle(rig.leftArm.localRotation,Quaternion.identity),Is.GreaterThan(45));
                rig.Drive(0);yield return new WaitForSeconds(.3f);
                Assert.That(rig.Animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),Is.True);
                Object.Destroy(baked);
            }
        }

        [UnityTest]
        public IEnumerator RandomThiefEntrancesStayReachableAndAwayFromPlayerAndPens()
        {
            foreach (int level in new[] { 1, 16, 31, 46 })
            {
                session.Introduce(level); session.Begin(); yield return null;
                var used = new System.Collections.Generic.HashSet<int>();
                for (int i = 0; i < 24; i++)
                {
                    Vector3 spawn = session.World.ChooseThiefSpawn(session.Thief.transform.position, true);
                    Assert.That(Vector3.Distance(spawn, session.Player.transform.position), Is.GreaterThanOrEqualTo(8));
                    Assert.That(session.Animals.All(a => Vector3.Distance(a.Home, spawn) >= 6), Is.True);
                    used.Add(Mathf.RoundToInt(spawn.x / 3) * 100 + Mathf.RoundToInt(spawn.z / 3));
                    var path = new NavMeshPath();
                    foreach (var animal in session.Animals)
                    {
                        Assert.That(NavMesh.CalculatePath(spawn, animal.Home, NavMesh.AllAreas, path), Is.True);
                        Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
                    }
                }
                Assert.That(used.Count, Is.GreaterThan(1), "Thief spawn should vary between eligible perimeter entrances.");
            }
        }

        [UnityTest]
        public IEnumerator CaptureRequiresRangeAndTwoAnimalsHaveLiveAnimation()
        {
            MovePlayer(new Vector3(0, 0, -15));
            session.CaptureNearest();
            Assert.That(session.Animals.All(a => a.State == AnimalState.Wandering), Is.True);
            var first = session.Animals[0];
            MovePlayer(first.transform.position + Vector3.back * 1.3f);
            session.CaptureNearest();
            Assert.That(first.State, Is.EqualTo(AnimalState.BeingCaptured));
            Assert.That(session.SecuredCount, Is.Zero);
            foreach (var animal in session.Animals.Take(2))
            {
                Assert.That(animal.Animator, Is.Not.Null);
                Assert.That(animal.Animator.runtimeAnimatorController.animationClips.Length, Is.GreaterThanOrEqualTo(2));
            }
            yield return new WaitForSeconds(0.25f);
            foreach (var animal in session.Animals.Take(2))
                Assert.That(animal.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator LifeAndScoreTrackObjectivesWithoutChangingCoins()
        {
            session.Introduce(2);session.Begin();yield return new WaitForSecondsRealtime(.3f);session.Thief.enabled=false;
            session.Save.coins=739;
            Assert.That(session.Lives,Is.EqualTo(4));Assert.That(session.Score,Is.Zero);
            var animal=session.Animals[0];yield return Secure(animal);
            Assert.That(session.Score,Is.EqualTo(100));Assert.That(session.Save.coins,Is.EqualTo(739));
            animal.Steal();Assert.That(session.Score,Is.Zero);Assert.That(session.Lives,Is.EqualTo(4),"Temporary theft is not a permanent loss.");
            MovePlayer(animal.transform.position+Vector3.back*.5f);animal.SecureRecovered();
            Assert.That(animal.State,Is.EqualTo(AnimalState.Carried));MovePlayer(animal.Home);
            float deadline=Time.realtimeSinceStartup+3;while(!session.CanDeposit&&Time.realtimeSinceStartup<deadline)yield return null;
            session.CaptureNearest();Assert.That(session.Score,Is.EqualTo(100),"Recovery must not farm extra score.");
            session.Plots[0].Plant();session.Plots[0].Plant();Assert.That(session.Score,Is.EqualTo(250));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(GameObject.Find("Life display").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo("LIFE: 4 / 4"));
            Assert.That(GameObject.Find("Score display").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo("SCORE: 250"));
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(1440,1080),new Vector2Int(2340,1080)})
                FarmPresentationTests.Capture("FarmGuardian-life-score-"+size.x+".png",size.x,size.y);
            session.Pause();yield return new WaitForSecondsRealtime(.2f);Assert.That(session.Score,Is.EqualTo(250));session.Resume();
            animal.Steal();session.AnimalLost(animal);session.AnimalLost(animal);
            Assert.That(session.Lives,Is.EqualTo(3));Assert.That(session.Score,Is.EqualTo(150));
            foreach(var other in session.Animals.Where(a=>a!=animal).ToArray()){other.Steal();session.AnimalLost(other);}
            Assert.That(session.State,Is.EqualTo(SessionState.Lost));Assert.That(session.Lives,Is.Zero);Assert.That(session.Score,Is.EqualTo(150));
            Assert.That(session.Save.coins,Is.EqualTo(739));yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Object.FindObjectsByType<TMPro.TMP_Text>().Any(t=>t.text.Contains("Life: 0 / 4    Score: 150")),Is.True);
            FarmPresentationTests.Capture("FarmGuardian-life-score-result.png",1920,1080);
            session.Introduce(2);session.Begin();yield return null;
            Assert.That(session.Score,Is.Zero);Assert.That(session.Lives,Is.EqualTo(4));
        }
        [UnityTest]
        public IEnumerator FullWinAwardsOnceAndRetryDoesNotDuplicateActors()
        {
            foreach (var animal in session.Animals.ToArray()) yield return Secure(animal);
            Assert.That(session.State, Is.EqualTo(SessionState.Won));
            Assert.That(session.Score,Is.EqualTo(300));Assert.That(session.Lives,Is.EqualTo(3));
            Assert.That(session.EarnedStars,Is.EqualTo(3));Assert.That(session.Save.BestStars(1),Is.EqualTo(3));
            int coins = session.Save.coins;
            Assert.That(coins, Is.GreaterThan(0));
            session.Finish(true, "duplicate");
            Assert.That(session.Save.coins, Is.EqualTo(coins));
            float time = session.Remaining;
            yield return null;
            Assert.That(session.Remaining, Is.EqualTo(time));
            session.Introduce(1); session.Begin(); yield return null;
            Assert.That(Object.FindObjectsByType<FarmAnimal>(FindObjectsSortMode.None).Length, Is.EqualTo(3));
            Assert.That(Object.FindObjectsByType<FarmPlayer>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<FarmThief>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(session.State, Is.EqualTo(SessionState.Playing));
        }

        [UnityTest]
        public IEnumerator TheftIsRecoverableHoldResetsAndStunPauses()
        {
            testConfig.thiefStartDelay = 0;
            testConfig.theftSeconds = 0.1f;
            var animal = session.Animals[0];
            yield return Secure(animal);
            Assert.That(session.Thief.State, Is.Not.EqualTo(ThiefState.Inactive));
            session.Thief.GetComponent<NavMeshAgent>().Warp(animal.Home + Vector3.forward * 0.7f);
            float timeout = Time.realtimeSinceStartup + 4;
            while (session.Thief.State != ThiefState.Escaping && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(session.Thief.State, Is.EqualTo(ThiefState.Escaping));
            Assert.That(animal.State, Is.EqualTo(AnimalState.Stolen));
            Assert.That(session.State, Is.EqualTo(SessionState.Playing));
            Assert.That(session.SecuredCount, Is.EqualTo(0));
            MovePlayer(session.Thief.transform.position - Vector3.forward * 0.8f);
            Assert.That(session.CanCatchThief, Is.True);
            session.UpdateHold(true, false, 1.9f);
            Assert.That(session.Thief.State, Is.EqualTo(ThiefState.Escaping));
            MovePlayer(new Vector3(0, 0, -10));
            session.UpdateHold(true, false, 0.2f);
            Assert.That(session.HoldProgress, Is.EqualTo(0));
            MovePlayer(session.Thief.transform.position - Vector3.forward * 0.8f);
            session.UpdateHold(true, false, 1f);
            Assert.That(session.HoldProgress, Is.EqualTo(0.5f).Within(0.01f));
            session.UpdateHold(true, false, 1.01f);
            Assert.That(session.Thief.State, Is.EqualTo(ThiefState.Stunned));
            Assert.That(session.Thief.StunRemaining, Is.EqualTo(15).Within(0.01f));
            Assert.That(animal.State, Is.EqualTo(AnimalState.Carried));
            Assert.That(session.Thief.StolenAnimal, Is.Null);
            session.Pause(); float stun = session.Thief.StunRemaining, remaining = session.Remaining;
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(session.Thief.StunRemaining, Is.EqualTo(stun));
            Assert.That(session.Remaining, Is.EqualTo(remaining));
            session.Resume(); Time.timeScale = 20;
            yield return new WaitForSeconds(15.2f);
            Time.timeScale = 1;
            Assert.That(session.Thief.State, Is.Not.EqualTo(ThiefState.Stunned));
        }

        [UnityTest]
        public IEnumerator ThiefStartsMovingOnAppearanceAndPatrolsWhilePensAreEmpty()
        {
            testConfig.thiefStartDelay=4; // Legacy configuration must not hold a visible thief still.
            yield return Secure(session.Animals[0]);
            Assert.That(session.Thief.State,Is.EqualTo(ThiefState.MovingToTarget));
            var origin=session.Thief.transform.position;float deadline=Time.realtimeSinceStartup+1.5f;
            while(Vector3.Distance(origin,session.Thief.transform.position)<.15f&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(Vector3.Distance(origin,session.Thief.transform.position),Is.GreaterThan(.15f),"A visible thief must move before the former four-second delay.");
            session.Introduce(2);session.Begin();yield return null;session.Thief.Activate();
            Assert.That(session.SecuredCount,Is.Zero);Assert.That(session.Thief.State,Is.EqualTo(ThiefState.Searching));
            origin=session.Thief.transform.position;deadline=Time.realtimeSinceStartup+1.5f;
            while(Vector3.Distance(origin,session.Thief.transform.position)<.15f&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(Vector3.Distance(origin,session.Thief.transform.position),Is.GreaterThan(.15f),"Without a secured target, the thief must patrol instead of idling at spawn.");
        }

        [UnityTest]
        public IEnumerator EscapeAndTimeoutEachLoseWithoutRewards()
        {
            foreach(var animal in session.Animals){animal.Steal();session.AnimalLost(animal);}session.CheckWin();
            Assert.That(session.State, Is.EqualTo(SessionState.Lost));
            Assert.That(session.PermanentLoss, Is.True);
            Assert.That(session.Save.coins, Is.Zero);
            Assert.That(session.EarnedStars,Is.Zero);Assert.That(session.LostCount,Is.EqualTo(3));
            testConfig.timeLimits = new[] { 0.05f };
            // Configuration intentionally enforces a 60s lower bound, so advance scaled time.
            session.Introduce(1); session.Begin();
            session.Animals[0].Steal();session.AnimalLost(session.Animals[0]);
            Time.timeScale = 100;
            yield return new WaitForSeconds(61);
            Time.timeScale = 1;
            Assert.That(session.State, Is.EqualTo(SessionState.Lost));
            Assert.That(session.Remaining, Is.Zero);
            Assert.That(session.ResultReason, Is.EqualTo("Time expired"));
            Assert.That(session.Reward, Is.Zero);
        }

        [UnityTest]
        public IEnumerator PartialLossWinsWithTwoStarsAndKeepsTheBestSavedScore()
        {
            session.Thief.enabled=false;
            session.Save.RecordStars(1,3);
            var lost=session.Animals[0];lost.Steal();session.AnimalLost(lost);
            Assert.That(session.Playing,Is.True);Assert.That(session.CurrentStars,Is.EqualTo(2));
            session.Finish(true,"premature");Assert.That(session.Playing,Is.True);
            foreach(var animal in session.Animals.Where(a=>a.State!=AnimalState.Lost).ToArray())yield return Secure(animal);
            Assert.That(session.State,Is.EqualTo(SessionState.Won));Assert.That(session.EarnedStars,Is.EqualTo(2));
            Assert.That(session.SecuredCount,Is.EqualTo(2));Assert.That(session.Save.unlockedLevel,Is.GreaterThanOrEqualTo(2));
            Assert.That(FarmSave.Load().BestStars(1),Is.EqualTo(3),"A lower replay must not replace the best score.");
            int coins=session.Save.coins;session.CheckWin();session.Finish(true,"duplicate");Assert.That(session.Save.coins,Is.EqualTo(coins));
            yield return new WaitForSecondsRealtime(.25f);FarmPresentationTests.Capture("FarmGuardian-two-star-win.png",1920,1080);
        }

        [UnityTest]
        public IEnumerator MajorityLossWinsWithOneStarAndRetryClearsRunCounters()
        {
            session.Thief.enabled=false;
            foreach(var animal in session.Animals.Take(2).ToArray()){animal.Steal();session.AnimalLost(animal);}
            Assert.That(session.Playing,Is.True);Assert.That(session.CurrentStars,Is.EqualTo(1));
            yield return Secure(session.Animals[2]);
            Assert.That(session.State,Is.EqualTo(SessionState.Won));Assert.That(session.EarnedStars,Is.EqualTo(1));
            Assert.That(FarmSave.Load().BestStars(1),Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(.25f);FarmPresentationTests.Capture("FarmGuardian-one-star-win.png",1920,1080);
            session.Introduce(1);session.Begin();yield return null;
            Assert.That(session.LostCount,Is.Zero);Assert.That(session.EarnedStars,Is.Zero);Assert.That(session.CurrentStars,Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator EightAnimalRatingBoundaryAndPlantingStillGateCompletion()
        {
            session.Introduce(10);session.Begin();yield return null;session.Thief.enabled=false;
            Assert.That(session.Level.animalCount,Is.EqualTo(8));Assert.That(session.OneStarLossThreshold,Is.EqualTo(4));
            for(int i=0;i<3;i++){session.Animals[i].Steal();session.AnimalLost(session.Animals[i]);}
            Assert.That(session.CurrentStars,Is.EqualTo(2));
            session.Animals[3].Steal();session.AnimalLost(session.Animals[3]);Assert.That(session.CurrentStars,Is.EqualTo(1));
            session.Introduce(2);session.Begin();yield return null;session.Thief.enabled=false;
            session.Animals[0].Steal();session.AnimalLost(session.Animals[0]);
            foreach(var animal in session.Animals.Skip(1).ToArray())yield return Secure(animal);
            Assert.That(session.Playing,Is.True,"Planting remains required after a theft.");
            MovePlayer(session.Plots[0].transform.position+Vector3.back*1.5f);
            session.UpdateHold(false,true,testConfig.plantingSeconds+.01f);
            Assert.That(session.State,Is.EqualTo(SessionState.Won));Assert.That(session.EarnedStars,Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator OldSaveMigratesWithoutInventingStarsAndScoresSurviveReload()
        {
            PlayerPrefs.SetString(SaveKey,"{\"version\":1,\"unlockedLevel\":12,\"coins\":123,\"speed\":2}");
            var migrated=FarmSave.Load();Assert.That(migrated.unlockedLevel,Is.EqualTo(12));Assert.That(migrated.coins,Is.EqualTo(123));
            Assert.That(migrated.BestStars(1),Is.Zero);migrated.RecordStars(12,2);migrated.Store();
            Assert.That(FarmSave.Load().BestStars(12),Is.EqualTo(2));
            migrated.RecordStars(12,1);migrated.Store();Assert.That(FarmSave.Load().BestStars(12),Is.EqualTo(2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FourThemesHaveReachablePensAndPlantingGatesWin()
        {
            foreach (int level in new[] { 2, 16, 31, 46 })
            {
                session.Introduce(level); session.Begin(); yield return null;
                Assert.That(session.World.Root, Is.Not.Null);
                foreach (var animal in session.Animals)
                {
                    var route = new NavMeshPath();
                    Assert.That(NavMesh.CalculatePath(animal.transform.position, animal.Home, NavMesh.AllAreas, route), Is.True);
                    Assert.That(route.status, Is.EqualTo(NavMeshPathStatus.PathComplete), "Pen unreachable on level " + level);
                }
                var escape = new NavMeshPath();
                Assert.That(NavMesh.CalculatePath(session.Thief.transform.position, session.EscapePosition, NavMesh.AllAreas, escape), Is.True);
                Assert.That(escape.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
            }
            session.Introduce(2); session.Begin(); yield return null;
            Assert.That(session.Plots.Count, Is.EqualTo(1));
            foreach (var animal in session.Animals.ToArray()) yield return Secure(animal);
            Assert.That(session.State, Is.EqualTo(SessionState.Playing), "Unfinished planting must prevent a win.");
            MovePlayer(session.Plots[0].transform.position + Vector3.back * 1.5f);
            session.UpdateHold(false, true, testConfig.plantingSeconds + 0.01f);
            Assert.That(session.PlantedCount, Is.EqualTo(1));
            Assert.That(session.State, Is.EqualTo(SessionState.Won));
        }

        [Test]
        public void SaveValidationAndPurchasesRejectInvalidState()
        {
            string backup = PlayerPrefs.GetString(SaveKey, "");
            try
            {
                PlayerPrefs.SetString(SaveKey, "invalid json");
                Assert.That(FarmSave.Load().unlockedLevel, Is.EqualTo(1));
                PlayerPrefs.SetString(SaveKey, "{\"version\":1,\"coins\":-50,\"unlockedLevel\":-1,\"speed\":100}");
                var save = FarmSave.Load();
                Assert.That(save.coins, Is.Zero); Assert.That(save.unlockedLevel, Is.EqualTo(1)); Assert.That(save.speed, Is.EqualTo(4));
                var config = ScriptableObject.CreateInstance<FarmConfig>();
                save = new FarmSave();
                Assert.That(save.BuyUpgrade(0, config), Is.False);
                save.coins = 1000;
                Assert.That(save.BuyUpgrade(0, config), Is.True);
                Assert.That(save.coins, Is.EqualTo(940));
                Assert.That(FarmSave.Load().speed, Is.EqualTo(1));
                Assert.That(save.BuyUpgrade(4, config), Is.False);
                Object.DestroyImmediate(config);
            }
            finally { if (backup.Length == 0) PlayerPrefs.DeleteKey(SaveKey); else PlayerPrefs.SetString(SaveKey, backup); PlayerPrefs.Save(); }
        }
    }
}



