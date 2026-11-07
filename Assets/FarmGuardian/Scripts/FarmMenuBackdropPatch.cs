using UnityEngine;
using UnityEngine.UI;

namespace FarmGuardian
{
    // Feather the existing clean plate into the original art without editing either PNG.
    public sealed class FarmMenuBackdropPatch : RawImage
    {
        public float Feather=20;
        public Vector2 FeatherAxes=Vector2.one;
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();var r=GetPixelAdjustedRect();
            float feather=Mathf.Min(Feather,Mathf.Min(r.width,r.height)*.25f);
            float fx=feather*FeatherAxes.x,fy=feather*FeatherAxes.y;
            float[] xs={0,fx,r.width-fx,r.width},ys={0,fy,r.height-fy,r.height};
            for(int y=0;y<4;y++)for(int x=0;x<4;x++)
            {
                var tint=color;tint.a*=x==0||x==3||y==0||y==3?0:1;
                helper.AddVert(new Vector3(r.xMin+xs[x],r.yMin+ys[y]),tint,
                    new Vector2(uvRect.x+xs[x]/r.width*uvRect.width,uvRect.y+ys[y]/r.height*uvRect.height));
            }
            for(int y=0;y<3;y++)for(int x=0;x<3;x++)
            {int i=y*4+x;helper.AddTriangle(i,i+4,i+5);helper.AddTriangle(i,i+5,i+1);}
        }
    }
}
