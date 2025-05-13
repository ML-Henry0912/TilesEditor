// =============================================
// 檔案名稱：PlaneGizmo.cs
// 1. 用於在特定平面（如 XY、XZ、YZ）上進行拖曳操作的 Gizmo。
// 2. 讓使用者可以在 2D 平面上移動物件。
// 3. 支援設定完全透明模式，可用於特殊視覺效果。
// 4. 使用 BoxCollider 進行滑鼠事件偵測，提供直覺的互動體驗。
// 5. 本元件為 TransformGizmo 的子物件，請勿手動移除或更改父子結構。
// =============================================
using UnityEngine;

namespace TilesEditor
{
    public class PlaneGizmo : MonoBehaviour, iGizmo
    {
        public enum PlaneType { XY, XZ, YZ }
        public PlaneType planeType;

        private bool isHovered = false;
        protected TransformGizmo gizmo;
        public Color baseColor;
        protected MaterialPropertyBlock propertyBlock;

        public void Initialize(PlaneType type, Color color, TransformGizmo gizmo)
        {
            planeType = type;
            baseColor = color;
            SetMaterialColor(color);
            this.gizmo = gizmo;
        }

        public void SetInvisible(bool value)
        {
            var ren = GetComponent<MeshRenderer>();
            if (value)
                ren.enabled = false;
            else
                ren.enabled = true;
        }

        public void SetMaterialColor(Color color)
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
        }

        public void ResetColor()
        {
            SetMaterialColor(baseColor);
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

        private void OnMouseEnter()
        {
            isHovered = true;
            SetMaterialColor(Color.yellow);
        }

        private void OnMouseExit()
        {
            isHovered = false;
            SetMaterialColor(baseColor);
        }

        public bool IsHovered()
        {
            return isHovered;
        }

        // 判斷此 handle 是否該顯示
        public bool ShouldBeActive()
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

        public void OnDrag()
        {
            if (gizmo == null || gizmo.target == null || gizmo.cam == null) return;

            Ray ray = gizmo.cam.ScreenPointToRay(Input.mousePosition);
            Plane dragPlane = GetDragPlane(gizmo.transform, gizmo.target.position);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 currentPoint = ray.GetPoint(enter);
                Vector3 delta = currentPoint - gizmo.dragStartPos;
                if (delta.magnitude < 100f)
                    gizmo.target.position = gizmo.objectStartPos + delta;
            }
        }
    }
}
