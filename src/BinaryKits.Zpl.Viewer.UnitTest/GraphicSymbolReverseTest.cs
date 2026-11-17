using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkiaSharp;

using System.Linq;

namespace BinaryKits.Zpl.Viewer.UnitTest
{
    [TestClass]
    public sealed class GraphicSymbolReverseTest
    {
        private const string Box = "^FO0,0^GB40,40,40^FS";

        [TestMethod]
        [DataRow("^XA^FO5,5^FR^GSN,20,20^FDA^FS^XZ", true, DisplayName = "^FR")]
        [DataRow("^XA^LRY^FO5,5^GSN,20,20^FDA^FS^XZ", true, DisplayName = "^LR")]
        [DataRow("^XA^FO5,5^GSN,20,20^FDA^FS^XZ", false, DisplayName = "plain")]
        public void GraphicSymbolReadsReversePrint(string zpl, bool expected)
        {
            AnalyzeInfo analyzeInfo = new ZplAnalyzer(new PrinterStorage()).Analyze(zpl);
            ZplGraphicSymbol graphicSymbol = analyzeInfo.LabelInfos[0].ZplElements.OfType<ZplGraphicSymbol>().Single();

            Assert.AreEqual(expected, graphicSymbol.ReversePrint);
        }

        [TestMethod]
        public void ReverseGraphicSymbolClearsTheBoxBehindIt()
        {
            int plain = CountBlackDots($"^XA{Box}^FO5,5^GSN,20,20^FDA^FS^XZ");
            int reversed = CountBlackDots($"^XA{Box}^FO5,5^FR^GSN,20,20^FDA^FS^XZ");

            Assert.AreEqual(40 * 40, plain, "symbol inside the black box");
            Assert.IsLessThan(plain, reversed, "symbol knocked out of the box");
            Assert.IsGreaterThan(0, reversed);
        }

        private static int CountBlackDots(string zpl)
        {
            using PrinterStorage storage = new();
            AnalyzeInfo analyzeInfo = new ZplAnalyzer(storage).Analyze(zpl);
            ZplElementDrawer drawer = new(storage);
            using SKBitmap bitmap = SKBitmap.Decode(drawer.Draw(analyzeInfo.LabelInfos[0].ZplElements, 100, 50, 8));
            int count = 0;

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    SKColor color = bitmap.GetPixel(x, y);
                    if (color.Alpha > 0 && color.Red < 128)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
