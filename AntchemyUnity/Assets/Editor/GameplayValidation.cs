#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Antchemy.Editor
{
    public static class GameplayValidation
    {
        // CLI entry: Unity -batchmode -projectPath ... -executeMethod
        // Antchemy.Editor.GameplayValidation.RunEditMode -logFile ...
        // Omit -quit: the asynchronous play-mode suite exits with its result.
        public static void RunEditMode()
        {
            try
            {
                ValidateSourceLevels();
                EditorSceneManager.OpenScene("Assets/Scenes/MainGame.unity",OpenSceneMode.Single);
                SessionState.SetBool("Antchemy.SmokeTest",true);
                EditorApplication.isPlaying=true;
            }
            catch(Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
        static void Count(Dictionary<int,int> map,int color,int amount) { if(!map.ContainsKey(color)) map[color]=0; map[color]+=amount; }
        public static void ValidateSourceLevels()
        {
            int totalCubes=0; int levelCount=Resources.LoadAll<TextAsset>("Levels").Length;
            for(int level=1;level<=levelCount;level++)
            {
                var asset=Resources.Load<TextAsset>($"Levels/Level_{level}");
                if(asset==null) throw new InvalidOperationException($"Missing source level {level}.");
                var data=JsonUtility.FromJson<LevelData>(asset.text);
                if(data!=null) data.Normalize();
                if(data==null||data.SizeX<=0||data.SizeY<=0||data.LaneData.Count==0) throw new InvalidOperationException($"Invalid source level {level}.");
                var ids=new HashSet<int>();
                foreach(var group in data.ColorData) foreach(int id in group.CubeDataIds)
                {
                    if(id<0||id>=data.SizeX*data.SizeY||!ids.Add(id)) throw new InvalidOperationException($"Duplicate/out-of-bounds cube {id} in level {level}.");
                }
                for(int columnIndex=0;columnIndex<data.LaneData.Count;columnIndex++)
                for(int colonyIndex=0;colonyIndex<data.LaneData[columnIndex].ColonyDatas.Count;colonyIndex++)
                {
                    var colony=data.LaneData[columnIndex].ColonyDatas[colonyIndex];
                    bool isLock=data.LockKeys!=null&&data.LockKeys.Exists(key=>key.LockPos!=null&&key.LockPos.LaneId==columnIndex&&key.LockPos.ColonyId==colonyIndex);
                    if(colony.AntCount<0||colony.AntCount==0&&!isLock&&colony.ObstacleType==0) throw new InvalidOperationException($"Invalid/unmapped zero colony capacity in level {level}.");
                }
                var excluded=new HashSet<int>();
                foreach(var key in data.LockKeys) foreach(int id in key.PixelIds) excluded.Add(id);
                foreach(var pipe in data.ConveyorData) foreach(int id in pipe.BeltPositions) excluded.Add(id);
                var food=new Dictionary<int,int>(); var capacity=new Dictionary<int,int>();
                foreach(var group in data.ColorData) foreach(int id in group.CubeDataIds) if(!excluded.Contains(id)) Count(food,group.ColorIndex,1);
                foreach(var pipe in data.ConveyorData) foreach(int color in pipe.BeltColors) Count(food,color,1);
                foreach(var column in data.LaneData)
                {
                    foreach(var colony in column.ColonyDatas) Count(capacity,colony.ColorIndex,colony.AntCount);
                    foreach(var spawner in column.ColonySpawnerDatas) foreach(var colony in spawner.ColonyDatas) Count(capacity,colony.ColorIndex,colony.AntCount);
                }
                foreach(var demand in food)
                    if(!capacity.TryGetValue(demand.Key,out int amount)||amount!=demand.Value) throw new InvalidOperationException($"Color {demand.Key} capacity mismatch in level {level}.");
                foreach(var supply in capacity)
                    if(supply.Value>0&&(!food.TryGetValue(supply.Key,out int amount)||amount!=supply.Value)) throw new InvalidOperationException($"Excess color {supply.Key} capacity in level {level}.");
                totalCubes+=ids.Count;
            }
            Debug.Log($"COLONY VALIDATION PASS: {levelCount} source levels, {totalCubes} unique cubes; entering simulation tests.");
        }
    }
}
#endif
