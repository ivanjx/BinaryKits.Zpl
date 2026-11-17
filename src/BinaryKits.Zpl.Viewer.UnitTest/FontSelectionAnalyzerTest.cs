using System.Linq;
using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BinaryKits.Zpl.Viewer.UnitTest
{
    [TestClass]
    public class FontSelectionAnalyzerTest
    {
        private static AnalyzeInfo Analyze(string zpl, PrinterStorage printerStorage = null)
        {
            return new ZplAnalyzer(printerStorage ?? new PrinterStorage()).Analyze(zpl);
        }

        private static ZplTextField[] TextFields(AnalyzeInfo analyzeInfo)
        {
            return analyzeInfo.LabelInfos.SelectMany(o => o.ZplElements).OfType<ZplTextField>().ToArray();
        }

        private static ZplFont SingleFont(string zpl)
        {
            AnalyzeInfo analyzeInfo = Analyze(zpl);
            Assert.IsEmpty(analyzeInfo.UnknownCommands, string.Join(" ", analyzeInfo.UnknownCommands));
            return TextFields(analyzeInfo).Single().Font;
        }

        [TestMethod]
        public void FontByNameSetsFileNameAndSize()
        {
            ZplFont font = SingleFont("^XA^FO10,10^A@N,40,30,e:logo.ttf^FDHi^FS^XZ");

            Assert.AreEqual("E:LOGO.TTF", font.FontFileName);
            Assert.AreEqual(40, font.FontHeight);
            Assert.AreEqual(30, font.FontWidth);
            Assert.AreEqual(FieldOrientation.Normal, font.FieldOrientation);
            Assert.AreEqual("A", font.FontName);
        }

        [TestMethod]
        public void FontByNameDefaultsDriveWithoutExtensionProbing()
        {
            ZplFont font = SingleFont("^XA^FO10,10^A@N,100,,TRI^FDHi^FS^XZ");

            Assert.AreEqual("R:TRI", font.FontFileName);
            Assert.AreEqual(100, font.FontHeight);
            Assert.AreEqual(0, font.FontWidth);
        }

        [TestMethod]
        public void FontByNameCarriesOverWithinLabel()
        {
            AnalyzeInfo analyzeInfo = Analyze("^XA^FO10,10^A@N,40,30,E:LOGO.TTF^FDA^FS^FO10,60^A@R,100,100^FDB^FS^XZ^XA^FO10,10^A@N,100,100^FDC^FS^XZ");
            ZplTextField[] fields = TextFields(analyzeInfo);

            Assert.AreEqual("E:LOGO.TTF", fields[1].Font.FontFileName);
            Assert.AreEqual(100, fields[1].Font.FontHeight);
            Assert.AreEqual(FieldOrientation.Rotated90, fields[1].Font.FieldOrientation);
            Assert.IsNull(fields[2].Font.FontFileName);
        }

        [TestMethod]
        public void FontByNameWithoutSizeUsesDefaultFontSize()
        {
            ZplFont font = SingleFont("^XA^CF0,60^FO10,10^A@N,,,E:X.TTF^FDHi^FS^XZ");

            Assert.AreEqual("E:X.TTF", font.FontFileName);
            Assert.AreEqual(60, font.FontHeight);
            Assert.AreEqual(0, font.FontWidth);
            Assert.AreEqual("0", font.FontName);
        }

        [TestMethod]
        public void FontByNameWithoutNameUsesDefaultFont()
        {
            ZplFont font = SingleFont("^XA^CFD,20^FO10,10^A@N,40,30^FDHi^FS^XZ");

            Assert.IsNull(font.FontFileName);
            Assert.AreEqual("D", font.FontName);
            Assert.AreEqual(40, font.FontHeight);
        }

        [TestMethod]
        public void FontByNameUsesFieldOrientationDefault()
        {
            ZplFont font = SingleFont("^XA^FWB^FO10,10^A@,40,30,E:LOGO.TTF^FDHi^FS^XZ");

            Assert.AreEqual(FieldOrientation.Rotated270, font.FieldOrientation);
        }

        [TestMethod]
        public void FontByNameIsNotParsedAsFontAt()
        {
            ZplFont font = SingleFont("^XA^FO10,10^A@N,40,30,E:LOGO.TTF^FDHi^FS^XZ");

            Assert.AreNotEqual("@", font.FontName);
        }

        [TestMethod]
        public void FontByNameWithoutNameUsesDefaultFontAlias()
        {
            ZplFont font = SingleFont("^XA^CWZ,E:LOGO.TTF^CFZ^FO10,10^A@N,40,30^FDHi^FS^XZ");

            Assert.AreEqual("E:LOGO.TTF", font.FontFileName);
            Assert.AreEqual(40, font.FontHeight);
        }

        [TestMethod]
        public void FontByNameWithInvalidNameUsesDefaultFontAndKeepsTheLastName()
        {
            AnalyzeInfo analyzeInfo = Analyze("^XA^CWZ,E:LOGO.TTF^CFZ^FO10,10^A@N,40,30,E:ONE.TTF^FDA^FS^FO10,60^A@N,40,30,1:BAD.TTF^FDB^FS^FO10,110^A@N,40,30^FDC^FS^XZ");
            ZplTextField[] fields = TextFields(analyzeInfo);

            Assert.AreEqual("E:LOGO.TTF", fields[1].Font.FontFileName);
            Assert.AreEqual("E:ONE.TTF", fields[2].Font.FontFileName);
        }

        [TestMethod]
        public void FontIdentifierAliasesScalableFontLetter()
        {
            ZplFont font = SingleFont("^XA^CWZ,E:LOGO.TTF^FO10,10^AZN,40,30^FDHi^FS^XZ");

            Assert.AreEqual("E:LOGO.TTF", font.FontFileName);
            Assert.AreEqual("A", font.FontName);
            Assert.AreEqual(40, font.FontHeight);
            Assert.AreEqual(30, font.FontWidth);
        }

        [TestMethod]
        public void FontIdentifierAliasesDefaultFont()
        {
            AnalyzeInfo analyzeInfo = Analyze("^XA^CWZ,E:LOGO.TTF^CFZ,50^FO10,10^FDA^FS^FO10,60^FB200,2^FDB^FS^FO10,120^TBN,200,100^FDC^FS^CF0^FO10,180^FDD^FS^XZ");
            ZplTextField[] fields = TextFields(analyzeInfo);

            Assert.AreEqual("E:LOGO.TTF", fields[0].Font.FontFileName);
            Assert.AreEqual(50, fields[0].Font.FontHeight);
            Assert.IsInstanceOfType<ZplFieldBlock>(fields[1]);
            Assert.AreEqual("E:LOGO.TTF", fields[1].Font.FontFileName);
            Assert.IsInstanceOfType<ZplTextBlock>(fields[2]);
            Assert.AreEqual("E:LOGO.TTF", fields[2].Font.FontFileName);
            Assert.IsNull(fields[3].Font.FontFileName);
            Assert.AreEqual("0", fields[3].Font.FontName);
        }

        [TestMethod]
        public void FontIdentifierPersistsAcrossLabelsAndLastWriteWins()
        {
            AnalyzeInfo analyzeInfo = Analyze("^XA^CWZ,E:ONE.TTF^XZ^XA^FO10,10^AZN,40,30^FDA^FS^XZ^XA^CWZ,E:TWO.FNT^FO10,10^AZN^FDB^FS^XZ");
            ZplTextField[] fields = TextFields(analyzeInfo);

            Assert.IsEmpty(analyzeInfo.UnknownCommands);
            Assert.AreEqual("E:ONE.TTF", fields[0].Font.FontFileName);
            Assert.AreEqual("E:TWO.FNT", fields[1].Font.FontFileName);
        }

        [TestMethod]
        public void FontIdentifierRejectsInvalidLetter()
        {
            ZplFont font = SingleFont("^XA^CW@,E:LOGO.TTF^FO10,10^AAN,40,30^FDHi^FS^XZ");

            Assert.IsNull(font.FontFileName);
            Assert.AreEqual("A", font.FontName);
        }

        [TestMethod]
        [DataRow("^CWz,E:LOGO.TTF", "^AZN,40,30", DisplayName = "lowercase letter")]
        [DataRow("^CW1,E:LOGO.TTF", "^A1N,40,30", DisplayName = "digit")]
        public void FontIdentifierAcceptsLettersAndDigits(string fontIdentifier, string fontCommand)
        {
            ZplFont font = SingleFont($"^XA{fontIdentifier}^FO10,10{fontCommand}^FDHi^FS^XZ");

            Assert.AreEqual("E:LOGO.TTF", font.FontFileName);
        }

        [TestMethod]
        [DataRow("^CWZ", DisplayName = "no name")]
        [DataRow("^CWZ,", DisplayName = "empty name")]
        [DataRow("^CWZ,1:LOGO.TTF", DisplayName = "invalid drive")]
        [DataRow("^CWZZ,E:LOGO.TTF", DisplayName = "two letters")]
        public void FontIdentifierIgnoresInvalidCommand(string fontIdentifier)
        {
            ZplFont font = SingleFont($"^XA{fontIdentifier}^FO10,10^AZN,40,30^FDHi^FS^XZ");

            Assert.IsNull(font.FontFileName);
            Assert.AreEqual("Z", font.FontName);
        }

        [TestMethod]
        public void FontLinkAddsAndRemovesLink()
        {
            using PrinterStorage printerStorage = new();

            AnalyzeInfo analyzeInfo = Analyze("^XA^FLE:EXT.TTF,E:BASE.TTF,1^FS^XZ", printerStorage);

            Assert.IsEmpty(analyzeInfo.UnknownCommands);
            CollectionAssert.Contains(printerStorage.GetFontLinks("E:BASE.TTF").ToArray(), "E:EXT.TTF");

            Analyze("^XA^FLe:ext.ttf,e:base.ttf,0^FS^XZ", printerStorage);

            CollectionAssert.DoesNotContain(printerStorage.GetFontLinks("E:BASE.TTF").ToArray(), "E:EXT.TTF");
        }

        [TestMethod]
        public void FontLinkIgnoresWildcardExtensionAndInvalidLinkValue()
        {
            using PrinterStorage printerStorage = new();

            Analyze("^XA^FLE:*.TTF,E:BASE.TTF,1^FLE:EXT.TTF,E:BASE.TTF,2^FLE:EXT.TTF,E:BASE.TTF^XZ", printerStorage);

            Assert.IsEmpty(printerStorage.GetFontLinks("E:BASE.TTF"));
        }

        [TestMethod]
        public void FontLinkAcceptsWildcardBase()
        {
            using PrinterStorage printerStorage = new();

            Analyze("^XA^FLE:EXT.TTF,E:*.TTF,1^XZ", printerStorage);

            CollectionAssert.Contains(printerStorage.GetFontLinks("E:BASE.TTF").ToArray(), "E:EXT.TTF");
        }
    }
}
