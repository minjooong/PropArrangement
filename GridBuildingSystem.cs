using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameLogic.Enum;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameLogic.Building
{
    /// <summary>
    /// 건설 시스템 관리 클래스
    /// </summary>
    public class GridBuildingSystem : MonoBehaviour
    {
        public static GridBuildingSystem current;
        
        [Header("Grid Layout")]
        public GridLayout                           gridLayout;

        [Header("GameObject Root")] 
        public Transform                            propMainRoot;
        public Transform                            propRoomRoot;
        public Transform                            propGarretRoot;
        public Transform                            propBasementRoot;
        public Transform                            tileMainRoot;
        public Transform                            tileRoomRoot;
        public Transform                            tileGarretRoot;
        public Transform                            tileBasementRoot;
        
        [Header("GameObject Dummy Root")] 
        public Transform                            propMainDummyRoot;
        public Transform                            propRoomDummyRoot;
        public Transform                            propGarretDummyRoot;
        public Transform                            propBasementDummyRoot;
        public Transform                            tileMainDummyRoot;
        public Transform                            tileRoomDummyRoot;
        public Transform                            tileGarretDummyRoot;
        public Transform                            tileBasementDummyRoot;
        
        [Header("Tile map")]
        public Tilemap                              tempTilemap;
        public Tilemap                              mainTilemap;
        public Tilemap                              roomTilemap;
        public Tilemap                              garretTilemap;
        public Tilemap                              basementTilemap;
        public Tilemap                              mainFloorTilemap;
        public Tilemap                              roomFloorTilemap;
        public Tilemap                              garretFloorTilemap;
        public Tilemap                              basementFloorTilemap;
        public Tilemap                              outsideTilemap;
        
        [Header("Tile Base")]
        public TileBase white;
        public TileBase green;
        public TileBase red;
        public TileBase yellow;
        
        public static Dictionary<TileType, TileBase>    tileBases               = new Dictionary<TileType, TileBase>();
        public static Dictionary<PlaceMode, Tilemap>    tileMaps                = new Dictionary<PlaceMode, Tilemap>();
        
        // Sub-components
        public BuildingGridMap map;
        public BuildingPropData data;
        public BuildingPlacement placement;
        public BuildingPathfinder pathfinder;

        // Delegates to sub-components
        public Dictionary<PlaceMode, List<PropObjectData>> placedProps => data.placedProps;
        public PlaceableObject temp
        {
            get => placement.Temp;
            set => placement.Temp = value;
        }

        private Dictionary<PlaceMode, Transform> rootMap = new Dictionary<PlaceMode, Transform>();
        private Dictionary<PlaceMode, Transform> dummyRootMap = new Dictionary<PlaceMode, Transform>();
        
        public List<PlaceableObject> dummyList = new List<PlaceableObject>();
        
        private bool                                initialized;
        // private BoundsInt                           prevArea; // Moved to Placement
        private Vector3                             prevPos;
        private Vector3                             mousePosition;
        
        public PlaceMode    currentMode     { get; set; } = PlaceMode.MAIN_PROP;
        public bool         isPlaceMode     
        { 
            get => placement.IsPlaceMode; 
            set => placement.IsPlaceMode = value; 
        }
        public bool         isFriendMode    { get; set; } = false;
        public bool         isPresetMode    { get; set; } = false;
        public bool         isFriendOpenRoom    { get; set; } = false;
        public bool         isFriendOpenGarret    { get; set; } = false;
        public bool         isFriendOpenBasement    { get; set; } = false;
        public GameProgressManager progressMgr;
        
        private void Awake()
        {
            current                 = this;
            
            // Initialize sub-components
            map = new BuildingGridMap(this);
            data = new BuildingPropData(this);
            placement = new BuildingPlacement(this);
            pathfinder = new BuildingPathfinder(this);

            tileBases       .Clear();
            tileMaps        .Clear();
            
            tileBases.Add(TileType.EMPTY    , (TileBase) null);
            tileBases.Add(TileType.WHITE    , white);
            tileBases.Add(TileType.GREEN    , green);
            tileBases.Add(TileType.RED      , red);
            tileBases.Add(TileType.YELLOW   , yellow);

            //배치 모드에 따라 타일 맵 딕셔너리 추가
            tileMaps.Add(PlaceMode.MAIN_PROP        , mainTilemap);
            tileMaps.Add(PlaceMode.ROOM_PROP        , roomTilemap);
            tileMaps.Add(PlaceMode.GARRET_PROP      , garretTilemap);
            tileMaps.Add(PlaceMode.BASEMENT_PROP    , basementTilemap);
            tileMaps.Add(PlaceMode.MAIN_OUTSIDE     , outsideTilemap);
            tileMaps.Add(PlaceMode.MAIN_GROUND      , mainFloorTilemap);
            tileMaps.Add(PlaceMode.ROOM_GROUND      , roomFloorTilemap);
            tileMaps.Add(PlaceMode.GARRET_GROUND    , garretFloorTilemap);
            tileMaps.Add(PlaceMode.BASEMENT_GROUND  , basementFloorTilemap);
            
            // Root Map 초기화
            rootMap.Clear();
            dummyRootMap.Clear();

            void AddRoot(PlaceMode mode, Transform root, Transform dummy)
            {
                rootMap[mode] = root;
                dummyRootMap[mode] = dummy;
            }

            AddRoot(PlaceMode.MAIN_PROP, propMainRoot, propMainDummyRoot);
            AddRoot(PlaceMode.MAIN_OUTSIDE, propMainRoot, propMainDummyRoot);
            AddRoot(PlaceMode.ROOM_PROP, propRoomRoot, propRoomDummyRoot);
            AddRoot(PlaceMode.GARRET_PROP, propGarretRoot, propGarretDummyRoot);
            AddRoot(PlaceMode.BASEMENT_PROP, propBasementRoot, propBasementDummyRoot);
            AddRoot(PlaceMode.MAIN_GROUND, tileMainRoot, tileMainDummyRoot);
            AddRoot(PlaceMode.ROOM_GROUND, tileRoomRoot, tileRoomDummyRoot);
            AddRoot(PlaceMode.GARRET_GROUND, tileGarretRoot, tileGarretDummyRoot);
            AddRoot(PlaceMode.BASEMENT_GROUND, tileBasementRoot, tileBasementDummyRoot);

            // data.InitializeDictionary() is called in constructor
        }

        #region [PROP LOGIC]

        public void StartPlaceProp(PlaceMode mode, PropState state, int order, bool isTrying = false, Vector3? position = null)
        {
            placement.StartPlaceProp(mode, state, order, isTrying, position);
        }

        public void SetAlpha(PlaceMode mode, PropState state, int order,  bool isTransparent)
        {
            if (mode == PlaceMode.MAIN_GROUND || mode == PlaceMode.BASEMENT_GROUND
                || mode == PlaceMode.ROOM_GROUND || mode == PlaceMode.GARRET_GROUND)
                return;
            
            if (!data.placedProps.TryGetValue(mode, out List<PropObjectData> list))
                return;

            foreach (PropObjectData objectData in list)
            {
                if (objectData.activeProp.ItemId == state.ItemId && objectData.activeProp.Order == order) continue;
                objectData?.viewer.SetAlpha(isTransparent ? 0.5f : 1f);
            }
        }

        public void SetAllProp(PlaceMode mode, PropState state)
        {
            List<ActivePropState> activeList = state.ActivePropStates.Where(x => !x.Active).ToList();
            if (activeList.Count == 0) return;

            Tilemap tilemap = map.GetTileMap(mode);
            BoundsInt bounds = tilemap.cellBounds;

            int placedCount = 0;

            for (int y = bounds.yMin; y <= bounds.yMax - 2 && placedCount < activeList.Count; y += 1)
            {
                for (int x = bounds.xMin; x <= bounds.xMax - 2 && placedCount < activeList.Count; x += 1)
                {
                    Vector2Int anchor = new Vector2Int(x, y);
                    if (!IsValid2By2Block(anchor, mode)) // Helper needed
                        continue;

                    // Instantiation
                    temp = UnityEngine.Object.Instantiate<GameObject>(state.Definition.clientData.Pref, Vector3.zero, Quaternion.identity)
                        .GetComponent<PlaceableObject>();

                    if (rootMap.TryGetValue(mode, out Transform parent))
                    {
                        temp.transform.SetParent(parent);
                    }

                    temp.transform.localScale = Vector3.one;
                    Vector3Int cellPos = new Vector3Int(anchor.x, anchor.y, 0);
                    temp.transform.localPosition = gridLayout.CellToLocalInterpolated((Vector3)cellPos + new Vector3(0.5f, 0.5f, 0.0f));

                    temp.Load(mode, state, activeList[placedCount].Order, false, false);
                    temp.Place(temp.transform.position);

                    progressMgr.UpdateProp(temp.State, temp.Order, PropUpdateType.PLACE, temp.Mode, temp.transform.position, temp.isFlip);

                    placedCount++;
                }
            }

            temp = null;
        }
        
        public void SetPropView(PlaceMode mode, bool active)
        {
            PlaceMode? targetPropMode = mode switch
            {
                PlaceMode.MAIN_GROUND => PlaceMode.MAIN_PROP,
                PlaceMode.ROOM_GROUND => PlaceMode.ROOM_PROP,
                PlaceMode.GARRET_GROUND => PlaceMode.GARRET_PROP,
                PlaceMode.BASEMENT_GROUND => PlaceMode.BASEMENT_PROP,
                _ => null
            };

            if (targetPropMode.HasValue && rootMap.TryGetValue(targetPropMode.Value, out Transform root))
            {
                root.gameObject.SetActive(active);
            }
        }
        
        public void FollowProp(PlaceMode mode, BoundsInt propArea) => placement.FollowProp(mode, propArea);

        public bool CanTakeArea(PlaceMode mode, BoundsInt area, bool canOverlap = false) => placement.CanTakeArea(mode, area, canOverlap);
        
        public void TakeArea(PlaceMode mode, BoundsInt area) => placement.TakeArea(mode, area);
        
        public void FindAndReleaseOverlappingProps(PlaceMode mode, BoundsInt area, PropState state, int order)
        {
            if (!data.placedProps.TryGetValue(mode, out List<PropObjectData> list))
                return;

            var toRelease = new List<PropObjectData>();

            foreach (var prop in list)
            {
                if (prop.activeProp.ItemId == state.ItemId && prop.activeProp.Order == order) continue;

                BoundsInt propArea = prop.viewer.area;
                if (!(propArea.xMax <= area.xMin ||
                      propArea.xMin >= area.xMax ||
                      propArea.yMax <= area.yMin ||
                      propArea.yMin >= area.yMax))
                    toRelease.Add(prop);
            }

            foreach (var prop in toRelease)
            {
                prop.viewer.Release();
                progressMgr.UpdateProp(prop.viewer.State, prop.activeProp.Order, PropUpdateType.RELEASE);
            }
        }

        public bool BlockEntrance => !IsValidTile(new Vector2Int(6, -12));
        public void DestroyProp(PlaceMode mode, BoundsInt area)
        {
             SetTilesBlock(area, TileType.WHITE, map.GetTileMap(mode));
        } 
        public void ClearArea() => placement.ClearArea();
        public void DeSelectProp() => placement.DeSelectProp();

        public void SetAllColliderActive(PlaceMode mode, bool active)
        {
            if (!data.placedProps.TryGetValue(mode, out List<PropObjectData> list)) 
                return;

            foreach (PropObjectData objectData in list)
            {
                objectData?.viewer.SetCollider(active);
            }
        }

        private void SetTilesBlock(BoundsInt area, TileType type, Tilemap tilemap)
        {
            int size                    = area.size.x * area.size.y * area.size.z;
            TileBase[] tileBaseArray    = new TileBase[size];
            FillTiles(tileBaseArray, type);
            tilemap.SetTilesBlock(area, tileBaseArray);
        }

        private static void FillTiles(TileBase[] arr, TileType type)
        {
             TileBase t = tileBases[type];
             for (int index = 0; index < arr.Length; ++index)
                arr[index] = t;
        }
        
        public void LoadProps()
        {
            // Re-init dict keys as in original LoadProps?
            // BuildingPropData handles init. Clearing lists is enough.
            foreach(var l in data.placedProps.Values) l.Clear();
            
            IList<PropState> list = GameManager.Instance.Progress.buildingLogic.GetKnownPropStates();
            for (int i = 0; i < list.Count; i++)
            {
                PropState state = list[i];
                if (state.Definition.infoData.PlaceMap == PropPlaceType.DEFAULT)
                    continue;
                
                for (int j = 0; j < state.ActivePropStates.Count; j++)
                {
                    ActivePropState activePropState =  state.ActivePropStates[j];
                    if (!activePropState.Active)
                        continue;
                    
                    GameObject prefab = state.Definition.clientData.Pref;
                    PlaceableObject placeableObject = UnityEngine.Object.Instantiate<GameObject>(original: prefab, Vector3.zero, Quaternion.identity).GetComponent<PlaceableObject>();
                    
                    if (rootMap.TryGetValue(activePropState.Place, out Transform parent))
                    {
                        placeableObject.transform.SetParent(parent);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(activePropState.Place), activePropState.Place, null);
                    }
                    
                    placeableObject.transform.localScale   = Vector3.one;
                    placeableObject.Load(activePropState.Place, state, activePropState.Order, activePropState.IsRotation, activePropState.IsTrash);

                    if (activePropState.Position != null)
                    {
                        placeableObject.transform.position = activePropState.Position.Value;
                        placeableObject.Place(activePropState.Position.Value, false);
                    }
                }
            }
        }

        public void ClearFriendProps()
        {
            for (int i = 0; i < dummyList.Count; i++)
                Destroy(dummyList[i].gameObject);
            
            dummyList.Clear();
        }
        public void SetFriendActive(bool active)
        {
            foreach (var root in rootMap.Values.Distinct())
            {
                if (root != null) root.gameObject.SetActive(!active);
            }

            foreach (var root in dummyRootMap.Values.Distinct())
            {
                if (root != null) root.gameObject.SetActive(active);
            }
        }
        public void LoadFriendProp(List<PropContentsData> list)
        {
            ClearFriendProps();
            
            for (int i = 0; i < list.Count; i++)
            {
                PropContentsData contentsData = list[i];
                
                if (!contentsData.active)
                    continue;
                PropDefinition definition       = progressMgr.CurrentProgress.Building.Config.TryGetPropDefinition(contentsData.itemId);
                PlaceableObject placeableObject = UnityEngine.Object.Instantiate<GameObject>(definition.clientData.Pref, Vector3.zero, Quaternion.identity)
                    .GetComponent<PlaceableObject>();

                if (dummyRootMap.TryGetValue(contentsData.place, out Transform parent))
                {
                    placeableObject.transform.SetParent(parent);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(contentsData.place), contentsData.place, null);
                }
                placeableObject.transform.localScale   = Vector3.one;
                placeableObject.DummyLoad(contentsData.isRotation); 
                Vector3 pos = new Vector3(contentsData.posX, contentsData.posY, 0);
        
                placeableObject.transform.position = pos;
                
                placeableObject.DummyLoad2();
                placeableObject.FriendLoad();
                dummyList.Add(placeableObject);
            }
            
        }
        
        public void LoadPresetProp(PresetData presetData)
        {
            List<PropContentsData> list = presetData.propContents;
            for (int i = 0; i < list.Count; i++)
            {
                PropContentsData contentsData = list[i];
                
                if (!contentsData.active)
                    continue;

                PropState state = progressMgr.CurrentProgress.Building.TryGetKnownPropState(contentsData.itemId);
                ActivePropState activePropState = state.ActivePropStates.First(x => !x.Active);

                progressMgr.UpdateProp(state, activePropState.Order, PropUpdateType.PLACE
                    , contentsData.place, new Vector3(contentsData.posX, contentsData.posY, 0), contentsData.isRotation);
            }
            LoadProps();
        }
        
        public void AddProp(PlaceMode mode, PropObjectData data) => this.data.AddProp(mode, data);
        public void RemoveProp(PlaceMode mode, int id, int order) => this.data.RemoveProp(mode, id, order);

        public int GetAllTilePositionsCount(PlaceMode mode) => map.GetAllTilePositionsCount(mode);
        
        #endregion

        #region [ROAD]
        
        public List<Vector2> GetRandomPath() => pathfinder.GetRandomPath();
        public List<Vector2Int> GetRandomWayPointList(PlaceMode mode = PlaceMode.MAIN_PROP, int maxCount =  1) => pathfinder.GetRandomWayPointList(mode, maxCount);
        public Vector2Int GetRandomWayPointByCurrentPos(Vector2Int currentPos,  PlaceMode mode = PlaceMode.MAIN_PROP) => pathfinder.GetRandomWayPointByCurrentPos(currentPos, mode);

        public bool IsValidTile(Vector2Int point, PlaceMode mode =  PlaceMode.MAIN_PROP)
        {
            // 타일맵 경계 확인
            BoundsInt bounds = map.GetTileMap(mode).cellBounds;
            if (point.x < bounds.xMin || point.x >= bounds.xMax ||
                point.y < bounds.yMin || point.y >= bounds.yMax)
            {
                return false;
            }
            
            if (!data.placedProps.TryGetValue(mode, out List<PropObjectData> list)) 
                return false;
            
            // 가구가 있는지 확인
            foreach (var prop in list)
            {
                if (prop == null) continue;
                
                Vector3Int cell = gridLayout.LocalToCell(prop.viewer.transform.position);
                BoundsInt area = prop.viewer.area;
        
                if (point.x >= cell.x && point.x < cell.x + area.size.x &&
                    point.y >= cell.y && point.y < cell.y + area.size.y)
                {
                    return false;
                }
            }
            
            Vector3Int position     = new Vector3Int(point.x, point.y, 0);
            TileBase currentTile    = map.GetTileMap(mode).GetTile(position);
            return currentTile == white;
        }

        private bool IsValid2By2Block(Vector2Int anchor, PlaceMode mode)
        {
            Vector2Int[] cells = new Vector2Int[]
            {
                new Vector2Int(anchor.x, anchor.y),
                new Vector2Int(anchor.x + 1, anchor.y),
                new Vector2Int(anchor.x, anchor.y + 1),
                new Vector2Int(anchor.x + 1, anchor.y + 1)
            };

            for (int i = 0; i < cells.Length; i++)
            {
                if (!IsValidTile(cells[i], mode))
                    return false;
            }
            return true;
        }

        public List<Vector2> GenerateTourPath(Vector2Int start, PlaceMode mode = PlaceMode.MAIN_PROP) => pathfinder.GenerateTourPath(start, mode);
        public List<Vector2> GenerateTourEndPath(Vector2Int start, Vector2Int end, PlaceMode mode = PlaceMode.MAIN_PROP) => pathfinder.GenerateTourEndPath(start, end, mode);
        
        #endregion
        
        public List<DisplayablePropView> GetDisplayablePropView(PlaceMode mode)
        {
            List<DisplayablePropView> result = new List<DisplayablePropView>();
            if (!data.placedProps.TryGetValue(mode, out List<PropObjectData> list)) 
                return result;
            
            foreach (var prop in list)
            {
                if (prop.viewer is not DisplayablePropView displayablePropView)
                    continue;

                result.Add(displayablePropView);
            }
            
            return result;
        }
        public List<DisplayablePropView> GetDisplayablePropViewByReadyStatus(PlaceMode mode)
        {
            List<DisplayablePropView> result = new List<DisplayablePropView>();
            if (!data.placedProps.TryGetValue(mode, out List<PropObjectData> list)) 
                return result;
            
            foreach (var prop in list)
            {
                if (prop.viewer is not DisplayablePropView displayablePropView)
                    continue;

                if (displayablePropView.IsPlacingBook())
                    result.Add(displayablePropView);
            }
            
            return result;
        }
        public int GetDisplayablePropViewCountByReadyStatus() => GetDisplayablePropViewByReadyStatus(currentMode).Count;
        
        public void AllDisplayablePropViewHideBookInfo()
        {
            List<DisplayablePropView> displayablePropViews = GetDisplayablePropViewByReadyStatus(currentMode);
            for (int i = 0; i < displayablePropViews.Count; i++)
            {
                displayablePropViews[i].HideBookInfoView();
            }
            
        }
        public DisplayablePropView GetDisplayablePropView(ActivePropState propState)
        {
            foreach (var list in data.placedProps.Values)
            {
                foreach (var prop in list)
                {
                    if (prop.activeProp.ItemId == propState.ItemId && prop.activeProp.Order == propState.Order)
                        return (DisplayablePropView)prop.viewer;
                }
            }

            return null;
        }

        public bool GetRoot(PlaceMode mode, out Transform root) => rootMap.TryGetValue(mode, out root);
    }
    
    public enum TileType
    {
        EMPTY,
        WHITE,
        GREEN,
        RED,
        YELLOW,
    }
}
