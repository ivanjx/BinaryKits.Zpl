using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;

namespace BinaryKits.Zpl.Viewer.UnitTest
{
    [TestClass]
    public sealed class PrinterObjectNameTest
    {
        [TestMethod]
        [DataRow("e:logo.ttf", null, "E:LOGO.TTF", DisplayName = "uppercased")]
        [DataRow(" LOGO ", ".FNT", "R:LOGO.FNT", DisplayName = "default drive and extension")]
        [DataRow("E:LOGO.TTF", ".FNT", "E:LOGO.TTF", DisplayName = "extension kept")]
        [DataRow("LOGO", null, "R:LOGO", DisplayName = "no extension probing")]
        [DataRow("E: LOGO.TTF", null, "E:LOGO.TTF", DisplayName = "space after the drive")]
        public void ParsesName(string raw, string defaultExtension, string expectedKey)
        {
            Assert.IsTrue(PrinterObjectName.TryParse(raw, defaultExtension, out PrinterObjectName name));
            Assert.AreEqual(expectedKey, name.Key);
            Assert.AreEqual(expectedKey, PrinterObjectName.Parse(raw, defaultExtension).Key);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(" ")]
        [DataRow("E:")]
        [DataRow("E: ")]
        [DataRow("1:LOGO.TTF")]
        public void RejectsName(string raw)
        {
            Assert.IsFalse(PrinterObjectName.TryParse(raw, ".FNT", out _));
            Assert.ThrowsExactly<FormatException>(() => PrinterObjectName.Parse(raw, ".FNT"));
        }
    }
}
