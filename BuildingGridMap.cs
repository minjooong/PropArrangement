using System;
using System.Collections.Generic;
using GameLogic.Enum;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameLogic.Building
{
    public class BuildingGridMap
    {
        private GridBuildingSystem _system;

        public BuildingGridMap(GridBuildingSystem system)
        {
            _system = system;
        }

        public Tilemap GetTileMap(PlaceMode mode)
        {
            return GridBuildingSystem.tileMaps.GetValueOrDefault(mode);
        }

        public Transform GetRoot(PlaceMode mode)
        {
            // Providing direct access or method access if rootMap was public or internal.
            // As rootMap is private in original, we might need a getter in System or move rootMap here.
            // For now, assuming we will expose it or move it.
            // Let's assume we will move the logic here but since rootMap is serialized/init in System, 
            // we will access it via System if we can, or we copies references.
            // Actually, best is if GridBuildingSystem exposes a way.
            // But wating to modify GridBuildingSystem means we have dependency cycle.
            // Let's rely on `_system.GetRoot(mode)` which we will add, or public fields.
            return null; 
        }

        public Vector2 GetCellCenterWorld(Vector2Int cell)
        {
            Vector3 local = _system.gridLayout.CellToLocalInterpolated((Vector3)new Vector3Int(cell.x, cell.y, 0) + new Vector3(0.5f, 0.5f, 0.0f));
            Vector3 world = _system.gridLayout.transform.TransformPoint(local);
            return new Vector2(world.x, world.y);
        }

        public int GetAllTilePositionsCount(PlaceMode mode)
        {
            int count = 0;
            
            Tilemap tilemap = GetTileMap(mode);
            BoundsInt bounds = tilemap.cellBounds;
            BoundsInt bounds2D = new BoundsInt(bounds.xMin, bounds.yMin, 0, bounds.size.x, bounds.size.y, 1);
            TileBase[] allTiles = tilemap.GetTilesBlock(bounds2D);
            
            int index = 0;
            for (int y = bounds2D.yMin; y < bounds2D.yMax; y++)
            {
                for (int x = bounds2D.xMin; x < bounds2D.xMax; x++)
                {
                    if (allTiles[index] != null)
                    {
                        count++;
                    }
                    index++;
                }
            }
            return count / 4;
        }

        public List<Vector2Int> GetAllTilePositionsEfficient(PlaceMode mode)
        {
            Tilemap tilemap = GetTileMap(mode);
            List<Vector2Int> positions = new List<Vector2Int>();
            BoundsInt bounds = tilemap.cellBounds;
            BoundsInt bounds2D = new BoundsInt(bounds.xMin, bounds.yMin, 0, bounds.size.x, bounds.size.y, 1);
            TileBase[] allTiles = tilemap.GetTilesBlock(bounds2D);
            
            int index = 0;
            for (int y = bounds2D.yMin; y < bounds2D.yMax; y++)
            {
                for (int x = bounds2D.xMin; x < bounds2D.xMax; x++)
                {
                    if (allTiles[index] != null)
                    {
                        if (_system.IsValidTile(new Vector2Int(x, y), mode)) // Callback to system for full validity check including props
                            positions.Add(new Vector2Int(x, y));
                    }
                    index++;
                }
            }
            return positions;
        }
        
        public List<Vector2Int> GetSpecificTilePositions(Tilemap tilemap, TileBase targetTile)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            BoundsInt bounds = tilemap.cellBounds;
        
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    TileBase currentTile = tilemap.GetTile(position);
                
                    if (currentTile == targetTile)
                        positions.Add(new Vector2Int(x, y));
                }
            }
        
            return positions;
        }
    }
}
