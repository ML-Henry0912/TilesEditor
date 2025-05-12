using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TilesEditor
{
    public class GameMain : MonoBehaviour
    {
        public static GameMain Main;

        public Camera cam;
        public Transform target;
        public GizmoMaterials materials;
        public TilePrefabList tilePrefabList;
        public GameObject gizmoRoot;

        public UiManager uiManager;

        public bool enableTileSelect = true;

        public TileBehavior[] tiles;

        TransformGizmo gizmo;

        private void Start()
        {
            uiManager?.Initialize();

            Main = this;
            // 將所有 tiles 設定隨機位置，z軸為0
            foreach (var tile in tiles)
            {
                if (tile != null)
                {
                    float x = Random.Range(-5f, 5f);
                    float y = Random.Range(-5f, 5f);
                    tile.transform.position = new Vector3(x, y, 0f);

                    tile.Initialize();
                }
            }
        }

        void Update()
        {
            if (!enableTileSelect) return;
            HandleTileSelect();
        }

        private void HandleTileSelect()
        {
            // 滑鼠左鍵點擊偵測
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider != null && hit.collider.CompareTag("Tile"))
                    {
                        target = hit.collider.transform;
                        // 初始化 gizmo 讓使用者編輯該物件
                        if (gizmo == null)
                        {
                            gizmo = gizmoRoot.AddComponent<TransformGizmo>();
                        }
                        gizmo.Initialize(target, cam, materials);
                        // 關閉 rotation X, Y 與 translate Z
                        gizmo.rotateX = false;
                        gizmo.rotateY = false;
                        gizmo.translateZ = false;
                    }
                }
            }
        }
    }
}