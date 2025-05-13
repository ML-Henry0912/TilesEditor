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
using System.Collections.Generic;
using static TilesEditor.PlaneGizmo;

namespace TilesEditor
{
    public class TransformGizmo : MonoBehaviour
    {
        // === 常數定義 ===
        const float AXIS_HANDLE_OFFSET = 1.25f;
        const float PLANE_HANDLE_OFFSET = 0.55f;
        const float PLANE_HANDLE_SIZE = 0.75f;
        const float ROTATE_HANDLE_SCALE = 1.2f;
        const float AXIS_HANDLE_SCALE = 0.1f;
        const float AXIS_HANDLE_LENGTH = 0.6f;
        const float AXIS_HANDLE_THICKNESS = 16.0f;

        public Transform target;
        public Camera cam;

        [Header("Translate Axis Enable")]
        public bool translateX = true;
        public bool translateY = true;
        public bool translateZ = true;

        [Header("Rotate Axis Enable")]
        public bool rotateX = true;
        public bool rotateY = true;
        public bool rotateZ = true;

        private iGizmo[] allGizmos = new iGizmo[9];

        public GizmoMaterials materials;

        public Vector3 dragStartPos, objectStartPos;
        [Header("Active Gizmo")]
        [SerializeField] private iGizmo activeGizmo;
        [Header("Active Gizmos")]
        [SerializeField] private AxisGizmo axisGizmo;
        [SerializeField] private PlaneGizmo planeGizmo;
        [SerializeField] private RotateGizmo rotateGizmo;

        public Vector3 rotateStartPoint;
        public Quaternion objectStartRot;
        public Plane rotationPlane;

        public Action action;

        bool initialized = false;

        public void Initialize(Transform target, Camera cam, GizmoMaterials materials)
        {
            // 檢查是否已經初始化過相同的目標
            if (initialized && this.cam == cam && this.materials == materials)
            {
                return;
            }

            this.target = target;
            this.cam = cam;
            this.materials = materials;

            CreateAllHandles();
            initialized = true;
            action = CheckHover;
        }

        private Color GetAxisColor(int index)
        {
            switch (index)
            {
                case 0: return Color.red;
                case 1: return Color.green;
                case 2: return Color.blue;
                default: return Color.white;
            }
        }

        private Material GetAxisMaterial(int index)
        {
            return materials.materials[index];

        }

        private Color GetPlaneColor(int index)
        {
            switch (index)
            {
                case 3: return new Color(1.0f, 1.0f, 0.0f, 0.3f);
                case 4: return new Color(1.0f, 0.0f, 1.0f, 0.3f);
                case 5: return new Color(0.0f, 1.0f, 1.0f, 0.3f);
                default: return Color.white;
            }
        }

        private Material GetPlaneMaterial(int index)
        {
            return materials.materials[index];

        }

        void Update()
        {
            if (!initialized || target == null || cam == null) return;

            transform.position = target.position;
            transform.rotation = target.rotation;

            action?.Invoke();
        }

        // 狀態：Idle，檢查是否 hover 到 handle
        void CheckHover()
        {
            if (TryHoverHandle())
            {
                action = CheckMouseDown;
            }
        }

        // 狀態：Hover，檢查是否按下滑鼠進入拖曳
        void CheckMouseDown()
        {
            if (Input.GetMouseButtonDown(0))
            {
                CheckDrag();
            }
            else if (!TryHoverHandle())
            {
                action = CheckHover;
            }
        }

        // 嘗試 hover 到 handle，並處理 hover 效果
        bool TryHoverHandle()
        {
            bool hoverFound = false;
            foreach (var gizmo in allGizmos)
            {
                gizmo.ResetColor();
                if (hoverFound)
                    continue;

                if (gizmo != null && gizmo.ShouldBeActive() && gizmo.IsHovered())
                {
                    gizmo.SetMaterialColor(Color.yellow);
                    var mr = (gizmo as MonoBehaviour)?.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = materials.materials[iGizmo.GIZMO_HOVER];
                    hoverFound = true;
                }
            }
            return hoverFound;
        }

        void CheckDrag()
        {
            Vector3 center = target.position;
            foreach (var gizmo in allGizmos)
            {
                if (gizmo == null || !gizmo.ShouldBeActive() || !gizmo.IsHovered())
                    continue;

                activeGizmo = gizmo;
                if (gizmo is RotateGizmo rotateGizmo)
                {
                    action = OnDragRotate;
                    rotationPlane = new Plane(rotateGizmo.WorldAxis, center);
                    Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                    if (rotationPlane.Raycast(ray, out float enter))
                    {
                        rotateStartPoint = ray.GetPoint(enter);
                        objectStartRot = target.rotation;
                        return;
                    }
                }
                else if (gizmo is AxisGizmo axisGizmo)
                {
                    action = OnDragAxis;
                    Vector3 axisDir = transform.TransformDirection(axisGizmo.WorldDirection).normalized;
                    dragStartPos = GetClosestPointOnAxis(cam.ScreenPointToRay(Input.mousePosition), target.position, axisDir);
                    objectStartPos = target.position;
                    return;
                }
                else if (gizmo is PlaneGizmo planeGizmo)
                {
                    action = OnDragPlane;
                    Plane dragPlane = planeGizmo.GetDragPlane(transform, target.position);
                    Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                    if (!dragPlane.Raycast(ray, out float enter))
                    {
                        dragPlane = new Plane(-dragPlane.normal, dragPlane.distance);
                        if (!dragPlane.Raycast(ray, out enter))
                            return;
                    }
                    dragStartPos = ray.GetPoint(enter);
                    objectStartPos = target.position;
                    return;
                }
            }
        }

        // 拖曳軸
        void OnDragAxis()
        {
            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
                return;
            }

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Vector3 axisDir = transform.TransformDirection(((AxisGizmo)activeGizmo).WorldDirection).normalized;
            Vector3 current = GetClosestPointOnAxis(ray, objectStartPos, axisDir);
            Vector3 delta = Vector3.Project(current - dragStartPos, axisDir);
            if (delta.magnitude < 100f)
                target.position = objectStartPos + delta;
        }

        // 拖曳平面
        void OnDragPlane()
        {
            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
                return;
            }

            if (activeGizmo == null)
                return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane dragPlane = ((PlaneGizmo)activeGizmo).GetDragPlane(transform, target.position);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 currentPoint = ray.GetPoint(enter);
                Vector3 delta = currentPoint - dragStartPos;
                if (delta.magnitude < 100f)
                    target.position = objectStartPos + delta;
            }
        }

        // 拖曳旋轉
        void OnDragRotate()
        {
            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
                return;
            }

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (rotationPlane.Raycast(ray, out float enter))
            {
                Vector3 currentPoint = ray.GetPoint(enter);
                Vector3 startDir = (rotateStartPoint - target.position).normalized;
                Vector3 currentDir = (currentPoint - target.position).normalized;

                Quaternion deltaRotation = Quaternion.FromToRotation(startDir, currentDir);
                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

                if (Vector3.Dot(axis, ((RotateGizmo)activeGizmo).WorldAxis) < 0f)
                    angle = -angle;

                target.rotation = objectStartRot * Quaternion.AngleAxis(angle, ((RotateGizmo)activeGizmo).WorldAxis);
            }
        }

        void EndDrag()
        {
            activeGizmo?.ResetColor();
            activeGizmo = null;
            action = CheckHover;
        }

        public Vector3 GetClosestPointOnAxis(Ray ray, Vector3 axisOrigin, Vector3 axisDir)
        {
            Vector3 p1 = ray.origin;
            Vector3 d1 = ray.direction;
            Vector3 p2 = axisOrigin;
            Vector3 d2 = axisDir;

            float a = Vector3.Dot(d1, d1);
            float b = Vector3.Dot(d1, d2);
            float e = Vector3.Dot(d2, d2);
            float d = a * e - b * b;

            if (Mathf.Abs(d) < 0.0001f)
                return axisOrigin;

            Vector3 r = p1 - p2;
            float c = Vector3.Dot(d1, r);
            float f = Vector3.Dot(d2, r);
            float s = (b * f - c * e) / d;

            return p1 + d1 * s;
        }

        void CreateAllHandles()
        {
            if (!initialized)
            {
                allGizmos[0] = CreateAxisHandle("X_Handle", new Vector3(AXIS_HANDLE_OFFSET, 0.0f, 0.0f), Quaternion.Euler(0.0f, 0.0f, -90.0f), Color.red, AxisGizmo.Axis.X);
                allGizmos[1] = CreateAxisHandle("Y_Handle", new Vector3(0.0f, AXIS_HANDLE_OFFSET, 0.0f), Quaternion.identity, Color.green, AxisGizmo.Axis.Y);
                allGizmos[2] = CreateAxisHandle("Z_Handle", new Vector3(0.0f, 0.0f, AXIS_HANDLE_OFFSET), Quaternion.Euler(90.0f, 0.0f, 0.0f), Color.blue, AxisGizmo.Axis.Z);

                allGizmos[3] = CreatePlaneHandle("XY_Handle", new Vector3(PLANE_HANDLE_OFFSET, PLANE_HANDLE_OFFSET, 0.0f), Quaternion.identity, new Color(1.0f, 1.0f, 0.0f, 0.3f), PlaneType.XY, PLANE_HANDLE_SIZE);
                allGizmos[4] = CreatePlaneHandle("XZ_Handle", new Vector3(PLANE_HANDLE_OFFSET, 0.0f, PLANE_HANDLE_OFFSET), Quaternion.Euler(90.0f, 0.0f, 0.0f), new Color(1.0f, 0.0f, 1.0f, 0.3f), PlaneType.XZ, PLANE_HANDLE_SIZE);
                allGizmos[5] = CreatePlaneHandle("YZ_Handle", new Vector3(0.0f, PLANE_HANDLE_OFFSET, PLANE_HANDLE_OFFSET), Quaternion.Euler(0.0f, -90.0f, 0.0f), new Color(0.0f, 1.0f, 1.0f, 0.3f), PlaneType.YZ, PLANE_HANDLE_SIZE);

                allGizmos[6] = CreateRotateHandle("X_Rotate", Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 90.0f), Color.red, RotateGizmo.Axis.X);
                allGizmos[7] = CreateRotateHandle("Y_Rotate", Vector3.zero, Quaternion.identity, Color.green, RotateGizmo.Axis.Y);
                allGizmos[8] = CreateRotateHandle("Z_Rotate", Vector3.zero, Quaternion.Euler(90.0f, 0.0f, 0.0f), Color.blue, RotateGizmo.Axis.Z);
            }
            else
            {
                for (int i = 0; i < allGizmos.Length; i++)
                {
                    if (allGizmos[i] != null)
                    {
                        var gizmo = allGizmos[i];
                        if (gizmo is AxisGizmo axisGizmo)
                        {
                            axisGizmo.Initialize((AxisGizmo.Axis)i, GetAxisColor(i), this);
                            var renderer = (gizmo as MonoBehaviour).GetComponent<MeshRenderer>();
                            if (renderer != null) renderer.sharedMaterial = GetAxisMaterial(i);
                        }
                        else if (gizmo is PlaneGizmo planeGizmo)
                        {
                            planeGizmo.Initialize((PlaneGizmo.PlaneType)(i - 3), GetPlaneColor(i), this);
                            var renderer = (gizmo as MonoBehaviour).GetComponent<MeshRenderer>();
                            if (renderer != null) renderer.sharedMaterial = GetPlaneMaterial(i);
                        }
                        else if (gizmo is RotateGizmo rotateGizmo)
                        {
                            rotateGizmo.Initialize((RotateGizmo.Axis)(i - 6), GetAxisColor(i - 6), this, AXIS_HANDLE_THICKNESS);
                            var renderer = (gizmo as MonoBehaviour).GetComponent<MeshRenderer>();
                            if (renderer != null) renderer.sharedMaterial = GetAxisMaterial(i - 6);
                        }
                    }

                }
            }

            for (int i = 0; i < allGizmos.Length; i++)
            {
                if (allGizmos[i] != null)
                {
                    (allGizmos[i] as MonoBehaviour).gameObject.SetActive(allGizmos[i].ShouldBeActive());
                }
            }
        }

        AxisGizmo CreateAxisHandle(string name, Vector3 localPos, Quaternion localRot, Color color, AxisGizmo.Axis axis)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = new Vector3(AXIS_HANDLE_SCALE, AXIS_HANDLE_LENGTH, AXIS_HANDLE_SCALE);

            var axisGizmo = go.AddComponent<AxisGizmo>();
            axisGizmo.Initialize(axis, color, this);
            // 指定材質
            var renderer = go.GetComponent<MeshRenderer>();
            if (axis == AxisGizmo.Axis.X) renderer.sharedMaterial = materials.materials[iGizmo.GIZMO_X];
            else if (axis == AxisGizmo.Axis.Y) renderer.sharedMaterial = materials.materials[iGizmo.GIZMO_Y];
            else if (axis == AxisGizmo.Axis.Z) renderer.sharedMaterial = materials.materials[iGizmo.GIZMO_Z];
            return axisGizmo;
        }

        PlaneGizmo CreatePlaneHandle(string name, Vector3 localPos, Quaternion localRot, Color color, PlaneGizmo.PlaneType type, float size)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;

            // 根據平面類型設定正確的 scale
            go.transform.localScale = new Vector3(size, size, 0.05f); ;

            var planeGizmo = go.AddComponent<PlaneGizmo>();
            planeGizmo.Initialize(type, color, this);
            // 指定材質
            var renderer = go.GetComponent<MeshRenderer>();
            if (type == PlaneType.XY) renderer.sharedMaterial = materials.materials[iGizmo.GIZMO_XY];
            else if (type == PlaneType.XZ) renderer.sharedMaterial = materials.materials[iGizmo.GIZMO_XZ];
            else if (type == PlaneType.YZ) renderer.sharedMaterial = materials.materials[iGizmo.GIZMO_YZ];
            return planeGizmo;
        }

        RotateGizmo CreateRotateHandle(string name, Vector3 localPos, Quaternion localRot, Color color, RotateGizmo.Axis axis)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = Vector3.one * ROTATE_HANDLE_SCALE;
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.mesh = TorusMeshGenerator.Generate(1.0f, 0.05f, 64, 12);
            var gizmo = go.AddComponent<RotateGizmo>();
            gizmo.Initialize(axis, color, this, AXIS_HANDLE_THICKNESS);
            // 指定材質
            if (axis == RotateGizmo.Axis.X) mr.sharedMaterial = materials.materials[iGizmo.GIZMO_X];
            else if (axis == RotateGizmo.Axis.Y) mr.sharedMaterial = materials.materials[iGizmo.GIZMO_Y];
            else if (axis == RotateGizmo.Axis.Z) mr.sharedMaterial = materials.materials[iGizmo.GIZMO_Z];
            return gizmo;
        }

        public static class TorusMeshGenerator
        {
            public static Mesh Generate(float ringRadius = 1f, float tubeRadius = 0.05f, int segments = 64, int sides = 12)
            {
                Mesh mesh = new Mesh();
                mesh.name = "FullTorus";

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

        protected void SetPlaneGizmoProperties(PlaneGizmo.PlaneType type, Vector3? position = null)
        {
            PlaneGizmo targetGizmo = null;
            switch (type)
            {
                case PlaneGizmo.PlaneType.XY:
                    targetGizmo = (PlaneGizmo)allGizmos[3];
                    break;
                case PlaneGizmo.PlaneType.XZ:
                    targetGizmo = (PlaneGizmo)allGizmos[4];
                    break;
                case PlaneGizmo.PlaneType.YZ:
                    targetGizmo = (PlaneGizmo)allGizmos[5];
                    break;
            }

            if (targetGizmo == null) return;

            if (position.HasValue)
            {
                targetGizmo.transform.localPosition = position.Value;
            }
        }

        protected void SetPlaneGizmoInvisible(PlaneGizmo.PlaneType type)
        {
            switch (type)
            {
                case PlaneGizmo.PlaneType.XY:
                    allGizmos[3].SetInvisible(true);
                    break;
                case PlaneGizmo.PlaneType.XZ:
                    allGizmos[4].SetInvisible(true);
                    break;
                case PlaneGizmo.PlaneType.YZ:
                    allGizmos[5].SetInvisible(true);
                    break;
            }
        }


    }
}
