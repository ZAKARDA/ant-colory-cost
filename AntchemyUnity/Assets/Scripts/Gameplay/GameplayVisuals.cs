using UnityEngine;

namespace Antchemy
{
    // Shared, lightweight primitive art keeps the game self-contained.
    public static class GameplayVisuals
    {
        private static Material _material;
        public static Material Material
        {
            get
            {
                if (_material == null)
                {
                    Shader shader = Resources.Load<Shader>("SoftToy") ?? Shader.Find("Standard") ?? Shader.Find("Mobile/Diffuse");
                    _material = new Material(shader);
                    _material.enableInstancing = true;
                    if (_material.HasProperty("_Glossiness")) _material.SetFloat("_Glossiness", .22f);
                    if (_material.HasProperty("_Smoothness")) _material.SetFloat("_Smoothness", .22f);
                }
                return _material;
            }
        }
        static Mesh _cubeMesh;
        public static Mesh CubeMesh
        {
            get
            {
                if (_cubeMesh != null) return _cubeMesh;
                var vertices = new System.Collections.Generic.List<Vector3>();
                var normals = new System.Collections.Generic.List<Vector3>();
                var triangles = new System.Collections.Generic.List<int>();
                const float h = .40f;
                for (int axis = 0; axis < 3; axis++) for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 n = Vector3.zero; n[axis] = sign;
                    int a = (axis + 1) % 3, b = (axis + 2) % 3;
                    var face = new Vector3[4];
                    int[] sa = {-1,1,1,-1}, sb = {-1,-1,1,1};
                    for(int i=0;i<4;i++){face[i]=n*.5f;face[i][a]=sa[i]*h;face[i][b]=sb[i]*h;}
                    AddFace(face,n,vertices,normals,triangles);
                }
                for (int axis = 0; axis < 3; axis++) for(int sa=-1;sa<=1;sa+=2) for(int sb=-1;sb<=1;sb+=2)
                {
                    int a=(axis+1)%3,b=(axis+2)%3;var face=new Vector3[4];
                    face[0][axis]=-h;face[0][a]=sa*.5f;face[0][b]=sb*h;
                    face[1][axis]=h;face[1][a]=sa*.5f;face[1][b]=sb*h;
                    face[2][axis]=h;face[2][a]=sa*h;face[2][b]=sb*.5f;
                    face[3][axis]=-h;face[3][a]=sa*h;face[3][b]=sb*.5f;
                    Vector3 n=Vector3.zero;n[a]=sa;n[b]=sb;AddFace(face,n.normalized,vertices,normals,triangles);
                }
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    AddFace(new[]{new Vector3(x*.5f,y*h,z*h),new Vector3(x*h,y*.5f,z*h),new Vector3(x*h,y*h,z*.5f)},new Vector3(x,y,z).normalized,vertices,normals,triangles);
                _cubeMesh=new Mesh();_cubeMesh.name="Shared beveled toy cube";_cubeMesh.SetVertices(vertices);_cubeMesh.SetNormals(normals);_cubeMesh.SetTriangles(triangles,0);_cubeMesh.RecalculateBounds();return _cubeMesh;
            }
        }
        static void AddFace(Vector3[] face,Vector3 normal,System.Collections.Generic.List<Vector3> vertices,System.Collections.Generic.List<Vector3> normals,System.Collections.Generic.List<int> triangles)
        {
            int start=vertices.Count;vertices.AddRange(face);for(int i=0;i<face.Length;i++)normals.Add(normal);
            bool forward=Vector3.Dot(Vector3.Cross(face[1]-face[0],face[2]-face[0]),normal)>0;
            for(int i=1;i<face.Length-1;i++){triangles.Add(start);triangles.Add(start+(forward?i:i+1));triangles.Add(start+(forward?i+1:i));}
        }
        public static Color Pastel(int index) => ColorPalette.GetColor(index);
        public static GameObject Shape(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            if (primitive == PrimitiveType.Cube) part.GetComponent<MeshFilter>().sharedMesh = CubeMesh;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Object.Destroy(part.GetComponent<Collider>());
            Paint(part.GetComponent<Renderer>(), color);
            return part;
        }
        public static void Paint(Renderer renderer, Color color)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = Material;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            block.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(block);
        }
        public static AntMotion Ant(Transform parent, int colorIndex, float scale = 1f)
        {
            var ant = new GameObject("Anatomical ant");
            ant.transform.SetParent(parent, false);
            ant.transform.localScale = Vector3.one * scale;
            var motion = ant.AddComponent<AntMotion>();
            motion.Initialize(colorIndex);
            return motion;
        }
    }
}
