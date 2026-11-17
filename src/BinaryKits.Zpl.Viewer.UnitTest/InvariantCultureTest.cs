using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Globalization;
using System.Linq;

namespace BinaryKits.Zpl.Viewer.UnitTest
{
    /// <summary>
    /// ZPL decimals use a dot whatever the culture of the host (de-DE reads "2.5" as 25).
    /// </summary>
    [TestClass]
    public sealed class InvariantCultureTest
    {
        private CultureInfo originalCulture;

        [TestInitialize]
        public void UseCommaDecimalCulture()
        {
            this.originalCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        }

        [TestCleanup]
        public void RestoreCulture()
        {
            CultureInfo.CurrentCulture = this.originalCulture;
        }

        [TestMethod]
        public void BarcodeDefaultReadsDecimalRatio()
        {
            ZplBarcode39 barcode = SingleElement<ZplBarcode39>("^XA^BY3,2.5,40^FO10,10^B3N^FD123^FS^XZ");

            Assert.AreEqual(2.5, barcode.WideBarToNarrowBarWidthRatio);
        }

        [TestMethod]
        [DataRow("^FO", DisplayName = "^FO")]
        [DataRow("^FT", DisplayName = "^FT")]
        public void FieldPositionTruncatesDecimalCoordinates(string command)
        {
            ZplTextField textField = SingleElement<ZplTextField>($"^XA{command}10.5,20.9^A0N,20^FDX^FS^XZ");

            Assert.AreEqual(10, textField.PositionX);
            Assert.AreEqual(20, textField.PositionY);
        }

        private static T SingleElement<T>(string zpl) where T : ZplElementBase
        {
            AnalyzeInfo analyzeInfo = new ZplAnalyzer(new PrinterStorage()).Analyze(zpl);

            return analyzeInfo.LabelInfos.SelectMany(o => o.ZplElements).OfType<T>().Single();
        }
    }
}
