using System.Collections.Generic;
using UnityEngine;

namespace FarmGuardian
{
    // All objective pens have generous inset from the 42 x 34 metre playable boundary.
    public sealed class FarmMapLayout
    {
        public const int ThemeCount=4;
        public int Theme;public string Name;
        public Vector3 PlayerSpawn,Escape,ResponseHub;
        public Vector3[] Pens,AnimalSpawns,Plots,ThiefEntrances;
        public readonly List<MapRoute> Routes=new List<MapRoute>();
        public readonly List<MapObstacle> Obstacles=new List<MapObstacle>();
        public readonly List<Vector3> CrossingCentres=new List<Vector3>();
        public static Vector3 Point(float x,float z)=>new Vector3(x,0,z);
        static Vector3[] Points(params float[] values)
        {var result=new Vector3[values.Length/2];for(int i=0;i<result.Length;i++)result[i]=Point(values[i*2],values[i*2+1]);return result;}
        void Route(string name,bool alternative,float width,params float[] values)
        {Routes.Add(new MapRoute{Name=name,Alternative=alternative,Width=width,Nodes=Points(values)});}
        void Obstacle(string name,string asset,float x,float z,float width,float height,float depth)
        {Obstacles.Add(new MapObstacle{Name=name,Asset=asset,Position=Point(x,z),Size=new Vector3(width,height,depth)});}
        public static FarmMapLayout ForTheme(int theme)
        {
            var m=new FarmMapLayout{Theme=Mathf.Clamp(theme,0,3)};m.Build();return m;
        }
        void Build()
        {
            Name=new[]{"Countryside Farm","Woodland Ranch","Mountain Farm","Advanced Farm"}[Theme];
            PlayerSpawn=Point(0,-13);ResponseHub=Point(0,0);
            int exitSide=Theme==3?-1:1;Escape=Point(exitSide*17,-13);
            Pens=Points(-10,7, 10,7, -10,-6, 10,-6);
            AnimalSpawns=Points(-5,-11.5f, 5,-12, -14,1.3f, 14,1.5f, -14,12.8f, 13,13.7f, -4,14, 6,-1.8f);
            Plots=Theme==2?Points(-4,5.8f, 4,-5.5f, -14,-4):Points(-4,5.8f, -4,-5.5f, Theme==3?14:-14,-4);
            ThiefEntrances=Points(-18,-12,18,12,-7,15,7,15,-18,0,18,-4,-8,-14,8,-14);
            if(Theme==2)
            {
                Route("Terrace response spine",false,3.2f,0,-13,-4.8f,-13,-4.8f,0,4.8f,0,4.8f,13.5f);
                Obstacle("South terrace ridge","Ridge",0,-6.5f,2,2.5f,3.8f);
                Obstacle("North terrace ridge","Ridge",0,8,2,2.8f,4);
                CrossingCentres.Add(Point(0,0));CrossingCentres.Add(Point(0,-9.9f));
            }
            else Route("Central response spine",false,3.2f,0,-13,0,0,0,13.5f);
            Route("Upper pen service apron",false,3,-18,3.1f,18,3.1f);
            float lowerRoad=Theme==1?-12.5f:-9.9f;
            Route("Lower pen service lane",false,3.2f,exitSide==1?-18:-12,lowerRoad,0,lowerRoad,exitSide==1?12:18,lowerRoad);
            if(Theme==1)
            {Route("Lower left pen approach",true,2.8f,-10,-12.5f,-10,-9.9f);Route("Lower right pen approach",true,2.8f,10,-12.5f,10,-9.9f);}
            Route("Village cross street",false,3.2f,-18,0,18,0);
            Route("North village loop",true,2.8f,-18,13.5f,18,13.5f);
            Route("West service connection",true,2.8f,-18,exitSide==1?-13:0,-18,13.5f);
            Route("East service connection",true,2.8f,18,exitSide==1?0:-13,18,13.5f);
            Route("Marked thief exit corridor",true,3,exitSide*18,0,exitSide*18,-13,exitSide*17,-13);
            // A single access from the north prevents the lower pen beside the exit from
            // giving the thief an unfair two-second escape through the nearest corner.
            Obstacle("Exit corridor divider","Fence",exitSide*14,-10,.18f,1,14);
            Obstacle("Village barn","Barn",-15,9.5f,4,4.8f,4);
            Obstacle("Village well","Well",Theme==2?-7:-4,Theme==2?-2.4f:-2.8f,1.6f,1.6f,1.6f);
            Obstacle("Relocated windmill","Windmill",Theme==2?-5:5,10.5f,2.6f,5.5f,2.6f);
            if(Theme==1)
            {Obstacle("Lower woodland grove","Grove",-5,-8,2.4f,1.5f,2.4f);Obstacle("Upper woodland grove","Grove",14,7,2.2f,1.5f,2.2f);}
            if(Theme==3)
            {
                Obstacle("Advanced working barn","Barn",5,-5.5f,3.6f,4.2f,3.6f);
                Obstacle("Central hay store","Hay_Bale",-5,10,2.2f,1.2f,1.7f);
            }
        }
    }
    public sealed class MapRoute{public string Name;public bool Alternative;public float Width;public Vector3[] Nodes;}
    public sealed class MapObstacle{public string Name,Asset;public Vector3 Position,Size;}
}
