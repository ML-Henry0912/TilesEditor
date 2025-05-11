using System;
using UnityEngine;

public class TransformGizmo : MonoBehaviour
{
    public const string GIZMO_TAG = "GizmoHandle";

    public Transform target;
    public Camera cam;

    [Header("Translate Axis Enable")]
    public bool enableTranslateX = true;
    public bool enableTranslateY = true;
    public bool enableTranslateZ = true;

    private Transform xHandle, yHandle, zHandle;
    private Transform xyHandle, xzHandle, yzHandle;

    private enum DragMode { None, Axis, Plane }
    private DragMode currentDrag = DragMode.None;

    private Vector3 dragStartPos, objectStartPos;
    private AxisGizmo activeAxis;
    private PlaneGizmo activePlane;

    Action action;

    void Start()
    {
        CreateAllHandles();
        action = CheckMouseDown;
    }

    void Update()
    {
        if (target == null || cam == null) return;

        transform.position = target.position;
        transform.rotation = target.rotation;

        UpdateHandleVisibility();

        action?.Invoke();

        if (currentDrag == DragMode.None)
            HandleHover();
    }

    void UpdateHandleVisibility()
    {
        xHandle?.gameObject.SetActive(enableTranslateX);
        yHandle?.gameObject.SetActive(enableTranslateY);
        zHandle?.gameObject.SetActive(enableTranslateZ);
    }

    void CheckMouseDown()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.CompareTag(GIZMO_TAG))
        {
            AxisGizmo axis = hit.transform.GetComponent<AxisGizmo>();
            if (axis != null)
            {
                currentDrag = DragMode.Axis;
                activeAxis = axis;
                Vector3 axisDir = transform.TransformDirection(axis.WorldDirection).normalized;
                dragStartPos = GetClosestPointOnAxis(ray, target.position, axisDir);
                objectStartPos = target.position;
                action = OnDragAxis;
            }
            else
            {
                PlaneGizmo plane = hit.transform.GetComponent<PlaneGizmo>();
                if (plane != null)
                {
                    currentDrag = DragMode.Plane;
                    activePlane = plane;
                    Plane dragPlane = plane.GetDragPlane(transform, target.position);
                    if (dragPlane.Raycast(ray, out float enter))
                    {
                        dragStartPos = ray.GetPoint(enter);
                        objectStartPos = target.position;
                        action = OnDragPlane;
                    }
                }
            }
        }
    }

    void OnDragAxis()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Input.GetMouseButton(0) && currentDrag == DragMode.Axis && activeAxis != null)
        {
            Vector3 axisDir = transform.TransformDirection(activeAxis.WorldDirection).normalized;
            Vector3 current = GetClosestPointOnAxis(ray, objectStartPos, axisDir);
            Vector3 delta = Vector3.Project(current - dragStartPos, axisDir);

            if (delta.magnitude < 100f)
                target.position = objectStartPos + delta;
        }

        if (Input.GetMouseButtonUp(0))
            EndDrag();
    }

    void OnDragPlane()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Input.GetMouseButton(0) && currentDrag == DragMode.Plane && activePlane != null)
        {
            Plane dragPlane = activePlane.GetDragPlane(transform, target.position);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 currentPoint = ray.GetPoint(enter);
                Vector3 delta = currentPoint - dragStartPos;

                if (delta.magnitude < 100f)
                    target.position = objectStartPos + delta;
            }
        }

        if (Input.GetMouseButtonUp(0))
            EndDrag();
    }

    void EndDrag()
    {
        activeAxis?.ResetColor();
        activePlane?.ResetColor();

        activeAxis = null;
        activePlane = null;
        currentDrag = DragMode.None;
        action = CheckMouseDown;
    }

    void HandleHover()
    {
        ResetAllGizmoColors();

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.CompareTag(GIZMO_TAG))
        {
            var gizmo = hit.transform.GetComponent<GizmoBase>();
            if (gizmo != null)
            {
                gizmo.SetMaterialColor(Color.yellow);
            }
        }
    }

    void ResetAllGizmoColors()
    {
        foreach (Transform t in transform)
        {
            var gizmo = t.GetComponent<GizmoBase>();
            if (gizmo != null)
                gizmo.ResetColor();
        }
    }

    private void CreateAllHandles()
    {
        xHandle = CreateAxisHandle("X_Handle", new Vector3(0.5f, 0, 0), Quaternion.Euler(0, 0, -90), Color.red, AxisGizmo.Axis.X);
        yHandle = CreateAxisHandle("Y_Handle", new Vector3(0, 0.5f, 0), Quaternion.identity, Color.green, AxisGizmo.Axis.Y);
        zHandle = CreateAxisHandle("Z_Handle", new Vector3(0, 0, 0.5f), Quaternion.Euler(90, 0, 0), Color.blue, AxisGizmo.Axis.Z);

        xyHandle = CreatePlaneHandle("XY_Handle", new Vector3(0.25f, 0.25f, 0), Quaternion.identity, new Color(1, 1, 0, 0.3f), PlaneGizmo.PlaneType.XY);
        xzHandle = CreatePlaneHandle("XZ_Handle", new Vector3(0.25f, 0, 0.25f), Quaternion.Euler(90, 0, 0), new Color(1, 0, 1, 0.3f), PlaneGizmo.PlaneType.XZ);
        yzHandle = CreatePlaneHandle("YZ_Handle", new Vector3(0, 0.25f, 0.25f), Quaternion.Euler(0, -90, 0), new Color(0, 1, 1, 0.3f), PlaneGizmo.PlaneType.YZ);
    }

    private Transform CreateAxisHandle(string name, Vector3 localPos, Quaternion localRot, Color color, AxisGizmo.Axis axis)
    {
        GameObject go = new GameObject(name);
        go.tag = GIZMO_TAG;
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        var temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mf.mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(temp);

        go.AddComponent<CapsuleCollider>().isTrigger = true;

        var axisGizmo = go.AddComponent<AxisGizmo>();
        axisGizmo.Initialize(axis, color);

        return go.transform;
    }

    private Transform CreatePlaneHandle(string name, Vector3 localPos, Quaternion localRot, Color color, PlaneGizmo.PlaneType type)
    {
        GameObject go = new GameObject(name);
        go.tag = GIZMO_TAG;
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = Vector3.one * 0.25f;

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        var temp = GameObject.CreatePrimitive(PrimitiveType.Quad);
        mf.mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(temp);

        go.AddComponent<MeshCollider>().convex = true;

        var planeGizmo = go.AddComponent<PlaneGizmo>();
        planeGizmo.Initialize(type, color);

        return go.transform;
    }

    private Vector3 GetClosestPointOnAxis(Ray mouseRay, Vector3 axisOrigin, Vector3 axisDirection)
    {
        Vector3 p1 = mouseRay.origin;
        Vector3 d1 = mouseRay.direction;
        Vector3 p2 = axisOrigin;
        Vector3 d2 = axisDirection;

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
}
