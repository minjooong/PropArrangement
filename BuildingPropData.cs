using System;
using System.Collections.Generic;
using System.Linq;
using GameLogic.Enum;
using UnityEngine;

namespace GameLogic.Building
{
    public class BuildingPropData
    {
        private GridBuildingSystem _system;
        
        // Moved from GridBuildingSystem
        public Dictionary<PlaceMode, List<PropObjectData>> placedProps = new Dictionary<PlaceMode, List<PropObjectData>>();

        public BuildingPropData(GridBuildingSystem system)
        {
            _system = system;
            InitializeDictionary();
        }

        private void InitializeDictionary()
        {
            placedProps.Clear();
            foreach (PlaceMode mode in System.Enum.GetValues(typeof(PlaceMode)))
            {
                if (mode == PlaceMode.NONE) continue; // Assuming None exists or not, but using known keys
                if (!placedProps.ContainsKey(mode))
                    placedProps.Add(mode, new List<PropObjectData>());
            }
            
            placedProps[PlaceMode.MAIN_PROP] = new List<PropObjectData>();
            placedProps[PlaceMode.ROOM_PROP] = new List<PropObjectData>();
            placedProps[PlaceMode.GARRET_PROP] = new List<PropObjectData>();
            placedProps[PlaceMode.BASEMENT_PROP] = new List<PropObjectData>();
            placedProps[PlaceMode.MAIN_OUTSIDE] = new List<PropObjectData>();
            placedProps[PlaceMode.MAIN_GROUND] = new List<PropObjectData>();
            placedProps[PlaceMode.ROOM_GROUND] = new List<PropObjectData>();
            placedProps[PlaceMode.GARRET_GROUND] = new List<PropObjectData>();
            placedProps[PlaceMode.BASEMENT_GROUND] = new List<PropObjectData>();
        }

        public void AddProp(PlaceMode mode, PropObjectData data)
        {
            if (!placedProps.TryGetValue(mode, out List<PropObjectData> list)) 
                return;
            
            int idx = list.FindIndex(x => x.activeProp.ItemId == data.activeProp.ItemId && x.activeProp.Order == data.activeProp.Order);
            if (idx >= 0)
                list[idx] = data;
            else
                list.Add(data);
        }

        public void RemoveProp(PlaceMode mode, int id, int order)
        {
            if (!placedProps.TryGetValue(mode, out List<PropObjectData> list)) 
                return;
            
            int idx = list.FindIndex(x => x.activeProp.ItemId == id && x.activeProp.Order == order);
            if (idx >= 0)
                list.RemoveAt(idx);
        }

        public List<PropObjectData> GetList(PlaceMode mode)
        {
            return placedProps.GetValueOrDefault(mode);
        }
    }
}
