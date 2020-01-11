namespace TX11Shared
{
    public interface IXScreen
    {
        double Dpi { get; }

        double RealWidth { get; }

        double RealHeight { get; }

        void Invalidate();
    }
}