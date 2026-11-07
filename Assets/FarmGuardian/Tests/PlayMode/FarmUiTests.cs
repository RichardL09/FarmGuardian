using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace FarmGuardian.Tests
{
    public sealed class FarmUiTests
    {
        FarmSession session;string save;float volume;int quality,savedQuality;bool hadVolume,hadQuality;
        [UnitySetUp]public IEnumerator Setup()
        {
            save=PlayerPrefs.GetString("FarmGuardian.Save.v1","");volume=FarmUiPreferences.SfxVolume;quality=QualitySettings.GetQualityLevel();
            hadVolume=PlayerPrefs.HasKey("FarmGuardian.UI.Sfx");hadQuality=PlayerPrefs.HasKey("FarmGuardian.UI.Quality");savedQuality=PlayerPrefs.GetInt("FarmGuardian.UI.Quality");
            yield return SceneManager.LoadSceneAsync("FarmGuardian");yield return null;yield return null;
            session=Object.FindFirstObjectByType<FarmSession>();session.Save.sound=false;
        }
        [UnityTearDown]public IEnumerator Teardown()
        {
            Time.timeScale=1;session.MainMenu();FarmUiPreferences.SfxVolume=volume;QualitySettings.SetQualityLevel(quality,true);
            if(!hadVolume)PlayerPrefs.DeleteKey("FarmGuardian.UI.Sfx");if(hadQuality)PlayerPrefs.SetInt("FarmGuardian.UI.Quality",savedQuality);else PlayerPrefs.DeleteKey("FarmGuardian.UI.Quality");
            if(string.IsNullOrEmpty(save))PlayerPrefs.DeleteKey("FarmGuardian.Save.v1");else PlayerPrefs.SetString("FarmGuardian.Save.v1",save);PlayerPrefs.Save();yield return null;
        }
        static void Click(string name)
        {
            var go=GameObject.Find(name);Assert.That(go,Is.Not.Null,name);var button=go.GetComponent<Button>();Assert.That(button.interactable,Is.True,name);
            Canvas.ForceUpdateCanvases();var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,go.transform.position),button=PointerEventData.InputButton.Left};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Assert.That(hits.Count,Is.GreaterThan(0),name+" must have a real raycast target.");
            Assert.That(hits[0].gameObject.transform.IsChildOf(go.transform),Is.True,name+" is blocked by another UI panel.");
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        [UnityTest]public IEnumerator NavigationLocksPurchasesPauseSettingsAndConfirmedReset()
        {
            session.Save.unlockedLevel=2;session.Save.coins=1000;session.Save.speed=0;session.Save.goldenOutfit=false;session.Save.wearGoldenOutfit=false;
            Assert.That(GameObject.Find("Level selection"),Is.Null);
            Assert.That(Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Length,Is.Zero,"All live UI text must use TMP.");
            Assert.That(Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Length,Is.GreaterThan(0),"Live wallet uses TMP; exact reference button typography is preserved in sliced artwork.");
            Click("PLAY");yield return null;Assert.That(session.State,Is.EqualTo(SessionState.LevelSelect));
            Assert.That(GameObject.Find("Level 3").GetComponent<Button>().interactable,Is.False);
            Assert.That(session.SelectedLevel,Is.LessThanOrEqualTo(2));
            Click("Level 2");yield return null;Click("START LEVEL");yield return null;
            session.Pause();yield return null;Click("SETTINGS");yield return null;
            Assert.That(Time.timeScale,Is.Zero);Object.FindFirstObjectByType<Slider>().value=.36f;Assert.That(FarmUiPreferences.SfxVolume,Is.EqualTo(.36f).Within(.01f));
            Assert.That(GameObject.Find("Controls guide"),Is.Null);
            Assert.That(GameObject.Find("Page - Settings").GetComponentsInChildren<Button>().Length,Is.EqualTo(2),"Settings exposes only reset and back, plus the sound slider.");
            Click("BACK");yield return null;Assert.That(session.State,Is.EqualTo(SessionState.Paused));Click("RESUME");yield return null;Assert.That(session.Playing,Is.True);
            session.MainMenu();session.OpenShop();yield return null;
            Assert.That(session.UiBuyUpgrade(0),Is.True);int coins=session.Save.coins;
            Assert.That(session.UiBuyUpgrade(0),Is.False);Assert.That(session.Save.coins,Is.EqualTo(coins));
            session.CloseShop();session.OpenSkins();yield return new WaitForSecondsRealtime(.45f);
            Assert.That(session.UiUseOutfit(true),Is.True,"Skin purchase: state="+session.State+", coins="+session.Save.coins+", price="+session.config.goldenOutfitCost);coins=session.Save.coins;
            Assert.That(session.UiUseOutfit(true),Is.True);Assert.That(session.Save.coins,Is.EqualTo(coins));Assert.That(session.Save.wearGoldenOutfit,Is.True);
            session.CloseMenuPanel();session.OpenSettings();yield return null;
            Click("RESET PROGRESS");yield return null;Assert.That(session.Save.coins,Is.EqualTo(coins));Click("CANCEL");yield return null;Assert.That(session.Save.unlockedLevel,Is.EqualTo(2));
            Click("RESET PROGRESS");yield return null;Click("RESET");yield return null;
            Assert.That(session.State,Is.EqualTo(SessionState.MainMenu));Assert.That(session.Save.coins,Is.Zero);Assert.That(session.Save.unlockedLevel,Is.EqualTo(1));Assert.That(session.Save.goldenOutfit,Is.False);
        }
        [UnityTest]public IEnumerator CaptureEveryUiScreenAtLandscapeSizes()
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)Assert.Ignore("Graphics required");
            session.Save.unlockedLevel=18;session.Save.coins=250;
            yield return new WaitForSecondsRealtime(.3f);
            FarmPresentationTests.Capture("FarmGuardian-ui-menu.png",1920,1080);
            session.OpenLevelSelect();yield return new WaitForSecondsRealtime(.3f);Capture("levels");session.MainMenu();
            session.OpenShop();yield return new WaitForSecondsRealtime(.3f);Capture("shop");session.CloseShop();
            session.OpenShop();yield return null;Click("SKINS");yield return new WaitForSecondsRealtime(.3f);Capture("shop-skins");session.CloseShop();
            session.OpenSkins();yield return new WaitForSecondsRealtime(.3f);Capture("skins");session.CloseMenuPanel();
            session.OpenSettings();yield return new WaitForSecondsRealtime(.3f);Capture("settings");Click("RESET PROGRESS");yield return null;FarmPresentationTests.Capture("FarmGuardian-ui-reset.png");Click("CANCEL");session.CloseMenuPanel();
            session.Introduce(2);yield return new WaitForSecondsRealtime(.3f);Capture("intro");session.Begin();yield return new WaitForSeconds(.3f);Capture("hud");
            session.Pause();yield return new WaitForSecondsRealtime(.3f);Capture("pause");session.Resume();foreach(var animal in session.Animals){animal.Steal();session.AnimalLost(animal);}yield return new WaitForSecondsRealtime(.3f);Capture("lose");
            session.Introduce(1);session.Begin();yield return null;session.Thief.enabled=false;
            foreach(var a in session.Animals.ToArray())
            {
                var controller=session.Player.GetComponent<CharacterController>();controller.enabled=false;
                session.Player.transform.position=a.transform.position+Vector3.back*.5f;controller.enabled=true;
                a.Capture(.01f);float end=Time.realtimeSinceStartup+4;
                while(a.State!=AnimalState.Carried&&Time.realtimeSinceStartup<end)yield return null;
                Assert.That(a.State,Is.EqualTo(AnimalState.Carried));
                controller.enabled=false;session.Player.transform.position=a.Home+Vector3.up*.05f;controller.enabled=true;
                Physics.SyncTransforms();float gateEnd=Time.realtimeSinceStartup+2;while(!session.CanDeposit&&Time.realtimeSinceStartup<gateEnd)yield return null;session.CaptureNearest();
            }
            float timeout=Time.realtimeSinceStartup+4;while(session.State!=SessionState.Won&&Time.realtimeSinceStartup<timeout)yield return null;
            Assert.That(session.State,Is.EqualTo(SessionState.Won));yield return new WaitForSecondsRealtime(.3f);Capture("win");
        }
        [UnityTest]public IEnumerator WindingMapSupportsEveryNodeAndTheLastProgressionPage()
        {
            session.Save.unlockedLevel=2;session.Save.coins=137;
            Click("PLAY");yield return null;
            Assert.That(Object.FindFirstObjectByType<FarmLevelMapView>(),Is.Not.Null);
            for(int number=1;number<=15;number++)
            {
                var node=GameObject.Find("Level "+number).GetComponent<Button>();
                Assert.That(node.interactable,Is.EqualTo(number<=2));
                if(number<=2){Click("Level "+number);yield return null;Assert.That(session.State,Is.EqualTo(SessionState.LevelIntroduction));session.OpenLevelSelect();yield return null;}
                Assert.That(session.SelectedLevel,Is.LessThanOrEqualTo(2));Assert.That(session.Save.coins,Is.EqualTo(137));
            }
            Assert.That(Object.FindObjectsByType<FarmSession>().Length,Is.EqualTo(1));
            Assert.That(Object.FindFirstObjectByType<GridLayoutGroup>(),Is.Null,"The farm route must remain a visual winding map.");
            session.Save.unlockedLevel=100;session.Introduce(100);session.OpenLevelSelect();yield return null;
            Assert.That(GameObject.Find("Level 101"),Is.Null);
            Assert.That(GameObject.Find(">").GetComponent<Button>().interactable,Is.False);Click("Level 100");yield return null;
            Assert.That(session.State,Is.EqualTo(SessionState.LevelIntroduction));Assert.That(session.Level.number,Is.EqualTo(100));
        }
        [UnityTest]public IEnumerator AssetMapAlignsRoadAnimatesTreesAndReleasesItsCamera()
        {
            session.Save.unlockedLevel=5;session.OpenLevelSelect();
            yield return new WaitForSecondsRealtime(.5f);
            var map=Object.FindFirstObjectByType<FarmLevelMapView>();Assert.That(map,Is.Not.Null);
            Assert.That(GameObject.Find("Level information"),Is.Null);
            Assert.That(GameObject.Find("LEVEL SELECT panel"),Is.Null);
            Assert.That(map.Stage.Find("Map Barn"),Is.Not.Null);
            Assert.That(map.Stage.Find("Map StonePath"),Is.Not.Null);
            Assert.That(map.Trees.Count,Is.GreaterThan(20));
            foreach(var p in FarmLevelMapView.DesignNodes())
            {
                var projected=map.MapCamera.WorldToViewportPoint(map.GroundPoint(p));
                Assert.That(projected.x,Is.EqualTo(.5f+p.x/2560).Within(.001));
                Assert.That(projected.y,Is.EqualTo(.5f+p.y/1440).Within(.001));
            }
            var rest=map.Trees[0].localRotation;Time.timeScale=0;
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(Quaternion.Angle(rest,map.Trees[0].localRotation),Is.GreaterThan(.05f));Time.timeScale=1;
            if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                map.MapCamera.Render();map.MapCamera.Render();
                FarmPresentationTests.Capture("FarmGuardian-asset-level-map.png",1920,1080);
                FarmPresentationTests.Capture("FarmGuardian-asset-level-map-tablet.png",1440,1080);
                FarmPresentationTests.Capture("FarmGuardian-asset-level-map-wide.png",2340,1080);
            }
            var oldCamera=map.MapCamera;session.MainMenu();yield return new WaitForSecondsRealtime(.3f);
            Assert.That(oldCamera==null,Is.True,"Leaving the map must release its render camera.");
        }
        [UnityTest]public IEnumerator CozyArtworkKeepsLevelStarsAndGameplayControlsFunctional()
        {
            session.Save.unlockedLevel=5;session.Save.coins=580;session.Save.levelStars=new int[FarmConfig.MaxLevel];
            session.Save.RecordStars(1,3);session.Save.RecordStars(2,2);session.Save.RecordStars(3,1);
            session.OpenLevelSelect();yield return new WaitForSecondsRealtime(.35f);
            for(int level=1;level<=3;level++)
            {
                var node=GameObject.Find("Level "+level).GetComponent<FarmUiElement>();
                Assert.That(node.Face.sprite.texture.name,Is.EqualTo("RoundButton1_white"));
                Assert.That(node.Label.font.faceInfo.familyName,Is.EqualTo("Coiny"));
                var row=node.transform.Find("Best stars - Level "+level);
                Assert.That(row.childCount,Is.EqualTo(3));
                Assert.That(((RectTransform)row).anchoredPosition.y,Is.LessThan(((RectTransform)node.Label.transform).anchoredPosition.y));
                int filled=row.GetComponentsInChildren<Image>().Count(i=>i.color==Resources.Load<FarmUiTheme>("UI/FarmUiTheme").yellow);
                Assert.That(filled,Is.EqualTo(4-level),"Level stars must display the saved best score.");
            }
            Assert.That(GameObject.Find("Level 6").GetComponent<Button>().interactable,Is.False);
            var map=Object.FindFirstObjectByType<FarmLevelMapView>();map.MapCamera.Render();map.MapCamera.Render();
            FarmPresentationTests.Capture("FarmGuardian-cozy-levels.png",1920,1080);
            FarmPresentationTests.Capture("FarmGuardian-cozy-levels-tablet.png",1440,1080);
            FarmPresentationTests.Capture("FarmGuardian-cozy-levels-wide.png",2340,1080);
            Click("Level 2");yield return new WaitForSecondsRealtime(.3f);
            Assert.That(session.State,Is.EqualTo(SessionState.LevelIntroduction));
            FarmPresentationTests.Capture("FarmGuardian-cozy-intro.png",1920,1080);
            Click("START LEVEL");yield return null;session.Thief.Activate();session.Thief.enabled=false;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(GameObject.Find("CAPTURE ANIMAL").GetComponent<FarmUiElement>().Face.sprite.texture.name,Is.EqualTo("SquareButton2_wood"));
            Assert.That(GameObject.Find("CATCH THIEF").GetComponent<FarmUiElement>().Face.sprite.texture.name,Is.EqualTo("SquareButton2_red"));
            Assert.That(GameObject.Find("Joystick").GetComponent<Image>().sprite.texture.name,Is.EqualTo("RoundButton1_wood"));
            Assert.That(GameObject.Find("Timer").GetComponent<FarmUiElement>().Face.color,Is.EqualTo(Color.white));
            Assert.That(GameObject.Find("CAPTURE ANIMAL").GetComponent<FarmUiElement>().Label.text,Is.EqualTo("PICK UP ANIMAL"));
            var missionText=GameObject.Find("Missions").GetComponentsInChildren<TMP_Text>().First(t=>t.text.StartsWith("ANIMALS:"));
            Assert.That(missionText.font.faceInfo.familyName,Is.EqualTo("Balsamiq Sans"));
            foreach(var text in Object.FindObjectsByType<TMP_Text>())
                Assert.That(System.Text.RegularExpressions.Regex.IsMatch(text.text,@"\b(HEWAN|KANDANG|PENCURI|TANAM|ANGKAT|MENANG|KALAH|KEMBALI|MULAI|PILIH)\b",System.Text.RegularExpressions.RegexOptions.IgnoreCase),Is.False,"Player-facing text must be English: "+text.text);
            foreach(string name in new[]{"CAPTURE ANIMAL","CATCH THIEF","PLANT SEED"})
            {
                var label=GameObject.Find(name).GetComponent<FarmUiElement>().Label;label.ForceMeshUpdate();
                Assert.That(label.textInfo.lineCount,Is.EqualTo(1),name+" must stay on one readable line.");
                Assert.That(label.isTextOverflowing,Is.False,name);
            }
            Assert.That(GameObject.Find("Shortcut - E").GetComponent<TMP_Text>().text,Is.EqualTo("E"));
            Assert.That(GameObject.Find("Shortcut - F").GetComponent<TMP_Text>().text,Is.EqualTo("HOLD F"));
            Assert.That(GameObject.Find("Shortcut - P").GetComponent<TMP_Text>().text,Is.EqualTo("HOLD P"));
            foreach(var hint in Object.FindObjectsByType<TMP_Text>().Where(t=>t.name.StartsWith("Shortcut - ")))
            {hint.ForceMeshUpdate();Assert.That(hint.isTextOverflowing,Is.False,hint.text);Assert.That(hint.raycastTarget,Is.False);}
            Assert.That(Object.FindObjectsByType<TMP_Text>().Count(t=>t.name.StartsWith("Shortcut - ")),Is.EqualTo(3));
            FarmPresentationTests.Capture("FarmGuardian-cozy-gameplay.png",1920,1080);
            FarmPresentationTests.Capture("FarmGuardian-cozy-gameplay-tablet.png",1440,1080);
            FarmPresentationTests.Capture("FarmGuardian-cozy-gameplay-wide.png",2340,1080);
            FarmPresentationTests.Capture("FarmGuardian-action-shortcuts.png",1920,1080);
            var nearby=session.Animals[0];Assert.That(session.TryCarry(nearby),Is.True);yield return new WaitForSecondsRealtime(.2f);
            Assert.That(GameObject.Find("CAPTURE ANIMAL").GetComponent<FarmUiElement>().Label.text,Is.EqualTo("DELIVER ANIMAL"));
            Assert.That(GameObject.Find("Shortcut - E").GetComponent<TMP_Text>().text,Is.EqualTo("E"));
            session.Pause();yield return new WaitForSecondsRealtime(.3f);
            FarmPresentationTests.Capture("FarmGuardian-cozy-pause.png",1920,1080);
            Click("RESUME");yield return null;Assert.That(session.Playing,Is.True);
            session.MainMenu();session.OpenShop();yield return new WaitForSecondsRealtime(.2f);
            Assert.That(GameObject.Find("UPGRADES").GetComponent<FarmUiElement>().Face.sprite.texture.name,Is.EqualTo("SquareButton2_wood"));
        }
        [UnityTest]public IEnumerator CozySettingsAndUpgradesRemainReadableAndFunctional()
        {
            session.Save.coins=1000;session.Save.speed=0;session.OpenShop();
            yield return new WaitForSecondsRealtime(.35f);
            var card=GameObject.Find("Upgrade 0");
            Assert.That(card.GetComponent<FarmUiElement>().Face.sprite.texture.name,Is.EqualTo("WoodenContainer1"));
            var heading=card.GetComponentsInChildren<TMP_Text>().First(t=>t.text=="MOVEMENT SPEED");
            Assert.That(heading.font.faceInfo.familyName,Is.EqualTo("Coiny"));
            Assert.That(card.GetComponentsInChildren<TMP_Text>().First(t=>t.text.StartsWith("Explore")).font.faceInfo.familyName,Is.EqualTo("Balsamiq Sans"));
            foreach(var text in card.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.text);}
            AssertCenteredCaptions();
            var emblem=(RectTransform)card.transform.Find("Upgrade emblem");
            Assert.That(emblem.anchoredPosition.x-emblem.rect.width*.5f,Is.GreaterThan(-270),"Keep the icon clear of the notebook gutter.");
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1440,1080),new Vector2Int(2340,1080)})
                FarmPresentationTests.Capture("FarmGuardian-cozy-upgrades-"+size.x+".png",size.x,size.y);
            var buy=card.GetComponentsInChildren<Button>().Single();buy.onClick.Invoke();yield return new WaitForSecondsRealtime(.45f);
            Assert.That(session.Save.speed,Is.EqualTo(1));Assert.That(session.Save.coins,Is.EqualTo(1000-session.config.upgradeBaseCost));
            Assert.That(GameObject.Find("Upgrade 0").GetComponentsInChildren<TMP_Text>().Any(t=>t.text.StartsWith("Level 1 /")),Is.True);
            Click("SKINS");yield return new WaitForSecondsRealtime(.3f);
            Assert.That(GameObject.Find("SKINS").GetComponent<FarmUiElement>().Face.sprite.texture.name,Is.EqualTo("SquareButton2_wood"));
            AssertCenteredCaptions();
            FarmPresentationTests.Capture("FarmGuardian-aligned-skins-1920.png",1920,1080);
            Click("UPGRADES");yield return null;
            session.CloseShop();session.OpenSettings();yield return new WaitForSecondsRealtime(.35f);
            var slider=Object.FindFirstObjectByType<Slider>();slider.value=.36f;
            Assert.That(GameObject.Find("SFX percentage").GetComponent<TMP_Text>().text,Is.EqualTo("36%"));
            Assert.That(FarmUiPreferences.SfxVolume,Is.EqualTo(.36f).Within(.01f));
            Assert.That(slider.handleRect.GetComponent<Image>().sprite.texture.name,Is.EqualTo("RoundButton1_white"));
            Assert.That(GameObject.Find("RESET PROGRESS").GetComponent<FarmUiElement>().Face.sprite.texture.name,Is.EqualTo("SquareButton2_red"));
            AssertCenteredCaptions();
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1440,1080),new Vector2Int(2340,1080)})
                FarmPresentationTests.Capture("FarmGuardian-cozy-settings-"+size.x+".png",size.x,size.y);
            Click("RESET PROGRESS");yield return null;FarmPresentationTests.Capture("FarmGuardian-cozy-reset.png",1920,1080);
            Assert.That(GameObject.Find("RESET").GetComponent<FarmUiElement>().Face.sprite.texture.name,Is.EqualTo("SquareButton2_red"));
            Click("CANCEL");yield return null;Assert.That(session.Save.speed,Is.EqualTo(1));
            Click("BACK");yield return null;Assert.That(session.State,Is.EqualTo(SessionState.MainMenu));
        }
        [UnityTest]public IEnumerator MainMenuFillsViewportWithoutClippingControls()
        {
            yield return new WaitForSecondsRealtime(.35f);
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1366,768),new Vector2Int(1920,1080),new Vector2Int(2560,1440),new Vector2Int(1440,1080),new Vector2Int(2340,1080),new Vector2Int(3200,1080)})
                FarmPresentationTests.Capture("FarmGuardian-fullscreen-menu-"+size.x+"x"+size.y+".png",size.x,size.y,()=>
                {
                    var viewport=GameObject.Find("Page - MainMenu").GetComponent<RectTransform>();
                    var scenery=GameObject.Find("Full screen menu scenery").GetComponent<RectTransform>();
                    Assert.That(scenery.rect.size,Is.EqualTo(viewport.rect.size));
                    var corners=new Vector3[4];
                    foreach(string name in new[]{"PLAY","SHOP","SETTINGS","EXIT","Main menu coins","FARM GUARDIAN logo"})
                    {
                        var rect=GameObject.Find(name).GetComponent<RectTransform>();rect.GetWorldCorners(corners);
                        foreach(var corner in corners)
                        {
                            var local=viewport.InverseTransformPoint(corner);
                            Assert.That(local.x,Is.InRange(viewport.rect.xMin-1,viewport.rect.xMax+1),name+" horizontal clipping");
                            Assert.That(local.y,Is.InRange(viewport.rect.yMin-1,viewport.rect.yMax+1),name+" vertical clipping");
                        }
                    }
                    var artwork=GameObject.Find("Reference layout - 1672 x 941").transform;
                    Assert.That(artwork.localScale.x,Is.EqualTo(artwork.localScale.y));
                    Assert.That(viewport.localScale,Is.EqualTo(Vector3.one));
                });
            Click("SETTINGS");yield return new WaitForSecondsRealtime(.25f);
            Click("BACK");yield return new WaitForSecondsRealtime(.25f);
            Click("PLAY");yield return null;Assert.That(session.State,Is.EqualTo(SessionState.LevelSelect));
        }
        [UnityTest]public IEnumerator CozyExitButtonSavesProgressBeforeQuit()
        {
            yield return new WaitForSecondsRealtime(.25f);
            var exit=GameObject.Find("EXIT").GetComponent<FarmUiElement>();
            Assert.That(exit.Face.sprite.texture.name,Is.EqualTo("SquareButton2_red"));
            Assert.That(exit.Label.text,Is.EqualTo("EXIT"));
            Assert.That(exit.Label.font.faceInfo.familyName,Is.EqualTo("Coiny"));
            AssertCenteredCaptions();
            session.Save.coins=173;Click("EXIT");yield return null;
            // Unity ignores Application.Quit in the Editor; verify the actual UI
            // callback persists progress without stopping the Play Mode test run.
            Assert.That(FarmSave.Load().coins,Is.EqualTo(173));
            Assert.That(session.State,Is.EqualTo(SessionState.MainMenu));
        }
        [UnityTest]public IEnumerator CozyMainMenuAndMinimalSettings()
        {
            session.Save.coins=139;yield return new WaitForSecondsRealtime(.35f);
            foreach(string name in new[]{"PLAY","SHOP","SETTINGS"})
            {
                var button=GameObject.Find(name).GetComponent<FarmUiElement>();
                Assert.That(button.Face.sprite.texture.name,Is.EqualTo("SquareButton2_wood"));
                Assert.That(button.Label.font.faceInfo.familyName,Is.EqualTo("Coiny"));
            }
            Assert.That(GameObject.Find("PLAY").GetComponent<FarmUiElement>().Label.text,Is.EqualTo("START"));
            Assert.That(GameObject.Find("Main menu coins").GetComponent<FarmUiElement>().Face.sprite.texture.name,Is.EqualTo("RoundButton2_white"));
            Assert.That(Object.FindFirstObjectByType<FarmIllustratedMenu>().Balance.text,Is.EqualTo("139"));
            AssertCenteredCaptions();
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1440,1080),new Vector2Int(2340,1080)})
                FarmPresentationTests.Capture("FarmGuardian-cozy-main-menu-"+size.x+".png",size.x,size.y);
            Click("COINS");yield return null;Assert.That(session.State,Is.EqualTo(SessionState.Shop));
            session.CloseShop();yield return new WaitForSecondsRealtime(.2f);
            Click("SHOP");yield return null;Assert.That(session.State,Is.EqualTo(SessionState.Shop));
            session.CloseShop();yield return new WaitForSecondsRealtime(.2f);
            Click("SETTINGS");yield return new WaitForSecondsRealtime(.3f);
            var page=GameObject.Find("Page - Settings");
            Assert.That(page.GetComponentsInChildren<Slider>().Length,Is.EqualTo(1));
            Assert.That(page.GetComponentsInChildren<Button>().Select(b=>b.name),Is.EquivalentTo(new[]{"RESET PROGRESS","BACK"}));
            Assert.That(page.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("GRAPHICS")||t.text.Contains("CONTROLS")),Is.False);
            var slider=page.GetComponentInChildren<Slider>();slider.value=.65f;slider.value=0;
            Assert.That(session.Save.sound,Is.False);Assert.That(FarmUiPreferences.SfxVolume,Is.Zero);
            slider.value=.65f;Assert.That(session.Save.sound,Is.True);
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1440,1080),new Vector2Int(2340,1080)})
                FarmPresentationTests.Capture("FarmGuardian-minimal-settings-"+size.x+".png",size.x,size.y);
            Click("RESET PROGRESS");yield return null;Click("CANCEL");yield return null;
            Assert.That(session.Save.coins,Is.EqualTo(139));Click("BACK");yield return new WaitForSecondsRealtime(.2f);
            Click("PLAY");yield return null;Assert.That(session.State,Is.EqualTo(SessionState.LevelSelect));
        }
        [UnityTest]public IEnumerator NewSkinCollectionPurchasesPersistsAndAppearsInGameplay()
        {
            session.Save.ownedSkins=new string[0];session.Save.equippedSkin=FarmSkinCatalog.Classic;
            session.Save.goldenOutfit=false;session.Save.wearGoldenOutfit=false;session.Save.coins=199;
            session.OpenSkins();yield return new WaitForSecondsRealtime(.35f);
            Assert.That(session.UiUseSkin("tourist"),Is.False);Assert.That(session.Save.coins,Is.EqualTo(199));
            Assert.That(GameObject.Find("Use skin - tourist").GetComponent<Button>().interactable,Is.False);
            session.Save.coins=10000;session.Hud.Rebuild();yield return new WaitForSecondsRealtime(.45f);
            string[] ids={"tourist","queen","ninja","samurai","golden-sensei"};int[] costs={200,400,700,1100,1600};
            string[] rarities={"UNCOMMON","RARE","EPIC","LEGENDARY","MYTHIC"};
            Assert.That(FarmCoinArtwork.Sprite,Is.Not.Null,"Clover coin must be runtime-loadable.");
            Assert.That(FarmCoinArtwork.Sprite.vertices.Length,Is.GreaterThan(4),"The JPG's white corners must be clipped by circular geometry.");
            foreach(var icon in Object.FindObjectsByType<Image>().Where(i=>i.name=="Icon - Coin"))
            {Assert.That(icon.sprite.texture.name,Is.EqualTo("CloverCoin"),icon.transform.parent.name);Assert.That(icon.color,Is.EqualTo(Color.white));Assert.That(icon.useSpriteMesh,Is.True);}
            for(int page=0;page<3;page++)
            {
                foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1440,1080),new Vector2Int(2340,1080)})
                    FarmPresentationTests.Capture("FarmGuardian-skin-collection-page"+(page+1)+"-"+size.x+".png",size.x,size.y);
                foreach(var text in GameObject.Find("Page - Skins").GetComponentsInChildren<TMP_Text>())
                {text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.text);}
                if(page<2){Click(">");yield return new WaitForSecondsRealtime(.3f);}
            }
            Click("<");yield return null;Click("<");yield return new WaitForSecondsRealtime(.3f);
            int balance=session.Save.coins;Click("Use skin - tourist");yield return new WaitForSecondsRealtime(.45f);
            Assert.That(session.Save.coins,Is.EqualTo(balance-200));
            for(int i=0;i<ids.Length;i++)
            {
                var skin=FarmSkinCatalog.Find(ids[i]);Assert.That(skin.Rarity,Is.EqualTo(rarities[i]));Assert.That(skin.Price,Is.EqualTo(costs[i]));
                var source=FarmSkinCatalog.Model(session.config,ids[i]);Assert.That(source,Is.Not.Null,ids[i]);
                balance=session.Save.coins;bool alreadyOwned=session.Save.OwnsSkin(ids[i]);
                Assert.That(session.UiUseSkin(ids[i]),Is.True,ids[i]);
                Assert.That(session.Save.coins,Is.EqualTo(balance-(alreadyOwned?0:costs[i])));
                balance=session.Save.coins;Assert.That(session.UiUseSkin(ids[i]),Is.True);Assert.That(session.Save.coins,Is.EqualTo(balance));
                var loaded=FarmSave.Load();Assert.That(loaded.OwnsSkin(ids[i]),Is.True);Assert.That(loaded.EquippedSkinId,Is.EqualTo(ids[i]));
                session.CloseMenuPanel();session.Introduce(1);session.Begin();yield return new WaitForSecondsRealtime(.3f);
                Assert.That(session.Player.Visual.GetComponentInChildren<FarmCharacterAnimation>(),Is.Not.Null,ids[i]);
                Assert.That(session.Player.Visual.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial,
                    Is.EqualTo(source.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial));
                Assert.That(session.Player.GetComponent<CharacterController>().height,Is.EqualTo(1.8f));
                session.Player.WalkAnimation.Drive(3,false,true);Assert.That(session.Player.WalkAnimation.Carrying,Is.True);
                FarmPresentationTests.Capture("FarmGuardian-equipped-"+ids[i]+".png",1920,1080);
                session.MainMenu();session.OpenSkins();yield return new WaitForSecondsRealtime(.45f);
            }
            balance=session.Save.coins;Assert.That(session.UiUseSkin("invalid"),Is.False);Assert.That(session.Save.coins,Is.EqualTo(balance));
            session.CloseMenuPanel();session.MainMenu();yield return new WaitForSecondsRealtime(.2f);
            FarmPresentationTests.Capture("FarmGuardian-clover-coin-menu.png",1920,1080);
            PlayerPrefs.SetString("FarmGuardian.Save.v1","{\"version\":1,\"coins\":123,\"unlockedLevel\":9,\"goldenOutfit\":true,\"wearGoldenOutfit\":true}");
            var legacy=FarmSave.Load();Assert.That(legacy.EquippedSkinId,Is.EqualTo(FarmSkinCatalog.GoldenFarmer));
            Assert.That(legacy.coins,Is.EqualTo(123));Assert.That(legacy.unlockedLevel,Is.EqualTo(9));
            session.OpenSettings();yield return null;Click("RESET PROGRESS");yield return null;Click("RESET");yield return null;
            Assert.That(session.Save.ownedSkins.Length,Is.Zero);Assert.That(session.Save.EquippedSkinId,Is.EqualTo(FarmSkinCatalog.Classic));
        }
        static void AssertCenteredCaptions()
        {
            foreach(var element in Object.FindObjectsByType<FarmUiElement>())
            {
                if(element.Label==null||string.IsNullOrWhiteSpace(element.Label.text))continue;
                var label=element.Label;label.ForceMeshUpdate();
                Assert.That(label.alignment,Is.EqualTo(TextAlignmentOptions.Midline),element.name);
                Assert.That(label.isTextOverflowing,Is.False,element.name);
                // TMP textBounds uses font ascender/descender metrics, not visible glyphs.
                var glyphs=label.textInfo.characterInfo.Take(label.textInfo.characterCount).Where(c=>c.isVisible).ToArray();
                if(glyphs.Length==0)continue;
                float visibleCenter=(glyphs.Min(c=>c.bottomLeft.y)+glyphs.Max(c=>c.topRight.y))*.5f;
                Assert.That(Mathf.Abs(visibleCenter-label.rectTransform.rect.center.y),Is.LessThan(1),element.name+" visible glyphs must be vertically centered.");
            }
        }
        static void Capture(string screen)
        {FarmPresentationTests.Capture("FarmGuardian-ui-"+screen+".png",1920,1080);FarmPresentationTests.Capture("FarmGuardian-ui-"+screen+"-tablet.png",1440,1080);FarmPresentationTests.Capture("FarmGuardian-ui-"+screen+"-wide.png",2340,1080);}
    }
}



