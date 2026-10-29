namespace Game.Core.Levels
{
    public interface ILevelCatalog
    {
        int Count { get; }
        LevelData Get(int index);
        bool HasNext(int index);       // index + 1 < Count; the only answer to "is this the last level?"
    }
}
