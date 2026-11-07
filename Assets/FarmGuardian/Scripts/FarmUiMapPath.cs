using UnityEngine;
using UnityEngine.UI;

namespace FarmGuardian
{
    // A visual route only. Level availability and selection remain owned by FarmSession/FarmSave.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FarmUiMapPath : MaskableGraphic
    {
        public Vector2[] points;
        public float width=24;
        protected override void Awake(){base.Awake();useLegacyMeshGeneration=false;}
        public static Vector2[] Nodes()
        {
            var nodes=new Vector2[15];
            for(int i=0;i<15;i++){int row=i/5,col=row%2==0?i%5:4-i%5;nodes[i]=new Vector2(-330+col*165,-210+row*170);}
            return nodes;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(points==null||points.Length<2)return;
            for(int i=0;i<points.Length-1;i++)
            {
                var a=points[Mathf.Max(0,i-1)];var b=points[i];var c=points[i+1];var d=points[Mathf.Min(points.Length-1,i+2)];
                Vector2 previous=b;
                for(int j=1;j<=12;j++)
                {
                    float t=j/12f,t2=t*t,t3=t2*t;
                    Vector2 next=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t2+(-a+3*b-3*c+d)*t3);
                    var side=new Vector2(-(next-previous).y,(next-previous).x).normalized*width*.5f;int n=vh.currentVertCount;
                    vh.AddVert(previous-side,color,Vector2.zero);vh.AddVert(previous+side,color,Vector2.zero);vh.AddVert(next+side,color,Vector2.zero);vh.AddVert(next-side,color,Vector2.zero);
                    vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);previous=next;
                }
            }
        }
    }
}
