using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MeshMaker
{
    public enum Axis3 : int
    {
        X, Y, Z
    }
    public class Resource<T> where T : UnityEngine.Object
    {
        public T Object
        {
            get
            {
                if (_object == null)
                    _object = Resources.Load<T>(path);
                return _object;
            }
        }
        private T _object = null;
        private string path = null;
        public Resource(string path)
        {
            this.path = path;
        }
    }
    public class Tools
    {
        private static Resource<Material> glmat = new Resource<Material>("XRGL");
        public static Material GLMaterial { get { return glmat.Object; } }
        private static Mesh quad;
        internal static Mesh Quad
        {
            get
            {
                if (quad == null)
                {

                    quad = new Mesh();
                    quad.vertices = new Vector3[] {
                    new Vector3(-0.5f,-0.5f,0.0f),new Vector3(-0.5f,0.5f,0.0f),
                    new Vector3(0.5f,0.5f,0.0f),new Vector3(0.5f,-0.5f,0.0f)
                    };
                    quad.uv = new Vector2[] {
                    new Vector2(0f,0f),new Vector2(0f,1f),
                    new Vector2(1f,1f),new Vector2(1f,0f)
                    };
                    quad.triangles = new int[] {
                        0,1,3,3,1,2
                    };
                    quad.RecalculateBounds();
                }
                return quad;
            }
        }
        /// <summary>
        /// 绘制线
        /// </summary>
        /// <param name="from">起点</param>
        /// <param name="to">终点</param>
        /// <param name="col">颜色</param>
        public static void DrawWrieLine(Vector3 from, Vector3 to, Color col, bool world = true)
        {
            GL.PushMatrix();
            if (!world)
                GL.LoadPixelMatrix();
            GLMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(col);
            GL.Vertex(from);
            GL.Vertex(to);
            GL.End();
            GL.PopMatrix();
        }
        public static void DrawTriangle(Vector3 p0, Vector3 p1, Vector3 p2, Color col, bool world = true)
        {

            GL.PushMatrix();
            GLMaterial.SetPass(0);
            if (!world)
                GL.LoadPixelMatrix();
            GL.Begin(GL.TRIANGLES);
            GL.Color(col);
            GL.Vertex(p0);
            GL.Vertex(p1);
            GL.Vertex(p2);
            GL.End();
            GL.PopMatrix();
        }
        public static void DrawQuad(Vector3 position, Quaternion rotation, Color col, bool world = true, float width = 1.0f, float height = 1.0f)
        {

            Vector3[] pos = new Vector3[4];
            GL.PushMatrix();
            GLMaterial.SetPass(0);
            if (!world)
                GL.LoadPixelMatrix();
            GL.Begin(GL.QUADS);
            GL.Color(col);
            pos[0] = rotation * new Vector3(-width * 0.5f, -height * 0.5f, 0.0f) + position;
            pos[1] = rotation * new Vector3(-width * 0.5f, height * 0.5f, 0.0f) + position;
            pos[2] = rotation * new Vector3(width * 0.5f, height * 0.5f, 0.0f) + position;
            pos[3] = rotation * new Vector3(width * 0.5f, -height * 0.5f, 0.0f) + position;
            for (int i = 0; i < 5; i++)
                GL.Vertex(pos[i % 4]);
            GL.End();
            GL.PopMatrix();
        }
    }
}