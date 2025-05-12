// =============================================
// 檔案名稱：PlaneGizmo.cs
// 1. 用於在特定平面（如 XY、XZ、YZ）上進行拖曳操作的 Gizmo。
// 2. 讓使用者可以在 2D 平面上移動物件。
// 3. 支援設定完全透明模式，可用於特殊視覺效果。
// 4. 使用四邊形面積計算實現精確的滑鼠互動。
// 5. 本元件為 TransformGizmo 的子物件，請勿手動移除或更改父子結構。
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

        public void SetInvisible(bool value)
        {
            var ren = GetComponent<MeshRenderer>();
            if (value)
                ren.enabled = false;
            else
                ren.enabled = true;
        }

        public override void SetMaterialColor(Color color)
        {
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            Color finalColor = new Color(color.r, color.g, color.b, 0.8f);

            propertyBlock.SetColor("_Color", finalColor);

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
                    block.SetColor("_Color", finalColor);
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
        public override bool ShouldBeActive()
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
