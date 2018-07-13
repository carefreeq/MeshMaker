#if XREditor|| UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshMaker
{
    public delegate void FloatCallback(float v);
    public delegate void Vector3Callback(Vector3 v);
    public delegate void QuaternionCallback(Quaternion r);
    public abstract class Handle : MonoBehaviour
    {
        private static Transform parent = null;
        public static Transform Parent { get { if (!parent) { parent = new GameObject("Handles").transform; parent.hideFlags = HideFlags.HideInHierarchy; } return parent; } }

        protected Property property;
        protected void Start()
        {
            gameObject.layer = property.Layer.value;

        }
        protected void Update()
        {
            transform.localScale = Vector3.one * Vector3.Distance(transform.position, property.Camera.transform.position) * property.Size;
        }
        public abstract void Close();

        public class Property
        {
            public Camera Camera { get; set; }
            public LayerMask Layer { get; set; }
            private float size = 0.1f;
            public float Size { get { return size; } set { size = Mathf.Clamp(value, 0.02f, 0.3f); } }
            public event Action<bool> HandlingEvent;
            private List<Move> mep = new List<Move>();
            private Queue<Move> mdp = new Queue<Move>();
            private List<Rota> rep = new List<Rota>();
            private Queue<Rota> rdp = new Queue<Rota>();
            private List<Scale> sep = new List<Scale>();
            private Queue<Scale> sdp = new Queue<Scale>();
            public Property(Camera c)
            {
                Layer = LayerMask.NameToLayer("UI");
                Camera = c;
            }
            public Handle CreatePosition(Vector3 pos, Vector3Callback callback)
            {
                return Move.Create(this, pos, callback);
            }
            public Handle CreateRotation(Quaternion rota, QuaternionCallback callback)
            {
                return Rota.Create(this, rota, callback);
            }
            public Handle CreateScale(Vector3 scal, Vector3Callback callback)
            {
                return Scale.Create(this, scal, callback);
            }

            [RequireComponent(typeof(MeshRenderer))]
            private class _Handle : Handle, IMouseDown, IMouseUp, IMouseDrag
            {
                private Material mat;
                private Color color;
                private Mesh mesh;
                protected void Awake()
                {
                    mat = GetComponent<MeshRenderer>().material;
                    color = mat.color;
                    mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
                }
                private new void Update()
                { }
                private void OnRenderObject()
                {
                    //Graphics.DrawMesh(mesh, transform.localToWorldMatrix, mat, property.Layer, property.Camera);
                    mat.SetPass(0);
                    Graphics.DrawMeshNow(mesh, transform.localToWorldMatrix);
                    mat.SetPass(1);
                    Graphics.DrawMeshNow(mesh, transform.localToWorldMatrix);
                }
                public virtual void Drag()
                {
                    mat.color = Color.yellow;
                }
                public virtual void Up()
                {
                    if (property.HandlingEvent != null)
                        property.HandlingEvent(false);
                    mat.color = color;
                }
                public virtual void Down()
                {
                    if (property.HandlingEvent != null)
                        property.HandlingEvent(true);
                }
                protected Vector3 GetPoint()
                {
                    return GetPoint(transform.right);
                }
                protected Vector3 GetPoint(Vector3 dir)
                {
                    return GetPoint(dir, transform.position);
                }
                protected Vector3 GetPoint(Vector3 dir, Vector3 pos)
                {
                    Ray ray = property.Camera.ScreenPointToRay(Input.mousePosition);
                    float dis = 0;
                    Plane plane = new Plane(dir, pos);
                    plane.Raycast(ray, out dis);
                    return ray.direction * dis + ray.origin;
                }

                public override void Close()
                {
                    //throw new NotImplementedException();
                }
            }
            private class Move : Handle
            {
                private Vector3Callback callback;
                public static Move Create(Property p, Vector3 o, Vector3Callback c)
                {
                    Move m = null;
                    if (p.mdp.Count > 0)
                    {
                        m = p.mdp.Dequeue();
                        m.gameObject.SetActive(true);
                        m.transform.position = o;
                    }
                    else
                    {
                        m = new GameObject("Position Handle").AddComponent<Move>();
                        m.transform.position = o;
                        m.transform.SetParent(Parent);
                        Vector3Callback _c = (v) => { m.transform.position = v; if (m.callback != null) m.callback(v); };

                        for (int i = 0; i < 3; i++)
                        {
                            MoveSingle s = MoveSingle.Create(p, o, (Axis3)i, null);
                            s.transform.SetParent(m.transform, false);
                            s.DragEvent += (v) => { _c(v); s.transform.position = v; };
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            MoveDouble d = MoveDouble.Create(p, o, (Axis3)i, null);
                            d.transform.SetParent(m.transform, false);
                            d.DragEvent += (v) => { _c(v); d.transform.position = v; };
                        }
                    }
                    m.callback = c;
                    m.property = p;
                    p.mep.Add(m);
                    return m;
                }
                public override void Close()
                {
                    property.mep.Remove(this);
                    property.mdp.Enqueue(this);
                    gameObject.SetActive(false);
                }
            }
            private class MoveSingle : _Handle
            {
                private static Resource<GameObject> x = new Resource<GameObject>("handle/position_x");
                private static Resource<GameObject> y = new Resource<GameObject>("handle/position_y");
                private static Resource<GameObject> z = new Resource<GameObject>("handle/position_z");
                public static MoveSingle Create(Property p, Vector3 o, Axis3 a, Vector3Callback c)
                {
                    GameObject g = null;
                    switch (a)
                    {
                        case Axis3.X:
                            g = GameObject.Instantiate(x.Object);
                            break;
                        case Axis3.Y:
                            g = GameObject.Instantiate(y.Object);
                            break;
                        case Axis3.Z:
                            g = GameObject.Instantiate(z.Object);
                            break;
                        default:
                            return null;
                    }
                    MoveSingle m = g.AddComponent<MoveSingle>();
                    m.property = p;
                    m.DragEvent += c;
                    return m;
                }

                public event Vector3Callback DownEvent;
                public event Vector3Callback DragEvent;
                public event Vector3Callback UpEvent;
                private Vector3 origin;
                private Vector3 dir;
                private Vector3 last;
                private Vector3 offset;
                public override void Drag()
                {
                    base.Drag();
                    Vector3 p = GetPoint(dir);
                    offset = Vector3.Dot(p - last, transform.forward) * transform.forward;
                    transform.position = origin + offset;
                    if (DragEvent != null)
                        DragEvent(transform.position);
                }
                public override void Up()
                {
                    base.Up();
                    if (UpEvent != null)
                        UpEvent(transform.position);
                }
                public override void Down()
                {
                    base.Down();
                    dir = Vector3.Cross(transform.forward, property.Camera.transform.forward);
                    dir = Vector3.Cross(dir, transform.forward);
                    last = GetPoint(dir);
                    origin = transform.position;
                    if (DownEvent != null)
                        DownEvent(transform.position);
                }
            }
            private class MoveDouble : _Handle
            {
                private static Resource<GameObject> x = new Resource<GameObject>("handle/position_d_x");
                private static Resource<GameObject> y = new Resource<GameObject>("handle/position_d_y");
                private static Resource<GameObject> z = new Resource<GameObject>("handle/position_d_z");
                public static MoveDouble Create(Property p, Vector3 o, Axis3 a, Vector3Callback c)
                {
                    GameObject g = null;
                    switch (a)
                    {
                        case Axis3.X:
                            g = GameObject.Instantiate(x.Object);
                            break;
                        case Axis3.Y:
                            g = GameObject.Instantiate(y.Object);
                            break;
                        case Axis3.Z:
                            g = GameObject.Instantiate(z.Object);
                            break;
                        default:
                            return null;
                    }
                    MoveDouble m = g.AddComponent<MoveDouble>();
                    m.property = p;
                    m.DragEvent += c;
                    return m;
                }

                public event Vector3Callback DownEvent;
                public event Vector3Callback DragEvent;
                public event Vector3Callback UpEvent;
                private Vector3 origin;
                private Vector3 offset;
                private Vector3 last;
                public override void Drag()
                {
                    base.Drag();
                    offset = GetPoint(transform.forward) - last;
                    transform.position = origin + offset;
                    if (DragEvent != null)
                        DragEvent(transform.position);
                }
                public override void Down()
                {
                    base.Down();
                    last = GetPoint(transform.forward);
                    origin = transform.position;
                    if (DownEvent != null)
                        DownEvent(transform.position);
                }
                public override void Up()
                {
                    base.Up();
                    if (UpEvent != null)
                        UpEvent(transform.position);
                }
                private void OnEnable()
                {
                    if (property != null)
                        Reset();
                }
                private new void Update()
                {
                    if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
                    {
                        Reset();
                    }
                }
                private void Reset()
                {
                    Vector3 view = Vector3.Normalize(property.Camera.transform.position - transform.position);
                    float up = Vector3.Dot(view, transform.up);
                    float right = Vector3.Dot(view, transform.right);
                    if (up < 0 && right > 0)
                    {
                        transform.Rotate(Vector3.forward, -90f);
                    }
                    else if (up > 0 && right < 0)
                    {
                        transform.Rotate(Vector3.forward, 90f);
                    }
                    else if (up < 0 && right < 0)
                    {
                        transform.Rotate(Vector3.forward, 180f);
                    }
                }
            }

            private class Rota : Handle
            {
                private QuaternionCallback callback;
                public static Rota Create(Property p, Quaternion o, QuaternionCallback c)
                {
                    Rota r = null;
                    if (p.rdp.Count > 0)
                    {
                        r = p.rdp.Dequeue();
                        r.gameObject.SetActive(true);
                        r.transform.rotation = o;
                    }
                    else
                    {
                        r = new GameObject("Rotation Handle").AddComponent<Rota>();
                        r.transform.rotation = o;
                        r.transform.SetParent(Parent);
                        QuaternionCallback _c = (v) => { r.transform.rotation = v; if (r.callback != null) r.callback(v); };
                        for (int i = 0; i < 3; i++)
                        {
                            RotaSingle s = RotaSingle.Create(p, o, (Axis3)i, null);
                            s.transform.SetParent(r.transform, false);
                            s.DragEvent += _c;
                        }
                    }
                    r.callback = c;
                    r.property = p;
                    p.rep.Add(r);
                    return r;
                }
                public override void Close()
                {
                    property.rep.Remove(this);
                    property.rdp.Enqueue(this);
                    gameObject.SetActive(false);
                }
            }
            private class RotaSingle : _Handle
            {
                private static Resource<GameObject> x = new Resource<GameObject>("handle/rotation_x");
                private static Resource<GameObject> y = new Resource<GameObject>("handle/rotation_y");
                private static Resource<GameObject> z = new Resource<GameObject>("handle/rotation_z");
                public static RotaSingle Create(Property p, Quaternion o, Axis3 a, QuaternionCallback c)
                {
                    GameObject g = null;
                    switch (a)
                    {
                        case Axis3.X:
                            g = GameObject.Instantiate(x.Object);
                            break;
                        case Axis3.Y:
                            g = GameObject.Instantiate(y.Object);
                            break;
                        case Axis3.Z:
                            g = GameObject.Instantiate(z.Object);
                            break;
                        default:
                            return null;
                    }
                    RotaSingle r = g.AddComponent<RotaSingle>();
                    r.property = p;
                    r.DragEvent += c;
                    return r;
                }

                public event QuaternionCallback DownEvent;
                public event QuaternionCallback DragEvent;
                public event QuaternionCallback UpEvent;
                public Quaternion origin;
                private Vector3 point;
                private Vector3 tangent;
                private Vector3 axis;
                private Quaternion start;
                private Quaternion instart;
                protected new void Awake()
                {
                    base.Awake();
                    axis = transform.right;
                    start = transform.rotation;
                    instart = Quaternion.Inverse(transform.rotation);
                }

                public override void Drag()
                {
                    base.Drag();
                    float o = Vector3.Dot(GetPoint(property.Camera.transform.forward, point) - point, tangent);
                    Quaternion offset = Quaternion.AngleAxis(o * -45f, axis);
                    transform.rotation = origin * offset * start;
                    if (DragEvent != null)
                        DragEvent(transform.rotation * instart);
                }
                public override void Down()
                {
                    base.Down();
                    point = GetPoint();
                    tangent = Vector3.Cross((point - transform.position).normalized, transform.right);
                    origin = transform.rotation * instart;
                    if (DownEvent != null)
                        DownEvent(origin);
                }
                public override void Up()
                {
                    base.Up();
                    if (UpEvent != null)
                        UpEvent(transform.rotation * instart);
                }
            }

            private class Scale : Handle
            {

                private Vector3Callback callback;
                private Vector3 origin;
                private ScaleSingle[] s = new ScaleSingle[3];
                private ScaleAll a;
                public static Scale Create(Property p, Vector3 o, Vector3Callback c)
                {
                    Scale s = null;
                    if (p.sdp.Count > 0)
                    {
                        s = p.sdp.Dequeue();
                        s.gameObject.SetActive(true);
                        s.origin = o;
                        for (int i = 0; i < 3; i++)
                            s.s[i].origin = o[i];
                        s.a.origin = 1.0f;
                    }
                    else
                    {
                        s = new GameObject("Scale Handle").AddComponent<Scale>();
                        s.origin = o;
                        s.transform.SetParent(Parent);
                        ScaleSingle x = ScaleSingle.Create(p, s.origin.x, Axis3.X, (v) => { s.origin.x = v; s.callback(s.origin); });
                        x.transform.SetParent(s.transform, false);
                        s.s[0] = x;
                        ScaleSingle y = ScaleSingle.Create(p, s.origin.y, Axis3.Y, (v) => { s.origin.y = v; s.callback(s.origin); });
                        y.transform.SetParent(s.transform, false);
                        s.s[1] = y;
                        ScaleSingle z = ScaleSingle.Create(p, s.origin.z, Axis3.Z, (v) => { s.origin.z = v; s.callback(s.origin); });
                        z.transform.SetParent(s.transform, false);
                        s.s[2] = z;
                        ScaleAll a = ScaleAll.Create(p, 1.0f, (v) => { s.callback(s.origin * v); });
                        a.UpEvent += (v) => { s.origin *= v; x.origin = s.origin.x; y.origin = s.origin.y; z.origin = s.origin.z; };
                        a.transform.SetParent(s.transform, false);
                        s.a = a;
                    }
                    s.callback = c;
                    s.property = p;
                    p.sep.Add(s);
                    return s;
                }
                public override void Close()
                {
                    property.sep.Remove(this);
                    property.sdp.Enqueue(this);
                    gameObject.SetActive(false);
                }
            }
            private class ScaleSingle : _Handle
            {
                private static Resource<GameObject> x = new Resource<GameObject>("handle/scale_x");
                private static Resource<GameObject> y = new Resource<GameObject>("handle/scale_y");
                private static Resource<GameObject> z = new Resource<GameObject>("handle/scale_z");

                public static ScaleSingle Create(Property p, float o, Axis3 a, FloatCallback c)
                {
                    GameObject g = null;
                    switch (a)
                    {
                        case Axis3.X:
                            g = GameObject.Instantiate(x.Object);
                            break;
                        case Axis3.Y:
                            g = GameObject.Instantiate(y.Object);
                            break;
                        case Axis3.Z:
                            g = GameObject.Instantiate(z.Object);
                            break;
                        default:
                            return null;
                    }
                    ScaleSingle m = g.AddComponent<ScaleSingle>();
                    m.property = p;
                    m.origin = o;
                    m.DragEvent = c;
                    return m;
                }

                public FloatCallback DownEvent = null;
                public FloatCallback DragEvent = null;
                public FloatCallback UpEvent = null;
                public float origin;
                private Vector3 point;
                private Vector3 dir;
                private float offset;
                public override void Drag()
                {
                    base.Drag();
                    offset = Vector3.Dot(GetPoint(dir) - point, transform.forward);
                    transform.localScale = Vector3.one + new Vector3(0f, 0f, offset);
                    if (DragEvent != null)
                        DragEvent(origin + offset);
                }
                public override void Down()
                {
                    base.Down();
                    dir = Vector3.Cross(transform.forward, property.Camera.transform.forward);
                    dir = Vector3.Cross(dir, transform.forward);
                    point = GetPoint(dir);
                    if (DownEvent != null)
                        DownEvent(origin);
                }
                public override void Up()
                {
                    base.Up();
                    transform.localScale = Vector3.one;
                    origin += offset;
                    if (UpEvent != null)
                        UpEvent(origin);
                }
            }
            private class ScaleAll : _Handle
            {
                private static Resource<GameObject> a = new Resource<GameObject>("handle/scale_all");
                public static ScaleAll Create(Property p, float o, FloatCallback c)
                {
                    ScaleAll m = GameObject.Instantiate(a.Object).AddComponent<ScaleAll>();
                    m.property = p;
                    m.temp = m.origin = o;
                    m.DragEvent = c;
                    return m;
                }

                public FloatCallback DownEvent = null;
                public FloatCallback DragEvent = null;
                public FloatCallback UpEvent = null;
                public float origin;
                private float temp;
                public override void Drag()
                {
                    base.Drag();
                    float offset = (Input.GetAxis("Mouse X") + Input.GetAxis("Mouse Y")) * 0.1f;
                    transform.localScale += Vector3.one * offset;
                    temp += offset;
                    if (DragEvent != null)
                        DragEvent(temp);
                }
                public override void Down()
                {
                    base.Down();
                    if (DownEvent != null)
                        DownEvent(origin);
                }
                public override void Up()
                {
                    base.Up();
                    if (UpEvent != null)
                        UpEvent(temp);
                    temp = origin;
                    transform.localScale = Vector3.one;
                }
            }
        }
    }
}
#endif