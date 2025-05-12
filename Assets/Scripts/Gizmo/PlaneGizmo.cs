// =============================================
// 檔案名稱：PlaneGizmo.cs
// 1. 用於在特定平面（如 XY、XZ、YZ）上進行拖曳操作的 Gizmo。
// 2. 讓使用者可以在 2D 平面上移動物件。
// 3. 本元件支援安全重複初始化，Initialize 可多次呼叫以覆蓋狀態，不會產生重複資源。
// 4. 所有 Gizmo 材質由 ScriptableObject（GizmoMaterials）統一管理，於建立時指定，顏色與透明度（80%）以 MaterialPropertyBlock 設定，避免記憶體浪費與提升一致性。
// 5. 本元件會自動產生正反兩面，確保雙面皆可顯示顏色與高光，反面僅作顯示不參與互動。
// 6. 所有互動偵測皆以數學計算為主，不依賴 Collider，確保精確度與效能。
// 7. 本元件為 TransformGizmo 的子物件，請勿手動移除或更改父子結構。
// =============================================
using UnityEngine;

namespace TilesEditor
{
    public class PlaneGizmo : GizmoBase
    {
        public enum PlaneType { XY, XZ, YZ }
        public PlaneType planeType;

        Camera cam;
        float size;

        public void Initialize(PlaneType type, Color color, TransformGizmo gizmo, float size)
        {
            planeType = type;
            baseColor = color;
            SetMaterialColor(color);
            this.gizmo = gizmo;
            this.cam = gizmo.cam;
            this.size = size;
        }

        public override void SetMaterialColor(Color color)
        {
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
            color.a = 0.8f; // 預設 80% 透明度
            propertyBlock.SetColor("_Color", color);
            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.SetPropertyBlock(propertyBlock);
            }
            // 設定反面
            var back = transform.Find(name + "_Back");
            if (back != null)
            {
                var backRenderer = back.GetComponent<MeshRenderer>();
                if (backRenderer != null)
                {
                    var block = new MaterialPropertyBlock();
                    block.SetColor("_Color", color);
                    backRenderer.SetPropertyBlock(block);
                }
            }
        }

        public Plane GetDragPlane(Transform gizmoRoot, Vector3 origin)
        {
            switch (planeType)
            {
                case PlaneType.XY: return new Plane(gizmoRoot.forward, origin);
                case PlaneType.XZ: return new Plane(gizmoRoot.up, origin);
                case PlaneType.YZ: return new Plane(gizmoRoot.right, origin);
                default: return new Plane(Vector3.up, origin);
            }
        }

        public bool IsMouseOnGizmo()
        {
            Vector3 c = transform.position;
            Vector3 r = transform.right * size * 0.5f;
            Vector3 u = transform.up * size * 0.5f;
            Vector3[] worldCorners = new Vector3[] {
                c - r - u,
                c + r - u,
                c + r + u,
                c - r + u
            };
            Vector2[] screenCorners = new Vector2[4];
            for (int i = 0; i < 4; i++)
                screenCorners[i] = cam.WorldToScreenPoint(worldCorners[i]);
            Vector2 mouse = Input.mousePosition;
            float QuadArea(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
            {
                float TriArea(Vector2 p1, Vector2 p2, Vector2 p3) => Mathf.Abs((p1.x * (p2.y - p3.y) + p2.x * (p3.y - p1.y) + p3.x * (p1.y - p2.y)) / 2f);
                return TriArea(a, b, c) + TriArea(a, c, d);
            }
            float quadArea = QuadArea(screenCorners[0], screenCorners[1], screenCorners[2], screenCorners[3]);
            float sumArea = 0f;
            for (int i = 0; i < 4; i++)
                sumArea += Mathf.Abs((screenCorners[i].x * (screenCorners[(i + 1) % 4].y - mouse.y) + screenCorners[(i + 1) % 4].x * (mouse.y - screenCorners[i].y) + mouse.x * (screenCorners[i].y - screenCorners[(i + 1) % 4].y)) / 2f);
            return Mathf.Abs(sumArea - quadArea) < 1.5f;
        }

        // 判斷此 handle 是否該顯示
        public override bool ShouldBeVisible()
        {
            if (gizmo == null) return false;
            switch (planeType)
            {
                case PlaneType.XY: return gizmo.translateX && gizmo.translateY;
                case PlaneType.XZ: return gizmo.translateX && gizmo.translateZ;
                case PlaneType.YZ: return gizmo.translateY && gizmo.translateZ;
                default: return false;
            }
        }
    }
}
