using UnityEngine;

namespace Antchemy
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public int CurrentLevel { get; private set; }
        public int Coins { get; private set; }
        public GameState CurrentState { get; private set; }
        public int LevelCount { get; private set; } = 2700;
        public int HighestUnlockedLevel => progress.HighestUnlockedLevel;
        LevelProgress progress;
        [System.Serializable] class Catalog { public int count; }
        const string CoinsKey = "Antchemy_Coins";
        const string DailyDayKey = "Antchemy_DailyDay", DailyStreakKey = "Antchemy_DailyStreak";
        public const int DailyBonusBase = 100;
        public bool IsDailyRun { get; private set; }
        public int LastBonus { get; private set; }
        public int DailyStreak => PlayerPrefs.GetInt(DailyStreakKey, 0);
        static int Today => (int)(System.DateTime.UtcNow.Date - new System.DateTime(2024,1,1)).TotalDays;
        public bool DailyAvailable => PlayerPrefs.GetInt(DailyDayKey, -1) != Today;
        public int DailyLevel => 1 + Mathf.Abs(Today * 7919 + 13) % Mathf.Max(1, Mathf.Max(6, HighestUnlockedLevel));
        public bool StartDaily() { bool ok = SelectLevel(Mathf.Min(DailyLevel, LevelCount)); IsDailyRun = ok; return ok; }

        void Awake()
        {
            Instance = this;
            var catalog = Resources.Load<TextAsset>("LevelCatalog");
            if (catalog != null) LevelCount = Mathf.Max(1, JsonUtility.FromJson<Catalog>(catalog.text).count);
            progress = new LevelProgress(LevelCount);
            CurrentLevel = progress.ResumeLevel;
            Coins = PlayerPrefs.GetInt(CoinsKey, 250);
            CurrentState = GameState.MainMenu;
        }
        public void OnLevelWon()
        {
            if (CurrentState == GameState.Win) return;
            if (!progress.Complete(CurrentLevel)) return;
            CurrentState = GameState.Win;
            LastBonus = 0;
            AddCoins(50);
            if (IsDailyRun && DailyAvailable)
            {
                int streak = PlayerPrefs.GetInt(DailyDayKey, -1) == Today - 1 ? DailyStreak + 1 : 1;
                PlayerPrefs.SetInt(DailyDayKey, Today); PlayerPrefs.SetInt(DailyStreakKey, streak);
                LastBonus = DailyBonusBase + 25 * Mathf.Min(streak - 1, 6); AddCoins(LastBonus);
            }
            AdManager.Instance?.RecordWin();
        }
        public void AddCoins(int amount)
        {
            Coins = Mathf.Max(0, Coins + amount);
            PlayerPrefs.SetInt(CoinsKey, Coins);
            PlayerPrefs.Save();
            UIManager.Instance?.UpdateCoins(Coins);
        }
        public bool SpendCoins(int amount)
        {
            if (amount < 0 || Coins < amount) return false;
            AddCoins(-amount);
            return true;
        }
        public void NextLevel() { if (!SelectLevel(CurrentLevel % Mathf.Max(1, LevelCount) + 1)) SelectLevel(progress.ResumeLevel); }
        public void RestartLevel() => SelectLevel(CurrentLevel);
        public bool IsLevelUnlocked(int level) => progress.IsUnlocked(level);
        public void UnlockLevel(int level) => progress.UnlockSingle(level);
        public bool SelectLevel(int level)
        {
            if(!progress.Select(level)) return false;
            Time.timeScale = 1;
            CurrentState = GameState.InGame;
            IsDailyRun = false;
            CurrentLevel = level;
            LevelManager.Instance?.LoadLevel(CurrentLevel);
            return true;
        }
        public void EnterMainMenu() => CurrentState = GameState.MainMenu;
        public void SetPaused(bool paused) => CurrentState = paused ? GameState.Paused : GameState.InGame;
        void OnDestroy() { if (Instance == this) Instance = null; Time.timeScale = 1; }
    }
}
