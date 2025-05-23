using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TilesEditor
{
    [RequireComponent(typeof(Canvas))]
    public class UiManager : MonoBehaviour
    {
        [SerializeField] private GameObject buttonPrefab;
        [SerializeField] private float buttonSpacing = 20f;
        [SerializeField] private float scrollSpeed = 90f;
        [SerializeField] private float panelMargin = 30f;
        [SerializeField] private float borderPadding = 10f;
        [SerializeField] private float dragSensitivity = 1f;

        private Canvas canvas;
        private RectTransform panelRect;
        private RectTransform backgroundRect;
        private List<GameObject> tileButtons = new List<GameObject>();
        private float containerHeight;
        private float contentHeight;
        private float currentScrollPosition = 0f;
        private bool isInitialized = false;
        private float buttonSize;
        private float panelWidth;
        private float panelHeight;
        private GameObject border;
        private bool isDragging = false;
        private Vector2 lastMousePosition;
        private Image backgroundImage;

        private void CalculateSizes()
        {
            // 計算面板寬度（螢幕寬度的六分之一）
            panelWidth = Screen.width / 6f;
            
            // 計算按鈕大小（考慮兩排按鈕和間距）
            float availableWidth = panelWidth - (panelMargin * 2) - buttonSpacing;
            buttonSize = (availableWidth / 2f) * 0.9f; // 按鈕大小縮小到90%
            
            // 計算面板高度（基於按鈕大小）
            panelHeight = Screen.height * 0.8f; // 使用螢幕高度的80%

            // 計算內容總高度（考慮兩排按鈕）
            int rows = Mathf.CeilToInt(GameMain.Main.tilePrefabList.count / 2f);
            contentHeight = (buttonSize + buttonSpacing) * rows + buttonSpacing * 2; // 加上上下邊距
        }

        private void CreateButtonPrefab()
        {
            // 創建按鈕物件
            GameObject button = new GameObject("TileButton");

            // 添加必要的組件
            RectTransform rectTransform = button.AddComponent<RectTransform>();
            Image image = button.AddComponent<Image>();
            Button buttonComponent = button.AddComponent<Button>();

            // 設置按鈕顏色
            image.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            // 設置按鈕大小
            rectTransform.sizeDelta = new Vector2(buttonSize, buttonSize);

            // 創建預製體
#if UNITY_EDITOR
            if (!System.IO.Directory.Exists("Assets/Prefabs"))
            {
                System.IO.Directory.CreateDirectory("Assets/Prefabs");
            }
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(button, "Assets/Prefabs/TileButton.prefab");
            DestroyImmediate(button);
            buttonPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TileButton.prefab");
#endif
        }

        public void Initialize()
        {
            if (isInitialized) return;

            if (GameMain.Main == null)
            {
                Debug.LogError("GameMain.Main is null");
                return;
            }

            if (GameMain.Main.tilePrefabList == null)
            {
                Debug.LogError("GameMain.Main.tilePrefabList is null");
                return;
            }

            // 計算所有尺寸
            CalculateSizes();

            if (buttonPrefab == null)
            {
                Debug.Log("Button Prefab is not assigned, creating one...");
                CreateButtonPrefab();
                if (buttonPrefab == null)
                {
                    Debug.LogError("Failed to create Button Prefab");
                    return;
                }
            }

            Debug.Log($"Initialize {GameMain.Main.tilePrefabList.count}, {GameMain.Main.tilePrefabList.tilePrefabs.Length}");

            // 初始化 Canvas
            canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // 創建面板
            GameObject panel = new GameObject("TilePanel");
            panel.transform.SetParent(transform, false);
            panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
            panelRect.anchoredPosition = new Vector2(panelMargin, -panelMargin);

            // 添加外框
            border = new GameObject("Border");
            border.transform.SetParent(panelRect, false);
            RectTransform borderRect = border.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0, 0);
            borderRect.anchorMax = new Vector2(1, 1);
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            Image borderImage = border.AddComponent<Image>();
            borderImage.color = new Color(1f, 1f, 1f, 1f);
            borderImage.type = Image.Type.Sliced;
            borderImage.raycastTarget = false;

            // 確保外框在最上層
            border.transform.SetAsFirstSibling();

            // 創建背景
            GameObject background = new GameObject("Background");
            background.transform.SetParent(border.transform, false);
            backgroundRect = background.AddComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0, 0);
            backgroundRect.anchorMax = new Vector2(1, 1);
            backgroundRect.offsetMin = new Vector2(borderPadding, borderPadding);
            backgroundRect.offsetMax = new Vector2(-borderPadding, -borderPadding);

            // 添加面板背景
            Image panelImage = background.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.2f, 0.8f, 0.8f);
            backgroundImage = panelImage;

            // 計算容器高度
            containerHeight = panelHeight - (panelMargin * 2) - (borderPadding * 2);

            // 創建按鈕
            for (int i = 0; i < GameMain.Main.tilePrefabList.count; i++)
            {
                GameObject button = Instantiate(buttonPrefab, backgroundRect);
                RectTransform rectTransform = button.GetComponent<RectTransform>();
                
                // 計算按鈕位置（兩排排列）
                int row = i / 2;
                int col = i % 2;
                float xPos = col * (buttonSize + buttonSpacing) + buttonSpacing;
                float yPos = -row * (buttonSize + buttonSpacing) - buttonSpacing;
                
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.pivot = new Vector2(0, 1);
                rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                rectTransform.sizeDelta = new Vector2(buttonSize, buttonSize);

                // 設置按鈕圖片
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    // 這裡可以設置按鈕的圖片，例如使用磁磚的預覽圖
                    // buttonImage.sprite = ...
                }

                // 添加點擊事件
                int index = i; // 捕獲當前索引
                button.GetComponent<Button>().onClick.AddListener(() => OnTileButtonClicked(index));

                tileButtons.Add(button);
            }

            isInitialized = true;
        }

        void Update()
        {
            if (!isInitialized || tileButtons.Count == 0) return;

            Vector2 mousePosition = Input.mousePosition;
            bool isMouseOverBackground = RectTransformUtility.RectangleContainsScreenPoint(backgroundRect, mousePosition, canvas.worldCamera);

            // 處理滑鼠拖曳
            if (Input.GetMouseButtonDown(0))
            {
                // 檢查是否點擊到背景
                if (isMouseOverBackground)
                {
                    isDragging = true;
                    lastMousePosition = mousePosition;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            if (isDragging)
            {
                Vector2 currentMousePosition = Input.mousePosition;
                float deltaY = (currentMousePosition.y - lastMousePosition.y) * dragSensitivity;
                currentScrollPosition += deltaY;
                currentScrollPosition = Mathf.Clamp(currentScrollPosition, 0, Mathf.Max(0, contentHeight - containerHeight));
                UpdateButtonPositions();
                lastMousePosition = currentMousePosition;
            }

            // 處理滾輪
            float scrollInput = -Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0 && isMouseOverBackground)
            {
                currentScrollPosition += scrollInput * scrollSpeed;
                currentScrollPosition = Mathf.Clamp(currentScrollPosition, 0, Mathf.Max(0, contentHeight - containerHeight));
                UpdateButtonPositions();
            }
        }

        private void UpdateButtonPositions()
        {
            // 更新外框位置（只更新Y軸）
            RectTransform borderRect = border.GetComponent<RectTransform>();
            borderRect.anchoredPosition = new Vector2(borderRect.anchoredPosition.x, currentScrollPosition);

            // 更新按鈕位置
            for (int i = 0; i < tileButtons.Count; i++)
            {
                RectTransform rectTransform = tileButtons[i].GetComponent<RectTransform>();
                int row = i / 2;
                int col = i % 2;
                float xPos = col * (buttonSize + buttonSpacing) + buttonSpacing;
                float yPos = -row * (buttonSize + buttonSpacing) - buttonSpacing;
                rectTransform.anchoredPosition = new Vector2(xPos, yPos);
            }
        }

        private void OnTileButtonClicked(int index)
        {
            // 處理按鈕點擊事件
            Debug.Log($"Tile button {index} clicked");
        }
    }
}
