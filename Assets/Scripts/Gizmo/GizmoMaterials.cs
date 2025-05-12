// =============================================
// 檔案名稱：GizmoMaterials.cs
// 1. 用於集中管理所有 Gizmo 相關材質的 ScriptableObject。
// 2. 於編輯器預先指定各種顏色材質，避免執行時動態產生，提升效能與一致性。
// 3. 方便統一設定、切換主題或批次調整。
// =============================================
using UnityEngine;

[CreateAssetMenu(fileName = "GizmoMaterials", menuName = "ScriptableObjects/GizmoMaterials", order = 1)]
public class GizmoMaterials : ScriptableObject
{
    public Material xRed;
    public Material yGreen;
    public Material zBlue;
    public Material xyYellow;
    public Material xzMagenta;
    public Material yzCyan;
    public Material hoverYellow;

}