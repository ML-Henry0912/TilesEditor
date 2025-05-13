using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TilesEditor.iGizmo;

namespace TilesEditor
{
    public class GizmoBase : MonoBehaviour//, iGizmoBase
    {
        protected GizmoType type;

        protected bool isHovered = false;
        protected TransformGizmo gizmoRoot;
        protected Color baseColor;
        protected MaterialPropertyBlock propertyBlock;
        protected Camera cam;

        private void OnMouseEnter()
        {
            isHovered = true;
            //SetMaterialColor(Color.yellow);
        }

        private void OnMouseExit()
        {
            isHovered = false;
            SetMaterialColor(baseColor);
        }

        public void SetInvisible(bool value)
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = !value;
            }
        }

        public bool ShouldBeActive()
        {
            if (gizmoRoot == null) return false;
            return gizmoRoot.gizmoEnable[(int)type];
        }

        public bool IsHovered()
        {
            if (isHovered && ((iGizmo)this).ShouldBeActive())
            {
                SetMaterialColor(Color.yellow);
                return true;
            }
            else
            {
                return false;
            }

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


    }
}