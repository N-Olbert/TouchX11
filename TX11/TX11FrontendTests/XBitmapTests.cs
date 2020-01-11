using Microsoft.VisualStudio.TestTools.UnitTesting;
using TX11Frontend.UIConnectorWrapper;

namespace TX11FrontendTests
{
    [TestClass]
    public class XBitmapTests
    {
        [TestMethod]
        public void TestGetPixelsSimple()
        {
            const string name = "TX11Ressources.Images.xc_x_cursor.png";
            var bitmap = new XBitmapFactory().DecodeResource(name);
            var info = ((XBitmap) bitmap).Bitmap.Info;

            var array = new int[bitmap.Width * bitmap.Height];
            bitmap.GetPixels(array, 0, info.RowBytes, 0, 0, bitmap.Width, bitmap.Height);
            var bitmap2 = new XBitmapFactory().CreateBitmap(array, bitmap.Width, bitmap.Height);
            CollectionAssert.AreEqual(((XBitmap) bitmap).Bitmap.Pixels, ((XBitmap) bitmap2).Bitmap.Pixels);
        }
    }
}