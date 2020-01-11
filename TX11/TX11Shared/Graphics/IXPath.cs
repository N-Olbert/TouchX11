using System;

namespace TX11Shared.Graphics
{
    public interface IXPath : IDisposable
    {
        XPathFillType FillType { set; }

        void MoveTo(float x, float y);

        void LineTo(float x, float y);

        void RLineTo(float x, float y);

        void Close();
    }
}