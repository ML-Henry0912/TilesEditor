// =============================================
// 檔案名稱：TransformGizmo.cs
// 1. 本類別作為主控 Gizmo，整合軸、平面、旋轉等子 Gizmo。
// 2. 負責處理物件的移動與旋轉互動邏輯。
// 3. 支援安全重複初始化，相同參數的初始化會被忽略。
// 4. 旋轉功能採用橢圓投影計算，確保在任意視角下都能精確操作。
// 5. 所有 Gizmo 材質由 ScriptableObject 統一管理，於建立時指定。
// 6. 支援個別軸向的啟用/禁用控制。
// 7. 所有 handle 必須為 TransformGizmo 的子物件，且不可手動移除或更改父子結構。
// =============================================
using System;
using UnityEngine;
using static TilesEditor.iGizmo;

namespace TilesEditor
{
    public class TransformGizmo : MonoBehaviour
    {
        // === 常數定義 ===
        const float AXIS_HANDLE_OFFSET = 1.25f;
        const float PLANE_HANDLE_OFFSET = 0.55f;
        const float PLANE_HANDLE_SIZE = 0.75f;
        const float AXIS_HANDLE_SCALE = 0.1f;
        const float AXIS_HANDLE_LENGTH = 0.6f;

        public Transform target;// { get; private set; }
        public Camera cam;

        [Header("Gizmo Enable")]
        public bool[] gizmoEnable = new bool[9] { true, true, true, true, true, true, true, true, true };

        private iGizmo[] allGizmos = new iGizmo[9];

        public GizmoMaterials materials;

        public Vector3 dragStartPos, objectStartPos;
        [Header("Active Gizmo")]
        public iGizmo activeGizmo;
        [Header("Active Gizmos")]
        [SerializeField] private AxisGizmo axisGizmo;
        [SerializeField] private PlaneGizmo planeGizmo;
        [SerializeField] private RotateGizmo rotateGizmo;

        public Vector3 rotateStartPoint;
        public Quaternion objectStartRot;
        public Plane rotationPlane;

        public Action action;

        bool initialized = false;
        bool isHidden = false;

        public Color[] gizmoColors = {
            Color.red,
            Color.green,
            Color.blue,
            new Color(1.0f, 1.0f, 0.0f, 0.3f),
            new Color(1.0f, 0.0f, 1.0f, 0.3f),
            new Color(0.0f, 1.0f, 1.0f, 0.3f),
            Color.red,
            Color.green,
            Color.blue,
        };

        public void Initialize(Transform target, Camera cam, GizmoMaterials materials)
        {
            this.target = target;
            this.cam = cam;
            this.materials = materials;

            // 檢查是否已經初始化過相同的目標
            if (initialized)
            {
                for (int _i = 0; _i < allGizmos.Length; _i++)
                {
                    allGizmos[_i].Initialize((GizmoType)_i, this);
                    allGizmos[_i].SetInvisible(false);
                }
            }
            else
            {
                CreateAllHandles();
            }

            initialized = true;
            isHidden = false;
            action = CheckHover;
        }

        public void HideAllGizmos()
        {
            if (!initialized) return;
            isHidden = true;
            foreach (var gizmo in allGizmos)
            {
                if (gizmo != null)
                {
                    gizmo.SetInvisible(true);
                }
            }
        }

        void Update()
        {
            if (!initialized || target == null || cam == null || isHidden) return;

            transform.position = target.position;
            transform.rotation = target.rotation;

            action?.Invoke();
        }


        // 狀態：Idle，檢查是否 hover 到 handle
        void CheckHover()
        {
            if (TryHoverHandle())
            {
                //action = CheckMouseDown;
            }
        }

        // 狀態：Hover，檢查是否按下滑鼠進入拖曳
        void CheckMouseDown()
        {

        }

        // 嘗試 hover 到 handle，並處理 hover 效果
        bool TryHoverHandle()
        {
            bool hoverFound = false;
            activeGizmo = null;
            foreach (var gizmo in allGizmos)
            {
                if (hoverFound)
                    continue;

                if (gizmo != null && gizmo.IsHovered())
                {
                    action = gizmo.OnHover;

                    activeGizmo = gizmo;
                    hoverFound = true;
                }
            }
            return hoverFound;
        }


        // 拖曳平面

        public void EndDrag()
        {
            activeGizmo?.ResetColor();
            activeGizmo = null;
            axisGizmo = null;
            planeGizmo = null;
            rotateGizmo = null;

            action = CheckHover;
        }


        void CreateAllHandles()
        {
            Debug.Log($"CreateAllHandles");
            allGizmos[0] = CreateAxisHandle("X_Handle", new Vector3(AXIS_HANDLE_OFFSET, 0.0f, 0.0f), Quaternion.Euler(0.0f, 0.0f, -90.0f), GizmoType.X);
            allGizmos[1] = CreateAxisHandle("Y_Handle", new Vector3(0.0f, AXIS_HANDLE_OFFSET, 0.0f), Quaternion.identity, GizmoType.Y);
            allGizmos[2] = CreateAxisHandle("Z_Handle", new Vector3(0.0f, 0.0f, AXIS_HANDLE_OFFSET), Quaternion.Euler(90.0f, 0.0f, 0.0f), GizmoType.Z);

            allGizmos[3] = CreatePlaneHandle("XY_Handle", new Vector3(PLANE_HANDLE_OFFSET, PLANE_HANDLE_OFFSET, 0.0f), Quaternion.identity, GizmoType.XY);
            allGizmos[4] = CreatePlaneHandle("XZ_Handle", new Vector3(PLANE_HANDLE_OFFSET, 0.0f, PLANE_HANDLE_OFFSET), Quaternion.Euler(90.0f, 0.0f, 0.0f), GizmoType.XZ);
            allGizmos[5] = CreatePlaneHandle("YZ_Handle", new Vector3(0.0f, PLANE_HANDLE_OFFSET, PLANE_HANDLE_OFFSET), Quaternion.Euler(0.0f, -90.0f, 0.0f), GizmoType.YZ);

            allGizmos[6] = CreateRotateHandle("X_Rotate", Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 90.0f), GizmoType.ROT_X);
            allGizmos[7] = CreateRotateHandle("Y_Rotate", Vector3.zero, Quaternion.identity, GizmoType.ROT_Y);
            allGizmos[8] = CreateRotateHandle("Z_Rotate", Vector3.zero, Quaternion.Euler(90.0f, 0.0f, 0.0f), GizmoType.ROT_Z);
        }

        AxisGizmo CreateAxisHandle(string name, Vector3 localPos, Quaternion localRot, GizmoType type)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = new Vector3(AXIS_HANDLE_SCALE, AXIS_HANDLE_LENGTH, AXIS_HANDLE_SCALE);

            var axisGizmo = go.AddComponent<AxisGizmo>();
            axisGizmo.Initialize(type, this);
            // 指定材質
            var renderer = go.GetComponent<MeshRenderer>();
            if (type == GizmoType.X) renderer.sharedMaterial = materials.materials[iGizmo.GIZMO_X];
            else if (type == GizmoType.Y) renderer.sharedMaterial = materials.materials[iGizmo.GIZMO_Y];
            else if (type == GizmoType.Z) renderer.sharedMaterial = materials.materials[iGizmo.GIZMO_Z];
            return axisGizmo;
        }

        PlaneGizmo CreatePlaneHandle(string name, Vector3 localPos, Quaternion localRot, GizmoType type)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;

            // 根據平面類型設定正確的 scale
            go.transform.localScale = new Vector3(PLANE_HANDLE_SIZE, PLANE_HANDLE_SIZE, 0.05f);

            var planeGizmo = go.AddComponent<PlaneGizmo>();
            planeGizmo.Initialize(type, this);
            // 指定材質
            var renderer = go.GetComponent<MeshRenderer>();
            if (type == GizmoType.XY) renderer.sharedMaterial = materials.materials[GIZMO_XY];
            else if (type == GizmoType.XZ) renderer.sharedMaterial = materials.materials[GIZMO_XZ];
            else if (type == GizmoType.YZ) renderer.sharedMaterial = materials.materials[GIZMO_YZ];
            return planeGizmo;
        }

        RotateGizmo CreateRotateHandle(string name, Vector3 localPos, Quaternion localRot, GizmoType type)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;

            var meshFilter = go.AddComponent<MeshFilter>();
            var mesh = TorusMeshGenerator.Generate(1.2f);
            meshFilter.sharedMesh = mesh;

            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = materials.materials[type == GizmoType.ROT_X ? GIZMO_X : type == GizmoType.ROT_Y ? GIZMO_Y : GIZMO_Z];

            var meshCollider = go.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;

            var rotateGizmo = go.AddComponent<RotateGizmo>();
            rotateGizmo.Initialize(type, this);
            return rotateGizmo;
        }

        public static class TorusMeshGenerator
        {
            public static Mesh Generate(float ringRadius = 1f, float tubeRadius = 0.05f, int segments = 32, int sides = 3)
            {
                Mesh mesh = new Mesh();
                mesh.name = "Torus";

                int vertexCount = segments * sides;
                Vector3[] vertices = new Vector3[vertexCount];
                Vector3[] normals = new Vector3[vertexCount];
                Vector2[] uvs = new Vector2[vertexCount];
                int[] triangles = new int[segments * sides * 6];

                float segmentStep = Mathf.PI * 2f / segments;

                for (int seg = 0; seg < segments; seg++)
                {
                    float theta = segmentStep * seg;
                    Vector3 ringCenter = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta)) * ringRadius;

                    for (int side = 0; side < sides; side++)
                    {
                        float phi = (float)side / sides * Mathf.PI * 2f;
                        Vector3 normal = new Vector3(
                            Mathf.Cos(phi) * Mathf.Cos(theta),
                            Mathf.Sin(phi),
                            Mathf.Cos(phi) * Mathf.Sin(theta)
                        );

                        int index = seg * sides + side;
                        vertices[index] = ringCenter + normal * tubeRadius;
                        normals[index] = normal;
                        uvs[index] = new Vector2((float)seg / segments, (float)side / sides);
                    }
                }

                for (int seg = 0; seg < segments; seg++)
                {
                    int nextSeg = (seg + 1) % segments;
                    for (int side = 0; side < sides; side++)
                    {
                        int nextSide = (side + 1) % sides;

                        int current = seg * sides + side;
                        int next = nextSeg * sides + side;
                        int currentNext = seg * sides + nextSide;
                        int nextNext = nextSeg * sides + nextSide;

                        int triIndex = (seg * sides + side) * 6;
                        triangles[triIndex] = current;
                        triangles[triIndex + 1] = next;
                        triangles[triIndex + 2] = currentNext;

                        triangles[triIndex + 3] = next;
                        triangles[triIndex + 4] = nextNext;
                        triangles[triIndex + 5] = currentNext;
                    }
                }

                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.uv = uvs;
                mesh.triangles = triangles;
                mesh.RecalculateBounds();

                return mesh;
            }
        }

        protected void SetPlaneGizmoProperties(GizmoType type, Vector3? position = null)
        {
            PlaneGizmo targetGizmo = null;
            switch (type)
            {
                case GizmoType.XY:
                    targetGizmo = (PlaneGizmo)allGizmos[3];
                    break;
                case GizmoType.XZ:
                    targetGizmo = (PlaneGizmo)allGizmos[4];
                    break;
                case GizmoType.YZ:
                    targetGizmo = (PlaneGizmo)allGizmos[5];
                    break;
            }

            if (targetGizmo == null) return;

            if (position.HasValue)
            {
                targetGizmo.transform.localPosition = position.Value;
            }
        }

        protected void SetPlaneGizmoInvisible(GizmoType type)
        {
            allGizmos[(int)type].SetInvisible(true);

        }
    }
}
