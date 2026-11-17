using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Linq;

namespace BinaryKits.Zpl.Viewer.UnitTest
{
    [TestClass]
    public sealed class FieldParameterAnalyzerTest
    {
        [TestMethod]
        [DataRow("9999999999", 9999, DisplayName = "above int range")]
        [DataRow("99999999999999999999999", 9999, DisplayName = "above long range")]
        [DataRow("-9999999999", 0, DisplayName = "below int range")]
        [DataRow("", 0, DisplayName = "empty")]
        public void CharacterGapClampsOutOfRangeValues(string gap, int expected)
        {
            AnalyzeInfo analyzeInfo = new ZplAnalyzer(new PrinterStorage()).Analyze($"^XA^FO10,10^A0N,30^FPV,{gap}^FDA^FS^XZ");
            ZplTextField textField = analyzeInfo.LabelInfos[0].ZplElements.OfType<ZplTextField>().Single();

            Assert.AreEqual(expected, textField.CharacterGap);
        }
    }
}
