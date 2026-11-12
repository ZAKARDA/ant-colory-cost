using System;
using System.Collections.Generic;
using UnityEngine;

namespace Antchemy
{
    [Serializable]
    public class LevelData
    {
        public int LevelDifficulty;
        public int SizeX;
        public int SizeY;
        public List<ColorGroup> ColorData = new List<ColorGroup>();
        public List<LaneColumn> LaneData = new List<LaneColumn>();
        public HiddenData HiddenData;
        public List<LinkedColonyData> LinkedColonies;
        public List<LockKeyData> LockKeys;
        public List<SealedTileData> SealedTiles = new List<SealedTileData>();
        public List<GateData> Gates = new List<GateData>();
        public List<ConveyorData> ConveyorData = new List<ConveyorData>();
        // Optional collections are omitted or explicitly null in authentic older payloads.
        public void Normalize()
        {
            if(ColorData==null) ColorData=new List<ColorGroup>();
            if(LaneData==null) LaneData=new List<LaneColumn>();
            if(HiddenData==null) HiddenData=new HiddenData();
            if(HiddenData.HiddenCubeDataIds==null) HiddenData.HiddenCubeDataIds=new List<int>();
            if(LinkedColonies==null) LinkedColonies=new List<LinkedColonyData>();
            if(LockKeys==null) LockKeys=new List<LockKeyData>();
            if(ConveyorData==null) ConveyorData=new List<ConveyorData>();
            if(SealedTiles==null) SealedTiles=new List<SealedTileData>();
            if(Gates==null) Gates=new List<GateData>();
            foreach(var group in ColorData) if(group.CubeDataIds==null) group.CubeDataIds=new List<int>();
            foreach(var column in LaneData)
            {
                if(column.ColonyDatas==null) column.ColonyDatas=new List<ColonyData>();
                if(column.ColonySpawnerDatas==null) column.ColonySpawnerDatas=new List<ColonySpawnerData>();
                if(column.Burrows==null) column.Burrows=new List<BurrowData>();
                foreach(var spawner in column.ColonySpawnerDatas) if(spawner.ColonyDatas==null) spawner.ColonyDatas=new List<ColonyData>();
            }
            foreach(var group in LinkedColonies) if(group.Ids==null) group.Ids=new List<ColonyPositionData>();
            foreach(var key in LockKeys) if(key.PixelIds==null) key.PixelIds=new List<int>();
            foreach(var pipe in ConveyorData)
            {
                if(pipe.BeltPositions==null) pipe.BeltPositions=new List<int>();
                if(pipe.BeltColors==null) pipe.BeltColors=new List<int>();
            }
            foreach(var blocker in SealedTiles) if(blocker.PixelIds==null) blocker.PixelIds=new List<int>();
            foreach(var door in Gates) if(door.PixelIds==null) door.PixelIds=new List<int>();
        }
    }

    [Serializable]
    public class ColorGroup
    {
        public int ColorIndex;
        public List<int> CubeDataIds = new List<int>();
    }

    [Serializable]
    public class LaneColumn
    {
        public List<ColonyData> ColonyDatas = new List<ColonyData>();
        public List<ColonySpawnerData> ColonySpawnerDatas = new List<ColonySpawnerData>();
        public List<BurrowData> Burrows = new List<BurrowData>();
    }

    [Serializable]
    public class ColonyData
    {
        public int ColorIndex;
        public int AntCount;
        public int ObstacleType;
        public int ObstaclePairId;
        public int IceHp;
        public bool IsPriority;
        public bool IsHidden;
    }

    [Serializable]
    public class ConveyorData
    {
        public List<int> BeltPositions = new List<int>();
        public List<int> BeltColors = new List<int>();
    }
    [Serializable]
    public class ColonySpawnerData
    {
        public int Index;
        public List<ColonyData> ColonyDatas = new List<ColonyData>();
    }
    [Serializable]
    public class BurrowData
    {
        public int StartIndex;
        public int CoverCount;
        public int RequireColor;
        public int RequireCount;
    }
    [Serializable]
    public class HiddenData
    {
        public List<int> HiddenCubeDataIds = new List<int>();
    }

    [Serializable]
    public class LinkedColonyData
    {
        public List<ColonyPositionData> Ids = new List<ColonyPositionData>();
    }

    [Serializable]
    public class SealedTileData
    {
        public List<int> PixelIds = new List<int>();
        public int RequireCount;
    }
    [Serializable]
    public class GateData
    {
        public List<int> PixelIds = new List<int>();
        public int RequireColor;
        public int RequireCount;
    }
    [Serializable]
    public class ColonyPositionData
    {
        public int LaneId;
        public int ColonyId;
    }

    [Serializable]
    public class LockKeyData
    {
        public ColonyPositionData LockPos;
        public int SpawnerQueueIndex = -1;
        public List<int> PixelIds = new List<int>();
    }
}
