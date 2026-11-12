using UnityEngine;

namespace Antchemy
{
    // The furthest unlocked picture is independent of the board chosen for replay.
    public sealed class LevelProgress
    {
        readonly int levelCount;
        readonly string legacyKey, unlockedKey, resumeKey, bonusKey;
        readonly System.Collections.Generic.HashSet<int> bonusUnlocked = new System.Collections.Generic.HashSet<int>();
        public int HighestUnlockedLevel { get; private set; }
        public int ResumeLevel { get; private set; }

        public LevelProgress(int count, string prefix = "Antchemy")
        {
            levelCount = Mathf.Max(1,count);
            legacyKey = prefix + "_Level";
            unlockedKey = prefix + "_UnlockedLevel";
            resumeKey = prefix + "_ResumeLevel";
            bonusKey = prefix + "_RewardUnlocked";
            foreach (var part in PlayerPrefs.GetString(bonusKey, "").Split(',')) if (int.TryParse(part, out int v)) bonusUnlocked.Add(v);
            int legacy = PlayerPrefs.GetInt(legacyKey,1);
            HighestUnlockedLevel = Mathf.Clamp(Mathf.Max(legacy,PlayerPrefs.GetInt(unlockedKey,1)),1,levelCount);
            ResumeLevel = Mathf.Clamp(PlayerPrefs.GetInt(resumeKey,legacy),1,levelCount);
            if (!IsUnlocked(ResumeLevel)) ResumeLevel = HighestUnlockedLevel;
            Save();
        }

        public bool IsUnlocked(int level) => level >= 1 && level <= levelCount && (level <= HighestUnlockedLevel || bonusUnlocked.Contains(level));

        // A single picture unlocked early with a rewarded video or coins.
        public void UnlockSingle(int level)
        {
            if (level < 1 || level > levelCount || IsUnlocked(level)) return;
            bonusUnlocked.Add(level);
            PlayerPrefs.SetString(bonusKey, string.Join(",", bonusUnlocked));
            PlayerPrefs.Save();
        }

        public bool Select(int level)
        {
            if(!IsUnlocked(level)) return false;
            ResumeLevel = level;
            Save();
            return true;
        }

        public bool Complete(int level)
        {
            if(!IsUnlocked(level)) return false;
            if (level >= HighestUnlockedLevel) HighestUnlockedLevel = Mathf.Max(HighestUnlockedLevel,Mathf.Min(level + 1,levelCount));
            int next = level < levelCount ? level + 1 : 1;
            ResumeLevel = IsUnlocked(next) ? next : HighestUnlockedLevel;
            Save();
            return true;
        }

        void Save()
        {
            PlayerPrefs.SetInt(unlockedKey,HighestUnlockedLevel);
            PlayerPrefs.SetInt(resumeKey,ResumeLevel);
            // Keep the previous version's progress key at the furthest unlocked level.
            PlayerPrefs.SetInt(legacyKey,HighestUnlockedLevel);
            PlayerPrefs.Save();
        }
    }
}
