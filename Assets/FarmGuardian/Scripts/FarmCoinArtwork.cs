using UnityEngine;

namespace FarmGuardian
{
    public static class FarmCoinArtwork
    {
        static Sprite sprite;
        public static Sprite Sprite
        {
            get
            {
                if(sprite!=null)return sprite;
                var texture=Resources.Load<Texture2D>("CozyUI/CloverCoin");if(texture==null)return null;
                // Clip the supplied circular artwork with sprite geometry. The original JPG
                // remains unchanged; white corners never enter the UI mesh.
                float side=Mathf.Min(texture.width,texture.height)*.836f;
                sprite=UnityEngine.Sprite.Create(texture,new Rect((texture.width-side)*.5f,(texture.height-side)*.5f,side,side),Vector2.one*.5f,100);
                const int segments=96;var vertices=new Vector2[segments+1];var triangles=new ushort[segments*3];
                vertices[0]=Vector2.one*side*.5f;
                for(int i=0;i<segments;i++)
                {float angle=i*Mathf.PI*2/segments;vertices[i+1]=vertices[0]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*side*.499f;
                    triangles[i*3]=0;triangles[i*3+1]=(ushort)(i+1);triangles[i*3+2]=(ushort)((i+1)%segments+1);}
                sprite.OverrideGeometry(vertices,triangles);return sprite;
            }
        }
    }
}
