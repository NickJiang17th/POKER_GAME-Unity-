using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public class SceneSetup : MonoBehaviour
    {
        [SerializeField] private bool initializeCameraOnStart = true;
        
        private void Start()
        {
            SetupScene();
        }
        
        private void SetupScene()
        {
            SetupCamera();
            SetupSortingLayers();
            CreateManagers();
        }
        
        private void SetupCamera()
        {
            CameraController cameraController = FindObjectOfType<CameraController>();
            if (cameraController == null)
            {
                GameObject controllerObject = new GameObject("CameraController");
                cameraController = controllerObject.AddComponent<CameraController>();
            }
            
            if (initializeCameraOnStart)
            {
                cameraController.LogCameraState();
            }
        }
        
        private void SetupSortingLayers()
        {
            // 排序图层会在编辑器中设置
        }
        
        private void CreateManagers()
        {
            if (CardSpriteManager.Instance == null)
            {
                GameObject assetManagerObject = new GameObject("CardSpriteManager");
                assetManagerObject.AddComponent<CardSpriteManager>();
            }
            
            SpriteCardGameManager gameManager = FindObjectOfType<SpriteCardGameManager>();
            if (gameManager == null)
            {
                GameObject gameManagerObject = new GameObject("SpriteCardGameManager");
                gameManagerObject.AddComponent<SpriteCardGameManager>();
            }
        }
        
        [ContextMenu("检查摄像机状态")]
        public void CheckCameraStatus()
        {
            if (CameraController.Instance != null)
            {
                CameraController.Instance.LogCameraState();
            }
        }
        
        [ContextMenu("重置摄像机")]
        public void ResetCamera()
        {
            if (CameraController.Instance != null)
            {
                CameraController.Instance.ResetToDefault();
            }
        }
    }
}