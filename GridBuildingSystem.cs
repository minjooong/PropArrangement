using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;


//[수]
//TODO: 건물 설정 UI
//TODO: 데이터 저장 및 실 데이터 설정
//TODO: 건물 프리팹
//TODO: Office Logic

//[토]
//TODO: 수집 기능 추가
//TODO: 데이터 저장
//TODO: EDIT MODE
//TODO: 길 생성
//TODO: 캐릭터 움직임 설정
namespace GameLogic.Building
{
    /// <summary>
    /// 그리드 건설 시스템 관리 클래스
    /// </summary>
    public class GridBuildingSystem : MonoBehaviour
    {
        public static GridBuildingSystem current;
        
        public GridLayout                        gridLayout;
        public Tilemap                           MainTilemap;
        public Tilemap                           TempTilemap;

        private BoundsInt                               prevArea;
        private PlaceableObject                         temp;
        private Vector3                                 prevPos;
        private Vector3                                 mousePosition;
        /// <summary>
        /// 테스트 변수
        /// </summary>
        public TileBase white;
        public TileBase green;
        public TileBase red;
        //public GameObject pref;
        
        public static Dictionary<TileType, TileBase>    tileBases           = new Dictionary<TileType, TileBase>();

        private void Start()
        {
            current = this;
            
            tileBases.Add(TileType.EMPTY, (TileBase) null);
            tileBases.Add(TileType.WHITE, white);
            tileBases.Add(TileType.GREEN, green);
            tileBases.Add(TileType.RED, red);
        }

        // private void Update()
        // {
        //     if (!temp)
        //         return;
        //
        //     if (Input.GetMouseButtonDown(0))
        //     {
        //         if (EventSystem.current.IsPointerOverGameObject(0))
        //             return;
        //
        //         if (!temp.Placed)
        //         {
        //             Vector2 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //             Vector3Int cellPos = gridLayout.LocalToCell(touchPos);
        //
        //             if (prevPos != cellPos)
        //             {
        //                 temp.transform.localPosition = gridLayout.CellToLocalInterpolated(cellPos + new Vector3(0.5f, 0.5f, 0f));
        //                 prevPos = cellPos;
        //                 FollowBuilding();
        //             }
        //         }
        //     }
        //     else if(Input.GetKeyDown(KeyCode.Space))
        //     {
        //         if (temp.CanBePlace())
        //         {
        //             temp.Place();
        //         }
        //     }
        //     else if(Input.GetKeyDown(KeyCode.Escape))
        //     {
        //         ClearArea();
        //         Destroy(temp.gameObject);
        //     }
        // }

        public void InitializeWithBuilding(GameObject pref)
        {
            temp = UnityEngine.Object.Instantiate<GameObject>(pref, Vector3.zero, Quaternion.identity).GetComponent<PlaceableObject>();
            temp.First();
            //FollowBuilding();
        }
        public void ClearArea()
        {
            TileBase[] toClear = new TileBase[prevArea.size.x * prevArea.size.y * prevArea.size.z];
            FillTiles(toClear, TileType.EMPTY);
            TempTilemap.SetTilesBlock(prevArea, toClear);
            
            //SetTilesBlock(GridBuildingSystem.prevArea, TileType.EMPTY, GridBuildingSystem.TempTilemap);
        }

        public void FollowBuilding(BoundsInt buildingArea)
        {
            // ClearArea();
            //
            // temp.area.position = gridLayout.WorldToCell(temp.gameObject.transform.position);
            // BoundsInt buildingArea = temp.area;
            //
            // TileBase[] baseArray = GetTilesBlock(buildingArea, MainTilemap);
            // int size = baseArray.Length;
            // TileBase[] tileArray = new TileBase[size];
            //
            // for (int i = 0; i < baseArray.Length; i++)
            // {
            //     if (baseArray[i] == tileBases[TileType.WHITE])
            //     {
            //         tileArray[i] = tileBases[TileType.GREEN];
            //     }
            //     else
            //     {
            //         FillTiles(tileArray, TileType.RED);
            //         break;
            //     }
            // }
            // TempTilemap.SetTilesBlock(buildingArea, tileArray);
            // prevArea = buildingArea;



            TileBase[] tilesBlock           = GetTilesBlock(buildingArea, MainTilemap);
            TileBase[] tileBaseArray        = new TileBase[tilesBlock.Length];
            
            ClearArea();
            //temp.GreenColor();
            for (int index = 0; index < tilesBlock.Length; ++index)
            {
                if ((UnityEngine.Object) tilesBlock[index] == (UnityEngine.Object) GridBuildingSystem.tileBases[TileType.WHITE])
                {
                    tileBaseArray[index] = GridBuildingSystem.tileBases[TileType.GREEN];
                }
                else
                {
                    GridBuildingSystem.FillTiles(tileBaseArray, TileType.RED);
                    //GridBuildingSystem.temp.RedColor();
                    break;
                }
            }
            TempTilemap.SetTilesBlock(buildingArea, tileBaseArray);
            prevArea = buildingArea;
        }
        private static TileBase[] GetTilesBlock(BoundsInt area, Tilemap tilemap)
        {
            TileBase[] array = new TileBase[area.size.x * area.size.y * area.size.z];
            int counter = 0;

            foreach (var v in area.allPositionsWithin)
            {
                Vector3Int pos = new Vector3Int(v.x, v.y, 0);
                array[counter] = tilemap.GetTile(pos);
                counter++;
            }

            return array;



            // Vector3Int size = area.size;
            // int x = size.x;
            // size = area.size;
            // int y = size.y;
            // int num = x * y;
            // size = area.size;
            // int z = size.z;
            // TileBase[] tilesBlock = new TileBase[num * z];
            // int index = 0;
            // using (BoundsInt.PositionEnumerator enumerator = area.allPositionsWithin.GetEnumerator())
            // {
            //     while (enumerator.MoveNext())
            //     {
            //         Vector3Int current = enumerator.Current;
            //         Vector3Int position = new Vector3Int(current.x, current.y, 0);
            //         tilesBlock[index] = tilemap.GetTile(position);
            //         ++index;
            //     }
            // }
            // return tilesBlock;
        }
        
        private static void FillTiles(TileBase[] arr, TileType type)
        {
            for (int index = 0; index < arr.Length; ++index)
                arr[index] = tileBases[type];
        }
    
        public void SetTilesBlock(BoundsInt area, TileType type, Tilemap tilemap)
        {
            int size = area.size.x * area.size.y * area.size.z;
            TileBase[] tileBaseArray = new TileBase[size];
            FillTiles(tileBaseArray, type);
            tilemap.SetTilesBlock(area, tileBaseArray);
            
            // Vector3Int size = area.size;
            // int x = size.x;
            // size = area.size;
            // int y = size.y;
            // int num = x * y;
            // size = area.size;
            // int z = size.z;
            // TileBase[] tileBaseArray = new TileBase[num * z];
            // GridBuildingSystem.FillTiles(tileBaseArray, type);
            // tilemap.SetTilesBlock(area, tileBaseArray);
        }
        public bool CanTakeArea(BoundsInt area)
        {
            TileBase[] baseArray = GetTilesBlock(area, MainTilemap);
            foreach (var b in baseArray)
            {
                if (b != tileBases[TileType.WHITE])
                {
                    Debug.Log("Cannot place here!");
                    return false;
                }
            }
            
            return true;

            // foreach (UnityEngine.Object @object in GetTilesBlock(area, MainTilemap))
            // {
            //     if (@object != (UnityEngine.Object) GridBuildingSystem.tileBases[TileType.WHITE])
            //     {
            //         Debug.Log((object) "Cannot place here!");
            //         return false;
            //     }
            // }
            // return true;
        }
        
        public void TakeArea(BoundsInt area)
        {
            SetTilesBlock(area, TileType.EMPTY, TempTilemap);
            SetTilesBlock(area, TileType.GREEN, MainTilemap);
        }
    }

    public enum TileType
    {
        EMPTY,
        WHITE,
        GREEN,
        RED
    }
}

