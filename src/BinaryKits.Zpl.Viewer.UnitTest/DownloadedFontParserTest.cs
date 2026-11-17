using System;
using System.IO;
using BinaryKits.Zpl.Label.Helpers;
using BinaryKits.Zpl.Viewer.DownloadedFonts;
using BinaryKits.Zpl.Viewer.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BinaryKits.Zpl.Viewer.UnitTest
{
    [TestClass]
    public class DownloadedFontParserTest
    {
        private const string tinyFont = "~DBR:TINY.FNT,N,5,8,5,4,1,TEST,#0041.5.8.0.5.8.FF818181FF";

        // Zebra ~DB documentation example, legacy O for 0 included
        private const string timesFont =
            "~DBR:TIMES.FNT,N,5,24,3,10,2,ZEBRA 1992,\r\n" +
            "#0025.5.16.2.5.18.\r\n" +
            "OOFF\r\n" +
            "OOFF\r\n" +
            "FFOO\r\n" +
            "FFOO\r\n" +
            "FFFF\r\n" +
            "#0037.4.24.3.6.26.\r\n" +
            "OOFFOO\r\n" +
            "OFOOFO\r\n" +
            "OFOOFO\r\n" +
            "OOFFOO\r\n";

        private static byte[] LoadTriumvirate()
        {
            string relativePath = Path.Combine("zplimgapi", "Resources", "Static", "Fonts", "Triumvirate-CondensedBold.ttf");

            for (DirectoryInfo directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                string path = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(path))
                {
                    return File.ReadAllBytes(path);
                }
            }

            throw new FileNotFoundException(relativePath);
        }

        private static AnalyzeInfo Analyze(PrinterStorage printerStorage, string zpl)
        {
            return new ZplAnalyzer(printerStorage).Analyze(zpl);
        }

        [TestMethod]
        public void ParsesZebraTimesExample()
        {
            bool parsed = ZplBitmapFontParser.TryParse("^XA\r\n" + timesFont + "^XZ\r\n", out PrinterObjectName name, out ZplDownloadedBitmapFont font, out string error);

            Assert.IsTrue(parsed, error);
            using (font)
            {
                Assert.AreEqual("R:TIMES.FNT", name.Key);
                Assert.AreEqual(5, font.CellHeight);
                Assert.AreEqual(24, font.CellWidth);
                Assert.AreEqual(3, font.Baseline);
                Assert.AreEqual(10, font.SpaceWidth);
                Assert.AreEqual("ZEBRA 1992", font.Copyright);
                Assert.HasCount(2, font.Glyphs);

                Assert.IsTrue(font.TryGetGlyph(0x25, out ZplDownloadedBitmapGlyph percent));
                Assert.AreEqual(16, percent.Width);
                Assert.AreEqual(5, percent.Height);
                Assert.AreEqual(2, percent.XOffset);
                Assert.AreEqual(5, percent.YOffset);
                Assert.AreEqual(18, percent.Advance);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0xFF, 0x00, 0xFF, 0xFF, 0x00, 0xFF, 0x00, 0xFF, 0xFF }, percent.Rows.ToArray());
                Assert.IsFalse(percent.IsSet(7, 0));
                Assert.IsTrue(percent.IsSet(8, 0));

                Assert.IsTrue(font.TryGetGlyph(0x37, out ZplDownloadedBitmapGlyph seven));
                Assert.AreEqual(24, seven.Width);
                Assert.AreEqual(4, seven.Height);
                Assert.AreEqual(3, seven.XOffset);
                Assert.AreEqual(6, seven.YOffset);
                Assert.AreEqual(26, seven.Advance);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0xFF, 0x00, 0x0F, 0x00, 0xF0, 0x0F, 0x00, 0xF0, 0x00, 0xFF, 0x00 }, seven.Rows.ToArray());

                Assert.IsFalse(font.TryGetGlyph(0x41, out _));
            }
        }

        [TestMethod]
        public void DefaultsNameAndExtension()
        {
            Assert.IsTrue(ZplBitmapFontParser.TryParse("~DB,N,5,8,5,4,1,TEST,#0041.5.8.0.5.8.FF818181FF", out PrinterObjectName unnamed, out ZplDownloadedBitmapFont first, out string error), error);
            first.Dispose();
            Assert.AreEqual("R:UNKNOWN.FNT", unnamed.Key);

            Assert.IsTrue(ZplBitmapFontParser.TryParse("~DBe:tiny,N,5,8,5,4,1,TEST,#0041.5.8.0.5.8.FF818181FF", out PrinterObjectName named, out ZplDownloadedBitmapFont second, out error), error);
            second.Dispose();
            Assert.AreEqual("E:TINY.FNT", named.Key);
        }

        [TestMethod]
        [DataRow("~DBR:BASE.FNT,N,5,8,0,4,1,TEST,#0041.1.8.0.1.8.80", DisplayName = "baseline 0")]
        [DataRow("~DBR:BASE.FNT,N,5,8,6,4,1,TEST,#0041.1.8.0.1.8.80", DisplayName = "baseline above cell height")]
        [DataRow("~DBR:SPACE.FNT,N,5,8,5,0,1,TEST,#0041.1.8.0.1.8.80", DisplayName = "space 0")]
        [DataRow("~DBR:HUGE.FNT,N,5,8,5,32001,1,TEST,#0041.1.8.0.1.8.80", DisplayName = "space above 32000")]
        [DataRow("~DBR:ROT.FNT,R,5,8,5,4,1,TEST,#0041.1.8.0.1.8.80", DisplayName = "orientation R")]
        [DataRow("~DBR:COPY.FNT,N,5,8,5,4,1,,#0041.1.8.0.1.8.80", DisplayName = "empty copyright")]
        [DataRow("~DBR:COUNT.FNT,N,5,8,5,4,2,TEST,#0041.1.8.0.1.8.80", DisplayName = "fewer glyphs than #char")]
        [DataRow("~DBR:COUNT.FNT,N,5,8,5,4,1,TEST,#0041.1.8.0.1.8.80#0042.1.8.0.1.8.80", DisplayName = "more glyphs than #char")]
        [DataRow("~DBR:SHORT.FNT,N,5,8,5,4,1,TEST,#0041.5.8.0.5.8.FF8181FF", DisplayName = "short payload")]
        [DataRow("~DBR:LONG.FNT,N,5,8,5,4,1,TEST,#0041.5.8.0.5.8.FF818181FF00", DisplayName = "long payload")]
        [DataRow("~DBR:DUP.FNT,N,5,8,5,4,2,TEST,#0041.1.8.0.1.8.80#0041.1.8.0.1.8.80", DisplayName = "duplicate code")]
        [DataRow("~DBR:ZERO.FNT,N,5,8,5,4,1,TEST,#0041.1.0.0.1.8.", DisplayName = "glyph width 0")]
        [DataRow("~DBR:ADV.FNT,N,5,8,5,4,1,TEST,#0041.1.8.0.1.-1.80", DisplayName = "negative advance")]
        [DataRow("~DBR:JUNK.FNT,N,5,8,5,4,1,TEST,XX#0041.1.8.0.1.8.80", DisplayName = "data before first glyph")]
        [DataRow("~DBR:NONE.FNT,N,5,8,5,4,1,TEST,", DisplayName = "no glyphs")]
        public void RejectsInvalidFont(string command)
        {
            Assert.IsFalse(ZplBitmapFontParser.TryParse(command, out _, out ZplDownloadedBitmapFont font, out string error));
            Assert.IsNull(font);
            Assert.IsFalse(string.IsNullOrEmpty(error));
        }

        [TestMethod]
        public void ChecksZ64Crc()
        {
            string z64 = ZebraZ64CompressionHelper.Compress(new byte[] { 0xFF, 0x81, 0x81, 0x81, 0xFF });
            string valid = $"~DBR:Z.FNT,N,5,8,5,4,1,TEST,#0041.5.8.0.5.8.{z64}";

            Assert.IsTrue(ZplBitmapFontParser.TryParse(valid, out _, out ZplDownloadedBitmapFont font, out string error), error);
            using (font)
            {
                Assert.IsTrue(font.TryGetGlyph(0x41, out ZplDownloadedBitmapGlyph glyph));
                CollectionAssert.AreEqual(new byte[] { 0xFF, 0x81, 0x81, 0x81, 0xFF }, glyph.Rows.ToArray());
            }

            string crc = z64.Substring(z64.LastIndexOf(':') + 1);
            string badCrc = ((Convert.ToUInt16(crc, 16) + 1) & 0xFFFF).ToString("x4");
            string invalid = valid.Substring(0, valid.Length - crc.Length) + badCrc;

            Assert.IsFalse(ZplBitmapFontParser.TryParse(invalid, out _, out font, out error));
            Assert.IsNull(font);
            StringAssert.Contains(error, "CRC");
        }

        [TestMethod]
        public void DownloadBitmapFontLandsInStorage()
        {
            using PrinterStorage printerStorage = new();

            AnalyzeInfo analyzeInfo = Analyze(printerStorage, tinyFont);

            Assert.IsEmpty(analyzeInfo.Errors, string.Join(" ", analyzeInfo.Errors));
            Assert.IsEmpty(analyzeInfo.UnknownCommands);
            Assert.IsTrue(printerStorage.TryGetFont('R', "TINY.FNT", out ZplDownloadedFont font));
            ZplDownloadedBitmapFont bitmapFont = (ZplDownloadedBitmapFont)font;
            Assert.AreEqual(4, bitmapFont.SpaceWidth);
            Assert.IsTrue(bitmapFont.TryGetGlyph('A', out ZplDownloadedBitmapGlyph glyph));
            Assert.IsTrue(glyph.IsSet(0, 1));
            Assert.IsFalse(glyph.IsSet(1, 1));
        }

        [TestMethod]
        public void InvalidBitmapFontIsReportedAndNotStored()
        {
            using PrinterStorage printerStorage = new();

            AnalyzeInfo analyzeInfo = Analyze(printerStorage, "~DBR:TINY.FNT,N,5,8,5,4,2,TEST,#0041.5.8.0.5.8.FF818181FF");

            Assert.HasCount(1, analyzeInfo.Errors);
            Assert.IsFalse(printerStorage.TryGetFont('R', "TINY.FNT", out _));
        }

        [TestMethod]
        public void BitmapFontRedownloadReplaces()
        {
            using PrinterStorage printerStorage = new();

            Analyze(printerStorage, tinyFont + "~DBR:TINY.FNT,N,5,8,5,7,1,TEST,#0041.5.8.0.5.8.FF818181FF");

            Assert.IsTrue(printerStorage.TryGetFont('R', "TINY.FNT", out ZplDownloadedFont font));
            Assert.AreEqual(7, ((ZplDownloadedBitmapFont)font).SpaceWidth);
        }

        [TestMethod]
        [DataRow("R:X.TTF", false)]
        [DataRow("R:X.OTF", false)]
        [DataRow("R:X", false)]
        [DataRow("R:X.OTF", true)]
        [DataRow("X", true)]
        public void DownloadObjectTrueTypeLandsAsOutlineFont(string fileName, bool z64)
        {
            byte[] ttf = LoadTriumvirate();
            string data = z64 ? ZebraZ64CompressionHelper.Compress(ttf) : ttf.ToHexFromBytes();
            using PrinterStorage printerStorage = new();

            AnalyzeInfo analyzeInfo = Analyze(printerStorage, $"~DY{fileName},A,T,{ttf.Length},,{data}");

            Assert.IsEmpty(analyzeInfo.Errors, string.Join(" ", analyzeInfo.Errors));
            Assert.IsTrue(printerStorage.TryGetFont('R', "X.TTF", out ZplDownloadedFont font));
            ZplDownloadedOutlineFont outlineFont = (ZplDownloadedOutlineFont)font;
            CollectionAssert.AreEqual(ttf, outlineFont.Data);
            Assert.IsNotNull(outlineFont.Typeface);
            Assert.IsFalse(printerStorage.TryGetFont('R', "X.OTF", out _));
            Assert.IsFalse(printerStorage.TryGetFont('R', "X", out _));
        }

        [TestMethod]
        public void DownloadObjectRejectsBadZ64Crc()
        {
            byte[] ttf = LoadTriumvirate();
            string z64 = ZebraZ64CompressionHelper.Compress(ttf);
            string crc = z64.Substring(z64.LastIndexOf(':') + 1);
            string badZ64 = z64.Substring(0, z64.Length - crc.Length) + ((Convert.ToUInt16(crc, 16) + 1) & 0xFFFF).ToString("x4");
            using PrinterStorage printerStorage = new();

            AnalyzeInfo analyzeInfo = Analyze(printerStorage, $"~DYR:X.TTF,A,T,{ttf.Length},,{badZ64}");

            Assert.HasCount(1, analyzeInfo.Errors);
            Assert.IsFalse(printerStorage.TryGetFont('R', "X.TTF", out _));
        }

        [TestMethod]
        public void DownloadObjectRejectsWrongByteCount()
        {
            byte[] ttf = LoadTriumvirate();
            using PrinterStorage printerStorage = new();

            AnalyzeInfo analyzeInfo = Analyze(printerStorage, $"~DYR:X.TTF,A,T,{ttf.Length + 1},,{ttf.ToHexFromBytes()}");

            Assert.HasCount(1, analyzeInfo.Errors);
            Assert.IsFalse(printerStorage.TryGetFont('R', "X.TTF", out _));
        }

        [TestMethod]
        public void DownloadObjectRejectsBinaryFormat()
        {
            using PrinterStorage printerStorage = new();

            AnalyzeInfo analyzeInfo = Analyze(printerStorage, "~DYR:X.TTF,B,T,4,,true");

            Assert.HasCount(1, analyzeInfo.Errors);
            Assert.IsFalse(printerStorage.TryGetFont('R', "X.TTF", out _));
        }

        [TestMethod]
        public void DownloadObjectRedownloadReplaces()
        {
            byte[] ttf = LoadTriumvirate();
            string command = $"~DYE:X.TTF,A,T,{ttf.Length},,{ttf.ToHexFromBytes()}";
            using PrinterStorage printerStorage = new();

            Analyze(printerStorage, tinyFont.Replace("R:TINY.FNT", "E:X.TTF"));
            Assert.IsInstanceOfType<ZplDownloadedBitmapFont>(Font(printerStorage, 'E', "X.TTF"));

            Analyze(printerStorage, command);
            ZplDownloadedFont first = Font(printerStorage, 'E', "X.TTF");
            Assert.IsInstanceOfType<ZplDownloadedOutlineFont>(first);

            Analyze(printerStorage, command);
            Assert.AreNotSame(first, Font(printerStorage, 'E', "X.TTF"));
        }

        [TestMethod]
        [DataRow("~DYR:X.TTF,A,T,4,,00FF00FF", "X.TTF", DisplayName = "T without sfnt magic")]
        [DataRow("~DYR:LOGO.PNG,P,P,4,,00FF00FF", "LOGO.PNG", DisplayName = "format P")]
        [DataRow("~DYR:IMG.GRF,A,,4,2,00FF00FF", "IMG.GRF", DisplayName = "default extension code")]
        public void DownloadObjectStoresOtherObjectsAsFiles(string command, string fileName)
        {
            using PrinterStorage printerStorage = new();

            AnalyzeInfo analyzeInfo = Analyze(printerStorage, command);

            Assert.IsEmpty(analyzeInfo.Errors, string.Join(" ", analyzeInfo.Errors));
            CollectionAssert.AreEqual(new byte[] { 0x00, 0xFF, 0x00, 0xFF }, printerStorage.GetFile('R', fileName));
            Assert.IsFalse(printerStorage.TryGetFont('R', fileName, out _));
        }

        [TestMethod]
        public void DownloadObjectChecksB64Crc()
        {
            byte[] ttf = LoadTriumvirate();
            string b64 = ZebraB64CompressionHelper.Compress(ttf);
            string crc = b64.Substring(b64.LastIndexOf(':') + 1);
            string badB64 = b64.Substring(0, b64.Length - crc.Length) + ((Convert.ToUInt16(crc, 16) + 1) & 0xFFFF).ToString("x4");
            using PrinterStorage printerStorage = new();

            AnalyzeInfo valid = Analyze(printerStorage, $"~DYR:X.TTF,A,T,{ttf.Length},,{b64}");

            Assert.IsEmpty(valid.Errors, string.Join(" ", valid.Errors));
            CollectionAssert.AreEqual(ttf, ((ZplDownloadedOutlineFont)Font(printerStorage, 'R', "X.TTF")).Data);

            AnalyzeInfo invalid = Analyze(printerStorage, $"~DYR:Y.TTF,A,T,{ttf.Length},,{badB64}");

            Assert.HasCount(1, invalid.Errors);
            Assert.IsFalse(printerStorage.TryGetFont('R', "Y.TTF", out _));
        }

        [TestMethod]
        public void DownloadUnboundedAcceptsZ64()
        {
            byte[] ttf = LoadTriumvirate();
            using PrinterStorage printerStorage = new();

            AnalyzeInfo analyzeInfo = Analyze(printerStorage, $"~DUR:LOGO.TTF,{ttf.Length},{ZebraZ64CompressionHelper.Compress(ttf)}");

            Assert.IsEmpty(analyzeInfo.Errors, string.Join(" ", analyzeInfo.Errors));
            CollectionAssert.AreEqual(ttf, ((ZplDownloadedOutlineFont)Font(printerStorage, 'R', "LOGO.TTF")).Data);
        }

        [TestMethod]
        [DataRow("~DUR:LOGO.TTF,5,00FF00FF", DisplayName = "byte count above the data")]
        [DataRow("~DUR:LOGO.TTF,4,00FF00GG", DisplayName = "not hexadecimal")]
        [DataRow("~DUR:LOGO.TTF,4", DisplayName = "no data")]
        [DataRow("~DU1:LOGO.TTF,4,00FF00FF", DisplayName = "invalid drive")]
        public void DownloadUnboundedRejectsInvalidCommand(string command)
        {
            using PrinterStorage printerStorage = new();

            AnalyzeInfo analyzeInfo = Analyze(printerStorage, command);

            Assert.HasCount(1, analyzeInfo.Errors);
            Assert.IsEmpty(printerStorage.GetFile('R', "LOGO.TTF"));
        }

        [TestMethod]
        [DataRow("~DU", "R:LOGO", "LOGO.FNT")]
        [DataRow("~DU", "R:LOGO.TTF", "LOGO.TTF")]
        [DataRow("~DT", "R:LOGO", "LOGO.DAT")]
        public void DownloadUnboundedTrueTypeLandsAsOutlineFont(string command, string name, string expectedFileName)
        {
            byte[] ttf = LoadTriumvirate();
            using PrinterStorage printerStorage = new();

            AnalyzeInfo analyzeInfo = Analyze(printerStorage, $"{command}{name},{ttf.Length},{ttf.ToHexFromBytes()}");

            Assert.IsEmpty(analyzeInfo.Errors, string.Join(" ", analyzeInfo.Errors));
            Assert.IsEmpty(analyzeInfo.UnknownCommands);
            Assert.IsInstanceOfType<ZplDownloadedOutlineFont>(Font(printerStorage, 'R', expectedFileName));
        }

        [TestMethod]
        public void DownloadUnboundedNonSfntPayloadIsStoredAsFile()
        {
            using PrinterStorage printerStorage = new();

            AnalyzeInfo analyzeInfo = Analyze(printerStorage, "~DUR:ZTOOLS,4,00FF00FF");

            Assert.IsEmpty(analyzeInfo.Errors, string.Join(" ", analyzeInfo.Errors));
            Assert.IsFalse(printerStorage.TryGetFont('R', "ZTOOLS.FNT", out _));
            CollectionAssert.AreEqual(new byte[] { 0x00, 0xFF, 0x00, 0xFF }, printerStorage.GetFile('R', "ZTOOLS.FNT"));
        }

        private static ZplDownloadedFont Font(PrinterStorage printerStorage, char device, string fileName)
        {
            Assert.IsTrue(printerStorage.TryGetFont(device, fileName, out ZplDownloadedFont font), $"{device}:{fileName} not stored");
            return font;
        }
    }
}
