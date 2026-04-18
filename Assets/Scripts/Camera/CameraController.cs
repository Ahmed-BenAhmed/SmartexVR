using UnityEngine;

namespace Smartex.CameraControl
{
    /// <summary>
    /// Three-mode camera using Legacy Input (requires activeInputHandler = 2 "Both" or 0 "Legacy").
    /// Orbit  : hold RMB + drag   |  scroll = zoom
    /// Fly    : press F -> WASD + RMB look, Shift = sprint, Q/E = down/up
    /// TopDown: press T -> WASD pan, scroll = ortho zoom
    /// R = reset view    |   1-8 = jump to machine
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public enum CameraMode { Orbit, Fly, TopDown }

        [Header("Targets")]
        public Transform factoryCenter;

        [Header("Orbit")]
        public float orbitDistance    = 60f;
        public float orbitMinDistance = 4f;
        public float orbitMaxDistance = 200f;
        public float orbitSensitivity = 4f;
        public float zoomSensitivity  = 6f;
        public float orbitSmoothing   = 10f;

        [Header("Fly")]
        public float flySpeed     = 12f;
        public float flyShiftMult = 3f;
        public float flyMouseSens = 3f;

        [Header("Top-down")]
        public float topDownHeight    = 45f;
        public float topDownOrthoSize = 26f;

        [Header("Focus")]
        public float focusDistance = 10f;
        public float focusDuration = 0.6f;

        private CameraMode _mode  = CameraMode.Orbit;
        private UnityEngine.Camera _cam;

        private float _yaw = 30f, _pitch = 35f, _dist;
        private float _tYaw, _tPitch, _tDist;
        private float _flyYaw, _flyPitch;

        private bool    _focusing;
        private Vector3 _focusFrom, _focusTo;
        private float   _focusT;

        void Awake()
        {
            _cam   = GetComponent<UnityEngine.Camera>();
            _dist  = orbitDistance;
            _tDist = orbitDistance;
            _tYaw  = _yaw;
            _tPitch = _pitch;
        }

        void Start()
        {
            string fc = factoryCenter != null ? $"{factoryCenter.name}@{factoryCenter.position}" : "NULL";
            Debug.Log($"[CameraController] Start: cam.pos={transform.position} cam.rot={transform.eulerAngles} factoryCenter={fc} orbitDistance={orbitDistance} _dist={_dist} _yaw={_yaw} _pitch={_pitch}");
        }

        void Update()
        {
            HandleHotkeys();

            switch (_mode)
            {
                case CameraMode.Orbit:   DoOrbit();   break;
                case CameraMode.Fly:     DoFly();     break;
                case CameraMode.TopDown: DoTopDown(); break;
            }

            if (_focusing) DoFocusAnim();
        }

        void HandleHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.T)) ToggleTopDown();
            if (Input.GetKeyDown(KeyCode.F)) ToggleFly();
            if (Input.GetKeyDown(KeyCode.R)) ResetCamera();

            if (Input.GetKeyDown(KeyCode.Alpha1)) FocusMachine(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) FocusMachine(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) FocusMachine(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) FocusMachine(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) FocusMachine(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) FocusMachine(5);
            if (Input.GetKeyDown(KeyCode.Alpha7)) FocusMachine(6);
            if (Input.GetKeyDown(KeyCode.Alpha8)) FocusMachine(7);
        }

        void DoOrbit()
        {
            if (Input.GetMouseButton(1))
            {
                float mx = Input.GetAxis("Mouse X");
                float my = Input.GetAxis("Mouse Y");
                _tYaw   += mx * orbitSensitivity * 15f;
                _tPitch -= my * orbitSensitivity * 15f;
                _tPitch  = Mathf.Clamp(_tPitch, 5f, 85f);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
                _tDist = Mathf.Clamp(_tDist - scroll * zoomSensitivity * _tDist,
                                     orbitMinDistance, orbitMaxDistance);

            _yaw   = Mathf.LerpAngle(_yaw,   _tYaw,   Time.deltaTime * orbitSmoothing);
            _pitch = Mathf.LerpAngle(_pitch, _tPitch, Time.deltaTime * orbitSmoothing);
            _dist  = Mathf.Lerp(_dist, _tDist, Time.deltaTime * orbitSmoothing);

            Vector3    pivot = factoryCenter != null ? factoryCenter.position : Vector3.zero;
            Quaternion rot   = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.position = pivot + rot * (Vector3.back * _dist);
            transform.LookAt(pivot + Vector3.up);
        }

        void DoFly()
        {
            if (Input.GetMouseButton(1))
            {
                _flyYaw   += Input.GetAxis("Mouse X") * flyMouseSens * 15f;
                _flyPitch -= Input.GetAxis("Mouse Y") * flyMouseSens * 15f;
                _flyPitch  = Mathf.Clamp(_flyPitch, -80f, 80f);
            }
            transform.rotation = Quaternion.Euler(_flyPitch, _flyYaw, 0f);

            float speed = flySpeed * (Input.GetKey(KeyCode.LeftShift) ? flyShiftMult : 1f);
            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    move += transform.forward;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  move -= transform.forward;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  move -= transform.right;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += transform.right;
            if (Input.GetKey(KeyCode.E)) move += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;
            transform.position += move * speed * Time.deltaTime;
            // Soft bounds so you can't fly into the void by accident
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, -100f, 100f);
            p.y = Mathf.Clamp(p.y, 0.5f,  60f);
            p.z = Mathf.Clamp(p.z, -100f, 100f);
            transform.position = p;
        }

        void DoTopDown()
        {
            float spd = flySpeed * 0.5f;
            Vector3 pan = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    pan += Vector3.forward;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  pan -= Vector3.forward;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  pan -= Vector3.right;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) pan += Vector3.right;
            transform.position += pan * spd * Time.deltaTime;

            if (_cam != null && _cam.orthographic)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                _cam.orthographicSize = Mathf.Clamp(
                    _cam.orthographicSize - scroll * 10f, 5f, 50f);
            }
        }

        void DoFocusAnim()
        {
            _focusT += Time.deltaTime / focusDuration;
            transform.position = Vector3.Lerp(_focusFrom, _focusTo, EaseInOut(_focusT));
            if (_focusT >= 1f) _focusing = false;
        }

        static float EaseInOut(float t) =>
            t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

        public void ToggleTopDown()
        {
            if (_mode != CameraMode.TopDown)
            {
                _mode = CameraMode.TopDown;
                transform.position = new Vector3(0f, topDownHeight, 0f);
                transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                if (_cam != null) { _cam.orthographic = true; _cam.orthographicSize = topDownOrthoSize; }
            }
            else ResetCamera();
        }

        public void ToggleFly()
        {
            _mode = (_mode == CameraMode.Fly) ? CameraMode.Orbit : CameraMode.Fly;
            if (_cam != null) _cam.orthographic = false;
            if (_mode == CameraMode.Fly) { _flyYaw = transform.eulerAngles.y; _flyPitch = transform.eulerAngles.x; }
        }

        public void ResetCamera()
        {
            _mode = CameraMode.Orbit;
            _tYaw = 30f; _tPitch = 35f; _tDist = orbitDistance;
            if (_cam != null) _cam.orthographic = false;
        }

        public void FocusMachine(int index)
        {
            string suffix = $"{index + 1:D3}";
            foreach (var go in GameObject.FindGameObjectsWithTag("Generated"))
            {
                var mc = go.GetComponent<Smartex.Machines.MachineController>();
                if (mc != null && mc.deviceId.EndsWith(suffix))
                {
                    _mode = CameraMode.Orbit; _focusing = true; _focusT = 0f;
                    _focusFrom = transform.position; _tDist = focusDistance;
                    if (factoryCenter != null) factoryCenter.position = go.transform.position;
                    _focusTo = go.transform.position
                               + Quaternion.Euler(_pitch, _yaw, 0f) * (Vector3.back * focusDistance);
                    break;
                }
            }
        }
    }
}
