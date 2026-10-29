namespace Game.Core.Progress
{
    public interface IProgressStorage
    {
        SaveData Load();
        void Save(SaveData data);
    }
}
