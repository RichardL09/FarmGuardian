using UnityEngine;
using UnityEngine.UI;

namespace FarmGuardian
{
    public enum FarmIcon { Paw, Leaf, Coin, Shield, Pause, Arrow, Barn, Check, Play, Cart, Shirt, Gear, Star, Lock }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FarmUiIcon : Graphic
    {
        public FarmIcon Icon;
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            switch (Icon)
            {
                case FarmIcon.Lock:
                    Rect(helper,.22f,.1f,.56f,.5f,color);
                    Line(helper,new Vector2(.3f,.55f),new Vector2(.3f,.8f),.1f);
                    Line(helper,new Vector2(.3f,.8f),new Vector2(.7f,.8f),.1f);
                    Line(helper,new Vector2(.7f,.8f),new Vector2(.7f,.55f),.1f); break;
                case FarmIcon.Paw:
                    Circle(helper, new Vector2(0.5f, 0.35f), new Vector2(0.25f, 0.21f));
                    Circle(helper, new Vector2(0.21f, 0.65f), new Vector2(0.10f, 0.13f));
                    Circle(helper, new Vector2(0.42f, 0.79f), new Vector2(0.10f, 0.13f));
                    Circle(helper, new Vector2(0.65f, 0.79f), new Vector2(0.10f, 0.13f));
                    Circle(helper, new Vector2(0.82f, 0.61f), new Vector2(0.10f, 0.13f)); break;
                case FarmIcon.Leaf:
                    Polygon(helper, new[] { new Vector2(.17f,.16f), new Vector2(.16f,.56f), new Vector2(.43f,.86f), new Vector2(.88f,.9f), new Vector2(.82f,.44f), new Vector2(.5f,.2f) });
                    Line(helper, new Vector2(.12f,.09f), new Vector2(.68f,.67f), .065f); break;
                case FarmIcon.Coin:
                    Circle(helper, Vector2.one * .5f, Vector2.one * .43f);
                    Rect(helper, .46f, .23f, .08f, .54f, new Color(.43f,.29f,.10f,1)); break;
                case FarmIcon.Shield:
                    Polygon(helper, new[] { new Vector2(.5f,.04f), new Vector2(.85f,.34f), new Vector2(.9f,.84f), new Vector2(.5f,.97f), new Vector2(.1f,.84f), new Vector2(.15f,.34f) }); break;
                case FarmIcon.Pause:
                    Rect(helper, .22f,.18f,.18f,.64f,color); Rect(helper, .60f,.18f,.18f,.64f,color); break;
                case FarmIcon.Arrow:
                    Line(helper, new Vector2(.15f,.5f), new Vector2(.82f,.5f), .10f);
                    Line(helper, new Vector2(.55f,.79f), new Vector2(.85f,.5f), .10f);
                    Line(helper, new Vector2(.55f,.21f), new Vector2(.85f,.5f), .10f); break;
                case FarmIcon.Check:
                    Line(helper, new Vector2(.15f,.48f), new Vector2(.4f,.24f), .12f);
                    Line(helper, new Vector2(.4f,.24f), new Vector2(.88f,.8f), .12f); break;
                case FarmIcon.Barn:
                    Rect(helper,.18f,.12f,.64f,.49f,color);
                    Polygon(helper, new[] { new Vector2(.08f,.58f), new Vector2(.5f,.94f), new Vector2(.92f,.58f) });
                    Rect(helper,.38f,.12f,.24f,.36f,new Color(.98f,.91f,.7f,1)); break;
                case FarmIcon.Play:
                    Polygon(helper, new[] { new Vector2(.20f,.06f), new Vector2(.88f,.5f), new Vector2(.20f,.94f) }); break;
                case FarmIcon.Cart:
                    Line(helper,new Vector2(.05f,.87f),new Vector2(.22f,.87f),.07f);
                    Line(helper,new Vector2(.22f,.87f),new Vector2(.36f,.26f),.07f);
                    Line(helper,new Vector2(.36f,.26f),new Vector2(.85f,.26f),.07f);
                    Line(helper,new Vector2(.27f,.73f),new Vector2(.94f,.73f),.07f);
                    Line(helper,new Vector2(.94f,.73f),new Vector2(.82f,.40f),.07f);
                    Line(helper,new Vector2(.82f,.40f),new Vector2(.33f,.40f),.07f);
                    Line(helper,new Vector2(.48f,.72f),new Vector2(.53f,.42f),.04f);
                    Line(helper,new Vector2(.72f,.72f),new Vector2(.69f,.42f),.04f);
                    Line(helper,new Vector2(.32f,.56f),new Vector2(.87f,.56f),.04f);
                    Circle(helper,new Vector2(.4f,.12f),Vector2.one*.085f);
                    Circle(helper,new Vector2(.8f,.12f),Vector2.one*.085f); break;
                case FarmIcon.Shirt:
                    Rect(helper,.29f,.06f,.42f,.71f,color);
                    Polygon(helper,new[] { new Vector2(.29f,.88f),new Vector2(.02f,.65f),new Vector2(.18f,.44f),new Vector2(.38f,.67f),new Vector2(.42f,.78f) });
                    Polygon(helper,new[] { new Vector2(.71f,.88f),new Vector2(.98f,.65f),new Vector2(.82f,.44f),new Vector2(.62f,.67f),new Vector2(.58f,.78f) });
                    break;
                case FarmIcon.Gear:
                    var gear = new Vector2[32];
                    for (int i=0;i<gear.Length;i++)
                    { float a=i*Mathf.PI*2/gear.Length; float radius=i%4==0||i%4==3?.34f:.46f; gear[i]=Vector2.one*.5f+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius; }
                    Fan(helper, gear);
                    Circle(helper, Vector2.one*.5f, Vector2.one*.16f, new Color(.40f,.22f,.11f)); break;
                case FarmIcon.Star:
                    var star = new Vector2[10];
                    for(int i=0;i<star.Length;i++)
                    { float a=Mathf.PI*.5f+i*Mathf.PI/5; star[i]=Vector2.one*.5f+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(i%2==0?.43f:.22f); }
                    Fan(helper,star); break;
            }
        }

        Vector3 Position(Vector2 point)
        { Rect rect = rectTransform.rect; return new Vector3(rect.xMin + point.x * rect.width, rect.yMin + point.y * rect.height, 0); }
        void Polygon(VertexHelper helper, Vector2[] points, Color? tint = null)
        {
            int start = helper.currentVertCount;
            foreach (Vector2 point in points) helper.AddVert(Position(point), tint ?? color, Vector2.zero);
            // Canvas geometry faces the camera with clockwise triangle winding.
            for (int i = 1; i < points.Length - 1; i++) helper.AddTriangle(start, start + i + 1, start + i);
        }
        void Rect(VertexHelper helper, float x, float y, float w, float h, Color tint)
        { Polygon(helper, new[] { new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h) }, tint); }
        void Circle(VertexHelper helper, Vector2 center, Vector2 radius, Color? tint = null)
        {
            const int count = 24; var points = new Vector2[count];
            for (int i = 0; i < count; i++) { float angle = i * Mathf.PI * 2 / count; points[i] = center + new Vector2(Mathf.Cos(angle)*radius.x, Mathf.Sin(angle)*radius.y); }
            Polygon(helper, points, tint);
        }
        void Fan(VertexHelper helper, Vector2[] edge)
        {
            int start = helper.currentVertCount;
            helper.AddVert(Position(Vector2.one*.5f), color, Vector2.zero);
            foreach(var point in edge) helper.AddVert(Position(point),color,Vector2.zero);
            for(int i=0;i<edge.Length;i++) helper.AddTriangle(start,start+1+(i+1)%edge.Length,start+1+i);
        }
        void Line(VertexHelper helper, Vector2 a, Vector2 b, float width)
        {
            Vector2 normal = new Vector2(-(b-a).y, (b-a).x).normalized * width * .5f;
            Polygon(helper, new[] { a-normal,b-normal,b+normal,a+normal });
        }
    }
}
