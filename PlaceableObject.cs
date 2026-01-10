using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using GameLogic.Core;
using GameLogic.Enum;
using GameLogic.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Building
{
    /// <summary>
    /// 배치 가능한 오브젝트
    /// </summary>
    public class PlaceableObject : MonoBehaviour
    {
        [SerializeField] protected  SortingObject       sortingObject;
        [SerializeField] protected  Transform           root;
        [SerializeField] protected  SpriteRenderer      image;
        [SerializeField] protected  PolygonCollider2D   col;
        [SerializeField] protected  TrashView           trash;
        [SerializeField] protected  BaseBookCoverView   book1;
        [SerializeField] protected  BaseBookCoverView   book2;
       
        public BoundsInt        area;
        public BoundsInt        prevArea;
        public Vector3          prevPos;
        public bool prevRotate;
        protected UIEditMode      editMode = null;
        protected Vector3         startPos;
        protected float           deltaX;
        protected float           deltaY;
        public UIBookInfoMode  bookInfoMode { get; set; }
        public PlaceMode        Mode            { get; protected set; }   = PlaceMode.MAIN_PROP;
        public PropActionType   ActionType      { get; protected set; }   = PropActionType.NONE;
        public bool             Selected        { get; set; }           = false;
        public bool             isFlip          => image.flipX;
        public PropState        State           { get; protected set; }   = null;
        public int              Order           { get; set; }
        public bool             IsTrying        { get; set; }           = false;
        public Vector3 CenterPos => root.position;
        private Tween tween = null;
        
        protected virtual void Awake() { }
        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
        
        public virtual void Load(PlaceMode mode, PropState state, int order, bool isRotate, bool isTrash)
        {
            this.Mode       = mode;
            this.State      = state;
            this.Order      = order;
            this.Selected   = false;
            
            // 스프라이트 X축 플립
            image.flipX = isRotate;
            prevRotate = isRotate;
            // 위치 조정
            root.transform.localPosition = new Vector3(
                root.transform.localPosition.x * (image.flipX ? -1 : 1),
                root.transform.localPosition.y,
                root.transform.localPosition.z
            );

            if (sortingObject.pivot1 != null)
            {
                sortingObject.pivot1.transform.localPosition = new Vector3(
                    sortingObject.pivot1.transform.localPosition.x * (image.flipX ? -1 : 1),
                    sortingObject.pivot1.transform.localPosition.y,
                    sortingObject.pivot1.transform.localPosition.z
                );
            }
            if (sortingObject.pivot2 != null)
            {
                sortingObject.pivot2.transform.localPosition = new Vector3(
                    sortingObject.pivot2.transform.localPosition.x * (image.flipX ? -1 : 1),
                    sortingObject.pivot2.transform.localPosition.y,
                    sortingObject.pivot2.transform.localPosition.z
                );
            }
            
            // Collider 포인트 조정
            if (col != null)
            {
                if (isRotate)
                {
                    Vector2[] points = col.points;
                    for (int i = 0; i < points.Length; i++)
                    {
                        points[i] = new Vector2(-points[i].x, points[i].y);
                    }
                    col.points = points;
                    
                    col.offset = new Vector2(
                        col.offset.x * (image.flipX ? -1 : 1),
                        col.offset.y
                    );
                }

                
            }
            if (isRotate)
                area = new BoundsInt(area.position, new Vector3Int(area.size.y, area.size.x, area.size.z));
            
            if (trash != null)
            {
                // if (isTrash)
                //     trash.SetSprite();
                
                if (isRotate)
                {
                    trash.transform.localPosition = new Vector3(
                        trash.transform.localPosition.x * (image.flipX ? -1 : 1),
                        trash.transform.localPosition.y,
                        trash.transform.localPosition.z
                    );
                }
                
                // col.enabled = !isTrash;
                // if (isTrash)
                //     DeActiveColor();
                // else
                //     ActiveColor();
                SetTrashView(isTrash);
                //trash.SetActive(isTrash);
            }
            // sortingObject.RefreshSortingOrder();
        }

        public void SetFlip(bool isRotate)
        {
            // 스프라이트 X축 플립
            image.flipX = isRotate;
            prevRotate = isRotate;
            // 위치 조정
            root.transform.localPosition = new Vector3(
                root.transform.localPosition.x * (image.flipX ? -1 : 1),
                root.transform.localPosition.y,
                root.transform.localPosition.z
            );
            if (sortingObject.pivot1 != null)
            {
                sortingObject.pivot1.transform.localPosition = new Vector3(
                    sortingObject.pivot1.transform.localPosition.x * (image.flipX ? -1 : 1),
                    sortingObject.pivot1.transform.localPosition.y,
                    sortingObject.pivot1.transform.localPosition.z
                );
            }
            if (sortingObject.pivot2 != null)
            {
                sortingObject.pivot2.transform.localPosition = new Vector3(
                    sortingObject.pivot2.transform.localPosition.x * (image.flipX ? -1 : 1),
                    sortingObject.pivot2.transform.localPosition.y,
                    sortingObject.pivot2.transform.localPosition.z
                );
            }
            // Collider 포인트 조정
            if (col != null)
            {
                if (isRotate)
                {
                    Vector2[] points = col.points;
                    for (int i = 0; i < points.Length; i++)
                    {
                        points[i] = new Vector2(-points[i].x, points[i].y);
                    }
                    col.points = points;
                    
                    col.offset = new Vector2(
                        col.offset.x * (image.flipX ? -1 : 1),
                        col.offset.y
                    );
                }

                
            }
            if (isRotate)
                area = new BoundsInt(area.position, new Vector3Int(area.size.y, area.size.x, area.size.z));
            
            if (trash != null)
            {
                if (isRotate)
                {
                    trash.transform.localPosition = new Vector3(
                        trash.transform.localPosition.x * (image.flipX ? -1 : 1),
                        trash.transform.localPosition.y,
                        trash.transform.localPosition.z
                    );
                }
            }
        }
        public virtual void DummyLoad(bool isRotate)
        {
            
            // 스프라이트 X축 플립
            image.flipX = isRotate;
            // 위치 조정
            root.transform.localPosition = new Vector3(
                root.transform.localPosition.x * (image.flipX ? -1 : 1),
                root.transform.localPosition.y,
                root.transform.localPosition.z
            );
            if (sortingObject.pivot1 != null)
            {
                sortingObject.pivot1.transform.localPosition = new Vector3(
                    sortingObject.pivot1.transform.localPosition.x * (image.flipX ? -1 : 1),
                    sortingObject.pivot1.transform.localPosition.y,
                    sortingObject.pivot1.transform.localPosition.z
                );
            }
            if (sortingObject.pivot2 != null)
            {
                sortingObject.pivot2.transform.localPosition = new Vector3(
                    sortingObject.pivot2.transform.localPosition.x * (image.flipX ? -1 : 1),
                    sortingObject.pivot2.transform.localPosition.y,
                    sortingObject.pivot2.transform.localPosition.z
                );
            }
            col.enabled = false;
        }

        public virtual void FriendLoad()
        {
            
        }
        public void DummyLoad2()
        {
            sortingObject.RefreshSortingOrder();
        }
        /// <summary>
        /// 패널에서 배치를 가져오는 경우
        /// </summary>
        public virtual void Init(PlaceMode mode, PropState state, int order, bool isTrying = false)
        {
            this.Mode       = mode;
            this.State      = state;
            this.Order      = order;
            this.Selected   = true;
            this.IsTrying   = isTrying;
            
            //최초 따라오도록 설정.
            this.Follow();
                
            Vector3Int cell                 = GridBuildingSystem.current.gridLayout.LocalToCell(this.transform.position);
            this.transform.localPosition    = GridBuildingSystem.current.gridLayout.CellToLocalInterpolated((Vector3) cell + new Vector3(0.5f, 0.5f, 0.0f));

            //가구 설정 패널 띄우기
            editMode = ResourceManager.Instance.SpawnDragDrop(root.transform, Vector3.zero, Quaternion.identity).GetComponent<UIEditMode>();
            editMode.transform.localPosition = Vector3.zero;
            editMode.SetData(UIEditMode.UIEditModeType.PLACE, this);
                        
            this.prevPos    = this.transform.position;
            this.ActionType = PropActionType.PROCESSING;
            sortingObject.SetMovable(true);
        }
        /// <summary>
        /// 해당 가구는 배치가 가능한가?
        /// </summary>
        public bool CanPlace(Vector3 position)
        {
            Vector3Int cell         = GridBuildingSystem.current.gridLayout.LocalToCell(position);
            this.area.position      = cell;
            return GridBuildingSystem.current.CanTakeArea(this.Mode, this.area, true);
        }
        
        
    
        
        /// <summary>
        /// 최종 배치
        /// </summary>
        /// <param name="position">배치 좌표</param>
        public virtual void Place(Vector3 position, bool isEffect = true)
        {
            Vector3Int cell         = GridBuildingSystem.current.gridLayout.LocalToCell(position);
            this.area.position      = cell;
            this.transform.position = position;
            this.prevArea           = area;
            this.prevRotate         = image.flipX;
            this.prevPos            = this.transform.position;
            this.ActionType         = PropActionType.PLACED;
            // this.Selected = false;
            sortingObject.SetMovable(false);
            
            GridBuildingSystem.current.FindAndReleaseOverlappingProps(this.Mode, area, State, Order);
            
            sortingObject.RefreshSortingOrder();
            
            GridBuildingSystem.current.ClearArea();
            GridBuildingSystem.current.TakeArea(this.Mode, area);
            GridBuildingSystem.current.AddProp(this.Mode, new PropObjectData()
            {
                //id          = State.Definition.Id,
                //order       = this.Order,
                //isRotation  = isFlip,
                activeProp  = State.ActivePropStates[Order],
                viewer      = this,
                worldPos    = this.transform.position,
                tilePos     = cell
            });
            
            if (isEffect)
            {
                transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                tween = transform.DOScale(1.0f, 0.5f)
                    .SetEase(Ease.OutElastic);
            }
        }
        
        /// <summary>
        /// 배치 취소
        /// </summary>
        public virtual void PlaceCancel()
        {
            ActionType = PropActionType.NONE;
            sortingObject.SetMovable(false);
            // this.Selected = false;

            GridBuildingSystem.current.DeSelectProp();
            
            //타일 맵 정상화
            GridBuildingSystem.current.ClearArea();

            //생성된 오브젝트 파괴
            if (tween != null && tween.IsActive())
                tween.Kill();
            Destroy(this.gameObject);
            
            //TODO: 다른 가구 콜라이더 재 활성화
        }
        
        /// <summary>
        /// 인벤에 넣기
        /// </summary>
        public virtual void Release()
        {
            // this.Selected = false;

            GridBuildingSystem.current.DeSelectProp();
            
            ActionType                          = PropActionType.NONE;
            sortingObject.SetMovable(false);
            
            GridBuildingSystem.current.ClearArea();
            GridBuildingSystem.current.DestroyProp(this.Mode, this.area);
                        
            //COMMENT: 이 부분을 다시 체크해볼 필요가 있음.
            GridBuildingSystem.current.RemoveProp(this.Mode, State.Definition.Id, this.Order); 
            
            if (tween != null && tween.IsActive())
                tween.Kill();
            Destroy(this.gameObject);
        }
        
        /// <summary>
        /// 가구 재배치
        /// </summary>
        public virtual void Relocate()
        {
            GridBuildingSystem.current.isPlaceMode = true;
            
            List<DisplayablePropView> propViews = GridBuildingSystem.current.GetDisplayablePropView(GridBuildingSystem.current.currentMode);
            for (int i = 0; i < propViews.Count; i++)
                propViews[i].HideBubbleView();
            
            ActionType = PropActionType.PROCESSING;
            sortingObject.SetMovable(true);
            // Selected   = false;

            editMode.SetData(UIEditMode.UIEditModeType.MOVE, this);
            
            GridBuildingSystem.current.temp = this;
            
            GridBuildingSystem.current.ClearArea();
            GridBuildingSystem.current.SetPropView(this.Mode, false);
            GridBuildingSystem.current.DestroyProp(this.Mode, this.area);
            
            Follow();
            
            this.prevPos = this.transform.position;
            
            transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            tween = transform.DOScale(1.0f, 0.5f)
                .SetEase(Ease.OutElastic);
        }
        
        /// <summary>
        /// 가구 회전
        /// </summary>
        public virtual void Rotate()
        {
            // 현재 영역 제거
            GridBuildingSystem.current.ClearArea();

            // 스프라이트 X축 플립
            image.flipX = !image.flipX;
            
            // 위치 조정
            root.transform.localPosition = new Vector3(
                root.transform.localPosition.x * -1f,
                root.transform.localPosition.y,
                root.transform.localPosition.z
            );
            if (sortingObject.pivot1 != null)
            {
                sortingObject.pivot1.transform.localPosition = new Vector3(
                    sortingObject.pivot1.transform.localPosition.x * -1f,
                    sortingObject.pivot1.transform.localPosition.y,
                    sortingObject.pivot1.transform.localPosition.z
                );
            }
            if (sortingObject.pivot2 != null)
            {
                sortingObject.pivot2.transform.localPosition = new Vector3(
                    sortingObject.pivot2.transform.localPosition.x * -1f,
                    sortingObject.pivot2.transform.localPosition.y,
                    sortingObject.pivot2.transform.localPosition.z
                );
            }
            // Collider 포인트 조정
            if (col != null)
            {
                Vector2[] points = col.points;
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = new Vector2(-points[i].x, points[i].y);
                }
                col.points = points;

                // Offset 조정
                col.offset = new Vector2(
                    col.offset.x * -1,
                    col.offset.y
                );
            }
            
            //쓰레기 위치도 회전 넣기
            if (trash != null)
            {
                trash.transform.localPosition = new Vector3(
                    trash.transform.localPosition.x * -1f,
                    trash.transform.localPosition.y,
                    trash.transform.localPosition.z
                );
            }
            
            // 영역 크기 변경 (width와 height 스왑)
            area = new BoundsInt(area.position, new Vector3Int(area.size.y, area.size.x, area.size.z));
            
            // if (editMode != null)
            //     editMode.transform.localPosition = col == null ? Vector3.zero : new Vector3(0f, image.transform.localPosition.y * -1f, 0f);
            // 새로운 영역으로 타일맵 업데이트
            Follow();
            
            sortingObject.RefreshSortingOrder();
        }
        
        public bool IsTrash => State.TryGetActivePropState(Order)?.IsTrash == true;
        
        /// <summary>
        /// 가구 클릭
        /// </summary>
        public virtual void OnClick()
        {
            if (UserSettings.Data.isTutorial)
                return;
            
            if (GridBuildingSystem.current.isFriendMode)
                return;
            if (ActionType != PropActionType.PLACED)
                return;
            if (this.State == null)
                return;
            if (Selected)
                return;
            
            SoundManager.Instance.PlaySfx(CommonSounds.GetClip(SfxType.CLICK3), 0.2f);
            Selected = true;

            GridBuildingSystem.current.temp = this;
            
            //가구 설정 패널 띄우기
            editMode = ResourceManager.Instance.SpawnDragDrop(root.transform, Vector3.zero, Quaternion.identity).GetComponent<UIEditMode>();
            // Sprite sprite = image.sprite;
            // Vector2 pivot = sprite.pivot;
            // Vector2 normalizedPivot = new Vector2(
            //     pivot.x / sprite.rect.width,
            //     pivot.y / sprite.rect.height
            // );
            // editMode.transform.localPosition = col == null ? Vector3.zero : new Vector3(0f, image.transform.localPosition.y * -1f, 0f);
            editMode.transform.localPosition = Vector3.zero;
            editMode.SetData(UIEditMode.UIEditModeType.SELECT, this);

            List<DisplayablePropView> propViews = GridBuildingSystem.current.GetDisplayablePropView(GridBuildingSystem.current.currentMode);
            for (int i = 0; i < propViews.Count; i++)
            {
                propViews[i].HideBookInfoView();
            }
        }
        
        public virtual void SetTrashView(bool active)
        {
            if (trash != null)
            {
                if (!active)
                    trash.Apply();
                else
                    trash.SetSprite();

                // col.enabled = !active;
                
                // if (active)
                //     DeActiveColor();
                // else
                //     ActiveColor();
                
                trash.SetActive(active);
            }
                
        }

        public void SetCollider(bool active)
        {
            if (col == null)
                return;

            col.enabled = active;
        }
        public virtual void OnDeselect()
        {
            if (!Selected)
                return;
            
            if (ActionType == PropActionType.PROCESSING)
            {
                editMode.OnClickedCancel();
                
                if (bookInfoMode != null)
                    bookInfoMode.gameObject.Release();
                
                return;
            }            

            sortingObject.RefreshSortingOrder();
            
            Selected = false;
            editMode.gameObject.Release();
            
            if (bookInfoMode != null)
                bookInfoMode.gameObject.Release();
        }
        
        public virtual void Follow()
        {
            this.area.position = GridBuildingSystem.current.gridLayout.LocalToCell(this.transform.position);
            GridBuildingSystem.current.FollowProp(this.Mode, this.area);
        }
        public virtual void DeActiveColor()     => image.color = Color.gray;
        public virtual void ActiveColor()       => image.color = Color.white;
        public virtual void SetAlpha(float alpha)
        {
            image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
            if (trash != null) trash.SetAlpha(alpha);
            if (book1 != null) book1.SetAlpha(alpha);
            if (book2 != null) book2.SetAlpha(alpha);
        }
        
        #region MOUSE EVENT CALL BACK

        public virtual void OnMouseDown()
        {
            HandleMouseDown();
        }

        public virtual void OnMouseDrag()
        {
            HandleMouseDrag();
        }

        public virtual void OnMouseUp()
        {
            HandleMouseUp();
        }
        
        public virtual void HandleMouseDown() { }
        public virtual void HandleMouseDrag() { }
        public virtual void HandleMouseUp() { }
        
        #endregion
        
    }
}

