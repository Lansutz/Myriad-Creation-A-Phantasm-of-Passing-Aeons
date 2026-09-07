using UnityEngine;

namespace CivilizationEvolution.Bootstrap
{
    public class EditorCameraController : MonoBehaviour
    {
        public Camera targetCamera;
        public float panSpeed = 50f;
        public float zoomSpeed = 10f;
        public float minZoom = 10f;
        public float maxZoom = 200f;

        private Vector3 _lastMousePos;
        private bool _isPanning;

        void Update()
        {
            if (targetCamera == null) return;

            // 右键拖拽平移
            if (Input.GetMouseButtonDown(1))
            {
                _isPanning = true;
                _lastMousePos = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(1))
            {
                _isPanning = false;
            }
            if (_isPanning)
            {
                Vector3 delta = Input.mousePosition - _lastMousePos;
                Vector3 pan = new Vector3(-delta.x, -delta.y, 0f) * panSpeed * 0.01f;
                targetCamera.transform.Translate(pan, Space.Self);
                _lastMousePos = Input.mousePosition;
            }

            // 滚轮缩放
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                targetCamera.orthographicSize = Mathf.Clamp(
                    targetCamera.orthographicSize - scroll * zoomSpeed,
                    minZoom, maxZoom);
            }

            // WASD平移
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                Vector3 move = new Vector3(h, v, 0f) * panSpeed * Time.deltaTime;
                targetCamera.transform.Translate(move, Space.Self);
            }
        }
    }
}
