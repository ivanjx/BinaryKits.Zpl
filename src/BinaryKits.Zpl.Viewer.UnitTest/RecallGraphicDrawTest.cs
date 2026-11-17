using BinaryKits.Zpl.Viewer.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;

namespace BinaryKits.Zpl.Viewer.UnitTest
{
    [TestClass]
    public sealed class RecallGraphicDrawTest
    {
        // 8×2 dots: a full top row and the first dot of the second row
        private const string Graphic = "~DGR:IMG.GRF,2,1,FF80";

        [TestMethod]
        [DataRow(1, 1)]
        [DataRow(2, 3)]
        public void RecallGraphicMagnifiesEachAxis(int magnificationX, int magnificationY)
        {
            HashSet<(int X, int Y)> ink = RenderInk($"{Graphic}^XA^FO10,20^XGR:IMG.GRF,{magnificationX},{magnificationY}^FS^XZ");

            List<(int X, int Y)> expected = [];
            for (int x = 0; x < 8 * magnificationX; x++)
            {
                for (int y = 0; y < magnificationY; y++)
                {
                    expected.Add((10 + x, 20 + y));
                }
            }

            for (int x = 0; x < magnificationX; x++)
            {
                for (int y = magnificationY; y < 2 * magnificationY; y++)
                {
                    expected.Add((10 + x, 20 + y));
                }
            }

            CollectionAssert.AreEquivalent(expected, ink.ToList());
        }

        [TestMethod]
        public void MissingGraphicDrawsNothing()
        {
            Assert.IsEmpty(RenderInk("^XA^FO10,20^XGR:MISSING.GRF,2,2^FS^XZ"));
        }

        private static HashSet<(int X, int Y)> RenderInk(string zpl)
        {
            using PrinterStorage storage = new();
            AnalyzeInfo analyzeInfo = new ZplAnalyzer(storage).Analyze(zpl);

            Assert.IsEmpty(analyzeInfo.Errors, string.Join(Environment.NewLine, analyzeInfo.Errors));

            ZplElementDrawer drawer = new(storage);
            using SKBitmap bitmap = SKBitmap.Decode(drawer.Draw(analyzeInfo.LabelInfos[0].ZplElements, 100, 50, 8));
            HashSet<(int X, int Y)> ink = [];

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (bitmap.GetPixel(x, y).Alpha > 0)
                    {
                        ink.Add((x, y));
                    }
                }
            }

            return ink;
        }
    }
}
