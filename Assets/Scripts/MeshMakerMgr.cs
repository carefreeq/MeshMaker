using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MeshMaker
{

    public class MeshMakerMgr : MonoBehaviour
    {
        public static MeshMakerMgr Instance { get; private set; }
        public static bool Enable { get; set; }
        public static Camera Camera { get { return Instance._camera; } set { Instance._camera = value; } }
        [SerializeField]
        private Camera _camera;
        public static CameraControl Control { get; private set; }
        public static Handle.Property Handle { get; private set; }
        public static SelectMgr Select { get; private set; }
        //public static MouseMgr Mouse { get; private set; }
        public static PolyType EditorType { get; set; }
        private static MouseMgr handlecast;
        public static MeshObject Target { get; private set; }
        public static event Action<MeshObject> TargetEvent;
        [SerializeField]
        private RectTransform editorUI;
        static MeshMakerMgr()
        {
            Enable = true;
            EditorType = PolyType.Point;
        }
        private void Awake()
        {
            Instance = this;
            Control = new CameraControl(Camera);
            Handle = new Handle.Property(Camera);
            Select = new SelectMgr(Camera);
            handlecast = new MouseMgr(Camera, 1 << Handle.Layer);

            MeshMakerMgr.TargetEvent += (t) => editorUI.gameObject.SetActive(t);
            MeshMakerMgr.TargetEvent += (t) => Select.Enable = !t;
            MeshObject.AddEvent += Select.SelectObjects._Add;
            MeshObject.RemoveEvent += (o) => Select.SelectObjects._Remove(o);
            Select.HandlingEvent += (h) => Control.Enable = handlecast.Enable = !h;
            Control.HandlingEvent += (h) => { if (Target == null) Select.Enable = !h; handlecast.Enable = MeshMakerMgr.Enable = !h; };
            Handle.HandlingEvent += (h) => { if (Target == null) Select.Enable = !h; Control.Enable = MeshMakerMgr.Enable = !h; };
            Select.SelectedEvent += (o) => SwitchTarget(o.Count > 0 ? o[0] as MeshObject : null);
            Select.SelectedEvent += (o) => {if (o.Count > 0)LookAt(o[0].transform.position); };
        }
        private void LateUpdate()
        {
            handlecast.Update();
            Select.LateUpdate();
            Control.LateUpdate();
            if (Input.GetKey(KeyCode.Minus))
            {
                Handle.Size -= 0.1f * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.Equals))
            {
                Handle.Size += 0.1f * Time.deltaTime;
            }
        }
        private void OnRenderObject()
        {
            Select.OnRenderObject();
        }
        public void ExitTarget()
        {
            SwitchTarget(null);
        }
        public void SwitchTarget(MeshObject t)
        {
            if (Target != t)
            {
                if (Target)
                {
                    Target.Enable = false;
                    Target = null;
                }
                Target = t;
                if (Target)
                {
                    Target.Enable = true;
                    Target.EditorType = EditorType;
                }
                if (TargetEvent != null)
                    TargetEvent(Target);
            }
        }
        public void SwitchEditorType(int i)
        {
            EditorType = (PolyType)i;
            if (Target != null)
                Target.EditorType = EditorType;
        }
        private IEnumerator lookAtCor;
        public void LookAt(Vector3 p)
        {
            if (lookAtCor != null)
                StopCoroutine(lookAtCor);
            lookAtCor = Control.LookAt(p);
            StartCoroutine(lookAtCor);
        }
    }
}