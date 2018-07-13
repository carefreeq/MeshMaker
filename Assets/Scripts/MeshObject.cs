using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MeshMaker
{
    public enum PolyType : int
    {
        Null, Point, Edge, Face
    }
    [RequireComponent(typeof(MeshFilter))]
    public class MeshObject : MonoBehaviour, IMeshObject
    {
        public static List<MeshObject> Objects { get; private set; }
        public static event Action<MeshObject> AddEvent, RemoveEvent;
        static MeshObject()
        {
            Objects = new List<MeshObject>();
        }
        public bool Enable { get; set; }

        public MeshFilter[] Meshs { get; private set; }
        public PolyType EditorType { get { return editortype; } set { editortype = value; } }
        [SerializeField]
        private PolyType editortype;
        private static Handle h;
        private Point p;
        private Edge e;
        private Face f;
        private void Awake()
        {
            Meshs = GetComponentsInChildren<MeshFilter>();
            Objects.Add(this);
            if (AddEvent != null) AddEvent(this);
        }
        private void Start()
        {
            Enable = true;
            MeshInfo m = new MeshInfo(gameObject.GetComponent<MeshFilter>());
            p = new Point(m);
            e = new Edge(m);
            f = new Face(m);
        }
        private void OnDestroy()
        {
            Objects.Remove(this);
            if (RemoveEvent != null) RemoveEvent(this);
        }
        private void OnRenderObject()
        {
            if (Enable)
                switch (EditorType)
                {
                    case PolyType.Null:
                        break;
                    case PolyType.Point:
                        p.Draw(200f, 5f);
                        break;
                    case PolyType.Edge:
                        e.Draw(1000f);
                        break;
                    case PolyType.Face:
                        f.Draw();
                        break;
                }
        }

        private void LateUpdate()
        {
            if (Enable && MeshMakerMgr.Enable)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    switch (EditorType)
                    {
                        case PolyType.Null:
                            break;
                        case PolyType.Point:
                            p.Select();
                            break;
                        case PolyType.Edge:
                            e.Select();
                            break;
                        case PolyType.Face:
                            f.Select();
                            break;
                    }
                }
            }
        }
        private class MeshInfo
        {
            private MeshFilter meshFilter;
            private Mesh mesh;
            public Vector3[] v { get; private set; }
            public Vector3[] n { get; private set; }
            public int[] t { get; private set; }
            public Transform transform { get; private set; }
            public MeshInfo(MeshFilter m)
            {
                meshFilter = m;
                transform = m.transform;
                mesh = m.mesh;
                v = mesh.vertices;
                n = mesh.normals;
                t = mesh.triangles;
            }
            public MeshInfo(MeshInfo m)
            {
                meshFilter = m.meshFilter;
                mesh = m.mesh;
                v = mesh.vertices;
                n = mesh.normals;
                t = mesh.triangles;
            }
            public void CalculateVertex()
            {
                mesh.vertices = v;
                meshFilter.mesh = mesh;
            }
        }
        private class Point
        {
            private MeshInfo m;
            private int index;
            private int select;
            public Point(MeshInfo m)
            {
                this.m = m;
                select = -1;
            }
            private void DrawQuad(int i, float s)
            {
                if (i < 0)
                    return;
                Vector3 p = (Vector2)MeshMakerMgr.Camera.WorldToScreenPoint(m.transform.TransformPoint(m.v[i]));
                Tools.DrawQuad(p, Quaternion.identity, Color.yellow, false, s, s);
            }
            public void Draw(float near, float size)
            {
                index = -1;
                float dis = float.MaxValue;

                int tl = m.t.Length / 3;
                for (int i = 0; i < tl; i++)
                {
                    int ti = i * 3;
                    for (int _i = 0; _i < 3; _i++)
                    {
                        int p0i = m.t[ti + _i];
                        int p1i = m.t[ti + (_i + 1) % 3];

                        if (Vector3.Dot(MeshMakerMgr.Camera.transform.forward, m.n[p0i]) < 0)
                        {
                            Vector3 p0 = (Vector2)MeshMakerMgr.Camera.WorldToScreenPoint(m.transform.TransformPoint(m.v[p0i]));
                            Vector3 p1 = (Vector2)MeshMakerMgr.Camera.WorldToScreenPoint(m.transform.TransformPoint(m.v[p1i]));
                            Tools.DrawWrieLine(p0, p1, Color.green * 0.5f, false);
                            Tools.DrawQuad(p0, Quaternion.identity, Color.green, false, size, size);
                            float d = (p0 - Input.mousePosition).sqrMagnitude;
                            if (d < near && d < dis)
                            {
                                dis = d;
                                index = p0i;
                            }
                        }
                    }
                }

                DrawQuad(select, size);
                DrawQuad(index, size);
            }
            public void Select()
            {
                select = index;
                if (select < 0)
                {
                    if (h)
                        h.Close();
                    h = null;
                }
                else
                {
                    if (h)
                        h.Close();
                    h = MeshMakerMgr.Handle.CreatePosition(m.transform.TransformPoint(m.v[select]), (_v) =>
                    {
                        m.v[select] = m.transform.InverseTransformPoint(_v);
                        m.CalculateVertex();
                    });
                }
            }
        }
        private class Edge
        {
            private MeshInfo m;
            private int i0, i1;
            private int s0, s1;
            public Edge(MeshInfo m)
            {
                this.m = m;
            }
            private float Distance(Vector2 p, Vector2 a, Vector2 b)
            {
                Vector2 ap = p - a;
                Vector2 ab = b - a;
                float r = Vector2.Dot(ap, ab.normalized) / ab.magnitude;
                if (r > 1 || r < 0)
                    return float.MaxValue;
                return (ap - ab * r).sqrMagnitude;
            }
            private void DrawWrieLine(int a, int b)
            {
                if (a < 0)
                    return;
                Vector3 p0 = (Vector2)MeshMakerMgr.Camera.WorldToScreenPoint(m.transform.TransformPoint(m.v[a]));
                Vector3 p1 = (Vector2)MeshMakerMgr.Camera.WorldToScreenPoint(m.transform.TransformPoint(m.v[b]));
                Tools.DrawWrieLine(p0, p1, Color.yellow, false);
            }
            public void Draw(float near)
            {
                i0 = i1 = -1;
                float dis = float.MaxValue;
                int tl = m.t.Length / 3;
                for (int i = 0; i < tl; i++)
                {
                    int ti = i * 3;
                    for (int _i = 0; _i < 3; _i++)
                    {
                        int p0i = m.t[ti + _i];
                        int p1i = m.t[ti + (_i + 1) % 3];

                        if (Vector3.Dot(MeshMakerMgr.Camera.transform.forward, (m.n[p0i] + m.n[p1i]).normalized) < 0)
                        {
                            Vector3 p0 = (Vector2)MeshMakerMgr.Camera.WorldToScreenPoint(m.transform.TransformPoint(m.v[p0i]));
                            Vector3 p1 = (Vector2)MeshMakerMgr.Camera.WorldToScreenPoint(m.transform.TransformPoint(m.v[p1i]));
                            Tools.DrawWrieLine(p0, p1, Color.green, false);
                            float d = Distance(Input.mousePosition, p0, p1);
                            if (d < near && d < dis)
                            {
                                dis = d;
                                i0 = p0i;
                                i1 = p1i;
                            }
                        }
                    }
                }
                DrawWrieLine(s0, s1);
                DrawWrieLine(i0, i1);
            }
            public void Select()
            {
                s0 = i0;
                s1 = i1;
                if (s0 < 0)
                {
                    if (h)
                        h.Close();
                    h = null;
                }
                else
                {
                    if (h)
                    {
                        h.Close();
                        h = null;
                    }
                    Vector3 p0 = m.transform.TransformPoint(m.v[s0]);
                    Vector3 p1 = m.transform.TransformPoint(m.v[s1]);
                    Vector3 c = (p0 + p1) * 0.5f;
                    Vector3 o0 = p0 - c;
                    Vector3 o1 = p1 - c;
                    h = MeshMakerMgr.Handle.CreatePosition(c, (_v) =>
                    {
                        m.v[s0] = m.transform.InverseTransformPoint(_v + o0);
                        m.v[s1] = m.transform.InverseTransformPoint(_v + o1);
                        m.CalculateVertex();
                    });
                }
            }
        }
        private class Face
        {
            private MeshInfo m;
            private int i0, i1, i2;
            private int s0, s1, s2;
            public Face(MeshInfo m)
            {
                this.m = m;
            }
            private bool Check(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
            {
                Vector2 ab = b - a;
                Vector2 ac = c - a;
                Vector2 pa = a - p;
                Vector2 pb = b - p;
                Vector2 pc = c - p;
                return Mathf.Abs(Mathf.Abs(Cross(ab, ac)) - Mathf.Abs(Cross(pa, pb)) - Mathf.Abs(Cross(pb, pc)) - Mathf.Abs(Cross(pc, pa))) < 1f;
            }
            private float Cross(Vector2 a, Vector2 b)
            {
                return a.x * b.y - a.y * b.x;
            }
            private void DrawTriangle(int a, int b, int c)
            {
                if (a < 0)
                    return;
                Vector3 p0 = (Vector2)MeshMakerMgr.Camera.WorldToScreenPoint(m.transform.TransformPoint(m.v[a]));
                Vector3 p1 = (Vector2)MeshMakerMgr.Camera.WorldToScreenPoint(m.transform.TransformPoint(m.v[b]));
                Vector3 p2 = (Vector2)MeshMakerMgr.Camera.WorldToScreenPoint(m.transform.TransformPoint(m.v[c]));
                Tools.DrawTriangle(p0, p1, p2, Color.green, false);
            }
            public void Draw()
            {
                i0 = i1 = i2 = -1;
                int tl = m.t.Length / 3;
                for (int i = 0; i < tl; i++)
                {
                    int ti = i * 3;
                    int p0i = m.t[ti];
                    int p1i = m.t[ti + 1];
                    int p2i = m.t[ti + 2];
                    if (Vector3.Dot(MeshMakerMgr.Camera.transform.forward, (m.n[p0i] + m.n[p1i] + m.n[p2i]).normalized) < 0)
                    {
                        Vector3 p0 = (Vector2)MeshMakerMgr.Camera.WorldToScreenPoint(m.transform.TransformPoint(m.v[p0i]));
                        Vector3 p1 = (Vector2)MeshMakerMgr.Camera.WorldToScreenPoint(m.transform.TransformPoint(m.v[p1i]));
                        Vector3 p2 = (Vector2)MeshMakerMgr.Camera.WorldToScreenPoint(m.transform.TransformPoint(m.v[p2i]));
                        Tools.DrawWrieLine(p0, p1, Color.green, false);
                        Tools.DrawWrieLine(p0, p2, Color.green, false);
                        Tools.DrawWrieLine(p1, p2, Color.green, false);
                        Tools.DrawTriangle(p0, p1, p2, new Color(0.0f, 1.0f, 0.0f, 0.2f), false);
                        if (i0 < 0 && Check(Input.mousePosition, p0, p1, p2))
                        {
                            i0 = p0i;
                            i1 = p1i;
                            i2 = p2i;
                        }
                    }
                }
                DrawTriangle(s0, s1, s2);
                DrawTriangle(i0, i1, i2);
            }
            public void Select()
            {
                s0 = i0;
                s1 = i1;
                s2 = i2;
                if (s0 < 0)
                {
                    if (h)
                        h.Close();
                    h = null;
                }
                else
                {
                    if (h)
                    {
                        h.Close();
                        h = null;
                    }
                    Vector3 p0 = m.transform.TransformPoint(m.v[s0]);
                    Vector3 p1 = m.transform.TransformPoint(m.v[s1]);
                    Vector3 p2 = m.transform.TransformPoint(m.v[s2]);
                    Vector3 c = (p0 + p1) * 0.25f + (p0 + p2) * 0.25f;
                    Vector3 o0 = p0 - c;
                    Vector3 o1 = p1 - c;
                    Vector3 o2 = p2 - c;
                    h = MeshMakerMgr.Handle.CreatePosition(c, (_v) =>
                    {
                        m.v[s0] = m.transform.InverseTransformPoint(_v + o0);
                        m.v[s1] = m.transform.InverseTransformPoint(_v + o1);
                        m.v[s2] = m.transform.InverseTransformPoint(_v + o2);
                        m.CalculateVertex();
                    });
                }
            }
        }
    }
}