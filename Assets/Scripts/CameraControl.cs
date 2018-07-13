using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;

namespace MeshMaker
{
    /// <summary>
    /// 摄像机控制类
    /// </summary>
    public class CameraControl
    {
        public event Action<bool> HandlingEvent;
        public bool Enable { get; set; }
        public Camera Camera { get { return _camera; } set { _camera = value; } }
        [SerializeField]
        private Camera _camera;
        public float moveRadius = 1000f;
        public float moveSpeed = 4f;
        public float rotaSpeed = 4f;
        public float offsetSpeed = 4.0f;
        private float scrollSpeed = 1f;
        private float dis = 5.0f;
        private float x, y;
        private Vector3 target;

        public CameraControl(Camera cam)
        {
            Camera = cam;
            Enable = true;
        }
        public void Start()
        {
            x = Camera.transform.eulerAngles.x;
            y = Camera.transform.eulerAngles.y;
            target = Camera.transform.forward * dis + Camera.transform.position;
        }

        public void LateUpdate()
        {
            if (!Enable)
                return;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            Vector3 from = Camera.transform.position;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            float scroll = Input.GetAxis("Mouse ScrollWheel") * scrollSpeed * 10;
            dis -= scroll;
            Camera.transform.Translate(Vector3.forward * scroll);
            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                target = Camera.transform.forward * dis + Camera.transform.position;
            }

            if (Input.GetMouseButton(1))
            {
                y += Input.GetAxis("Mouse X") * rotaSpeed;
                x -= Input.GetAxis("Mouse Y") * rotaSpeed;
                y = Mathf.Repeat(y, 360f);
                x = Mathf.Repeat(x, 360f);
                Camera.transform.rotation = Quaternion.Euler(x, y, 0.0f);
                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    Camera.transform.position = Camera.transform.rotation * new Vector3(0, 0, -dis) + target;
                }
                if (Input.GetKey(KeyCode.W))
                {
                    Camera.transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
                }
                if (Input.GetKey(KeyCode.A))
                {
                    Camera.transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
                }
                if (Input.GetKey(KeyCode.S))
                {
                    Camera.transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);
                }
                if (Input.GetKey(KeyCode.D))
                {
                    Camera.transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
                }
            }
            if (Input.GetMouseButton(2))
            {
                Camera.transform.position -= Input.GetAxis("Mouse X") * offsetSpeed * Camera.transform.right;
                Camera.transform.position -= Input.GetAxis("Mouse Y") * offsetSpeed * Camera.transform.up;
            }
            if (Camera.transform.position.magnitude > moveRadius)
                Camera.transform.position = from;
#else
            if (Input.touchCount > 0)
            {
                if (Input.GetTouch(0).phase == TouchPhase.Moved)
                {

                }
            }
#endif
        }
        public IEnumerator LookAt(Vector3 pos)
        {
            Vector3 p = pos - Camera.transform.position;
            dis = p.magnitude;
            Quaternion f = Camera.transform.rotation;
            Quaternion t = Quaternion.LookRotation(p.normalized, Vector3.up);
            float _t = 0f;
            while (_t < 1.0f)
            {
                _t += Time.deltaTime * 2f;
                Camera.transform.rotation = Quaternion.Lerp(f, t, _t);
                yield return new WaitForEndOfFrame();
            }
            Camera.transform.rotation = t;
            x = Camera.transform.eulerAngles.x;
            y = Camera.transform.eulerAngles.y;
        }
    }
}