namespace WZSDK
{
    public interface ILoad
    {
        void Load();
    }

    public interface IDispose
    {
        void Dispose();
    }

    public interface IUpdate
    {
        void Update();
    }

    public interface IFixedUpdate
    {
        void FixedUpdate();
    }

    public interface ILateUpdate
    {
        void LateUpdate();
    }
}
