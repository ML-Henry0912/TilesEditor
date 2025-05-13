// =============================================
// 檔案名稱：RotateGizmo.cs
// 1. 用於旋轉操作的 Gizmo，顯示三個圓環。
// 2. 讓使用者可以分別對 X、Y、Z 軸進行精確旋轉。
// 3. 使用橢圓投影計算實現精確的滑鼠互動，支援任意視角操作。
// 4. 本元件為 TransformGizmo 的子物件，請勿手動移除或更改父子結構。
// =============================================
using UnityEngine;
using static TilesEditor.iGizmo;

namespace TilesEditor
{
    public class RotateGizmo : MonoBehaviour, iGizmo
    {
        const float RING_RADIUS = 1.2f;

        public GizmoType type;

        [HideInInspector]
        public Vector3 WorldAxis;

        Camera cam;
        protected TransformGizmo gizmo;
        public Color baseColor;
        protected MaterialPropertyBlock propertyBlock;

        private bool isHovered = false;

        public void Initialize(GizmoType type, Color color, TransformGizmo gizmo)
        {
            this.type = type;
            baseColor = color;
            SetMaterialColor(color);

            switch (type)
            {
                case GizmoType.ROT_X: WorldAxis = Vector3.right; break;
                case GizmoType.ROT_Y: WorldAxis = Vector3.up; break;
                case GizmoType.ROT_Z: WorldAxis = Vector3.forward; break;
            }
            this.gizmo = gizmo;
            this.cam = gizmo.cam;
            gameObject.SetActive(ShouldBeActive());
        }

        public void SetMaterialColor(Color color)
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
        }

        public void ResetColor()
        {
            SetMaterialColor(baseColor);
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
            switch (type)
            {
                case GizmoType.ROT_X: return gizmo.rotateX;
                case GizmoType.ROT_Y: return gizmo.rotateY;
                case GizmoType.ROT_Z: return gizmo.rotateZ;
                default: return false;
            }
        }

        public void SetInvisible(bool value)
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = !value;
            }
        }

        public void OnDrag()
        {
            if (gizmo == null || gizmo.target == null || cam == null) return;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (gizmo.rotationPlane.Raycast(ray, out float enter))
            {
                Vector3 currentPoint = ray.GetPoint(enter);
                Vector3 startDir = (gizmo.rotateStartPoint - gizmo.target.position).normalized;
                Vector3 currentDir = (currentPoint - gizmo.target.position).normalized;

                Quaternion deltaRotation = Quaternion.FromToRotation(startDir, currentDir);
                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

                if (Vector3.Dot(axis, WorldAxis) < 0f)
                    angle = -angle;

                gizmo.target.rotation = gizmo.objectStartRot * Quaternion.AngleAxis(angle, WorldAxis);
            }
        }
    }


    /// <summary>
    /// 提供將任意 3D 空間中圓形投影至螢幕並計算橢圓焦點與主次軸資訊的工具。
    /// </summary>
    public static class EllipseProjectionUtility
    {
        public struct EllipseFociOnScreen
        {
            public Vector2 screenCenter;                 // 圓心的螢幕座標
            public Vector2 majorAxisDirection;           // 橢圓主軸方向（螢幕空間）
            public float majorAxisLength;                // 主軸半長度
            public float minorAxisLength;                // 次軸半長度
        }

        /// <summary>
        /// 計算一個在任意方向的 3D 圓形，在螢幕上的投影橢圓的幾何焦點與軸資訊。
        /// </summary>
        /// <param name="cam">攝影機</param>
        /// <param name="circleCenter">圓心位置（世界空間）</param>
        /// <param name="circleNormal">圓面法向量（世界空間）</param>
        /// <param name="radius">圓半徑</param>
        /// <returns>橢圓主軸資訊</returns>
        public static EllipseFociOnScreen ProjectCircleToScreen(Camera cam, Vector3 circleCenter, Vector3 circleNormal, float radius)
        {
            EllipseFociOnScreen result = default;

            if (cam == null)
            {
                Debug.LogWarning("Camera is null.");
                return result;
            }

            Vector3 normal = circleNormal.normalized;

            // 建立圓面上的兩個正交向量（軸）
            Vector3 axis1 = Vector3.Cross(normal, Vector3.up).normalized;
            if (axis1 == Vector3.zero)
                axis1 = Vector3.Cross(normal, Vector3.right).normalized;

            Vector3 axis2 = Vector3.Cross(normal, axis1).normalized;

            // 在圓上取四個方向端點
            Vector3 worldA = circleCenter + axis1 * radius;
            Vector3 worldB = circleCenter - axis1 * radius;
            Vector3 worldC = circleCenter + axis2 * radius;
            Vector3 worldD = circleCenter - axis2 * radius;

            // 投影至螢幕空間
            Vector3 screenA = cam.WorldToScreenPoint(worldA);
            Vector3 screenB = cam.WorldToScreenPoint(worldB);
            Vector3 screenC = cam.WorldToScreenPoint(worldC);
            Vector3 screenD = cam.WorldToScreenPoint(worldD);
            Vector3 screenCenter3 = cam.WorldToScreenPoint(circleCenter);
            float screenDepth = screenCenter3.z;

            if (screenDepth <= 0f)
            {
                Debug.LogWarning("Circle is behind the camera.");
                return result;
            }

            Vector2 screenCenter = new Vector2(screenCenter3.x, screenCenter3.y);

            // 主軸與次軸
            float aLen = (screenA - screenB).magnitude;
            float bLen = (screenC - screenD).magnitude;
            float a = aLen / 2f;
            float b = bLen / 2f;
            Vector2 mainAxisDir = (screenA - screenB).normalized;

            if (b > a)
            {
                (a, b) = (b, a);
                mainAxisDir = (screenC - screenD).normalized;
            }

            // 輸出填值
            result.screenCenter = screenCenter;
            result.majorAxisDirection = mainAxisDir;
            result.majorAxisLength = a;
            result.minorAxisLength = b;

            return result;
        }
    }
}

