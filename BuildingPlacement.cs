using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic.Enum;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameLogic.Building
{
    public class BuildingPlacement
    {
        private GridBuildingSystem _system;
        
        // State extracted from GridBuildingSystem
        public PlaceableObject Temp { get; set; }
        public bool IsPlaceMode { get; set; } = false;
        private BoundsInt prevArea;

        public BuildingPlacement(GridBuildingSystem system)
        {
            _system = system;
        }

        public void StartPlaceProp(PlaceMode mode, PropState state, int order, bool isTrying = false, Vector3? position = null)
        {
            IsPlaceMode = true;
  
            // Position Logic
            Vector3 screenCenterInWorld = PanZoom.current.targetCamera.ViewportToWorldPoint(new Vector3(0.55f, 0.425f, 0));
            Vector3 startPos = position ?? new Vector3(screenCenterInWorld.x, screenCenterInWorld.y, 0f);

            // Instantiate
            GameObject prefab = state.Definition.clientData.Pref;
            GameObject go = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            Temp = go.GetComponent<PlaceableObject>();

            // Parent
            if (_system.GetRoot(mode, out Transform parent)) // Assuming we add GetRoot helper to System
            {
                Temp.transform.SetParent(parent);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
            
            // Snap to grid
            Vector3Int cell = _system.gridLayout.LocalToCell(startPos);
            Vector2Int nearest = FindNearestPlacableArea(new Vector2Int(cell.x, cell.y), mode, Temp.area);
            Vector2 worldPos = _system.gridLayout.CellToWorld(new Vector3Int(nearest.x, nearest.y, 0));
            Temp.transform.position = worldPos;
            Temp.transform.localScale = Vector3.one;
            
            // Init
            Temp.Init(mode, state, order, isTrying);
            
            // Hide UI bubbles
            List<DisplayablePropView> propViews = _system.GetDisplayablePropView(_system.currentMode);
            for (int i = 0; i < propViews.Count; i++)
                propViews[i].HideBubbleView();
        }

        public void FollowProp(PlaceMode mode, BoundsInt propArea)
        {
            Tilemap tilemap = _system.map.GetTileMap(mode);
            TileBase[] tilesBlock = GetTilesBlock(propArea, tilemap);
            TileBase[] tileBaseArray = new TileBase[tilesBlock.Length];
            
            ClearArea();
            
            if (Temp != null)
                Temp.ActiveColor();
            else
                Debug.LogWarning("Temp 오브젝트가 없습니다...");
            
            for (int i = 0; i < tilesBlock.Length; ++i)
            {
                if (tilesBlock[i] == _system.white)
                    tileBaseArray[i] = _system.green;
                else if (tilesBlock[i] == _system.red)
                {
                    FillTiles(tileBaseArray, TileType.YELLOW);
                    break;
                }
                else
                {
                    FillTiles(tileBaseArray, TileType.RED);
                    if (Temp != null)
                        Temp.DeActiveColor();
                    else
                        Debug.LogWarning("Temp 오브젝트가 없습니다...");
                    
                    break;
                }
            }
            _system.tempTilemap.SetTilesBlock(propArea, tileBaseArray);
            prevArea = propArea;
        }

        public bool CanTakeArea(PlaceMode mode, BoundsInt area, bool canOverlap = false)
        {
            Tilemap tilemap = _system.map.GetTileMap(mode);
            TileBase[] baseArray = GetTilesBlock(area, tilemap);
            
            if (baseArray.All(tileBase => tileBase == _system.white)) 
                return true;
            if (canOverlap && baseArray.All(tileBase =>
                    ( tileBase == _system.white || tileBase == _system.red))) 
                return true;
            
            return false;
        }

        public void TakeArea(PlaceMode mode, BoundsInt area)
        {
             // isPlaceMode = false; // Original commented out
            Tilemap tilemap = _system.map.GetTileMap(mode);
            tilemap.gameObject.SetActive(false);
            
            SetTilesBlock(area, TileType.EMPTY, _system.tempTilemap);
            SetTilesBlock(area, TileType.RED, tilemap);
        }

        public void ClearArea()
        {
             SetTilesBlock(prevArea, TileType.EMPTY, _system.tempTilemap);
        }

        public void DeSelectProp()
        {
            Temp?.OnDeselect();
            Temp = null;
        }

        // Helper methods...
        private Vector2Int FindNearestPlacableArea(Vector2Int targetPoint, PlaceMode mode, BoundsInt templateArea, int maxSearchRadius = 50)
        {
            // Accessing _system.map.GetTileMap(mode)
            Tilemap tilemap = _system.map.GetTileMap(mode);
			BoundsInt bounds = tilemap.cellBounds;

			{
				BoundsInt candidate = new BoundsInt(new Vector3Int(targetPoint.x, targetPoint.y, 0), templateArea.size);
				if (CanTakeArea(mode, candidate))
					return targetPoint;
			}

            // Search logic
			for (int r = 1; r <= maxSearchRadius; r++)
			{
				for (int dx = -r; dx <= r; dx++)
				{
					int dyAbs = r - Math.Abs(dx);
					for (int sign = dyAbs == 0 ? 0 : -1; sign <= 1; sign += (dyAbs == 0 ? 2 : 2))
					{
						int dy = (dyAbs == 0) ? 0 : dyAbs * (sign < 0 ? -1 : 1);
						int cx = targetPoint.x + dx;
						int cy = targetPoint.y + dy;

						int areaXMin = cx;
						int areaYMin = cy;
						int areaXMax = cx + templateArea.size.x; 
						int areaYMax = cy + templateArea.size.y; 
						if (areaXMin < bounds.xMin || areaYMin < bounds.yMin ||
							areaXMax > bounds.xMax || areaYMax > bounds.yMax)
						{
							continue;
						}

						BoundsInt candidate = new BoundsInt(new Vector3Int(cx, cy, 0), templateArea.size);
						if (CanTakeArea(mode, candidate))
						{
							return new Vector2Int(cx, cy);
						}
					}
				}
			}

            if (mode == PlaceMode.MAIN_GROUND || mode == PlaceMode.ROOM_GROUND 
                || mode == PlaceMode.GARRET_GROUND || mode == PlaceMode.BASEMENT_GROUND)
            {
                return targetPoint + new Vector2Int(-2, 0);
            }
			return targetPoint;
        }

        private TileBase[] GetTilesBlock(BoundsInt area, Tilemap tilemap)
        {
             // Reuse System helper or duplicate simple logic
            TileBase[] array = new TileBase[area.size.x * area.size.y * area.size.z];
            int counter = 0;
            foreach (var v in area.allPositionsWithin)
            {
                Vector3Int pos = new Vector3Int(v.x, v.y, 0);
                array[counter] = tilemap.GetTile(pos);
                counter++;
            }
            return array;
        }

        private void SetTilesBlock(BoundsInt area, TileType type, Tilemap tilemap)
        {
            int size = area.size.x * area.size.y * area.size.z;
            TileBase[] tileBaseArray = new TileBase[size];
            FillTiles(tileBaseArray, type);
            tilemap.SetTilesBlock(area, tileBaseArray);
        }

        private void FillTiles(TileBase[] arr, TileType type)
        {
            // Accessing static Dict from System
            TileBase tile = GridBuildingSystem.tileBases[type];
            for (int index = 0; index < arr.Length; ++index)
                arr[index] = tile;
        }
    }
}
