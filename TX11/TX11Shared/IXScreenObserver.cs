using System.Drawing;
using TX11Shared.Graphics;
using TX11Shared.Keyboard;

namespace TX11Shared
{
    public interface IXScreenObserver
    {
        void OnSizeChanged(IXScreen source, Size newSize, Size oldSize);

        void OnTouch(IXScreen source, int x, int y);

        void OnDraw(IXScreen source, IXCanvas canvas);

        void OnKeyDown(IXScreen source, XKeyEvent e);

        void OnKeyUp(IXScreen source, XKeyEvent e);
    }
}