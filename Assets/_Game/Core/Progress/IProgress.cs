namespace Game.Core.Progress
{
    public interface IProgress
    {
        bool IsUnlocked(int levelIndex);
        bool IsCompleted(int levelIndex);
        void MarkCompleted(int levelIndex);
    }
}
