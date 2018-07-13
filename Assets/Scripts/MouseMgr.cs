#if XREditor|| UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace MeshMaker
{
    /// <summary>
    /// 场景操作接口,需要collider支持
    /// </summary>
    public interface IMouse { }
    /// <summary>
    /// 鼠标按下接口
    /// </summary>
    public interface IMouseDown : IMouse
    {
        void Down();
    }
    /// <summary>
    /// 鼠标双击接口
    /// </summary>
    public interface IMouseDouble : IMouse
    {
        void Double();
    }
    /// <summary>
    /// 鼠标抬起接口
    /// </summary>
    public interface IMouseUp : IMouse
    {
        void Up();
    }
    /// <summary>
    /// 鼠标拖动接口
    /// </summary>
    public interface IMouseDrag : IMouse
    {
        void Drag();
    }
    //public interface IMouseEnter : IMouse
    //{
    //    void Enter();
    //}
    //public interface IMouseExit : IMouse
    //{
    //    void Exit();
    //}
    public class MouseMgr
    {
        /// <summary>
        /// 功能开关
        /// </summary>
        public bool Enable { get; set; }
        public Camera Camera { get { return _camera; } set { _camera = value; } }
        [SerializeField]
        private Camera _camera;
        public LayerMask MouseLayer { get { return layer; } set { layer = value; } }
        [SerializeField]
        private LayerMask layer;
        private IMouse target;
        private float time = 0;

        public MouseMgr(Camera camera, LayerMask layer)
        {
            Enable = true;
            target = null;
            Camera = camera;
            MouseLayer = layer;
        }
        public void Update()
        {
            if (!Enable && target == null)
                return;
            if (Input.GetMouseButtonUp(0))
            {
                if (target != null)
                {
                    OnUp(target);
                    target = null;
                }
            }
            if (Input.GetMouseButton(0))
            {
                if (target != null)
                {
                    OnDrag(target);
                }
            }
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            if (Input.GetMouseButtonDown(0))
            {
                IMouse iMouse = MainRay<IMouse>(Input.mousePosition, MouseLayer);
                if (iMouse != null)
                {
                    target = iMouse;
                    if (Time.unscaledTime - time < 0.5f)
                    {
                        OnDouble(target);
                    }
                    time = Time.unscaledTime;
                    {
                        OnDown(target);
                    }
                }
            }
        }
        private void OnDown(IMouse mouse)
        {
            IMouseDown down = mouse as IMouseDown;
            if (down != null)
                down.Down();
        }
        private void OnUp(IMouse mouse)
        {
            IMouseUp up = mouse as IMouseUp;
            if (up != null)
                up.Up();
        }
        private void OnDrag(IMouse mouse)
        {
            IMouseDrag drag = mouse as IMouseDrag;
            if (drag != null)
                drag.Drag();
        }
        private void OnDouble(IMouse mouse)
        {
            IMouseDouble _double = mouse as IMouseDouble;
            if (_double != null)
                _double.Double();
        }
        private T MainRay<T>(Vector3 pos, int layer)
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.ScreenPointToRay(pos), out hit, 1000, layer))
            {
                return hit.transform.GetComponent<T>();
            }
            return default(T);
        }
    }
}
#endif