using UnityEngine;

namespace TilesEditor
{
    // =============================================
    // 檔案名稱：GizmoBase.cs
    // 1. 定義所有 Gizmo 元件的介面。
    // 2. 提供共用的顏色管理與材質設定功能。
    // 3. 定義必要方法供實作類別使用。
    // 4. 使用 MaterialPropertyBlock 來設定顏色，避免材質實例化。
    // 5. 提供基礎的顏色重置功能。
    // =============================================
    public interface iGizmo
    {
        public const int GIZMO_X = 0;
        public const int GIZMO_Y = 1;
        public const int GIZMO_Z = 2;
        public const int GIZMO_XY = 3;
        public const int GIZMO_XZ = 4;
        public const int GIZMO_YZ = 5;
        public const int GIZMO_HOVER = 6;

        public enum GizmoType : int { X, Y, Z, XY, XZ, YZ, ROT_X, ROT_Y, ROT_Z }

        void Initialize(GizmoType type, TransformGizmo gizmo);
        void SetMaterialColor(Color color);
        void ResetColor();
        bool ShouldBeActive();
        bool IsHovered();
        void SetInvisible(bool value);
        void OnDrag();
    }


}
