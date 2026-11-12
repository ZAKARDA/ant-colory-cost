namespace Antchemy
{
    public enum GameState
    {
        MainMenu,
        InGame,
        Win,
        Lose,
        Paused
    }

    public enum LoseReason
    {
        None,
        OutOfSlot,
        LockStuck,
        TimeOut
    }

    public enum BoosterType
    {
        None,
        Balloon,
        VacuumCleaner,
        AddSlot,
        Shuffle
    }

    public enum ColonyState
    {
        InQueue,
        MovingToSlot,
        ActiveShooting,
        Completed
    }
}
