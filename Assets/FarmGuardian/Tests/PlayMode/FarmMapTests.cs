using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FarmGuardian.Tests
{
    public sealed class FarmMapTests
    {
        FarmSession session;
        FarmConfig config;
        string save;
        const string SaveKey = "FarmGuardian.Save.v1";

        [UnitySetUp]
        public IEnumerator Setup()
        {
            save = PlayerPrefs.GetString(SaveKey,"");
            yield return SceneManager.LoadSceneAsync("FarmGuardian"); yield return null;
            session = Object.FindFirstObjectByType<FarmSession>();
            config = Object.Instantiate(session.config); session.config = config;
            session.Save.unlockedLevel = 100; session.Save.sound = false; session.Save.speed = 0;
            config.plantingChance = 1; config.thiefStartDelay = 0; config.theftSeconds = .1f;
            config.thiefBaseSpeed = config.thiefMaxSpeed;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Time.timeScale = 1;
            if (session != null) session.MainMenu();
            if (config != null) Object.Destroy(config);
            if (string.IsNullOrEmpty(save)) PlayerPrefs.DeleteKey(SaveKey); else PlayerPrefs.SetString(SaveKey,save);
            PlayerPrefs.Save(); yield return null;
        }

        IEnumerator Begin(int level)
        { session.Introduce(level); session.Begin(); yield return null; session.Player.enabled = false; }

        void PlacePlayer(Vector3 position)
        {
            var controller = session.Player.GetComponent<CharacterController>();
            controller.enabled = false; session.Player.transform.position = position + Vector3.up * .04f;
            controller.enabled = true; Physics.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator FourLayoutsHaveReachableObjectivesClearLanesAndInterceptionWindows()
        {
            foreach (int level in new[] {15,30,45,60})
            {
                yield return Begin(level);
                var layout = session.World.Layout;
                Assert.That(layout.Theme,Is.EqualTo((level-1)/15));
                Assert.That(session.Animals.Count,Is.EqualTo(8));
                session.World.ValidateNavigation(session.Level);
                foreach (var route in layout.Routes) Assert.That(route.Width,Is.GreaterThanOrEqualTo(2.8f));
                Assert.That(layout.Pens.Select(p => (p.x<0?0:1)+(p.z<0?0:2)).Distinct().Count(),Is.EqualTo(4));
                foreach(var pen in layout.Pens)
                {
                    Assert.That(Mathf.Abs(pen.x),Is.LessThan(15));Assert.That(Mathf.Abs(pen.z),Is.LessThan(11));
                    foreach(var other in layout.Pens.Where(p=>p!=pen))Assert.That(Vector3.Distance(pen,other),Is.GreaterThan(12));
                }
                if (layout.Theme == 1)
                    Assert.That(NavMesh.Raycast(FarmWorld.RequirePoint(FarmMapLayout.Point(-7,-8),"Grove approach"),FarmWorld.RequirePoint(FarmMapLayout.Point(-2,-8),"Grove exit"),out var groveHit,NavMesh.AllAreas),Is.True,"Woodland grove blocks the direct route.");
                if (layout.Theme == 2)
                {
                    Assert.That(NavMesh.Raycast(FarmMapLayout.Point(-3,-6.5f),FarmMapLayout.Point(3,-6.5f),out var rockHit,NavMesh.AllAreas),Is.True);
                    foreach (var crossing in layout.CrossingCentres)
                        Assert.That(NavMesh.Raycast(crossing+Vector3.left*4,crossing+Vector3.right*4,out var passHit,NavMesh.AllAreas),Is.False,"Both mountain crossings must work.");
                }
                if (layout.Theme == 3)
                {
                    Assert.That(layout.Escape.x,Is.LessThan(0));
                    Assert.That(NavMesh.Raycast(FarmMapLayout.Point(2,-5.5f),FarmMapLayout.Point(7.5f,-5.5f),out var barnHit,NavMesh.AllAreas),Is.True,"Working barn divides the lower service area.");
                }
                foreach (var animal in session.Animals)
                {
                    float escape = FarmWorld.RouteLength(animal.Home,layout.Escape);
                    float margin = InterceptionMargin(layout.ResponseHub,animal.Home,layout.Escape,escape);
                    Assert.That(margin,Is.GreaterThan(1),layout.Name + " must allow response from its guardian hub before escape.");
                    Assert.That(Vector3.Distance(animal.WanderOrigin,layout.AnimalSpawns[session.Animals.IndexOf(animal)]),Is.LessThan(.21f));
                }
                // Test the exact boundary controller and a real building collider, not only NavMesh connectivity.
                var controller = session.Player.GetComponent<CharacterController>();
                PlacePlayer(new Vector3(-20,0,-15));
                controller.Move(Vector3.left*5); Assert.That(session.Player.transform.position.x,Is.GreaterThan(-20.7f));
                PlacePlayer(new Vector3(19,0,15));
                controller.Move(Vector3.forward*5); Assert.That(session.Player.transform.position.z,Is.LessThan(16.7f));
                var barn = layout.Obstacles.First(o => o.Asset == "Barn");
                PlacePlayer(barn.Position + Vector3.right*(barn.Size.x*.5f+1));
                controller.Move(Vector3.left*2);
                Assert.That(session.Player.transform.position.x,Is.GreaterThan(barn.Position.x+barn.Size.x*.5f+.2f));
            }
            Assert.That(config.GetLevel(61).theme,Is.Zero);
            Assert.That(config.GetLevel(45).theme,Is.EqualTo(2)); Assert.That(config.GetLevel(46).theme,Is.EqualTo(3));
        }

        float InterceptionMargin(Vector3 hub,Vector3 home,Vector3 exit,float length)
        {
            var route = new NavMeshPath(); NavMesh.CalculatePath(home,exit,NavMesh.AllAreas,route);
            float travelled = 0, best = float.NegativeInfinity;
            for (int i = 1; i < route.corners.Length; i++)
            {
                float segment = Vector3.Distance(route.corners[i-1],route.corners[i]);
                for (int j = 0; j < 5; j++)
                {
                    float fraction = j/4f;
                    var intercept = Vector3.Lerp(route.corners[i-1],route.corners[i],fraction);
                    float playerSeconds = Mathf.Max(0,FarmWorld.RouteLength(hub,intercept)-config.interactionRange)/config.playerSpeed;
                    float thiefSeconds = (travelled+segment*fraction)/config.thiefMaxSpeed;
                    best = Mathf.Max(best,length/config.thiefMaxSpeed-Mathf.Max(playerSeconds,thiefSeconds)-config.thiefCatchSeconds);
                }
                travelled += segment;
            }
            return best;
        }

        [UnityTest]
        public IEnumerator AnimalsStayLocalAndCanBeCarriedToEveryPenOnEveryLayout()
        {
            foreach (int level in new[] {15,30,45,60})
            {
                yield return Begin(level); session.Thief.enabled = false;
                Time.timeScale = 6;
                yield return new WaitForSeconds(6);
                foreach (var animal in session.Animals)
                {
                    Assert.That(Vector3.Distance(animal.transform.position,animal.WanderOrigin),Is.LessThan(FarmAnimal.WanderRadius+.3f));
                    yield return CarryToPen(animal);
                }
                Assert.That(session.SecuredCount,Is.EqualTo(8),session.Level.ThemeName+" animals must be carried into pens.");                Time.timeScale = 1;
            }
        }

        IEnumerator CarryToPen(FarmAnimal animal)
        {
            var path=new NavMeshPath();Assert.That(NavMesh.CalculatePath(animal.transform.position,animal.Home,NavMesh.AllAreas,path),Is.True);
            Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete));
            PlacePlayer(animal.transform.position+Vector3.back*.5f);
            float end=Time.realtimeSinceStartup+4;
            while(!animal.CanCapture&&Time.realtimeSinceStartup<end)yield return null;
            animal.Capture(.01f);
            while(animal.State!=AnimalState.Carried&&Time.realtimeSinceStartup<end)yield return null;
            Assert.That(animal.State,Is.EqualTo(AnimalState.Carried));
            PlacePlayer(animal.Home);float gateEnd=Time.realtimeSinceStartup+2;while(!session.CanDeposit&&Time.realtimeSinceStartup<gateEnd)yield return null;session.CaptureNearest();
            Assert.That(animal.State,Is.EqualTo(AnimalState.Secured));
        }
        IEnumerator StartEscape()
        {
            var animal = session.Animals[0];
            yield return CarryToPen(animal);
            float timeout=Time.realtimeSinceStartup+4;            session.Thief.GetComponent<NavMeshAgent>().Warp(animal.Home + Vector3.back*.6f);
            while (session.Thief.State != ThiefState.Escaping && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(session.Thief.State,Is.EqualTo(ThiefState.Escaping));
        }

        [UnityTest]
        public IEnumerator BaseSpeedPlayerCanChaseAndCatchFromResponseHubOnEveryLayout()
        {
            foreach (int level in new[] {15,30,45,60})
            {
                yield return Begin(level); PlacePlayer(session.World.Layout.ResponseHub);
                yield return StartEscape();
                var controller = session.Player.GetComponent<CharacterController>();
                var route = new NavMeshPath();
                float timeout = Time.realtimeSinceStartup+16;
                while (session.Thief.State != ThiefState.Stunned && session.Playing && Time.realtimeSinceStartup < timeout)
                {
                    Vector3 start = FarmWorld.RequirePoint(session.Player.transform.position,"Chase player");
                    Assert.That(NavMesh.CalculatePath(start,session.Thief.transform.position,NavMesh.AllAreas,route),Is.True);
                    Assert.That(route.status,Is.EqualTo(NavMeshPathStatus.PathComplete));
                    if (route.corners.Length > 1)
                    {
                        Vector3 delta = route.corners[1] - session.Player.transform.position; delta.y = 0;
                        controller.Move(delta.normalized*Mathf.Min(config.playerSpeed*Time.deltaTime,delta.magnitude) + Vector3.down*2*Time.deltaTime);
                    }
                    session.SetCatchHeld(session.CanCatchThief);
                    yield return null;
                }
                Assert.That(session.Thief.State,Is.EqualTo(ThiefState.Stunned),session.Level.ThemeName + " should permit a physical base-speed chase from the response hub.");
                Assert.That(session.Animals[0].State,Is.EqualTo(AnimalState.Carried));
            }
        }

        [UnityTest]
        public IEnumerator UnchallengedThiefActuallyReachesEachExitBeforeDeadline()
        {
            foreach (int level in new[] {15,30,45,60})
            {
                yield return Begin(level); yield return StartEscape(); Time.timeScale = 5;
                float timeout = Time.realtimeSinceStartup+12;
                while (session.LostCount==0 && session.Playing && Time.realtimeSinceStartup < timeout) yield return null;
                Assert.That(session.State,Is.EqualTo(SessionState.Playing));Assert.That(session.LostCount,Is.EqualTo(1));
                Assert.That(Vector3.Distance(session.Animals[0].transform.position,session.EscapePosition),Is.LessThan(2f),
                    "Loss must come from reaching the real exit, not an impossible-route deadline.");
                Time.timeScale = 1;
            }
        }
    }
}



