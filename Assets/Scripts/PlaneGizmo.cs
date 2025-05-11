// =============================================
// 檔案名稱：PlaneGizmo.cs
// 功能說明：用於在特定平面（如 XY、XZ、YZ）上進行拖曳操作的 Gizmo，
//          讓使用者可以在 2D 平面上移動物件。
// =============================================
using UnityEngine;

public class PlaneGizmo : GizmoBase
{
    public enum PlaneType { XY, XZ, YZ }
    public PlaneType planeType;

    private Camera cam;
    private float size;

    public void Initialize(PlaneType type, Color color, Camera cam, float size)
    {
        planeType = type;
        baseColor = color;
        SetMaterialColor(color);
        this.cam = cam;
        this.size = size;
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

    public bool IsMouseOnPlaneGizmo(Vector3 planeCenter, Vector3 planeRight, Vector3 planeUp)
    {
        Vector3 c = planeCenter;
        Vector3 r = planeRight * size * 0.5f;
        Vector3 u = planeUp * size * 0.5f;
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
            float TriArea(Vector2 p1, Vector2 p2, Vector2 p3) => Mathf.Abs((p1.x*(p2.y-p3.y) + p2.x*(p3.y-p1.y) + p3.x*(p1.y-p2.y))/2f);
            return TriArea(a, b, c) + TriArea(a, c, d);
        }
        float quadArea = QuadArea(screenCorners[0], screenCorners[1], screenCorners[2], screenCorners[3]);
        float sumArea = 0f;
        for (int i = 0; i < 4; i++)
            sumArea += Mathf.Abs((screenCorners[i].x*(screenCorners[(i+1)%4].y-mouse.y) + screenCorners[(i+1)%4].x*(mouse.y-screenCorners[i].y) + mouse.x*(screenCorners[i].y-screenCorners[(i+1)%4].y))/2f);
        return Mathf.Abs(sumArea - quadArea) < 1.5f;
    }
}
