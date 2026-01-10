using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic.Core.Extensions;
using GameLogic.Enum;
using UnityEngine;

/// <summary>
/// PROP ITEM 각각의 상태를 저장하는 클래스
/// </summary>
public class PropState
{
    /// <summary>
    /// PROP ITEM ID
    /// </summary>
    public int ItemId;
    
    /// <summary>
    /// PROP ITEM HIGHEST LEVEL
    /// </summary>
    public int HighestLevel  => ActivePropStates.Count <= 0 ? 0 : ActivePropStates.Max(x => x.Level);

    public int OrderByHighestLevel
    {
        get
        {
            for (int i = 0; i < ActivePropStates.Count; i++)
            {
                if (ActivePropStates.ElementAt(i).Level == HighestLevel)
                    return i;
            }
            return 0;
        }
    }

    public int ActiveTrashCount => ActivePropStates.Count(x => x.IsTrash);
    
    /// <summary>
    /// PROP ITEM SUM SCORE
    /// </summary>
    public int Score => ActivePropStates.Count <= 0 ? 0 : ActivePropStates.Where(x=>x.Active).Sum(x => Definition.Score(x.Level));
    
    /// <summary>
    /// PROP ITEM 데이터 계산을 도와주는 클래스
    /// </summary>
    public BuildingConfiguration Config;
    
    /// <summary>
    /// PROP ITEM 정의
    /// </summary>
    public PropDefinition Definition;
    
    /// <summary>
    /// 현재 구매한 수량
    /// </summary>
    public int BuyCount;

    /// <summary>
    /// 이용 가능한 수량
    /// </summary>
    public int AvailableCount;

    /// <summary>
    /// 구매한 가구 클래스 리스트
    /// </summary>
    public List<ActivePropState> ActivePropStates;

    public int                          PlaceCount { get; private set; } = 0;
    
    public bool                         IsAvailable()       => AvailableCount > 0;
    
    public bool                         IsSoldOut           => BuyCount >= Definition.AvailableCount;

    public bool IsPlaceAvailable(PlaceMode mode)
    {
        if (Definition.clientData.FixedData == null || Definition.clientData.FixedData.Count <= 0)
            return true;
        
        return Definition.clientData.FixedData.Any(x => x.Mode == mode);   
    }
    
    
    private GameProgress                progress;

    public PropState()
    {
        ActivePropStates    = new List<ActivePropState>();
    }
    
    public ActivePropState TryGetActivePropState(int order) => ActivePropStates.FirstOrDefault(x => x.Order == order);
    
    public void Init(GameProgress progress, int itemId)
    {
        this.progress       = progress;
        this.Config         = progress.Building.Config;
        Definition          = Config.TryGetPropDefinition(itemId);
        ItemId              = itemId;
        PlaceCount          = 0;
        BuyCount            = 0;
        AvailableCount      = 0;
    }

    public void Load(PropContentsData data)
    {
        if (Definition == null)
            return;
        
        if (Definition.AvailableCount == 0)
            return;

        ActivePropState activePropState = new ActivePropState()
        {
            ItemId              = ItemId,
            Order               = data.order,
            Position            = new Vector3(data.posX, data.posY, 0),
            Level               = data.level,
            Place               = data.place,
            IsRotation          = data.isRotation,
            IsTrash             = data.isTrash,
            Active              = data.active,
            LastTrashUpdateTime = (float)TimeProvider.Instance.UnscaledTimeSinceStartup,
            AvailableTrash      = Config.IsAvailableTrash(ItemId)
        };
        activePropState.Init(progress);
        ActivePropStates.Add(activePropState);
        
        BuyCount            += 1;
        AvailableCount      += 1;
        
        if (data.active)
        {
            Place(data.place, activePropState);
        }

    }
    
    /// <summary>
    /// 구매하기
    /// </summary>
    public void Buy()
    {
        if (Definition == null)
            return;
        
        if (Definition.AvailableCount == 0)
            return;
        
        ActivePropState activePropState = new ActivePropState()
        {
            ItemId              = ItemId,
            Order               = BuyCount,
            Position            = null,
            Level               = 1,
            Place               = PlaceMode.NONE,
            IsRotation          = false,
            IsTrash             = false,
            Active              = false,
            LastTrashUpdateTime = (float)TimeProvider.Instance.UnscaledTimeSinceStartup,
            AvailableTrash      = Config.IsAvailableTrash(ItemId)
        };
        activePropState.Init(progress);
        ActivePropStates.Add(activePropState);
        
        BuyCount            += 1;
        AvailableCount      += 1;
    }
    
    /// <summary>
    /// 사용하기
    /// </summary>
    public void Place(PlaceMode mode, ActivePropState activePropState, bool isRelocate = false)
    {
        if (Definition == null)
            return;
        
        if (Definition.AvailableCount == 0)
            return;

        if (!isRelocate)
        {
            PlaceCount      += 1;
            AvailableCount  -= 1;
        }
        
        activePropState.SetUse(mode, true);
    }

    public void Release(ActivePropState activePropState)
    {
        if (Definition == null)
            return;
        
        if (Definition.AvailableCount == 0)
            return;
        
        AvailableCount  += 1;
        PlaceCount      -= 1;
        activePropState.Position = null;
        activePropState.SetUse(PlaceMode.NONE, false);
    }

    public void Rotate(ActivePropState activePropState, bool flip)
    {
        if (Definition == null)
            return;
        
        activePropState.SetFlip(flip);
    }
    

    public void CatchUp()
    {
        if (progress == null)
            return;
        
        if (Definition == null)
            return;

        int len = ActivePropStates.Count;
        for (int i = 0; i < len; i++)
            ActivePropStates[i].CatchUp();
    }
}
