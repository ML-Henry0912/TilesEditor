using UnityEngine;

public class PlaneGizmo : GizmoBase
{
    public enum PlaneType { XY, XZ, YZ }
    public PlaneType planeType;

    public void Initialize(PlaneType type, Color color)
    {
        planeType = type;
        baseColor = color;
        SetMaterialColor(color);
    }

    protected override Material CreateDefaultMaterial()
    {
        // 使用 Unlit/Color shader，支援顏色與透明度
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off); // 雙面可見
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        return mat;
    }

    public override void SetMaterialColor(Color color)
    {
        if (material == null)
        {
            var renderer = GetComponent<MeshRenderer>();
            material = CreateDefaultMaterial();
            renderer.material = material;
        }
        material.SetColor("_Color", color); // 設置顏色與透明度
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
}
