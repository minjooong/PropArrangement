using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic.Enum;
using UnityEngine;

namespace GameLogic.Building
{
    public class BuildingPathfinder
    {
        private GridBuildingSystem _system;

        public BuildingPathfinder(GridBuildingSystem system)
        {
            _system = system;
        }

        public List<Vector2> GetRandomPath()
        {
            List<Vector2> path = new List<Vector2>();
            List<Vector2Int> positionList = _system.map.GetAllTilePositionsEfficient(PlaceMode.MAIN_PROP);
            for (int i = 0; i < positionList.Count; i++)
            {
                if (_system.IsValidTile(positionList[i]))
                {
                    Vector2 worldPos = _system.map.GetCellCenterWorld(positionList[i]);
                    path.Add(worldPos);
                }
            }
            return path.GetShuffled();
        }

        public List<Vector2Int> GetRandomWayPointList(PlaceMode mode = PlaceMode.MAIN_PROP, int maxCount = 1)
        {
            List<Vector2Int> positionList = _system.map.GetSpecificTilePositions(_system.map.GetTileMap(mode), _system.white);
            List<Vector2Int> result = positionList.GetShuffled();
            return result.Where((t, i) => maxCount > i).ToList();
        }

        public Vector2Int GetRandomWayPointByCurrentPos(Vector2Int currentPos, PlaceMode mode = PlaceMode.MAIN_PROP)
        {
            if (_system.IsValidTile(currentPos, mode))
            {
                return currentPos;
            }
            
            List<Vector2Int> allValidPositions = _system.map.GetSpecificTilePositions(_system.map.GetTileMap(mode), _system.white);
            List<Vector2Int> validPositions = new List<Vector2Int>();
            
            foreach (var pos in allValidPositions)
            {
                if (_system.IsValidTile(pos, mode))
                {
                    validPositions.Add(pos);
                }
            }
            
            if (validPositions.Count == 0)
            {
                Debug.LogWarning("유효한 타일을 찾을 수 없습니다.");
                return currentPos;
            }
            
            Vector2Int nearestPos = validPositions[0];
            float minDistance = Vector2Int.Distance(currentPos, nearestPos);
            
            foreach (var pos in validPositions)
            {
                float distance = Vector2Int.Distance(currentPos, pos);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPos = pos;
                }
            }
            return nearestPos;
        }

        public List<Vector2> GenerateTourPath(Vector2Int start, PlaceMode mode = PlaceMode.MAIN_PROP)
        {
            List<Vector2> path = new List<Vector2>();
            HashSet<Vector2> pathSet = new HashSet<Vector2>();
            
            if (!_system.IsValidTile(start, mode))
            {
                start = GetRandomWayPointByCurrentPos(start, mode);
            }
            
            Vector2 entranceWorldPos = _system.map.GetCellCenterWorld(start);
            if (float.IsNaN(entranceWorldPos.x) || float.IsNaN(entranceWorldPos.y))
            {
                Debug.LogWarning("시작 좌표 변환 실패");
                return path;
            }
            
            if (pathSet.Add(entranceWorldPos))
            {
                path.Add(entranceWorldPos);
            }
        
            Vector2Int currentPoint = start;
            List<Vector2Int> waypoints = null;
            List<Vector2Int> pathToTarget = null;
            int count = 0;
            do
            {
                waypoints = GetRandomWayPointList(mode, 1);
                pathToTarget = FindPathToTarget(currentPoint, waypoints.First(), mode);
                count = pathToTarget?.Count ?? 0;
            } 
            while (count <= 1);
            
            foreach (var waypoint in waypoints)
            {
                Vector2Int targetPoint = FindNearestEmptyTile(waypoint, mode);
                if (pathToTarget != null)
                {
                    foreach (var point in pathToTarget)
                    {
                        Vector2 worldPos = _system.map.GetCellCenterWorld(point);
                        if (float.IsNaN(worldPos.x) || float.IsNaN(worldPos.y))
                            continue;
                        
                        if (pathSet.Add(worldPos))
                            path.Add(worldPos);
                    }
                    currentPoint = targetPoint;
                }
                else
                {
                    Debug.LogWarning($"경로 생성 실패: {currentPoint} -> {targetPoint}");
                }
            }
            return path;
        }

        public List<Vector2> GenerateTourEndPath(Vector2Int start, Vector2Int end, PlaceMode mode = PlaceMode.MAIN_PROP)
        {
            List<Vector2> path = new List<Vector2>();
            HashSet<Vector2> pathSet = new HashSet<Vector2>();
            
            if (!_system.IsValidTile(start, mode))
            {
                start = GetRandomWayPointByCurrentPos(start, mode);
            }
            
            Vector2 entranceWorldPos = _system.map.GetCellCenterWorld(start);
            if (float.IsNaN(entranceWorldPos.x) || float.IsNaN(entranceWorldPos.y))
            {
                Debug.LogWarning("시작 좌표 변환 실패");
                return path;
            }
            
            if (pathSet.Add(entranceWorldPos))
            {
                path.Add(entranceWorldPos);
            }
        
            Vector2Int currentPoint = start;
            Vector2Int targetPoint = FindNearestEmptyTile(end, mode);
        
            List<Vector2Int> pathToTarget = FindPathToTarget(currentPoint, targetPoint, mode);
            if (pathToTarget != null)
            {
                foreach (var point in pathToTarget)
                {
                    Vector2 worldPos = _system.map.GetCellCenterWorld(point);
                    if (float.IsNaN(worldPos.x) || float.IsNaN(worldPos.y))
                    {
                        Debug.LogWarning($"유효하지 않은 월드 좌표: {point}");
                        continue;
                    }
                        
                    if (pathSet.Add(worldPos))
                    {
                        path.Add(worldPos);
                    }
                }
                currentPoint = targetPoint;
            }
            else
            {
                Debug.LogWarning($"경로 생성 실패: {currentPoint} -> {targetPoint}");
            }
            return path;
        }

        private Vector2Int FindNearestEmptyTile(Vector2Int targetPoint, PlaceMode mode = PlaceMode.MAIN_PROP, int minDistance = 1)
        {
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0)
            };
        
            if (_system.IsValidTile(targetPoint, mode))
            {
                return targetPoint;
            }
        
            for (int distance = minDistance; distance < 10; distance++)
            {
                foreach (var dir in directions)
                {
                    Vector2Int checkPoint = targetPoint + new Vector2Int(dir.x * distance, dir.y * distance);
                    if (_system.IsValidTile(checkPoint, mode))
                    {
                        return checkPoint;
                    }
                }
            }
            return targetPoint;
        }

        private List<Vector2Int> FindPathToTarget(Vector2Int start, Vector2Int target, PlaceMode mode = PlaceMode.MAIN_PROP)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
        
            if (!_system.IsValidTile(start, mode) || !_system.IsValidTile(target, mode))
            {
                Debug.LogWarning($"유효하지 않은 시작점 또는 목표점: {start} -> {target}");
                return null;
            }
        
            queue.Enqueue(start);
            visited.Add(start);
            parent[start] = start;
        
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (current == target)
                {
                    Vector2Int pos = target;
                    while (pos != start)
                    {
                        path.Add(pos);
                        pos = parent[pos];
                    }
                    path.Add(start);
                    path.Reverse();
                    return path;
                }
        
                foreach (var next in GetAdjacentEmptyTiles(current, mode))
                {
                    if (visited.Add(next))
                    {
                        parent[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }
            return null;
        }

        private List<Vector2Int> GetAdjacentEmptyTiles(Vector2Int currentPos, PlaceMode mode = PlaceMode.MAIN_PROP)
        {
            List<Vector2Int> adjacentTiles = new List<Vector2Int>();
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0)
            };
            directions.Shuffle(); // Extension method call
            foreach (var dir in directions)
            {
                Vector2Int checkPos = currentPos + dir;
                if (_system.IsValidTile(checkPos, mode))
                {
                    adjacentTiles.Add(checkPos);
                }
            }
            return adjacentTiles;
        }
    }
}
