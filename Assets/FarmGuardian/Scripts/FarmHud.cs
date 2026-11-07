using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FarmGuardian
{
    public sealed class FarmHud : MonoBehaviour
    {
        public FarmSession Session;
        public Vector2 MoveInput=>joystick!=null?joystick.Value:Vector2.zero;
        Canvas canvas; RectTransform safe,design,page; FarmUiFactory ui; FarmUiTheme theme; FarmMenuDiorama diorama;
        FarmJoystick joystick; TMP_Text timer,objectives,status,currency,holdText,lifeText,scoreText; Image missionFill,holdFill,clockFace;
        Button capture,catchThief,plant; Rect lastSafe; Vector2Int lastSize;
        float refresh,shownProgress; int levelPage=-1,infoLevel=1,skinPage; bool shopSkins;
        int shownCoins=-1; float coinPulse;
        void Start()
        {
            theme=Resources.Load<FarmUiTheme>("UI/FarmUiTheme");
            if(theme==null||theme.font==null){Debug.LogError("UI kit missing. Run Tools > Farm Guardian > Build UI Kit.");enabled=false;return;}
            ui=new FarmUiFactory(theme);diorama=gameObject.AddComponent<FarmMenuDiorama>();
            var go=new GameObject("Farm Guardian UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(transform,false);
            canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            safe=FarmUiFactory.Rect("Safe Area",go.transform,Vector2.zero,Vector2.zero);FarmUiFactory.Stretch(safe);
            design=FarmUiFactory.Rect("Responsive design",safe,new Vector2(1920,1080),Vector2.zero);
            if(FindFirstObjectByType<EventSystem>()==null)
            {var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(transform,false);events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            ApplySafeArea();Rebuild();
        }
        void ApplySafeArea()
        {
            if(safe==null||Screen.width<=0||Screen.height<=0)return;
            var area=Screen.safeArea;safe.anchorMin=new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);safe.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
            Canvas.ForceUpdateCanvases();design.localScale=Vector3.one*Mathf.Min(safe.rect.width/1920,safe.rect.height/1080);
            lastSafe=area;lastSize=new Vector2Int(Screen.width,Screen.height);
        }
        public void RefreshLayout(){ApplySafeArea();if(diorama!=null)diorama.RefreshFraming();if(page!=null)page.GetComponent<FarmIllustratedMenu>()?.RefreshLayout();}
        public void PrepareLevelSelection(){infoLevel=Session.SelectedLevel;levelPage=(infoLevel-1)/15;}
        void LateUpdate()
        {
            if(safe==null||design==null)return;
            float scale=Mathf.Min(safe.rect.width/1920,safe.rect.height/1080);
            if(!Mathf.Approximately(design.localScale.x,scale))design.localScale=Vector3.one*scale;
        }
        public void ResetInput(){if(joystick!=null)joystick.ResetInput();Session.SetCatchHeld(false);Session.SetPlantHeld(false);}
        public void Rebuild()
        {
            if(ui==null)return;
            if(page!=null)page.gameObject.AddComponent<FarmUiExit>().Begin();
            ui.UseCozy=Session.State!=SessionState.MainMenu;
            bool menu=Session.State==SessionState.MainMenu||Session.State==SessionState.Shop||Session.State==SessionState.Skins||Session.State==SessionState.LevelSelect||Session.State==SessionState.LevelIntroduction||Session.State==SessionState.Settings&&!Session.SettingsFromPause;
            diorama.Show(Session.config,menu&&Session.State!=SessionState.MainMenu&&Session.State!=SessionState.LevelSelect);
            bool mainMenu=Session.State==SessionState.MainMenu;
            page=FarmUiFactory.Rect("Page - "+Session.State,mainMenu?canvas.transform:design,new Vector2(1920,1080),Vector2.zero);
            if(mainMenu)FarmUiFactory.Stretch(page);else page.gameObject.AddComponent<FarmUiMotion>().Panel=true;
            if(menu&&Session.State!=SessionState.MainMenu&&Session.State!=SessionState.LevelSelect){var veil=ui.Image("Quiet cream background",page,new Vector2(6000,6000),Vector2.zero,new Color(1,.96f,.9f,.40f));veil.sprite=null;}
            joystick=null;timer=objectives=status=currency=holdText=lifeText=scoreText=null;capture=catchThief=plant=null;missionFill=holdFill=clockFace=null;refresh=0;shownProgress=0;
            switch(Session.State)
            {
                case SessionState.MainMenu:Menu();break;case SessionState.LevelSelect:LevelSelect();break;
                case SessionState.LevelIntroduction:Introduction();break;case SessionState.Playing:Gameplay();break;
                case SessionState.Shop:Shop();break;case SessionState.Skins:Skins();break;case SessionState.Settings:Settings();break;
                case SessionState.Paused:Pause();break;case SessionState.Won:case SessionState.Lost:Result();break;
            }
        }
        RectTransform Panel(string name,Vector2 size,Vector2 position,Transform parent=null,GameObject prefab=null)=>ui.Panel(name,parent??page,size,position,prefab);
        TMP_Text Text(string text,Transform parent,Vector2 size,Vector2 position,float font=28,Color? color=null,TextAlignmentOptions align=TextAlignmentOptions.Center)=>ui.Text(text,parent,size,position,font,color,align);
        Button Button(string name,Transform parent,Vector2 size,Vector2 position,UnityEngine.Events.UnityAction action,Color? color=null,FarmIcon? icon=null)=>ui.Button(name,parent,size,position,action,color,icon);
        void Shade(float alpha=.22f)
        {var image=ui.Image("Modal shade",page,new Vector2(6000,6000),Vector2.zero,new Color(.73f,.65f,.83f,alpha),blocks:true);image.sprite=null;}
        void Wallet()
        {
            var wallet=ui.Instantiate(theme.currencyCounter,"Currency counter",page,new Vector2(252,64),new Vector2(780,468));
            wallet.Bind("",theme.cream);ui.Symbol(FarmIcon.Coin,wallet.transform,new Vector2(34,34),new Vector2(-87,4),theme.brown);
            currency=Text(Session.Save.coins.ToString("N0"),wallet.transform,new Vector2(164,48),new Vector2(25,4),30);shownCoins=Session.Save.coins;
        }
        void Back(UnityEngine.Events.UnityAction action)=>Button("BACK",page,new Vector2(186,64),new Vector2(-790,468),action,theme.cyan);
        RectTransform ScreenPanel(string title,UnityEngine.Events.UnityAction back)
        {Shade(.12f);var p=Panel(title+" panel",new Vector2(1650,800),new Vector2(0,-16));ui.Ribbon(title,p,650,375);Back(back);Wallet();return p;}
        RectTransform Modal(string title,string subtitle,GameObject prefab=null)
        {Shade();var p=Panel(title+" panel",new Vector2(1000,830),new Vector2(0,-5),prefab:prefab);ui.Ribbon(title,p,770,370);Text(subtitle,p,new Vector2(870,70),new Vector2(0,260),25);return p;}
        void Menu()
        {
            var menu=page.gameObject.AddComponent<FarmIllustratedMenu>();
            menu.Build(page,Session,theme);currency=menu.Balance;shownCoins=Session.Save.coins;
        }
        void LevelSelect()
        {
            if(levelPage<0)levelPage=(Session.SelectedLevel-1)/15;
            infoLevel=Mathf.Clamp(infoLevel,1,FarmConfig.MaxLevel);if(infoLevel==1&&Session.SelectedLevel>1)infoLevel=Session.SelectedLevel;
            // A transparent layout root: nodes sit directly over the farm scenery.
            var map=FarmUiFactory.Rect("Full screen level map",page,new Vector2(1920,1080),Vector2.zero);
            map.gameObject.AddComponent<FarmLevelMapView>().Build(map,Session.config);
            var nodes=FarmLevelMapView.DesignNodes();
            for(int i=0;i<15;i++)
            {
                int number=levelPage*15+i+1;if(number>FarmConfig.MaxLevel)break;bool unlocked=number<=Session.Save.unlockedLevel;
                if(!ui.CozyEnabled)ui.Image("Level pedestal "+number,map,new Vector2(154,139),nodes[i]+new Vector2(0,-12),unlocked?theme.brown:theme.purple,theme.circle);
                if(number==infoLevel)ui.Image("Selected level outline",map,new Vector2(158,158),nodes[i],ui.CozyEnabled?theme.yellow:theme.pink,theme.circle);
                var element=ui.Instantiate(theme.levelButton,"Level "+number,map,new Vector2(136,136),nodes[i]);
                element.Bind(number.ToString(),number==infoLevel?theme.green:unlocked?theme.cyan:theme.lavenderLight);element.Label.fontSize=46;
                element.Label.rectTransform.anchorMin=element.Label.rectTransform.anchorMax=Vector2.one*.5f;
                element.Label.rectTransform.sizeDelta=new Vector2(120,66);
                element.Label.rectTransform.anchoredPosition=new Vector2(0,20);
                if(ui.CozyEnabled)element.Face.color=unlocked?Color.white:new Color(.72f,.70f,.72f);
                else
                {
                    foreach(var image in element.GetComponentsInChildren<Image>())if(image.name!="Soft gradient"){image.sprite=theme.circle;image.type=Image.Type.Simple;}
                    element.Face.gameObject.AddComponent<Mask>().showMaskGraphic=true;
                }
                element.Button.interactable=unlocked;
                element.Button.onClick.AddListener(()=>{infoLevel=number;Session.Introduce(number);});
                int best=Session.Save.BestStars(number);
                var stars=FarmUiFactory.Rect("Best stars - Level "+number,element.transform,new Vector2(100,34),new Vector2(0,-27));
                for(int star=0;star<3;star++)
                    ui.Symbol(FarmIcon.Star,stars,new Vector2(30,30),new Vector2((star-1)*32,0),star<best?theme.yellow:Color.Lerp(theme.purple,theme.ink,.45f));
                if(!unlocked)ui.Symbol(FarmIcon.Lock,element.transform,new Vector2(23,23),new Vector2(45,42),theme.ink);
            }
            Back(Session.MainMenu);Wallet();
            ui.Ribbon("FARM GUARDIAN",page,560,468);
            Text(Session.config.GetLevel(levelPage*15+1).ThemeName+"  /  SELECT LEVEL",page,new Vector2(900,55),new Vector2(0,388),30,Color.white);
            var previous=Button("<",page,new Vector2(100,80),new Vector2(-820,-450),()=>{levelPage--;Rebuild();},theme.cyan);previous.interactable=levelPage>0;
            var next=Button(">",page,new Vector2(100,80),new Vector2(820,-450),()=>{levelPage++;Rebuild();},theme.cyan);next.interactable=levelPage<(FarmConfig.MaxLevel-1)/15;
            Text("LEVELS "+(levelPage*15+1)+" – "+Mathf.Min(FarmConfig.MaxLevel,levelPage*15+15)+"  /  "+FarmConfig.MaxLevel+"\nTap an unlocked level to play",page,new Vector2(900,85),new Vector2(0,-450),28,Color.white);
        }
        void Introduction()
        {
            var spec=Session.Level;var p=Modal("LEVEL "+spec.number,spec.ThemeName);
            ui.Symbol(FarmIcon.Paw,p,new Vector2(52,52),new Vector2(-330,143),theme.brown);
            Text("Rescue "+spec.animalCount+" animals",p,new Vector2(650,60),new Vector2(30,143),31);
            ui.Symbol(FarmIcon.Leaf,p,new Vector2(52,52),new Vector2(-330,55),theme.brown);
            Text(spec.plantingCount>0?"Plant "+spec.plantingCount+(spec.plantingCount==1?" plot":" plots"):"No planting mission",p,new Vector2(650,60),new Vector2(30,55),29);
            Text("Time limit: "+spec.seconds+" seconds",p,new Vector2(820,65),new Vector2(0,-35),29);
            Text("Rescue all remaining animals and finish planting before time runs out.\n3 stars: no losses | 2 stars: 1–"+(Session.OneStarLossThreshold-1)+" lost\n1 star: "+Session.OneStarLossThreshold+"–"+(spec.animalCount-1)+" lost | All lost: defeat\nHold Catch for "+Session.config.thiefCatchSeconds.ToString("0.#")+"s to recover a stolen animal before the exit.",p,new Vector2(870,108),new Vector2(0,-123),20);
            Button("START LEVEL",p,new Vector2(600,85),new Vector2(0,-219),Session.Begin,theme.green,FarmIcon.Play);
            Button("LEVEL SELECT",p,new Vector2(600,70),new Vector2(0,-315),Session.OpenLevelSelect,theme.cyan);
        }
        void Shop()
        {
            var p=ScreenPanel("SHOP",Session.CloseShop);
            var upgrades=Button("UPGRADES",p,new Vector2(315,70),new Vector2(-180,268),()=>{shopSkins=false;Rebuild();},shopSkins?theme.cream:theme.lavenderLight);
            var skins=Button("SKINS",p,new Vector2(315,70),new Vector2(180,268),()=>{shopSkins=true;Rebuild();},shopSkins?theme.lavenderLight:theme.cream);
            if(ui.CozyEnabled)
            {
                FarmCozyUiSkin.Paint(upgrades.GetComponent<FarmUiElement>().Face,shopSkins?FarmCozyUiSkin.Shared.CreamButton:FarmCozyUiSkin.Shared.WoodButton,true);
                FarmCozyUiSkin.Paint(skins.GetComponent<FarmUiElement>().Face,shopSkins?FarmCozyUiSkin.Shared.WoodButton:FarmCozyUiSkin.Shared.CreamButton,true);
                FarmCozyUiSkin.CenterCaption(upgrades.GetComponent<FarmUiElement>(),shopSkins?4:7);
                FarmCozyUiSkin.CenterCaption(skins.GetComponent<FarmUiElement>(),shopSkins?7:4);
            }
            if(shopSkins){SkinCards(p,-35);return;}
            string[] names={"MOVEMENT SPEED","COIN BONUS","FARMING SPEED","ANIMAL RESCUE"};
            string[] descriptions={"Explore the farm a little faster.","Earn more coins when you complete a level.","Spend less time planting each plot.","Rescue animals more quickly."};
            float[] effects={Session.config.movementUpgradePerLevel,Session.config.coinUpgradePerLevel,Session.config.farmingUpgradePerLevel,Session.config.captureUpgradePerLevel};
            FarmIcon[] icons={FarmIcon.Arrow,FarmIcon.Coin,FarmIcon.Leaf,FarmIcon.Paw};
            for(int i=0;i<4;i++)
            {
                int index=i,current=Session.Save.UpgradeLevel(i),cost=Session.config.upgradeBaseCost*(current+1);bool max=current>=Mathf.Min(4,Session.config.maxUpgradeLevel);
                var card=Panel("Upgrade "+i,new Vector2(700,240),new Vector2(i%2==0?-375:375,i<2?87:-177),p,theme.upgradeCard);
                ui.Image("Upgrade emblem",card,new Vector2(90,90),new Vector2(-210,42),Color.white,ui.CozyEnabled?FarmCozyUiSkin.Shared.RoundCream:theme.circle);
                ui.Symbol(icons[i],card,new Vector2(48,48),new Vector2(-210,47),theme.brown);
                Text(names[i],card,new Vector2(430,44),new Vector2(80,74),28);
                Text(descriptions[i],card,new Vector2(430,54),new Vector2(80,28),21);
                Text("Level "+current+" / "+Mathf.Min(4,Session.config.maxUpgradeLevel)+"   |   Bonus +"+Mathf.RoundToInt(current*effects[i]*100)+"%",card,new Vector2(430,32),new Vector2(80,-15),22);
                Text(max?"Fully upgraded":cost+" coins",card,new Vector2(220,42),new Vector2(-155,-68),26);
                var buy=Button(max?"MAX":"UPGRADE",card,new Vector2(260,72),new Vector2(156,-68),()=>Session.UiBuyUpgrade(index),theme.green);buy.interactable=!max&&Session.Save.coins>=cost;
            }
            Text("Upgrades are optional. Purchases use your saved coins.",p,new Vector2(1200,42),new Vector2(0,-331),23);
        }
        void SkinCards(Transform parent,float y)
        {
            const int perPage=3;int pages=(FarmSkinCatalog.All.Length+perPage-1)/perPage;
            skinPage=Mathf.Clamp(skinPage,0,pages-1);
            for(int i=0;i<perPage;i++)
            {
                int index=skinPage*perPage+i;if(index>=FarmSkinCatalog.All.Length)break;
                var skin=FarmSkinCatalog.All[index];bool owned=Session.Save.OwnsSkin(skin.Id),equipped=Session.Save.EquippedSkinId==skin.Id;
                int price=FarmSkinCatalog.Cost(skin.Id,Session.config);
                var card=Panel("Skin card - "+skin.Id,new Vector2(440,480),new Vector2((i-1)*480,y),parent,theme.skinCard);
                Text(skin.Name,card,new Vector2(330,48),new Vector2(20,170),28);
                ui.Image("Rarity badge",card,new Vector2(260,40),new Vector2(20,120),Color.white,FarmCozyUiSkin.Shared.CreamButton);
                Text(skin.Rarity,card,new Vector2(250,36),new Vector2(20,123),22,skin.Color);
                var preview=FarmUiFactory.Rect("Character preview - "+skin.Id,card,new Vector2(196,196),new Vector2(20,-3));
                preview.gameObject.AddComponent<FarmSkinPreview>().Configure(Session.config,skin.Id);
                if(!owned)ui.Symbol(FarmIcon.Coin,card,new Vector2(28,28),new Vector2(-55,-125));
                Text(owned?equipped?"Currently equipped":"Owned":price.ToString("N0"),card,new Vector2(220,38),new Vector2(owned?20:45,-125),24);
                var use=Button(equipped?"EQUIPPED":owned?"EQUIP":"BUY",card,new Vector2(310,72),new Vector2(20,-190),()=>Session.UiUseSkin(skin.Id),theme.green);
                use.name="Use skin - "+skin.Id;
                use.interactable=!equipped&&(owned||Session.Save.coins>=price);
            }
            var previous=Button("<",parent,new Vector2(80,72),new Vector2(-680,-331),()=>{skinPage--;Rebuild();});previous.interactable=skinPage>0;
            var next=Button(">",parent,new Vector2(80,72),new Vector2(680,-331),()=>{skinPage++;Rebuild();});next.interactable=skinPage<pages-1;
            Text("OUTFITS "+(skinPage*perPage+1)+" – "+Mathf.Min((skinPage+1)*perPage,FarmSkinCatalog.All.Length)+" / "+FarmSkinCatalog.All.Length+"   •   COSMETIC ONLY",parent,new Vector2(1100,48),new Vector2(0,-331),23);
        }
        void Skins()
        {
            var p=ScreenPanel("SKINS",Session.CloseMenuPanel);SkinCards(p,-18);
            Text("Choose your look. Every outfit has the same abilities.",p,new Vector2(1300,60),new Vector2(0,268),25);
        }
        void Settings()
        {
            Shade();var p=Panel("SETTINGS panel",new Vector2(1000,580),new Vector2(0,-5));
            ui.Ribbon("SETTINGS",p,770,250);
            Text(Session.SettingsFromPause?"Your game stays paused.":"Make yourself at home.",p,new Vector2(850,55),new Vector2(0,165),25);
            Text("SOUND EFFECTS",p,new Vector2(320,55),new Vector2(-200,75),29);
            var volume=Text("",p,new Vector2(90,50),new Vector2(360,75),28);
            var sliderRect=FarmUiFactory.Rect("SFX Volume",p,new Vector2(360,55),new Vector2(120,75));
            var track=ui.Image("Track",sliderRect,new Vector2(360,30),Vector2.zero,theme.brown,blocks:true);
            if(ui.CozyEnabled)FarmCozyUiSkin.Paint(track,FarmCozyUiSkin.Shared.WoodButton,true);
            var fillArea=FarmUiFactory.Rect("Fill area",sliderRect,new Vector2(316,12),Vector2.zero);
            var fill=ui.Image("Fill",fillArea,Vector2.zero,Vector2.zero,theme.green);FarmUiFactory.Stretch(fill.rectTransform);
            var handleArea=FarmUiFactory.Rect("Handle area",sliderRect,new Vector2(306,0),Vector2.zero);
            var handle=ui.Image("Handle",handleArea,new Vector2(54,54),Vector2.zero,Color.white,ui.CozyEnabled?FarmCozyUiSkin.Shared.RoundCream:theme.circle,blocks:true);handle.type=Image.Type.Simple;
            var slider=sliderRect.gameObject.AddComponent<Slider>();slider.targetGraphic=handle;slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;
            slider.minValue=0;slider.maxValue=1;slider.SetValueWithoutNotify(Session.Save.sound?FarmUiPreferences.SfxVolume:0);
            volume.name="SFX percentage";volume.text=Mathf.RoundToInt(slider.value*100)+"%";
            slider.onValueChanged.AddListener(value=>{Session.SetEffectsVolume(value);volume.text=Mathf.RoundToInt(value*100)+"%";});
            Text("Slide to adjust volume. Set to 0% to mute.\nYour sound preference is saved automatically.",p,new Vector2(830,70),new Vector2(0,-8),22);
            Button("RESET PROGRESS",p,new Vector2(600,72),new Vector2(0,-110),ConfirmReset,theme.red);
            Button("BACK",p,new Vector2(600,72),new Vector2(0,-210),Session.CloseMenuPanel,theme.cyan);
        }
        void ConfirmReset()
        {
            ResetInput();var shield=FarmUiFactory.Rect("Reset confirmation",page,new Vector2(1920,1080),Vector2.zero);
            var dim=ui.Image("Block input",shield,new Vector2(6000,6000),Vector2.zero,new Color(.57f,.48f,.65f,.45f),blocks:true);dim.sprite=null;
            var p=Panel("Reset confirmation panel",new Vector2(900,500),Vector2.zero,shield,theme.popupPanel);ui.Ribbon("RESET PROGRESS?",p,720,200);
            Text("This removes your Farm Guardian coins, upgrades,\nowned outfit and unlocked levels.\nYou will return to the main menu.\nYour device settings are kept.",p,new Vector2(790,180),new Vector2(0,20),27);
            Button("CANCEL",p,new Vector2(330,75),new Vector2(-195,-162),()=>{shield.gameObject.SetActive(false);Destroy(shield.gameObject);},theme.cyan);
            Button("RESET",p,new Vector2(330,75),new Vector2(195,-162),Session.ResetProgress,theme.red);
        }
        void Pause()
        {
            var p=Modal("PAUSED","Take a breather. Your animals and timer are paused.",theme.pausePanel);
            Button("RESUME",p,new Vector2(600,80),new Vector2(0,125),Session.Resume,theme.green,FarmIcon.Play);
            Button("RESTART LEVEL",p,new Vector2(600,75),new Vector2(0,22),()=>Session.Introduce(Session.Level.number),theme.cyan);
            Button("SETTINGS",p,new Vector2(600,75),new Vector2(0,-81),Session.OpenSettings,theme.purple,FarmIcon.Gear);
            Button("MAIN MENU",p,new Vector2(600,75),new Vector2(0,-184),Session.MainMenu,theme.lavenderLight,FarmIcon.Barn);
        }
        void Result()
        {
            bool won=Session.State==SessionState.Won;var p=Modal(won?"LEVEL COMPLETED!":"GAME OVER",Session.ResultReason,theme.resultPanel);
            if(won)
                for(int i=0;i<3;i++)ui.Symbol(FarmIcon.Star,p,new Vector2(80,80),new Vector2((i-1)*105,157),i<Session.EarnedStars?theme.yellow:theme.lavenderLight);
            else ui.Symbol(FarmIcon.Shield,p,new Vector2(95,95),new Vector2(0,157),theme.red);
            Text("Level "+Session.Level.number+" — "+Session.Level.ThemeName+"\nRescued: "+Session.SecuredCount+" / "+Session.Level.animalCount+"    Lost: "+Session.LostCount+(Session.Level.plantingCount>0?"    Plants: "+Session.PlantedCount+" / "+Session.Level.plantingCount:"")+"\nStars: "+Session.EarnedStars+" / 3    Best: "+Session.Save.BestStars(Session.Level.number)+" / 3\nLife: "+Session.Lives+" / "+Session.Level.animalCount+"    Score: "+Session.Score+"\nCoins earned: "+Session.Reward+"    Balance: "+Session.Save.coins,p,new Vector2(900,155),new Vector2(0,17),25);
            bool last=won&&Session.Level.number>=FarmConfig.MaxLevel;
            Button(last?"LEVEL SELECT":won?"NEXT LEVEL":"RETRY",p,new Vector2(600,76),new Vector2(0,-113),()=>{if(last)Session.OpenLevelSelect();else Session.Introduce(won?Session.Level.number+1:Session.Level.number);},theme.green,FarmIcon.Play);
            Button(won?"REPLAY":"LEVEL SELECT",p,new Vector2(600,65),new Vector2(0,-207),()=>{if(won)Session.Introduce(Session.Level.number);else Session.OpenLevelSelect();},theme.cyan);
            Button("MAIN MENU",p,new Vector2(600,65),new Vector2(0,-298),Session.MainMenu,theme.lavenderLight,FarmIcon.Barn);
        }
        void Gameplay()
        {
            var missions=Panel("Missions",new Vector2(350,275),new Vector2(-742,357));
            Text("LEVEL "+Session.Level.number,missions,new Vector2(320,38),new Vector2(0,105),28);
            lifeText=Text("LIFE: "+Session.Lives+" / "+Session.Level.animalCount,missions,new Vector2(300,36),new Vector2(0,65),28);lifeText.name="Life display";
            scoreText=Text("SCORE: "+Session.Score,missions,new Vector2(300,36),new Vector2(0,24),28);scoreText.name="Score display";
            objectives=Text("",missions,new Vector2(320,92),new Vector2(0,-55),22);
            var track=ui.Image("Mission track",missions,new Vector2(290,15),new Vector2(0,-116),theme.lavenderLight);
            if(ui.CozyEnabled)track.color=new Color(.65f,.44f,.33f,.45f);
            missionFill=ui.Image("Mission fill",track.transform,new Vector2(0,15),Vector2.zero,theme.green,anchor:new Vector2(0,.5f));missionFill.rectTransform.pivot=new Vector2(0,.5f);
            var clock=ui.Instantiate(theme.currencyCounter,"Timer",page,new Vector2(220,83),new Vector2(0,455));clock.Bind("",theme.yellow);clockFace=clock.Face;timer=Text("",clock.transform,new Vector2(206,70),Vector2.zero,43);
            Wallet();Button("",page,new Vector2(78,78),new Vector2(885,362),Session.Pause,theme.lavenderLight,FarmIcon.Pause);
            var warning=Panel("Rescue status",new Vector2(590,65),new Vector2(0,347));status=Text("",warning,new Vector2(550,58),Vector2.zero,22);warning.gameObject.SetActive(false);
            var pad=ui.Image("Joystick",page,new Vector2(210,210),new Vector2(-765,-337),new Color(1,.95f,.87f,.65f),theme.circle,blocks:true);
            var knob=ui.Image("Thumb",pad.transform,new Vector2(88,88),Vector2.zero,theme.cyan,theme.circle);joystick=pad.gameObject.AddComponent<FarmJoystick>();joystick.Knob=knob.rectTransform;
            if(ui.CozyEnabled)
            {
                FarmCozyUiSkin.Paint(pad,FarmCozyUiSkin.Shared.RoundWood,false);pad.color=new Color(1,1,1,.9f);
                FarmCozyUiSkin.Paint(knob,FarmCozyUiSkin.Shared.RoundCream,false);
                ui.Image("Cozy movement plaque",page,new Vector2(180,46),new Vector2(-765,-475),Color.white,FarmCozyUiSkin.Shared.WoodButton);
            }
            Text("MOVE",page,new Vector2(240,42),new Vector2(-765,-475),25);
            capture=Button("CAPTURE ANIMAL",page,new Vector2(400,88),new Vector2(690,-419),Session.CaptureNearest,theme.green,FarmIcon.Paw);ActionShortcut(capture,"E",false);
            catchThief=Button("CATCH THIEF",page,new Vector2(400,88),new Vector2(690,-307),null,theme.cyan,FarmIcon.Shield);catchThief.gameObject.AddComponent<FarmHoldButton>().Changed=Session.SetCatchHeld;ActionShortcut(catchThief,"F",true);
            if(Session.Level.plantingCount>0){plant=Button("PLANT SEED",page,new Vector2(400,88),new Vector2(690,-195),null,theme.lavenderLight,FarmIcon.Leaf);plant.gameObject.AddComponent<FarmHoldButton>().Changed=Session.SetPlantHeld;ActionShortcut(plant,"P",true);}
            var holdTrack=ui.Image("Hold progress",page,new Vector2(440,20),new Vector2(0,-416),theme.lavenderLight);
            holdFill=ui.Image("Fill",holdTrack.transform,new Vector2(0,20),Vector2.zero,theme.yellow,anchor:new Vector2(0,.5f));holdFill.rectTransform.pivot=new Vector2(0,.5f);
            holdText=Text("",page,new Vector2(600,50),new Vector2(0,-369),27);
        }
        void ActionShortcut(Button button,string key,bool hold)
        {
            var caption=button.GetComponent<FarmUiElement>().Label;
            caption.rectTransform.offsetMax=new Vector2(-112,-8);caption.fontSizeMax=26;caption.fontSizeMin=20;
            float x=((RectTransform)button.transform).rect.width*.5f-57;
            ui.Image("Keyboard hint",button.transform,new Vector2(86,44),new Vector2(x,7),Color.white,FarmCozyUiSkin.Shared.CreamButton);
            var hint=Text(hold?"HOLD "+key:key,button.transform,new Vector2(80,36),new Vector2(x,10),hold?18:24);
            hint.name="Shortcut - "+key;hint.font=FarmUiTypography.ForText(true,theme.font);hint.fontStyle=FontStyles.Normal;
            hint.alignment=TextAlignmentOptions.Midline;hint.margin=Vector4.zero;
        }
        void Update()
        {
            if(ui==null)return;
            if(lastSafe!=Screen.safeArea||lastSize!=new Vector2Int(Screen.width,Screen.height))ApplySafeArea();
            if(currency!=null&&shownCoins!=Session.Save.coins){shownCoins=Session.Save.coins;currency.text=shownCoins.ToString("N0");coinPulse=.25f;}
            if(currency!=null){coinPulse=Mathf.Max(0,coinPulse-Time.unscaledDeltaTime);currency.transform.localScale=Vector3.one*(1+Mathf.Sin(coinPulse/.25f*Mathf.PI)*.035f);}
            if(!Session.Playing||timer==null)return;
            float target=(Session.SecuredCount+Session.PlantedCount)/(float)Mathf.Max(1,Session.RemainingAnimalCount+Session.Level.plantingCount);
            shownProgress=Mathf.Lerp(shownProgress,target,1-Mathf.Exp(-8*Time.unscaledDeltaTime));missionFill.rectTransform.sizeDelta=new Vector2(290*shownProgress,15);
            holdFill.rectTransform.sizeDelta=new Vector2(440*Session.HoldProgress,20);holdFill.transform.parent.gameObject.SetActive(Session.HoldProgress>0);holdText.text=Session.HoldProgress>0?Session.HoldLabel+" "+Mathf.RoundToInt(Session.HoldProgress*100)+"%":"";
            refresh-=Time.unscaledDeltaTime;if(refresh>0)return;refresh=.1f;
            int seconds=Mathf.CeilToInt(Session.Remaining);timer.text=(seconds/60).ToString("00")+":"+(seconds%60).ToString("00");
            clockFace.color=ui.CozyEnabled?Color.white:seconds<=15?theme.red:theme.yellow;
            timer.color=seconds<=15?theme.red:ui.CozyEnabled?FarmCozyUiSkin.Shared.Ink:theme.ink;
            objectives.text="ANIMALS: "+Session.SecuredCount+" / "+Session.RemainingAnimalCount+"\nLOST: "+Session.LostCount+" / "+Session.Level.animalCount+(Session.Level.plantingCount>0?"\nPLANTS: "+Session.PlantedCount+" / "+Session.Level.plantingCount:"");
            lifeText.text="LIFE: "+Session.Lives+" / "+Session.Level.animalCount;
            lifeText.color=Session.Lives<=Mathf.CeilToInt(Session.Level.animalCount/3f)?theme.red:FarmCozyUiSkin.Shared.Ink;
            scoreText.text="SCORE: "+Session.Score.ToString("N0");
            capture.GetComponent<FarmUiElement>().Label.text=Session.CarriedAnimal!=null?"DELIVER ANIMAL":"PICK UP ANIMAL";
            capture.interactable=Session.CarriedAnimal!=null?Session.CanDeposit:Session.NearestAnimal()!=null;catchThief.interactable=Session.CanCatchThief;if(plant!=null)plant.interactable=Session.NearestPlot()!=null;
            var thief=Session.Thief;string direction="";
            status.transform.parent.gameObject.SetActive(Session.CarriedAnimal!=null||Session.CaptureInProgress||thief!=null&&thief.State!=ThiefState.Inactive);
            catchThief.gameObject.SetActive(thief!=null&&thief.State!=ThiefState.Inactive);
            if(plant!=null)plant.gameObject.SetActive(Session.PlantedCount<Session.Level.plantingCount);
            if(thief!=null&&thief.State!=ThiefState.Inactive){var d=thief.transform.position-Session.Player.transform.position;direction="  "+(d.z>2?"N":d.z< -2?"S":"")+(d.x>2?"E":d.x< -2?"W":"")+" "+Mathf.RoundToInt(d.magnitude)+"m";}
            status.text=thief==null||thief.State==ThiefState.Inactive?"Bring your animals home safely.":thief.State==ThiefState.Stunned?"THIEF STUNNED • "+Mathf.CeilToInt(thief.StunRemaining)+"s":thief.State==ThiefState.Escaping?"STOP THE THIEF BEFORE THE EXIT!"+direction:thief.State==ThiefState.Stealing?"THEFT IN PROGRESS!"+direction:"Protect your pens!"+direction;
            if(Session.CarriedAnimal!=null)status.text="Carry the animal to its pen • "+Mathf.CeilToInt(Vector3.Distance(Session.Player.transform.position,Session.CarriedAnimal.Home))+" m";
            else if(Session.CaptureInProgress)status.text="Picking up animal... stay close!";
        }
    }
}




