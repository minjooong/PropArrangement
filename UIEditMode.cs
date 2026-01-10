using System;
using System.Collections;
using System.Collections.Generic;
using GameLogic.Building;
using GameLogic.Core;
using GameLogic.Enum;
using GameLogic.Manager;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;

public class UIEditMode : MonoBehaviour
{
    public enum UIEditModeType
    {
        PLACE,          //배치 상태 (구매 및 사용) (확인, 취소)
        MOVE,           //이동 상태 (확인, 취소)
        SELECT,         //선택 상태 (취소, 인벤, 업그레이드, 이동)
    }
    
    [Dependency] public GameProgressManager progressMgr { get; private set; }
    [Dependency] public ConfigurationManager configMgr { get; private set; }
    [SerializeField] private SimpleButton buttonConfirm;
    [SerializeField] private SimpleButton buttonCancel;
    [SerializeField] private SimpleButton buttonInven;
    [SerializeField] private SimpleButton buttonColor;
    [SerializeField] private SimpleButton buttonInfo;
    [SerializeField] private SimpleButton buttonRotate;
    [SerializeField] private SimpleButton buttonRelocate;
    [SerializeField] private SimpleButton buttonSetAll;
    [SerializeField] private SimpleButton buttonCopy;
    [SerializeField] private UIDraggable  dragger;
    [SerializeField] private UIDraggable  dragger2;
    [SerializeField] private Canvas       canvas;
    [SerializeField] private CanvasGroup  buttonCancelCG;
    [SerializeField] private CanvasGroup  rootCanvasGroup;
    
    private PlaceableObject placeableObject;
    private UIEditModeType  currentType;
    
    private void Awake()
    {
        buttonConfirm   .OnClick = OnClickedConfirm;
        buttonCancel    .OnClick = OnClickedCancel;
        buttonInven     .OnClick = OnClickedInven;
        buttonColor     .OnClick = OnClickedColor;
        buttonInfo      .OnClick = OnClickedInfo;
        buttonRotate    .OnClick = OnClickedRotate;
        buttonRelocate  .OnClick = OnClickedRelocate;
        buttonSetAll    .OnClick = OnClickedSetAll;
        buttonCopy      .OnClick = OnClickedCopy;
    }
    public void SetAlpha(float alpha) => rootCanvasGroup.alpha = alpha;
    
    public void SetData(UIEditModeType type, PlaceableObject placeableObject)
    {
        this.Inject();

        this.placeableObject    = placeableObject;
        this.currentType        = type;
        bool isDeco = placeableObject.Mode == PlaceMode.MAIN_GROUND || placeableObject.Mode == PlaceMode.ROOM_GROUND ||
                      placeableObject.Mode == PlaceMode.GARRET_GROUND || placeableObject.Mode == PlaceMode.BASEMENT_GROUND;
        
        buttonConfirm   .gameObject.SetActive(type is UIEditModeType.PLACE or UIEditModeType.MOVE);
        buttonCancel    .gameObject.SetActive(true);
        buttonInven     .gameObject.SetActive(type is UIEditModeType.MOVE);
        buttonColor     .gameObject.SetActive(false);
        buttonInfo      .gameObject.SetActive(type is UIEditModeType.SELECT && !isDeco);
        buttonRotate    .gameObject.SetActive(type is UIEditModeType.PLACE or UIEditModeType.MOVE && !isDeco);
        buttonRelocate  .gameObject.SetActive(type is UIEditModeType.SELECT);
        buttonSetAll    .gameObject.SetActive(type is UIEditModeType.PLACE or UIEditModeType.MOVE && isDeco);
        buttonCopy      .gameObject.SetActive(type is UIEditModeType.PLACE or UIEditModeType.MOVE && isDeco);
        dragger         .gameObject.SetActive(type is UIEditModeType.PLACE or UIEditModeType.MOVE);
        dragger2        .gameObject.SetActive(type is UIEditModeType.PLACE or UIEditModeType.MOVE);
        
        dragger.SetData(placeableObject);
        dragger2.SetData(placeableObject);

        SetAlpha(1f);
        
        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas.worldCamera = PanZoom.current.targetCamera;
        }

        if (type == UIEditModeType.SELECT)
        {
            OnClickedRelocate();
        }
        
        //튜토리얼일 때 X 는 숨기기.
        buttonCancelCG.alpha = UserSettings.Data.isTutorial ? 0f : 1f;
        buttonCancelCG.interactable = !UserSettings.Data.isTutorial;
        buttonCancelCG.blocksRaycasts = !UserSettings.Data.isTutorial;
    }

    private void OnClickedConfirm()
    {
        switch (currentType)
        {
            case UIEditModeType.PLACE:
                {
                    if (!placeableObject.CanPlace(placeableObject.transform.position))
                    {
#if !UNITY_EDITOR
                        if (UserSettings.Data.VibrationEnabled)
                            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Warning);
#endif
                        SoundManager.Instance.PlaySfx(CommonSounds.GetClip(SfxType.ALERT), 0.8f);
                        UIManager.Instance.PushPanel(UIPanelType.POPUP_PANEL, PopupType.CONFIRM, "여기는 설치할 수 없습니다");
                        return;
                    }
                    
                    // 연속 타일 배치중의 경우 여기서 구매
                    if (placeableObject.IsTrying)
                    {
                        progressMgr.PurchaseProp(progressMgr.CurrentProgress.Building.Config.TryGetPropDefinition(placeableObject.State.ItemId), false, true);
                        placeableObject.Order = placeableObject.State.BuyCount - 1;
                    }
        
                    placeableObject.Place(placeableObject.transform.position);
                    
                    //최종 선택.
                    progressMgr.UpdateProp(placeableObject.State, placeableObject.Order, PropUpdateType.PLACE
                        , placeableObject.Mode, placeableObject.transform.position, placeableObject.isFlip);
                    //다시 제자리로.
                    GridBuildingSystem.current.DeSelectProp();
                }
                break;
            case UIEditModeType.MOVE:
                {
                    if (!placeableObject.CanPlace(placeableObject.transform.position))
                    {
#if !UNITY_EDITOR
                        if (UserSettings.Data.VibrationEnabled)
                            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Warning);
#endif
                        SoundManager.Instance.PlaySfx(CommonSounds.GetClip(SfxType.ALERT), 0.8f);
                        UIManager.Instance.PushPanel(UIPanelType.POPUP_PANEL, PopupType.CONFIRM, "여기는 설치할 수 없습니다");
                        return;
                    }
                    GridBuildingSystem.current.DestroyProp(placeableObject.Mode, placeableObject.prevArea);
                    
                    placeableObject.Place(placeableObject.transform.position);
                    
                    //최종 선택.
                    progressMgr.UpdateProp(placeableObject.State, placeableObject.Order, PropUpdateType.PLACE, placeableObject.Mode
                        , placeableObject.transform.position, placeableObject.isFlip, true, true);
                    //다시 제자리로.
                    GridBuildingSystem.current.DeSelectProp();
                }
                break;
            case UIEditModeType.SELECT:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
#if !UNITY_EDITOR
        if (UserSettings.Data.VibrationEnabled)
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
#endif
    }
    public void OnClickedCancel()
    {
        switch (currentType)
        {
            case UIEditModeType.PLACE:
                {
                    placeableObject.PlaceCancel();
                        
                    //최종 선택.
                    progressMgr.UpdateProp(placeableObject.State, placeableObject.Order, PropUpdateType.CANCEL);
                    //다시 제자리로.
                    GridBuildingSystem.current.DeSelectProp();
                }
                break;
            case UIEditModeType.MOVE:
                {
                    if (placeableObject.prevRotate != placeableObject.isFlip) placeableObject.Rotate();
                    // placeableObject.SetFlip(placeableObject.prevRotate);
                    placeableObject.ActiveColor();
                    placeableObject.Place(placeableObject.prevPos);
                    //최종 선택.
                    progressMgr.UpdateProp(placeableObject.State, placeableObject.Order, PropUpdateType.CANCEL
                        , placeableObject.Mode, placeableObject.prevPos, false, true);
                    //다시 제자리로.
                    GridBuildingSystem.current.DeSelectProp();
                }
                break;
            case UIEditModeType.SELECT:
                {
                    placeableObject.OnDeselect();
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    private void OnClickedInven()
    {
        placeableObject.Release();
        //최종 릴리즈.
        progressMgr.UpdateProp(placeableObject.State, placeableObject.Order, PropUpdateType.RELEASE);
        //다시 제자리로.
        GridBuildingSystem.current.DeSelectProp();
    }
    private void OnClickedColor()
    {
        //TODO: 준비 중.
        ResourceManager.Instance.SpawnToast("Comming Soon..");
    }
    private void OnClickedRotate()
    {
        placeableObject.Rotate();
        
        progressMgr.UpdateProp(placeableObject.State, placeableObject.Order, PropUpdateType.ROTATE
            , placeableObject.Mode, placeableObject.transform.position, placeableObject.isFlip);
    }
    private void OnClickedInfo()
    {
        UIManager.Instance.PushPanel(UIPanelType.PROP_INFO_PANEL, PropInfoPanel.PropInfoType.INFO, placeableObject.State.Definition.Id, placeableObject.Order);
    }
    private void OnClickedRelocate()
    {
#if !UNITY_EDITOR
        if (UserSettings.Data.VibrationEnabled)
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
#endif
        placeableObject.Relocate();
        
        progressMgr.UpdateProp(placeableObject.State, placeableObject.Order, PropUpdateType.RELOCATE, placeableObject.Mode);
    }
    private void OnClickedSetAll()
    {
        Action confirmAction = delegate()
        {
            switch (currentType)
            {
                case UIEditModeType.PLACE:
                {
                    placeableObject.PlaceCancel();
                        
                    //최종 선택.
                    if (!placeableObject.IsTrying) progressMgr.UpdateProp(placeableObject.State, placeableObject.Order, PropUpdateType.CANCEL);
                }
                    break;
                case UIEditModeType.MOVE:
                {
                    if (placeableObject.prevRotate != placeableObject.isFlip) placeableObject.Rotate();
                    // placeableObject.SetFlip(placeableObject.prevRotate);
                    placeableObject.ActiveColor();
                    placeableObject.Place(placeableObject.prevPos);
                    //최종 선택.
                    progressMgr.UpdateProp(placeableObject.State, placeableObject.Order, PropUpdateType.CANCEL
                        , placeableObject.Mode, placeableObject.prevPos, false, true);
                }
                    break;
                case UIEditModeType.SELECT:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
#if !UNITY_EDITOR
            if (UserSettings.Data.VibrationEnabled)
                HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
#endif
            progressMgr.UpdateProp(placeableObject.State, placeableObject.Order, PropUpdateType.SETALL, placeableObject.Mode);
            GridBuildingSystem.current.DeSelectProp();
        };
        UIManager.Instance.PushPanel(UIPanelType.POPUP_PANEL, PopupType.YES_OR_NO, "바닥 전체를 이 타일로 변경하시겠습니까?", confirmAction);
    }
    private void OnClickedCopy()
    {
        switch (currentType)
        {
            case UIEditModeType.PLACE:
                {
                    if (!placeableObject.CanPlace(placeableObject.transform.position))
                    {
#if !UNITY_EDITOR
                        if (UserSettings.Data.VibrationEnabled)
                            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Warning);
#endif
                        SoundManager.Instance.PlaySfx(CommonSounds.GetClip(SfxType.ALERT), 0.8f);
                        UIManager.Instance.PushPanel(UIPanelType.POPUP_PANEL, PopupType.CONFIRM, "여기는 설치할 수 없습니다");
                        return;
                    }
                    
                    // 연속 타일 배치중의 경우 여기서 구매
                    if (placeableObject.IsTrying)
                    {
                        progressMgr.PurchaseProp(progressMgr.CurrentProgress.Building.Config.TryGetPropDefinition(placeableObject.State.ItemId), false, true);
                        placeableObject.Order = placeableObject.State.BuyCount - 1;
                    }
                
                    placeableObject.Place(placeableObject.transform.position);
                    
                    //최종 선택.
                    progressMgr.UpdateProp(placeableObject.State, placeableObject.Order, PropUpdateType.PLACE
                        , placeableObject.Mode, placeableObject.transform.position, placeableObject.isFlip, endEditMode:false);
                    //다시 제자리로.
                    GridBuildingSystem.current.DeSelectProp();
                }
                break;
            case UIEditModeType.MOVE:
                {
                    if (!placeableObject.CanPlace(placeableObject.transform.position))
                    {
#if !UNITY_EDITOR
                        if (UserSettings.Data.VibrationEnabled)
                            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Warning);
#endif
                        SoundManager.Instance.PlaySfx(CommonSounds.GetClip(SfxType.ALERT), 0.8f);
                        UIManager.Instance.PushPanel(UIPanelType.POPUP_PANEL, PopupType.CONFIRM, "여기는 설치할 수 없습니다");
                        return;
                    }
                    GridBuildingSystem.current.DestroyProp(placeableObject.Mode, placeableObject.prevArea);
                    
                    placeableObject.Place(placeableObject.transform.position);
                    
                    //최종 선택.
                    progressMgr.UpdateProp(placeableObject.State, placeableObject.Order, PropUpdateType.PLACE, placeableObject.Mode
                        , placeableObject.transform.position, placeableObject.isFlip, true, true, false);
                    //다시 제자리로.
                    GridBuildingSystem.current.DeSelectProp();
                }
                break;
            case UIEditModeType.SELECT:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
#if !UNITY_EDITOR
        if (UserSettings.Data.VibrationEnabled)
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
#endif
        progressMgr.UpdateProp(placeableObject.State, placeableObject.Order, PropUpdateType.COPY, placeableObject.Mode);
    }
}
