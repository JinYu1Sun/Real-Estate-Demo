using UnityEngine;

namespace RealEstateDemo.Unity
{
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        private Camera cam; private Vector3 target = new Vector3(0, 0, 2); private float yaw = 220f;
        private float pitch = 50f; private float distance = 125f;

        private void Awake()
        {
            cam = GetComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 36;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.28f, 0.68f, 0.84f);
            cam.nearClipPlane = 0.3f; cam.farClipPlane = 800; Apply();
        }
        private void Update()
        {
            float pan = 35f * Time.deltaTime;
            var right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            var forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            if (Input.GetKey(KeyCode.W)) target += forward * pan;
            if (Input.GetKey(KeyCode.S)) target -= forward * pan;
            if (Input.GetKey(KeyCode.A)) target -= right * pan;
            if (Input.GetKey(KeyCode.D)) target += right * pan;
            if (Input.GetKey(KeyCode.Q)) yaw -= 55f * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) yaw += 55f * Time.deltaTime;
            if (Input.GetMouseButton(2))
            { target -= right * Input.GetAxis("Mouse X") * 2f; target -= forward * Input.GetAxis("Mouse Y") * 2f; }
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - Input.mouseScrollDelta.y * 6f, 18, 110);
            target.x = Mathf.Clamp(target.x, -105, 105); target.z = Mathf.Clamp(target.z, -105, 105);
            Apply();
        }
        public void Focus(Vector3 position) { target = position; cam.orthographicSize = 26; Apply(); }
        private void Apply()
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            transform.position = target + rotation * Vector3.back * distance;
            transform.rotation = rotation;
        }
    }
}
