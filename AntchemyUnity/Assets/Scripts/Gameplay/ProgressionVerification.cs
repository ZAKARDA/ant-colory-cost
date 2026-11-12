using System;
using System.Collections.Generic;
using UnityEngine;

namespace Antchemy
{
    // Isolated preferences exercise fresh installs, migration, replays and completion boundaries.
    static class ProgressionVerification
    {
        public static void Run(Action<bool,string> check)
        {
            const string prefix = "Antchemy_ProgressVerification";
            string[] keys = {prefix+"_Level",prefix+"_UnlockedLevel",prefix+"_ResumeLevel"};
            var saved = new Dictionary<string,int?>();
            foreach(string key in keys) { saved[key]=PlayerPrefs.HasKey(key)?(int?)PlayerPrefs.GetInt(key):null; PlayerPrefs.DeleteKey(key); }
            try
            {
                var progress = new LevelProgress(3,prefix);
                check(progress.ResumeLevel==1 && progress.IsUnlocked(1) && !progress.IsUnlocked(2) && !progress.IsUnlocked(3),"fresh progress unlocks only level 1");
                check(!progress.Select(2) && !progress.Complete(2) && progress.HighestUnlockedLevel==1 && progress.ResumeLevel==1,"locked levels cannot be selected or used to skip progression");
                check(progress.Complete(1) && progress.HighestUnlockedLevel==2 && progress.IsUnlocked(2) && !progress.IsUnlocked(3),"beating level 1 unlocks exactly level 2");
                progress.Complete(1);
                check(progress.HighestUnlockedLevel==2 && !progress.IsUnlocked(3),"replaying a completed level cannot unlock past its successor");
                progress.Select(1); progress = new LevelProgress(3,prefix);
                check(progress.ResumeLevel==1 && progress.HighestUnlockedLevel==2 && progress.IsUnlocked(2),"replay choice and unlocked progress survive a reload independently");
                progress.Select(2); progress.Complete(2);
                check(progress.HighestUnlockedLevel==3 && progress.IsUnlocked(1) && progress.IsUnlocked(2) && progress.IsUnlocked(3),"beating level 2 unlocks level 3 while earlier levels remain replayable");
                progress.Complete(3); progress = new LevelProgress(3,prefix);
                check(progress.HighestUnlockedLevel==3 && !progress.IsUnlocked(4) && !progress.Select(0) && !progress.Select(4),"final completion never creates an out-of-range unlocked level");
                foreach(string key in keys) PlayerPrefs.DeleteKey(key);
                PlayerPrefs.SetInt(keys[0],42);
                progress = new LevelProgress(2700,prefix);
                check(progress.HighestUnlockedLevel==42 && progress.ResumeLevel==42 && progress.IsUnlocked(41) && !progress.IsUnlocked(43),"legacy saved progress migrates with every earlier level unlocked");
                progress.Select(1); progress.Complete(1); progress = new LevelProgress(2700,prefix);
                check(progress.HighestUnlockedLevel==42 && progress.IsUnlocked(42) && PlayerPrefs.GetInt(keys[0])==42,"replaying and winning an early level never reduces migrated unlocks");
                PlayerPrefs.SetInt(keys[1],9999); PlayerPrefs.SetInt(keys[2],9999);
                progress = new LevelProgress(2700,prefix);
                check(progress.HighestUnlockedLevel==2700 && progress.ResumeLevel==2700 && !progress.IsUnlocked(2701),"saved progression is bounded by the actual level catalog");
            }
            finally
            {
                foreach(var item in saved) if(item.Value.HasValue) PlayerPrefs.SetInt(item.Key,item.Value.Value); else PlayerPrefs.DeleteKey(item.Key);
                PlayerPrefs.Save();
            }
        }
    }
}
