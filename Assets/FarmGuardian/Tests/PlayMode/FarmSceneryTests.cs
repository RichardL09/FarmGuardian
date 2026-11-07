using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FarmGuardian.Tests
{
    public sealed class FarmSceneryTests
    {
        FarmSession session;string save;
        [UnitySetUp]public IEnumerator Setup()
        {save=PlayerPrefs.GetString("FarmGuardian.Save.v1","");yield return SceneManager.LoadSceneAsync("FarmGuardian");yield return null;yield return null;session=Object.FindFirstObjectByType<FarmSession>();session.Save.unlockedLevel=100;session.Save.sound=false;}
        [UnityTearDown]public IEnumerator Cleanup()
        {Time.timeScale=1;if(session!=null)session.MainMenu();if(save.Length==0)PlayerPrefs.DeleteKey("FarmGuardian.Save.v1");else PlayerPrefs.SetString("FarmGuardian.Save.v1",save);PlayerPrefs.Save();yield return null;}
        [UnityTest]public IEnumerator DenseSceneryUsesOriginalAssetsAndKeepsAllFourMapsPlayable()
        {
            var set=Resources.Load<FarmScenerySet>("FarmScenery");Assert.That(set,Is.Not.Null);Assert.That(set.themes.Length,Is.EqualTo(4));
            Assert.That(set.sourceNames,Does.Contain("Well").Or.Contain("Daisy"));
#if UNITY_EDITOR
            Assert.That(UnityEditor.AssetDatabase.IsValidFolder("Assets/FarmGuardianRedesign"),Is.False);
#endif
            Assert.That(SceneUtility.GetScenePathByBuildIndex(0),Is.EqualTo("Assets/FarmGuardian/Scenes/FarmGuardian.unity"));
            var previews=Object.FindObjectsByType<FarmScenePreview>(FindObjectsInactive.Include,FindObjectsSortMode.None);Assert.That(previews.Length,Is.EqualTo(1));Assert.That(previews[0].gameObject.activeInHierarchy,Is.False);
            foreach(int level in new[]{1,16,31,46})
            {
                session.Introduce(level);session.Begin();yield return null;
                session.World.ValidateNavigation(session.Level);
                int theme=session.Level.theme;
                Assert.That(set.sourceInstances[theme],Is.GreaterThan(400));Assert.That(set.villageHouses[theme],Is.GreaterThanOrEqualTo(17));
                Assert.That(set.renderedBatches[theme],Is.LessThan(180));
                var stage=session.World.Root.GetComponentInChildren<FarmSceneryStage>();Assert.That(stage,Is.Not.Null);
                var grass=stage.transform.Find("Grass blade meadows - no miniature trees");Assert.That(grass,Is.Not.Null);
                Assert.That(grass.GetComponentsInChildren<Collider>().Length,Is.Zero);
                Assert.That(grass.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.vertexCount),Is.GreaterThan(25000));
                Assert.That(grass.childCount,Is.LessThanOrEqualTo(24));
                for(int i=0;i<stage.restingGardens.Length;i++)Assert.That(stage.restingGardens[i].activeSelf,Is.EqualTo(i>=session.Level.plantingCount));
                Assert.That(session.World.Root.Find("AnimalPens").childCount,Is.EqualTo(4));
                Assert.That(session.Animals.Count,Is.EqualTo(session.Level.animalCount));Assert.That(Object.FindObjectsByType<FarmPlayer>().Length,Is.EqualTo(1));Assert.That(Object.FindObjectsByType<FarmThief>().Length,Is.EqualTo(1));
            }
        }
        [UnityTest]public IEnumerator RecordDenseFarmFromGameplayCameraAndOverview()
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)Assert.Ignore("Graphics device required");
            foreach(int level in new[]{1,16,31,46})
            {
                session.Introduce(level);session.Begin();yield return new WaitForSeconds(.25f);session.Thief.enabled=false;session.Player.enabled=false;
                var camera=Camera.main;var follow=camera.GetComponent<FarmCamera>();follow.enabled=false;
                var canvas=Object.FindFirstObjectByType<Canvas>();
                var layout=session.World.Layout;
                var points=new[]{layout.PlayerSpawn,layout.Pens[0],layout.Obstacles.First(x=>x.Asset=="Well").Position,new Vector3(-11,0,-2),new Vector3(11,0,5)};
                for(int view=0;view<points.Length;view++)
                {
                    camera.transform.position=points[view]+follow.Offset;camera.transform.LookAt(points[view]+Vector3.up*.8f);
                    canvas.enabled=view==0;FarmPresentationTests.Capture("FarmGuardian-dense-"+layout.Theme+"-view"+view+".png",1920,1080);
                }
                camera.orthographic=true;camera.orthographicSize=25;camera.transform.position=new Vector3(0,45,0);camera.transform.rotation=Quaternion.Euler(90,0,0);canvas.enabled=false;
                FarmPresentationTests.Capture("FarmGuardian-dense-"+layout.Theme+"-overview.png",1700,1400);
                camera.orthographic=false;canvas.enabled=true;follow.enabled=true;
            }
        }
    }
}

