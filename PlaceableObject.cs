using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Building
{
    /// <summary>
    /// 움직일 수 있는 오브젝트 클래스
    /// </summary>
    public class PlaceableObject : MonoBehaviour
    {
        [SerializeField] protected  SpriteRenderer         image;
        [SerializeField] private    PolygonCollider2D      col;
        
        public Vector3 offsetUp;
        
        public BoundsInt        area;
        public string           id;
        
        private Vector3 prevPos;
        private Vector3 startPos;
        private float deltaX;
        private float deltaY;
        
        public bool Placed { get; set; }

        protected virtual void Awake()
        {
            //오브젝트 Bounds 계산.
            Bounds bounds = this.col.bounds;
            this.offsetUp = new Vector3(0.0f,  bounds.max.y - bounds.min.y, 0.0f);
        }
        
        // private void LateUpdate()
        // {
        //     if (this.Placed)
        //         return;
        //     
        //     Vector3 position = this.transform.position - Camera.main.transform.position;
        //     //this.buttonsTr.transform.position = Camera.main.WorldToScreenPoint(PlaceableObject.cam.transform.TransformPoint(position));
        // }
        
        public void Initialize()
        {
            //canvasPrefab            = UnityEngine.Resources.Load<GameObject>(PlaceableObject.GenPath + "PlacementCanvas");
            //canvasExtendedPrefab    = UnityEngine.Resources.Load<GameObject>(PlaceableObject.GenPath + "ExtendedPlacementCanvas");
            //tagPrefab               = UnityEngine.Resources.Load<GameObject>(PlaceableObject.GenPath + "ItemTag");
            //cam                     = Camera.main;
        }
        public virtual void InitializeInstance(/*string id, ObjectData objectData*/)
        {
            //this.ID = id;
            //this.item = info;
            //this.transform.position = objectData.position;
        }
        public bool CanBePlace()
        {
            Vector3Int cell = GridBuildingSystem.current.gridLayout.LocalToCell(transform.position);
            BoundsInt areaTemp = area;
            areaTemp.position = cell;

            if (GridBuildingSystem.current.CanTakeArea(areaTemp))
                return true;

            return false;
            // return GridBuildingSystem.current.CanTakeArea(this.area with
            // {
            //     position = cell
            // });
        }
        
        // public bool CanPlace(Vector3 position)
        // {
        //     Vector3Int cell = GridBuildingSystem.current.gridLayout.LocalToCell(position);
        //     return GridBuildingSystem.current.CanTakeArea(this.area with
        //     {
        //         position = cell
        //     });
        // }
        
        public virtual void Place()
        {
            Vector3Int cell = GridBuildingSystem.current.gridLayout.LocalToCell(transform.position);
            BoundsInt areaTemp = this.area;
            areaTemp.position = cell;
            this.Placed = true;
            
            //this.sr.color = Color.white;
            //this.data.position = this.transform.position;
            GridBuildingSystem.current.TakeArea(areaTemp);
        }
        
        private void OnMouseDown()
        {
            //Debug.Log(Placed);
            if (this.Placed)
                return;
            
            //PanZoom.current.enabled = false;
            this.startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            this.deltaX = this.startPos.x - this.transform.position.x;
            this.deltaY = this.startPos.y - this.transform.position.y;
        }
        private void OnMouseDrag()
        {
            if (!this.Placed)
            {
                Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                // this.transform.position = new Vector3(Mathf.Clamp(worldPoint.x - this.deltaX, PanZoom.current.leftLimit, PanZoom.current.rightLimit)
                //     , Mathf.Clamp(worldPoint.y - this.deltaY, PanZoom.current.bottomLimit, PanZoom.current.upperLimit)
                //     , this.transform.position.z);
                this.transform.position = new Vector3(worldPoint.x - this.deltaX,worldPoint.y - this.deltaY, this.transform.position.z);
                //Debug.Log(transform.position);
                
                this.Follow();
            }
            else
            {
                if (this.startPos == Input.mousePosition)
                    return;
                
                //this.OnDeselect();
            }
        }
        
        private void OnMouseUp()
        {
            if (this.Placed)
                return;
            
            Vector3Int cell = GridBuildingSystem.current.gridLayout.LocalToCell(this.transform.position);
            this.transform.localPosition = GridBuildingSystem.current.gridLayout.CellToLocalInterpolated((Vector3) cell + new Vector3(0.5f, 0.5f, 0.0f));
            //PanZoom.current.enabled = true;

            Place();
        }
        
        public void Follow()
        {
            this.area.position = GridBuildingSystem.current.gridLayout.LocalToCell(this.transform.position);
            GridBuildingSystem.current.FollowBuilding(this.area);
        }
        
        public void First()
        {
            this.Follow();
            //this.InstantiateButtons();
            this.prevPos = this.transform.position;
        }
    }
}

