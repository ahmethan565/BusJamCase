namespace Game.Core.Levels
{
    public interface ILevelCatalog
    {
        int Count { get; }
        LevelData Get(int index);
        bool HasNext(int index);       // the single "is last level?" check
    }
}
