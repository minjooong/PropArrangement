using System.Collections;
using System.Collections.Generic;
using GameLogic.Core;
using GameLogic.Enum;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic.Building
{
    /// <summary>
    /// 이동형 가구 뷰어
    /// </summary>
    public class MovablePropView : PlaceableObject
    {
        private bool isOnButton = false;
        private Vector3 checkPos = Vector3.zero;
        public override void OnMouseDown()
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                isOnButton = true;
                return;
            }

            base.OnMouseDown();
        }

        public override void HandleMouseDown()
        {
            if (ActionType == PropActionType.PROCESSING)
                PanZoom.current.enabled = false;
            
            this.startPos   = PanZoom.current.targetCamera.ScreenToWorldPoint(Input.mousePosition);
            checkPos        = this.area.position;
            this.deltaX     = this.startPos.x - this.transform.position.x;
            this.deltaY     = this.startPos.y - this.transform.position.y;
        }

        public override void OnMouseDrag()
        {
            if (isOnButton)
                return;

            base.OnMouseDrag();
        }
        public override void HandleMouseDrag()
        {
            if (ActionType != PropActionType.PROCESSING)
                return;
            
            // image.sortingOrder = 10000;
           
            
            Vector3 worldPoint      = PanZoom.current.targetCamera.ScreenToWorldPoint(Input.mousePosition);
            this.transform.position = new Vector3(Mathf.Clamp(worldPoint.x - this.deltaX, PanZoom.current.leftLimit, PanZoom.current.rightLimit)
                , Mathf.Clamp(worldPoint.y - this.deltaY, PanZoom.current.bottomLimit, PanZoom.current.upperLimit)
                , this.transform.position.z);

            if (editMode != null)
            {
                if (checkPos != this.area.position)
                {
                    editMode.SetAlpha(0f);
                    //checkPos = Vector3.one * 1000f;
                }
                   
            }
               
            
            this.Follow();
        }

        public override void HandleMouseUp()
        {
            if (ActionType != PropActionType.PROCESSING)
                return;
            
            if (editMode != null)
                editMode.SetAlpha(1f);
            
            PanZoom.current.enabled = true;
            Vector3Int cell                 = GridBuildingSystem.current.gridLayout.LocalToCell(this.transform.position);
            this.transform.localPosition    = GridBuildingSystem.current.gridLayout.CellToLocalInterpolated((Vector3) cell + new Vector3(0.5f, 0.5f, 0.0f));
            
            isOnButton = false;
        }
    }
}

