using System;
using JetBrains.Annotations;
using SkiaSharp;
using TX11Shared.Graphics;

namespace TX11Frontend.UIConnectorWrapper
{
    public sealed class XPath : IXPath
    {
        [NotNull]
        internal SKPath Path { get; } = new SKPath();

        public XPathFillType FillType
        {
            set => Path.FillType = value.ToSKEnum();
        }

        ~XPath()
        {
            Dispose(false);
        }

        public void MoveTo(float x, float y)
        {
            Path.MoveTo(x, y);
        }

        public void LineTo(float x, float y)
        {
            Path.LineTo(x, y);
        }

        public void RLineTo(float x, float y)
        {
            Path.RLineTo(x, y);
        }

        public void Close()
        {
            Path.Close();
        }

        #region IDisposable

        private void ReleaseUnmanagedResources()
        {
            Path.Dispose();
        }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}