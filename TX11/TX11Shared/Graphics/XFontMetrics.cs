namespace TX11Shared.Graphics
{
    public class XFontMetrics
    {
        public int Ascent { get; }

        public int Descent { get; }

        public int Bottom { get; }

        public int Top { get; }

        public XFontMetrics(float ascent, float descent, float bottom, float top)
        {
            Ascent = (int) ascent;
            Descent = (int) descent;
            Bottom = (int) bottom;
            Top = (int) top;
        }
    }
}