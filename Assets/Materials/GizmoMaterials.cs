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