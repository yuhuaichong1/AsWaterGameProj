namespace XrCode
{
    public enum GameStatus
    {
        None = 0,
        Ready = 1,
        Gaming = 2,
        Moving = 3,
        UsingProp = 4,
        Lose = 5,
        Win = 6,
        Pause = 7,
        Over = 8
    }

    public enum GameProp
    {
        Shuffle = 0,
        Undo = 1,
        AddBottle = 2
    }

    public enum SceneId
    {
        Loading,
        Home,
        Game
    }
}
