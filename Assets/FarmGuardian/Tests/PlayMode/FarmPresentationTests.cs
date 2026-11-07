using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FarmGuardian.Tests
{
    public sealed class FarmPresentationTests
    {
        [UnityTest]
        public IEnumerator StaticMenuPreservesCompositionAndNavigation()
        {
            yield return SceneManager.LoadSceneAsync("FarmGuardian");yield return null;yield return null;
            var session=Object.FindFirstObjectByType<FarmSession>();
            var menu=GameObject.Find("Page - MainMenu").GetComponent<FarmIllustratedMenu>();
            Assert.That(GameObject.Find("Farmer head - slow left right"),Is.Null);
            Assert.That(GameObject.Find("Chicken - occasional hop"),Is.Null);
            var images=menu.GetComponentsInChildren<UnityEngine.UI.RawImage>();
            var positions=new Vector3[images.Length];for(int i=0;i<images.Length;i++)positions[i]=images[i].transform.localPosition;
            yield return new WaitForSecondsRealtime(1);
            for(int i=0;i<images.Length;i++)Assert.That(images[i].transform.localPosition,Is.EqualTo(positions[i]));
            session.Save.coins=321;yield return null;Assert.That(menu.Balance.text,Is.EqualTo("321"));
            session.OpenShop();yield return null;session.CloseShop();yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Object.FindObjectsByType<FarmIllustratedMenu>().Length,Is.EqualTo(1));
        }        [UnityTest]
        public IEnumerator RecordFourStrategicMapPreviews()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("Visual verification requires a graphics device.");
            yield return SceneManager.LoadSceneAsync("FarmGuardian"); yield return null;
            var session = Object.FindFirstObjectByType<FarmSession>();
            int unlocked = session.Save.unlockedLevel; session.Save.unlockedLevel = 100;
            var camera = Camera.main; var canvas = Object.FindFirstObjectByType<Canvas>();
            string[] names = { "Countryside", "Woodland", "Mountain", "Advanced" };
            for (int theme = 0; theme < 4; theme++)
            {
                camera.orthographic = false; canvas.enabled = true;
                session.Introduce(theme*15+15); session.Begin();
                camera.GetComponent<FarmCamera>().enabled = true;
                yield return new WaitForSeconds(.2f); session.Pause();
                // Capture gameplay without a pause modal, with stationary actors for comparison.
                canvas.enabled = false;
                Capture("FarmGuardian-map-"+names[theme]+".png");
                camera.GetComponent<FarmCamera>().enabled = false;
                camera.orthographic = true; camera.orthographicSize = 21;
                camera.transform.position = new Vector3(0,48,0); camera.transform.rotation = Quaternion.Euler(90,0,0);
                Capture("FarmGuardian-map-"+names[theme]+"-top.png",1440,1100);
            }
            camera.orthographic = false; camera.GetComponent<FarmCamera>().enabled = true; canvas.enabled = true;
            session.Save.unlockedLevel = unlocked; session.MainMenu();
        }

        [UnityTest]
        public IEnumerator RecordMenuAndGameplayPreviews()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("Visual verification requires a graphics device.");
            yield return SceneManager.LoadSceneAsync("FarmGuardian");
            yield return null; yield return null;
            var session = Object.FindFirstObjectByType<FarmSession>();
            yield return new WaitForSecondsRealtime(0.3f);
            var theme=Resources.Load<FarmUiTheme>("UI/FarmUiTheme");
            Assert.That(theme.font,Is.Not.Null);Assert.That(theme.icons.Length,Is.GreaterThan(12));
            Assert.That(GameObject.Find("Level selection"),Is.Null,"Main menu must have no bottom-left level selector.");
            Capture("FarmGuardian-menu.png");
            Capture("FarmGuardian-menu-wide.png",2340,1080);
            Capture("FarmGuardian-menu-tablet.png",1440,1080);
            session.OpenSkins(); yield return new WaitForSecondsRealtime(.3f);
            Capture("FarmGuardian-skins.png");
            session.CloseMenuPanel(); session.OpenSettings(); yield return new WaitForSecondsRealtime(.3f);
            Capture("FarmGuardian-settings.png"); session.CloseMenuPanel();
            session.Introduce(1); session.Begin();
            yield return new WaitForSeconds(0.4f);
            Capture("FarmGuardian-gameplay.png");
            Capture("FarmGuardian-wide.png", 2340, 1080);
            Capture("FarmGuardian-tablet.png", 1440, 1080);
            Assert.That(session.Player, Is.Not.Null);
            session.Player.enabled = false; session.Player.WalkAnimation.Drive(2.5f);
            session.Thief.enabled = false; session.Thief.Visual.gameObject.SetActive(true);
            session.Thief.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(session.Player.transform.position + Vector3.right * 2);
            session.Thief.WalkAnimation.Drive(2.5f);
            var camera = Camera.main; camera.GetComponent<FarmCamera>().enabled = false;
            camera.transform.position = session.Player.transform.position + new Vector3(3, 2, 4);
            camera.transform.LookAt(session.Player.transform.position + new Vector3(1, .9f, 0));
            Object.FindFirstObjectByType<Canvas>().enabled = false;
            yield return new WaitForSeconds(.15f);
            Capture("FarmGuardian-walk.png");
            session.MainMenu();
        }

        public static void Capture(string filename, int width = 1600, int height = 900, System.Action afterLayout = null)
        {
#if UNITY_EDITOR
            bool asyncShaders = UnityEditor.ShaderUtil.allowAsyncCompilation;
            UnityEditor.ShaderUtil.allowAsyncCompilation = false;
#endif
            var camera = Camera.main;
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var texture = new RenderTexture(width, height, 24);
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            try
            {
                camera.targetTexture = texture;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera; canvas.planeDistance = 0.5f;
                var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.enabled=false;scaler.enabled=true;
                Canvas.ForceUpdateCanvases();
                Object.FindFirstObjectByType<FarmHud>().RefreshLayout();
                  afterLayout?.Invoke();
                // Prime freshly-created URP materials and UI batches before recording the frame.
                camera.Render();
                camera.Render();
                RenderTexture.active = texture;
                var output = new Texture2D(width, height, TextureFormat.RGB24, false);
                output.ReadPixels(new Rect(0, 0, width, height), 0, 0); output.Apply();
                // Store alongside the test project, outside Assets; do not import generated previews.
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string directory = Path.Combine(projectRoot, "Logs"); Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, filename), output.EncodeToPNG());
                Object.Destroy(output);
            }
            finally
            {
#if UNITY_EDITOR
                UnityEditor.ShaderUtil.allowAsyncCompilation = asyncShaders;
#endif
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                camera.targetTexture = previous; RenderTexture.active = active;
                texture.Release(); Object.Destroy(texture);
            }
        }
    }
}



