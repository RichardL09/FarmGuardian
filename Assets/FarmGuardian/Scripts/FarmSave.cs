using System;
using UnityEngine;

namespace FarmGuardian
{
    [Serializable]
    public sealed class FarmSave
    {
        const string Key = "FarmGuardian.Save.v1";
        public int version = 1;
        public int unlockedLevel = 1;
        public int coins;
        public int speed, coinBonus, farming, capture;
        public bool sound = true;
        public bool goldenOutfit;
        public bool wearGoldenOutfit;
        public string[] ownedSkins=new string[0];
        public string equippedSkin;
        public int[] levelStars = new int[FarmConfig.MaxLevel];

        public static FarmSave Load()
        {
            FarmSave data = null;
            try { data = JsonUtility.FromJson<FarmSave>(PlayerPrefs.GetString(Key, "")); }
            catch (Exception) { /* Invalid JSON falls back to a fresh local save. */ }
            if (data == null || data.version != 1) data = new FarmSave();
            data.unlockedLevel = Mathf.Clamp(data.unlockedLevel, 1, FarmConfig.MaxLevel);
            data.coins = Mathf.Clamp(data.coins, 0, 10000000);
            data.speed = Mathf.Clamp(data.speed, 0, 4);
            data.coinBonus = Mathf.Clamp(data.coinBonus, 0, 4);
            data.farming = Mathf.Clamp(data.farming, 0, 4);
            data.capture = Mathf.Clamp(data.capture, 0, 4);
            data.wearGoldenOutfit &= data.goldenOutfit;
            data.NormalizeStars();
            data.NormalizeSkins();
            return data;
        }

        public void Store()
        {
            unlockedLevel=Mathf.Clamp(unlockedLevel,1,FarmConfig.MaxLevel);
            NormalizeStars();
            NormalizeSkins();
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public string EquippedSkinId=>FarmSkinCatalog.Find(equippedSkin)!=null&&OwnsSkin(equippedSkin)?equippedSkin:
            wearGoldenOutfit&&goldenOutfit?FarmSkinCatalog.GoldenFarmer:FarmSkinCatalog.Classic;
        public bool OwnsSkin(string id)=>id==FarmSkinCatalog.Classic||id==FarmSkinCatalog.GoldenFarmer&&goldenOutfit||
            ownedSkins!=null&&Array.IndexOf(ownedSkins,id)>=0;
        void NormalizeSkins()
        {
            var known=new System.Collections.Generic.List<string>();
            foreach(var skin in FarmSkinCatalog.All)
                if(skin.Resource!=null&&ownedSkins!=null&&Array.IndexOf(ownedSkins,skin.Id)>=0)known.Add(skin.Id);
            ownedSkins=known.ToArray();equippedSkin=EquippedSkinId;
            wearGoldenOutfit=equippedSkin==FarmSkinCatalog.GoldenFarmer;
        }
        public bool BuyOrEquipSkin(string id,FarmConfig config)
        {
            if(FarmSkinCatalog.Find(id)==null)return false;
            if(!OwnsSkin(id))
            {
                int price=FarmSkinCatalog.Cost(id,config);if(coins<price)return false;
                coins-=price;
                if(id==FarmSkinCatalog.GoldenFarmer)goldenOutfit=true;
                else {var owned=new System.Collections.Generic.List<string>(ownedSkins??new string[0]);owned.Add(id);ownedSkins=owned.ToArray();}
            }
            equippedSkin=id;wearGoldenOutfit=id==FarmSkinCatalog.GoldenFarmer;Store();return true;
        }
        void NormalizeStars()
        {
            if(levelStars==null)levelStars=new int[FarmConfig.MaxLevel];
            else if(levelStars.Length!=FarmConfig.MaxLevel)Array.Resize(ref levelStars,FarmConfig.MaxLevel);
            for(int i=0;i<levelStars.Length;i++)levelStars[i]=Mathf.Clamp(levelStars[i],0,3);
        }
        public int BestStars(int level)
        {
            if(level<1||level>FarmConfig.MaxLevel||levelStars==null||level>levelStars.Length)return 0;
            return Mathf.Clamp(levelStars[level-1],0,3);
        }
        public void RecordStars(int level,int stars)
        {
            if(level<1||level>FarmConfig.MaxLevel)return;
            NormalizeStars();levelStars[level-1]=Mathf.Max(levelStars[level-1],Mathf.Clamp(stars,0,3));
        }

        public int UpgradeLevel(int index) => index == 0 ? speed : index == 1 ? coinBonus : index == 2 ? farming : capture;

        public bool BuyUpgrade(int index, FarmConfig config)
        {
            if (index < 0 || index > 3) return false;
            int current = UpgradeLevel(index);
            int cost = config.upgradeBaseCost * (current + 1);
            if (current >= Mathf.Min(4, config.maxUpgradeLevel) || coins < cost) return false;
            coins -= cost;
            if (index == 0) speed++; else if (index == 1) coinBonus++; else if (index == 2) farming++; else capture++;
            Store();
            return true;
        }
    }
}

