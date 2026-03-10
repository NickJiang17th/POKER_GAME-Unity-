using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }
        
        [Header("Camera Settings")]
        [SerializeField] private Vector3 cameraPosition = new Vector3(0, 0, -35f);
        [SerializeField] private float orthographicSize = 12f;
        [SerializeField] private bool useOrthographic = true;
        [SerializeField] private Color backgroundColor = new Color(0.15f, 0.25f, 0.1f);
        
        [Header("Camera Protection")]
        [SerializeField] private bool lockCameraPosition = true;
        [SerializeField] private bool lockCameraRotation = true;
        [SerializeField] private bool lockCameraSize = true;
        [SerializeField] private float protectionCheckInterval = 0.5f;
        
        private Camera mainCamera;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private float originalOrthographicSize;
        private float protectionTimer = 0f;
        
        public Camera MainCamera => mainCamera;
        public Vector3 CameraPosition => cameraPosition;
        public float OrthographicSize => orthographicSize;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCamera();
        }
        
        private void Start()
        {
            InitializeCamera();
        }
        
        private void Update()
        {
            protectionTimer += Time.deltaTime;
            if (protectionTimer >= protectionCheckInterval)
            {
                ProtectCamera();
                protectionTimer = 0f;
            }
            
            #if UNITY_EDITOR
            HandleDebugControls();
            #endif
        }
        
        #if UNITY_EDITOR
        private void HandleDebugControls()
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll != 0)
                {
                    float newSize = orthographicSize - scroll * 2f;
                    SetOrthographicSize(Mathf.Clamp(newSize, 8f, 25f));
                }
            }
            
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
            {
                LogCameraState();
            }
            
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
            {
                ResetToDefault();
            }
        }
        #endif
        
        private void InitializeCamera()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                cameraObject.tag = "MainCamera";
                Camera.SetupCurrent(mainCamera);
            }
            
            ApplyCameraSettings();
            
            originalPosition = mainCamera.transform.position;
            originalRotation = mainCamera.transform.rotation;
            originalOrthographicSize = mainCamera.orthographicSize;
        }
        
        private void ApplyCameraSettings()
        {
            if (mainCamera == null) return;
            
            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.rotation = Quaternion.identity;
            
            if (useOrthographic)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = orthographicSize;
            }
            else
            {
                mainCamera.orthographic = false;
            }
            
            mainCamera.backgroundColor = backgroundColor;
            
            if (Camera.current != mainCamera)
            {
                Camera.SetupCurrent(mainCamera);
            }
        }
        
        private void ProtectCamera()
        {
            if (mainCamera == null) return;
            
            bool needsReset = false;
            
            if (lockCameraPosition && mainCamera.transform.position != cameraPosition)
            {
                needsReset = true;
            }
            
            if (lockCameraRotation && mainCamera.transform.rotation != Quaternion.identity)
            {
                needsReset = true;
            }
            
            if (lockCameraSize && useOrthographic && 
                Mathf.Abs(mainCamera.orthographicSize - orthographicSize) > 0.01f)
            {
                needsReset = true;
            }
            
            if (useOrthographic && !mainCamera.orthographic)
            {
                needsReset = true;
            }
            
            if (needsReset)
            {
                ApplyCameraSettings();
            }
        }
        
        public void SetCameraPosition(Vector3 newPosition)
        {
            cameraPosition = newPosition;
            ApplyCameraSettings();
        }
        
        public void SetOrthographicSize(float newSize)
        {
            orthographicSize = Mathf.Clamp(newSize, 8f, 25f);
            ApplyCameraSettings();
        }
        
        public void ResetToDefault()
        {
            cameraPosition = new Vector3(0, 0, -35f);
            orthographicSize = 12f;
            ApplyCameraSettings();
        }
        
        public void SetCameraProtection(bool enable)
        {
            lockCameraPosition = enable;
            lockCameraRotation = enable;
            lockCameraSize = enable;
            
            if (enable)
            {
                ApplyCameraSettings();
            }
        }
        
        public void LogCameraState()
        {
            if (mainCamera != null)
            {
                Debug.Log($"摄像机状态 - 位置: {mainCamera.transform.position}, " +
                         $"尺寸: {mainCamera.orthographicSize}, " +
                         $"视野范围: {CalculateViewSize()}");
            }
        }
        
        private Vector2 CalculateViewSize()
        {
            if (mainCamera != null && mainCamera.orthographic)
            {
                float height = mainCamera.orthographicSize * 2f;
                float width = height * mainCamera.aspect;
                return new Vector2(width, height);
            }
            return Vector2.zero;
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}