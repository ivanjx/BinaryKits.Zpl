using BinaryKits.Zpl.Viewer.DownloadedFonts;
using BinaryKits.Zpl.Viewer.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BinaryKits.Zpl.Viewer.UnitTest
{
    /// <summary>
    /// Downloaded fonts through the whole ZPL pipeline (analyze + draw) at 8 dpmm, docs/custom-fonts.md Tests.
    /// </summary>
    [TestClass]
    public sealed class DownloadedFontEndToEndTest
    {
        private const int Dpmm = 8;

        // 8×5 frame
        private const string TinyFont = "~DBR:TINY.FNT,N,5,8,5,4,1,TEST,#0041.5.8.0.5.8.FF818181FF";

        // Same cell, the 'A' is a single top row
        private const string TinyFontTopRow = "~DBR:TINY.FNT,N,5,8,5,4,1,TEST,#0041.5.8.0.5.8.FF00000000";

        [TestMethod]
        public void BitmapFontThroughFontIdentifierDrawsAtOneTimes()
        {
            HashSet<(int X, int Y)> ink = RenderInk($"{TinyFont}^XA^CWZ,R:TINY.FNT^FO10,10^AZN,5,8^FDA^FS^XZ");

            CollectionAssert.AreEquivalent(Frame(10, 10, 1, 1).ToList(), ink.ToList());
        }

        [TestMethod]
        public void BitmapFontThroughFontByNameDrawsAtTwoTimesWithSpaceAdvance()
        {
            HashSet<(int X, int Y)> ink = RenderInk($"{TinyFont}^XA^FO10,10^A@N,10,16,R:TINY.FNT^FDA A^FS^XZ");

            // A (16 dots) + space (4·2) before the second A
            List<(int X, int Y)> expected = [.. Frame(10, 10, 2, 2), .. Frame(10 + 16 + 8, 10, 2, 2)];
            CollectionAssert.AreEquivalent(expected, ink.ToList());
        }

        [TestMethod]
        public void OutlineFontDownloadedWithDyMatchesLabelaryMetrics()
        {
            byte[] font = File.ReadAllBytes(FindTriumvirate());
            string download = $"~DYE:TRI.TTF,A,T,{font.Length},,{Convert.ToHexString(font)}";

            HashSet<(int X, int Y)> ink = RenderInk($"{download}^XA^FO20,20^A@N,100,100,E:TRI.TTF^FDHH^FS^XZ");

            Assert.AreEqual(20, ink.Min(o => o.Y), "cap top");
            Assert.AreEqual(20 + 74, ink.Max(o => o.Y), "cap bottom");
            Assert.AreEqual(61, GetSecondGlyphAdvance(ink), "H→H advance");
        }

        [TestMethod]
        public void OutlineFontTakesCfSizeWhenBothAxesAreMissing()
        {
            using PrinterStorage storage = CreateSeededStorage();

            HashSet<(int X, int Y)> ink = RenderInk("^XA^CF0,60^FO20,20^A@N,,,E:TRI.TTF^FDHH^FS^XZ", storage);

            // 60×60: cap = 750/1000·60 = 45 rows, baseline y + floor(3·60/4) = y + 45
            Assert.AreEqual(20, ink.Min(o => o.Y), "cap top");
            Assert.AreEqual(20 + 44, ink.Max(o => o.Y), "cap bottom");
            Assert.AreEqual(37, GetSecondGlyphAdvance(ink), "H→H advance 611/1000·60");
        }

        [TestMethod]
        public void FontByNameCarriesOverTheName()
        {
            using PrinterStorage named = CreateSeededStorage();
            using PrinterStorage carried = CreateSeededStorage();

            byte[] expected = Render("^XA^FO20,20^A@N,100,100,E:TRI.TTF^FDHH^FS^XZ", named);
            byte[] actual = Render("^XA^FO300,20^A@N,40,40,E:TRI.TTF^FDX^FS^FO20,20^A@N,100,100^FDHH^FS^XZ", carried);

            HashSet<(int X, int Y)> expectedInk = Decode(expected);
            HashSet<(int X, int Y)> actualInk = Decode(actual).Where(o => o.X < 300).ToHashSet();
            CollectionAssert.AreEquivalent(expectedInk.ToList(), actualInk.ToList());
        }

        [TestMethod]
        public void RedownloadReplacesTheFont()
        {
            HashSet<(int X, int Y)> ink = RenderInk($"{TinyFont}{TinyFontTopRow}^XA^FO10,10^A@N,5,8,R:TINY.FNT^FDA^FS^XZ");

            CollectionAssert.AreEquivalent(Enumerable.Range(10, 8).Select(x => (x, 10)).ToList(), ink.ToList());
        }

        [TestMethod]
        public void InlineDownloadBeatsSeededFont()
        {
            using PrinterStorage storage = new();
            storage.AddFont('R', "TINY.FNT", new ZplDownloadedBitmapFont(
                5,
                8,
                5,
                4,
                "SEEDED",
                [new ZplDownloadedBitmapGlyph(0x41, 8, 5, 0, 5, 8, [0xFF, 0x81, 0x81, 0x81, 0xFF])]));

            HashSet<(int X, int Y)> ink = RenderInk($"{TinyFontTopRow}^XA^FO10,10^A@N,5,8,R:TINY.FNT^FDA^FS^XZ", storage);

            CollectionAssert.AreEquivalent(Enumerable.Range(10, 8).Select(x => (x, 10)).ToList(), ink.ToList());
        }

        [TestMethod]
        [DataRow("R:TRI.TTF")]
        [DataRow("TRI.TTF")]
        [DataRow("E:TRI")]
        [DataRow("E:TRI.OTF")]
        [DataRow("B:TRI.TTF")]
        public void FontSeededOnFlashFallsBackForOtherNames(string reference)
        {
            using PrinterStorage seeded = CreateSeededStorage();
            using PrinterStorage plain = new();

            byte[] fontA = Render("^XA^FO20,20^AAN,40,40^FDHH^FS^XZ", plain);
            byte[] fontB = Render("^XA^CFB^FO20,20^ABN,40,40^FDHH^FS^XZ", plain);

            Assert.IsNotEmpty(Decode(fontA), "font A has ink");
            CollectionAssert.AreNotEqual(fontA, fontB, "fonts A and B differ");

            CollectionAssert.AreEqual(fontA, Render($"^XA^FO20,20^A@N,40,40,{reference}^FDHH^FS^XZ", seeded), "no ^CF: font A");
            CollectionAssert.AreEqual(fontB, Render($"^XA^CFB^FO20,20^A@N,40,40,{reference}^FDHH^FS^XZ", seeded), "^CF font");
        }

        [TestMethod]
        public void FontAddedWithoutOwnershipSurvivesStorageDisposal()
        {
            using ZplDownloadedOutlineFont font = new(File.ReadAllBytes(FindTriumvirate()));
            byte[] first;

            using (PrinterStorage storage = new())
            {
                storage.AddFont('E', "TRI.TTF", font, ownsFont: false);
                first = Render("^XA^FO20,20^A@N,100,100,E:TRI.TTF^FDHH^FS^XZ", storage);
            }

            using PrinterStorage next = new();
            next.AddFont('E', "TRI.TTF", font, ownsFont: false);

            Assert.AreEqual(61, GetSecondGlyphAdvance(Decode(first)), "drawn with the outline font");
            CollectionAssert.AreEqual(first, Render("^XA^FO20,20^A@N,100,100,E:TRI.TTF^FDHH^FS^XZ", next));
        }

        private static PrinterStorage CreateSeededStorage()
        {
            PrinterStorage storage = new();
            storage.AddFont('E', "TRI.TTF", new ZplDownloadedOutlineFont(File.ReadAllBytes(FindTriumvirate())));
            return storage;
        }

        private static IEnumerable<(int X, int Y)> Frame(int x, int y, int magnificationX, int magnificationY)
        {
            byte[] rows = [0xFF, 0x81, 0x81, 0x81, 0xFF];

            for (int row = 0; row < rows.Length; row++)
            {
                for (int column = 0; column < 8; column++)
                {
                    if ((rows[row] & (0x80 >> column)) == 0)
                    {
                        continue;
                    }

                    for (int dy = 0; dy < magnificationY; dy++)
                    {
                        for (int dx = 0; dx < magnificationX; dx++)
                        {
                            yield return (x + column * magnificationX + dx, y + row * magnificationY + dy);
                        }
                    }
                }
            }
        }

        private static byte[] Render(string zpl, PrinterStorage storage)
        {
            AnalyzeInfo analyzeInfo = new ZplAnalyzer(storage).Analyze(zpl);

            Assert.IsEmpty(analyzeInfo.Errors, string.Join(Environment.NewLine, analyzeInfo.Errors));
            Assert.HasCount(1, analyzeInfo.LabelInfos);

            ZplElementDrawer drawer = new(storage);
            return drawer.Draw(analyzeInfo.LabelInfos[0].ZplElements, 100, 50, Dpmm);
        }

        private static HashSet<(int X, int Y)> RenderInk(string zpl, PrinterStorage storage = null)
        {
            using PrinterStorage ownStorage = storage == null ? new PrinterStorage() : null;
            return Decode(Render(zpl, storage ?? ownStorage));
        }

        private static HashSet<(int X, int Y)> Decode(byte[] png)
        {
            using SKBitmap bitmap = SKBitmap.Decode(png);
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

        private static int GetSecondGlyphAdvance(HashSet<(int X, int Y)> ink)
        {
            int[] columns = [.. ink.Select(o => o.X).Distinct().Order()];
            List<int> runStarts = [columns[0]];

            for (int i = 1; i < columns.Length; i++)
            {
                if (columns[i] != columns[i - 1] + 1)
                {
                    runStarts.Add(columns[i]);
                }
            }

            Assert.HasCount(2, runStarts, "two separate H");
            return runStarts[1] - runStarts[0];
        }

        private static string FindTriumvirate()
        {
            string relativePath = Path.Combine("zplimgapi", "Resources", "Static", "Fonts", "Triumvirate-CondensedBold.ttf");

            for (DirectoryInfo directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                string path = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            throw new FileNotFoundException("Triumvirate-CondensedBold.ttf not found above the test directory.", relativePath);
        }
    }
}
