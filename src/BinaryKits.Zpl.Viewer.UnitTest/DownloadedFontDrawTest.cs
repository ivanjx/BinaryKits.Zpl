using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.DownloadedFonts;
using BinaryKits.Zpl.Viewer.ElementDrawers;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BinaryKits.Zpl.Viewer.UnitTest
{
    /// <summary>
    /// Drawing with downloaded fonts (~DB .FNT and TrueType) at 8 dpmm; outline numbers are the Labelary probes of
    /// docs/custom-fonts.md.
    /// </summary>
    [TestClass]
    public sealed class DownloadedFontDrawTest
    {
        private const int Dpmm = 8;

        [TestMethod]
        public void BitmapFontDrawsGlyphDotsAtOneTimes()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextField("A", 10, 20, new ZplFont(0, 0, "A", fontFileName: "E:TINY.FNT")));

            CollectionAssert.AreEquivalent(GlyphA(10, 20, 1, 1).ToList(), ink.ToList());
        }

        [TestMethod]
        public void BitmapFontDrawsGlyphDotsAtTwoTimes()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextField("A", 10, 20, new ZplFont(16, 10, "A", fontFileName: "E:TINY.FNT")));

            CollectionAssert.AreEquivalent(GlyphA(10, 20, 2, 2).ToList(), ink.ToList());
        }

        [TestMethod]
        [DataRow("A A", 1, DisplayName = "space 1x")]
        [DataRow("ABA", 1, DisplayName = "missing glyph 1x")]
        [DataRow("A A", 2, DisplayName = "space 2x")]
        [DataRow("ABA", 2, DisplayName = "missing glyph 2x")]
        public void BitmapFontSpaceAndMissingGlyphAdvanceBySpaceWidth(string text, int magnification)
        {
            using PrinterStorage storage = CreateStorage();
            ZplFont font = new(8 * magnification, 5 * magnification, "A", fontFileName: "E:TINY.FNT");

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextField(text, 10, 20, font));

            // advance 8 for A, spaceWidth 4 for the space or the missing B
            int secondX = 10 + (8 + 4) * magnification;
            List<(int X, int Y)> expected = [.. GlyphA(10, 20, magnification, magnification), .. GlyphA(secondX, 20, magnification, magnification)];
            CollectionAssert.AreEquivalent(expected, ink.ToList());
        }

        [TestMethod]
        public void OutlineFontAtFieldOriginPutsCapBetweenYAndBaseline()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextField("HH", 50, 50, new ZplFont(100, 100, "A", fontFileName: "E:TRI.TTF")));

            Assert.AreEqual(50, ink.Min(o => o.Y), "cap top row");
            Assert.AreEqual(50 + 74, ink.Max(o => o.Y), "cap bottom row");
            Assert.AreEqual(61, GetSecondGlyphAdvance(ink), "H→H advance");
        }

        [TestMethod]
        public void OutlineFontScalesAdvancesByWidth()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextField("HH", 50, 50, new ZplFont(50, 100, "A", fontFileName: "E:TRI.TTF")));

            Assert.AreEqual(31, GetSecondGlyphAdvance(ink), "H→H advance");
            Assert.AreEqual(50 + 74, ink.Max(o => o.Y), "cap bottom row");
        }

        [TestMethod]
        public void OutlineFontAtFieldTypesetPutsBaselineOnY()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextField("HH", 50, 200, new ZplFont(100, 100, "A", fontFileName: "E:TRI.TTF"), bottomToTop: true));

            Assert.AreEqual(199, ink.Max(o => o.Y), "cap bottom row");
            Assert.AreEqual(200 - 75, ink.Min(o => o.Y), "cap top row");
        }

        [TestMethod]
        [DataRow("E:MISSING.TTF", DisplayName = "unknown name")]
        [DataRow("R:TRI.TTF", DisplayName = "other drive")]
        [DataRow("E:TRI", DisplayName = "no extension")]
        [DataRow("E:BAD.TTF", DisplayName = "sfnt magic Skia cannot load")]
        public void UnresolvedFontPrintsWithFontName(string fontFileName)
        {
            using PrinterStorage storage = CreateStorage();

            byte[] named = Render(storage, new ZplTextField("HELLO", 20, 20, new ZplFont(30, 40, "A", fontFileName: fontFileName)));
            byte[] plain = Render(storage, new ZplTextField("HELLO", 20, 20, new ZplFont(30, 40, "A")));

            CollectionAssert.AreEqual(plain, named);
        }

        [TestMethod]
        public void OutlineFieldBlockWrapsByRealAdvances()
        {
            using PrinterStorage storage = CreateStorage();
            ZplFont font = new(100, 100, "A", fontFileName: "E:TRI.TTF");

            Assert.IsTrue(ZplDownloadedFontText.TryCreate(font, storage, Dpmm, out ZplDownloadedFontText text));
            float wordWidth;
            float lineWidth;
            using (text)
            {
                wordWidth = text.MeasureText("HH");
                lineWidth = text.MeasureText("HH HH");
            }

            // H advance 611/1000 em: 2 × 61.1 dots
            Assert.AreEqual(122.2f, wordWidth, 0.01f);

            int narrow = (int)MathF.Floor(lineWidth) - 1;
            HashSet<(int X, int Y)> wrapped = RenderInk(storage, new ZplFieldBlock("HH HH", 20, 20, narrow, font, maxLineCount: 2));

            Assert.AreEqual(20 + 74, wrapped.Where(o => o.Y < 20 + 100).Max(o => o.Y), "first line cap bottom");
            Assert.AreEqual(20 + 100, wrapped.Where(o => o.Y >= 20 + 100).Min(o => o.Y), "second line cap top");
            Assert.AreEqual(20 + 174, wrapped.Max(o => o.Y), "second line cap bottom");

            int wide = (int)MathF.Ceiling(lineWidth);
            HashSet<(int X, int Y)> single = RenderInk(storage, new ZplFieldBlock("HH HH", 20, 20, wide, font, maxLineCount: 2));

            Assert.AreEqual(20 + 74, single.Max(o => o.Y), "one line");
            Assert.HasCount(4, GetColumnRuns(single), "four H on the line");
        }

        [TestMethod]
        public void MissingGlyphComesFromLinkedFont()
        {
            using PrinterStorage storage = CreateStorage();

            // .FNT at 10×; the linked TrueType font gets the same h/w through its own rules (em 50, scale 80/50)
            ZplTextField field = new("AH", 10, 20, new ZplFont(80, 50, "A", fontFileName: "E:TINY.FNT"));

            HashSet<(int X, int Y)> unlinked = RenderInk(storage, field);
            CollectionAssert.AreEquivalent(GlyphA(10, 20, 10, 10).ToList(), unlinked.ToList());

            storage.SetFontLink("E:TRI.TTF", "E:TINY.FNT", true);
            HashSet<(int X, int Y)> linked = RenderInk(storage, field);

            // H after the A advance (8 × 10), on the .FNT baseline (5 × 10 dots below the cell top)
            List<(int X, int Y)> linkedH = [.. linked.Where(o => o.X >= 10 + 80)];
            Assert.IsNotEmpty(linkedH, "linked H has ink");
            Assert.AreEqual(20 + 49, linkedH.Max(o => o.Y), "linked H sits on the base baseline");
        }

        [TestMethod]
        [DataRow(0, 10, 2, 2, DisplayName = "height only")]
        [DataRow(16, 0, 2, 2, DisplayName = "width only")]
        [DataRow(12, 13, 2, 3, DisplayName = "per axis, rounded half away from zero")]
        [DataRow(4, 2, 1, 1, DisplayName = "clamped to 1")]
        [DataRow(1000, 1000, 10, 10, DisplayName = "clamped to 10")]
        public void BitmapFontMagnifiesEachAxisByTheCell(int fontWidth, int fontHeight, int magnificationX, int magnificationY)
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextField("A", 10, 20, new ZplFont(fontWidth, fontHeight, "A", fontFileName: "E:TINY.FNT")));

            CollectionAssert.AreEquivalent(GlyphA(10, 20, magnificationX, magnificationY).ToList(), ink.ToList());
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        public void BitmapGlyphOffsetsPlaceTheInkInsideTheCell(int magnification)
        {
            using PrinterStorage storage = CreateStorage();
            ZplFont font = new(8 * magnification, 8 * magnification, "A", fontFileName: "E:OFS.FNT");

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextField("A", 10, 20, font));

            // x offset 2; ascent 3 above the baseline 6 → 3 rows below the cell top
            CollectionAssert.AreEquivalent(Dots(10 + 2 * magnification, 20 + 3 * magnification, [0xF0, 0xF0], magnification, magnification).ToList(), ink.ToList());
        }

        [TestMethod]
        public void BitmapGlyphTallerThanTheBaselineStartsAtTheCellTop()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextField("B", 10, 20, new ZplFont(0, 0, "A", fontFileName: "E:OFS.FNT")));

            CollectionAssert.AreEquivalent(new List<(int X, int Y)> { (10, 20) }, ink.ToList());
        }

        [TestMethod]
        [DataRow(100, 0, DisplayName = "width only")]
        [DataRow(0, 100, DisplayName = "height only")]
        public void OutlineFontTakesTheMissingAxisFromTheOther(int fontWidth, int fontHeight)
        {
            using PrinterStorage storage = CreateStorage();

            byte[] expected = Render(storage, new ZplTextField("HH", 50, 50, new ZplFont(100, 100, "A", fontFileName: "E:TRI.TTF")));
            byte[] actual = Render(storage, new ZplTextField("HH", 50, 50, new ZplFont(fontWidth, fontHeight, "A", fontFileName: "E:TRI.TTF")));

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void OutlineFontWithoutSizeTakesTheFontACell()
        {
            using PrinterStorage storage = CreateStorage();

            byte[] expected = Render(storage, new ZplTextField("HH", 50, 50, new ZplFont(5, 9, "A", fontFileName: "E:TRI.TTF")));
            byte[] actual = Render(storage, new ZplTextField("HH", 50, 50, new ZplFont(0, 0, "A", fontFileName: "E:TRI.TTF")));

            Assert.IsNotEmpty(Decode(expected), "9×5 field has ink");
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CharacterGapFollowsEveryGlyph()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextField("AB", 10, 20, OneTimes("E:AB.FNT"), characterGap: 3));

            CollectionAssert.AreEquivalent(Expected(GlyphA(10, 20, 1, 1), TopRow(10 + 8 + 3, 20)), ink.ToList());
        }

        [TestMethod]
        public void VerticalFieldStacksCellsWithTheGap()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextField("AB", 10, 20, OneTimes("E:AB.FNT"), fieldDirection: FieldDirection.Vertical, characterGap: 2));

            CollectionAssert.AreEquivalent(Expected(GlyphA(10, 20, 1, 1), TopRow(10, 20 + 5 + 2)), ink.ToList());
        }

        [TestMethod]
        public void ReverseFieldEndsOneCellRightOfX()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextField("AB", 10, 20, OneTimes("E:AB.FNT"), fieldDirection: FieldDirection.Reverse));

            // "BA" (16 dots) ends one cell width (8) right of x
            CollectionAssert.AreEquivalent(Expected(TopRow(10 + 8 - 16, 20), GlyphA(10 + 8 - 8, 20, 1, 1)), ink.ToList());
        }

        [TestMethod]
        public void RightJustifiedFieldOriginEndsTheTextAtX()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextField("AB", 50, 20, OneTimes("E:AB.FNT"), fieldJustification: FieldJustification.Right));

            CollectionAssert.AreEquivalent(Expected(GlyphA(50 - 16, 20, 1, 1), TopRow(50 - 8, 20)), ink.ToList());
        }

        [TestMethod]
        [DataRow(FieldJustification.Left, 50)]
        [DataRow(FieldJustification.Right, 50 - 8)]
        public void FieldTypesetPutsTheBaselineOnY(FieldJustification fieldJustification, int expectedX)
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextField("A", 50, 30, OneTimes("E:TINY.FNT"), bottomToTop: true, fieldJustification: fieldJustification));

            // baseline 5: the cell top is 5 dots above y
            CollectionAssert.AreEquivalent(GlyphA(expectedX, 30 - 5, 1, 1).ToList(), ink.ToList());
        }

        [TestMethod]
        [DataRow(FieldOrientation.Normal, 10, 20, 1, 0)]
        [DataRow(FieldOrientation.Rotated90, 14, 20, 0, 1)]
        [DataRow(FieldOrientation.Rotated180, 17, 24, -1, 0)]
        [DataRow(FieldOrientation.Rotated270, 10, 27, 0, -1)]
        public void RotatedFieldOriginKeepsTheBoxTopLeftOnTheOrigin(FieldOrientation fieldOrientation, int firstX, int firstY, int stepX, int stepY)
        {
            using PrinterStorage storage = CreateStorage();
            ZplFont font = new(0, 0, "A", fieldOrientation, "E:AB.FNT");

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextField("B", 10, 20, font));

            // B is the top row of an 8×5 cell: the glyph top faces up (N), right (R), down (I) or left (B)
            List<(int X, int Y)> expected = [.. Enumerable.Range(0, 8).Select(i => (firstX + i * stepX, firstY + i * stepY))];
            CollectionAssert.AreEquivalent(expected, ink.ToList());
        }

        [TestMethod]
        public void ReversePrintClearsTheGlyphDots()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(
                storage,
                new ZplGraphicBox(0, 0, 40, 40, 40),
                new ZplTextField("A", 10, 20, OneTimes("E:TINY.FNT"), reversePrint: true));

            HashSet<(int X, int Y)> expected = [.. Enumerable.Range(0, 40).SelectMany(x => Enumerable.Range(0, 40).Select(y => (x, y)))];
            expected.ExceptWith(GlyphA(10, 20, 1, 1));
            CollectionAssert.AreEquivalent(expected.ToList(), ink.ToList());
        }

        [TestMethod]
        public void TextBlockWrapsByAdvancesOneCellApart()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextBlock("A A A", 10, 20, 20, 100, OneTimes("E:TINY.FNT")));

            // "A A" is 8 + 4 + 8 = 20 dots and fits; the third A wraps one cell (5 dots) down
            CollectionAssert.AreEquivalent(Expected(GlyphA(10, 20, 1, 1), GlyphA(22, 20, 1, 1), GlyphA(10, 25, 1, 1)), ink.ToList());
        }

        [TestMethod]
        public void TextBlockCutsTheLastLineAtTheBlockBottom()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextBlock("A A A A A", 10, 20, 20, 7, OneTimes("E:TINY.FNT")));

            // Block rows 20..26: the second line keeps its top 2 rows, the third line starts below the block
            List<(int X, int Y)> expected = Expected(GlyphA(10, 20, 1, 1), GlyphA(22, 20, 1, 1), GlyphA(10, 25, 1, 1), GlyphA(22, 25, 1, 1));
            CollectionAssert.AreEquivalent(expected.Where(o => o.Y < 27).ToList(), ink.ToList());
        }

        [TestMethod]
        public void TextBlockFieldTypesetPutsTheBlockBottomOnY()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextBlock("A A A", 10, 40, 20, 10, OneTimes("E:TINY.FNT"), bottomToTop: true));

            CollectionAssert.AreEquivalent(Expected(GlyphA(10, 30, 1, 1), GlyphA(22, 30, 1, 1), GlyphA(10, 35, 1, 1)), ink.ToList());
        }

        [TestMethod]
        public void RightJustifiedTextBlockEndsAtXAndAlignsEveryLine()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplTextBlock("A A A", 50, 20, 20, 100, OneTimes("E:TINY.FNT"), fieldJustification: FieldJustification.Right));

            CollectionAssert.AreEquivalent(Expected(GlyphA(30, 20, 1, 1), GlyphA(42, 20, 1, 1), GlyphA(42, 25, 1, 1)), ink.ToList());
        }

        [TestMethod]
        [DataRow(TextJustification.Left, 10)]
        [DataRow(TextJustification.Center, 10 + 6)]
        [DataRow(TextJustification.Right, 10 + 12)]
        public void FieldBlockJustifiesByTheGlyphAdvance(TextJustification textJustification, int expectedX)
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplFieldBlock("A", 10, 20, 20, OneTimes("E:TINY.FNT"), textJustification: textJustification));

            CollectionAssert.AreEquivalent(GlyphA(expectedX, 20, 1, 1).ToList(), ink.ToList());
        }

        [TestMethod]
        public void FieldBlockLinesAreOneCellPlusLineSpaceApart()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplFieldBlock("A A", 10, 20, 8, OneTimes("E:TINY.FNT"), maxLineCount: 2, lineSpace: 3));

            CollectionAssert.AreEquivalent(Expected(GlyphA(10, 20, 1, 1), GlyphA(10, 20 + 5 + 3, 1, 1)), ink.ToList());
        }

        [TestMethod]
        public void FieldBlockFieldTypesetPutsTheLastLineBaselineOnY()
        {
            using PrinterStorage storage = CreateStorage();

            HashSet<(int X, int Y)> ink = RenderInk(storage, new ZplFieldBlock("A A", 10, 40, 8, OneTimes("E:TINY.FNT"), maxLineCount: 2, bottomToTop: true));

            CollectionAssert.AreEquivalent(Expected(GlyphA(10, 30, 1, 1), GlyphA(10, 35, 1, 1)), ink.ToList());
        }

        private static PrinterStorage CreateStorage()
        {
            PrinterStorage storage = new();

            // ~DBR:TINY.FNT,N,5,8,5,4,1,TEST,#0041.5.8.0.5.8.FF818181FF
            storage.AddFont('E', "TINY.FNT", new ZplDownloadedBitmapFont(
                5,
                8,
                5,
                4,
                "TEST",
                [new ZplDownloadedBitmapGlyph(0x41, 8, 5, 0, 5, 8, [0xFF, 0x81, 0x81, 0x81, 0xFF])]));

            // Same cell, A is the frame and B its top row only (shows the glyph orientation)
            storage.AddFont('E', "AB.FNT", new ZplDownloadedBitmapFont(
                5,
                8,
                5,
                4,
                "TEST",
                [
                    new ZplDownloadedBitmapGlyph(0x41, 8, 5, 0, 5, 8, [0xFF, 0x81, 0x81, 0x81, 0xFF]),
                    new ZplDownloadedBitmapGlyph(0x42, 8, 5, 0, 5, 8, [0xFF, 0x00, 0x00, 0x00, 0x00])
                ]));

            // 8×8 cell, baseline 6: A is a 4×2 block 2 dots right and 3 dots above the baseline, B starts above the cell
            storage.AddFont('E', "OFS.FNT", new ZplDownloadedBitmapFont(
                8,
                8,
                6,
                4,
                "TEST",
                [
                    new ZplDownloadedBitmapGlyph(0x41, 4, 2, 2, 3, 8, [0xF0, 0xF0]),
                    new ZplDownloadedBitmapGlyph(0x42, 1, 1, 0, 9, 8, [0x80])
                ]));

            storage.AddFont('E', "TRI.TTF", new ZplDownloadedOutlineFont(File.ReadAllBytes(FindTriumvirate())));
            storage.AddFont('E', "BAD.TTF", new ZplDownloadedOutlineFont([0x00, 0x01, 0x00, 0x00, 0xDE, 0xAD, 0xBE, 0xEF]));

            return storage;
        }

        /// <summary>
        /// A downloaded .FNT font at 1× (no h/w).
        /// </summary>
        private static ZplFont OneTimes(string fontFileName)
        {
            return new ZplFont(0, 0, "A", fontFileName: fontFileName);
        }

        private static List<(int X, int Y)> Expected(params IEnumerable<(int X, int Y)>[] parts)
        {
            return [.. parts.SelectMany(o => o)];
        }

        /// <summary>
        /// Dots of the TINY.FNT 'A' (a 8×5 frame) with its top-left corner at (x, y).
        /// </summary>
        private static IEnumerable<(int X, int Y)> GlyphA(int x, int y, int magnificationX, int magnificationY)
        {
            return Dots(x, y, [0xFF, 0x81, 0x81, 0x81, 0xFF], magnificationX, magnificationY);
        }

        /// <summary>
        /// Dots of the AB.FNT 'B' (the top row of the 8×5 cell) at 1×.
        /// </summary>
        private static IEnumerable<(int X, int Y)> TopRow(int x, int y)
        {
            return Dots(x, y, [0xFF], 1, 1);
        }

        /// <summary>
        /// Dots of an 8 dot wide glyph with its top-left ink corner at (x, y).
        /// </summary>
        private static IEnumerable<(int X, int Y)> Dots(int x, int y, byte[] rows, int magnificationX, int magnificationY)
        {
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

        private static byte[] Render(PrinterStorage storage, params ZplElementBase[] elements)
        {
            ZplElementDrawer drawer = new(storage, new DrawerOptions());
            return drawer.Draw(elements, 100, 50, Dpmm);
        }

        private static HashSet<(int X, int Y)> RenderInk(PrinterStorage storage, params ZplElementBase[] elements)
        {
            return Decode(Render(storage, elements));
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

        /// <summary>
        /// Contiguous runs of inked columns as (first, last); every H is one run (the crossbar joins the stems).
        /// </summary>
        private static List<(int First, int Last)> GetColumnRuns(HashSet<(int X, int Y)> ink)
        {
            int[] columns = [.. ink.Select(o => o.X).Distinct().Order()];
            List<(int First, int Last)> runs = [];

            foreach (int column in columns)
            {
                if (runs.Count > 0 && runs[^1].Last == column - 1)
                {
                    runs[^1] = (runs[^1].First, column);
                }
                else
                {
                    runs.Add((column, column));
                }
            }

            return runs;
        }

        private static int GetSecondGlyphAdvance(HashSet<(int X, int Y)> ink)
        {
            List<(int First, int Last)> runs = GetColumnRuns(ink);
            Assert.HasCount(2, runs, "two separate H");

            return runs[1].First - runs[0].First;
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
