using System;
using UnityEngine;

namespace FarmGuardian
{
    public static class FarmSkinCatalog
    {
        public const string Classic="classic",GoldenFarmer="golden-farmer";
        public sealed class Skin
        {
            public readonly string Id,Name,Rarity,Resource;public readonly int Price;public readonly Color Color;
            public Skin(string id,string name,string rarity,int price,Color color,string resource=null)
            {Id=id;Name=name;Rarity=rarity;Price=price;Color=color;Resource=resource;}
        }
        public static readonly Skin[] All={
            new Skin(Classic,"CLASSIC FARMER","COMMON",0,new Color(.45f,.40f,.35f)),
            new Skin("tourist","TOURIST","UNCOMMON",200,new Color(.20f,.48f,.22f),"Tourist"),
            new Skin("queen","QUEEN","RARE",400,new Color(.16f,.39f,.70f),"Queen"),
            new Skin("ninja","NINJA","EPIC",700,new Color(.51f,.25f,.68f),"Ninja"),
            new Skin("samurai","SAMURAI","LEGENDARY",1100,new Color(.70f,.36f,.08f),"Samurai"),
            new Skin("golden-sensei","GOLDEN SENSEI","MYTHIC",1600,new Color(.70f,.19f,.30f),"GoldenSensei"),
            new Skin(GoldenFarmer,"GOLDEN FARMER","RARE",300,new Color(.16f,.39f,.70f))
        };
        public static Skin Find(string id)=>Array.Find(All,s=>s.Id==id);
        public static int Cost(string id,FarmConfig config)=>id==GoldenFarmer?config.goldenOutfitCost:Find(id)?.Price??int.MaxValue;
        public static GameObject Model(FarmConfig config,string id)
        {var skin=Find(id);return skin==null||skin.Resource==null?config.farmerModel:Resources.Load<GameObject>("Skins/"+skin.Resource);}
        public static Material Material(FarmConfig config,string id)=>Find(id)?.Resource==null?config.farmerMaterial:null;
    }
}
