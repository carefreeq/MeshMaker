#if XREditor|| UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.EventSystems;

namespace MeshMaker
{
    public interface ISelectObject
    {
        Transform transform { get; }
    }
    public interface IMeshObject : ISelectObject
    {
        MeshFilter[] Meshs { get; }
    }
    public interface ISkinObject : ISelectObject
    {
        SkinnedMeshRenderer[] Skins { get; }
    }
    public interface IIconObject : ISelectObject
    {
        Material Icon { get; }
    }
    public static class SelectObjectListExtensions
    {
        public static void _Add(this List<ISelectObject> os, ISelectObject o)
        {
            if (o != null)
                os.Add(o);
        }
        public static void _Remove(this List<ISelectObject> os, ISelectObject o)
        {
            if (o != null)
                os.Remove(o);
        }
    }
    /// <summary>
    /// 像素选择类
    /// </summary>
    public class SelectMgr
    {
        /// <summary>
        /// 功能开关
        /// </summary>
        public bool Enable { get; set; }
        public Camera Camera { get { return _camera; } set { _camera = value; } }
        [SerializeField]
        private Camera _camera;
        public event Action<bool> HandlingEvent;
        public List<ISelectObject> SelectObjects { get; set; }
        public event Action<List<ISelectObject>> SelectingEvent;
        public event Action<List<ISelectObject>> SelectedEvent;
        private Vector3 point;
        private bool isDown = false, isDrawBox = false;
        private Resource<Material> selectMat = new Resource<Material>("XRGL");
        private Resource<Material> drawObject = new Resource<Material>("SelectObject");
        private List<MaterialPropertyBlock> blocks = new List<MaterialPropertyBlock>();
        private List<ISelectObject> renders = new List<ISelectObject>();
        private List<ISelectObject> selected = new List<ISelectObject>();
        private List<ISelectObject> results = new List<ISelectObject>();
        [SerializeField]
        private Texture2D tex;
        private int w, h;
        private Vector2 min, max;

        public SelectMgr(Camera camera)
        {
            Enable = true;
            Camera = camera;
            SelectObjects = new List<ISelectObject>();
            for (int r = 0; r < 256; r++)
                for (int g = 1; g < 256; g++)
                {
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    block.SetColor("_Color", new Color(r / 255f, g / 255f, 0f, 1.0f));
                    blocks.Add(block);
                }
        }
        public void LateUpdate()
        {
            if (!Enable && !isDrawBox)
                return;
            if (Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                isDown = true;
                point = Input.mousePosition;
            }
            if (!isDown)
                return;
            if (Input.GetMouseButtonUp(0))
            {
                isDown = false;
                if (HandlingEvent != null)
                    HandlingEvent(false);
                if (!isDrawBox)
                {
                    CalcTexture(0.5f, 0.5f);
                    Vector2 uv = GetPointerUV();
                    Color col = tex.GetPixelBilinear(uv.x, uv.y);
                    ISelectObject s = GetSelectObject(col);
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl))
                    {
                        if (results.Contains(s))
                            results._Remove(s);
                        else
                            results._Add(s);
                    }
                    else
                    {
                        results.Clear();
                        results._Add(s);
                    }
                    if (SelectingEvent != null)
                        SelectingEvent(results);
                }
                isDrawBox = false;
                if (SelectedEvent != null)
                    SelectedEvent(results);
            }
            if (!isDown)
                return;
            if (!isDrawBox)
            {
                if ((Input.mousePosition - point).sqrMagnitude > 100)
                {
                    if (HandlingEvent != null)
                        HandlingEvent(true);
                    CalcTexture(0.1f, 0.1f);
                    isDrawBox = true;
                }
            }
            else
            {
                Vector2 _min = GetPointerUV(min);
                Vector2 _max = GetPointerUV(max) - _min;
                int x = (int)(_min.x * w);
                int y = (int)(_min.y * h);
                int sw = (int)(_max.x * w);
                int sh = (int)(_max.y * h);
                Color[] cols = tex.GetPixels(x, y, sw, sh);

                List<Color> _cols = new List<Color>();
                foreach (var col in cols)
                {
                    if (!_cols.Contains(col))
                        _cols.Add(col);
                }
                selected.Clear();
                foreach (var col in _cols)
                {
                    ISelectObject ele = GetSelectObject(col);
                    if (ele != null)
                        selected._Add(ele);
                }
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    results.AddRange(from s in selected where !results.Contains(s) select s);
                }
                else if (Input.GetKey(KeyCode.LeftControl))
                {
                    results = (from r in results where !selected.Contains(r) select r).ToList();
                }
                else
                {
                    results = selected;
                }
                if (SelectingEvent != null)
                    SelectingEvent(results);
            }
        }
        public void OnRenderObject()
        {
            if (isDrawBox)
            {
                if (point.x > Input.mousePosition.x)
                {
                    max.x = point.x;
                    min.x = Input.mousePosition.x;
                }
                else
                {
                    max.x = Input.mousePosition.x;
                    min.x = point.x;
                }
                if (point.y > Input.mousePosition.y)
                {
                    max.y = point.y;
                    min.y = Input.mousePosition.y;
                }
                else
                {
                    max.y = Input.mousePosition.y;
                    min.y = point.y;
                }

                GL.PushMatrix();
                selectMat.Object.SetPass(0);
                GL.LoadPixelMatrix();

                GL.Begin(GL.QUADS);
                GL.Color(new Color(0.2f, 0.3f, 0.5f, 0.1f));
                GL.Vertex3(min.x, min.y, 0);
                GL.Vertex3(min.x, max.y, 0);
                GL.Vertex3(max.x, max.y, 0);
                GL.Vertex3(max.x, min.y, 0);
                GL.End();

                GL.Begin(GL.LINES);
                GL.Color(new Color(0.2f, 0.5f, 0.8f, 0.6f));
                GL.Vertex3(min.x, min.y, 0);
                GL.Vertex3(min.x, max.y, 0);

                GL.Vertex3(min.x, max.y, 0);
                GL.Vertex3(max.x, max.y, 0);

                GL.Vertex3(max.x, max.y, 0);
                GL.Vertex3(max.x, min.y, 0);

                GL.Vertex3(max.x, min.y, 0);
                GL.Vertex3(min.x, min.y, 0);
                GL.End();
                GL.PopMatrix();
            }
        }
        private Vector2 GetPointerUV()
        {
            return GetPointerUV(Input.mousePosition);
        }
        private Vector2 GetPointerUV(Vector3 position)
        {
            return new Vector2(position.x / Screen.width, position.y / Screen.height);
        }
        private void CalcTexture(float width = 1.0f, float height = 1.0f)
        {
            w = (int)(Screen.width * width);
            h = (int)(Screen.height * height);
            Camera c = new GameObject("Camera").AddComponent<Camera>();
            c.CopyFrom(Camera);
            c.clearFlags = CameraClearFlags.Nothing;
            c.cullingMask = 64;
            c.allowMSAA = false;
            renders.Clear();
            int count = 0;
            foreach (ISelectObject o in SelectObjects)
            {
                if (count > 65534)
                    break;
                if (1 - Vector3.Dot((o.transform.position - c.transform.position).normalized, c.transform.forward) < c.fieldOfView / 180)
                {
                    renders._Add(o);
                    IMeshObject m = o as IMeshObject;
                    if (m != null)
                        for (int i = 0; i < m.Meshs.Length; i++)
                        {
                            Mesh mesh = m.Meshs[i].sharedMesh;
                            if (mesh != null)
                                for (int _i = 0; _i < mesh.subMeshCount; _i++)
                                    Graphics.DrawMesh(mesh, m.Meshs[i].transform.localToWorldMatrix, drawObject.Object, 6, c, _i, blocks[count], false, false, false);
                        }
                    ISkinObject s = o as ISkinObject;
                    if (s != null)
                        for (int i = 0; i < s.Skins.Length; i++)
                        {
                            Mesh _s = new Mesh();
                            s.Skins[i].BakeMesh(_s);
                            for (int _i = 0; _i < _s.subMeshCount; _i++)
                            {
                                Graphics.DrawMesh(_s, s.Skins[i].transform.localToWorldMatrix, drawObject.Object, 6, c, _i, blocks[count], false, false, false);
                            }
                            GameObject.Destroy(_s);
                        }
                    IIconObject n = o as IIconObject;
                    if (n != null)
                        if (n.Icon != null)
                        {
                            Quaternion rota = Quaternion.LookRotation(Vector3.Normalize(n.transform.position - Camera.transform.position), Vector3.up);
                            Graphics.DrawMesh(Tools.Quad, n.transform.position, rota, drawObject.Object, 6, c, 0, blocks[count], false, false, false);
                        }
                    count++;
                }
            }

            RenderTexture rtex = new RenderTexture(w, h, 0);
            c.targetTexture = rtex;
            c.Render();

            RenderTexture current = RenderTexture.active;
            RenderTexture.active = c.activeTexture;
            if (tex != null)
            {
                GameObject.Destroy(tex);
                tex = null;
            }
            tex = new Texture2D(w, h);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = current;
            GameObject.Destroy(rtex);
            GameObject.Destroy(c.gameObject);
        }
        private ISelectObject GetSelectObject(Color color)
        {
            if (color.a > 0.0f)
            {
                if (color.r > 0 || color.g > 0)// || color.b > 0)
                {
                    int r = (int)(color.r * 255f);
                    int g = (int)(color.g * 255f);
                    int i = r * 255 + g;
                    if (i > 0)
                        try
                        {
                            return renders[i - 1];
                        }
                        catch
                        {
                            Debug.Log("color:" + color + "index:" + i + "count:" + renders.Count);
                        }
                }
            }
            return null;
        }
    }
}
#endif