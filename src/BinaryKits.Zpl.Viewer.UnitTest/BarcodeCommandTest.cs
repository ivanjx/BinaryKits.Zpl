using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.ElementDrawers;
using BinaryKits.Zpl.Viewer.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;

namespace BinaryKits.Zpl.Viewer.UnitTest
{
    /// <summary>
    /// Barcode commands added in this fork: ^BX parameters, ^B0, ^BQ defaults, ^BI and the ^BA check digit line.
    /// </summary>
    [TestClass]
    public sealed class BarcodeCommandTest
    {
        [TestMethod]
        public void DataMatrixReadsAllParameters()
        {
            ZplDataMatrix dataMatrix = SingleElement<ZplDataMatrix>("^XA^FO10,10^BXR,10,200,16,18,6,_,1^FDABC^FS^XZ");

            Assert.AreEqual(FieldOrientation.Rotated90, dataMatrix.FieldOrientation);
            Assert.AreEqual(10, dataMatrix.Height);
            Assert.AreEqual(QualityLevel.ECC200, dataMatrix.QualityLevel);
            Assert.AreEqual(16, dataMatrix.Columns);
            Assert.AreEqual(18, dataMatrix.Rows);
            Assert.AreEqual(6, dataMatrix.Format);
            Assert.AreEqual('_', dataMatrix.EscapeSequence);
            Assert.AreEqual(1, dataMatrix.AspectRatio);
        }

        [TestMethod]
        public void DataMatrixDefaultsOmittedParameters()
        {
            ZplDataMatrix dataMatrix = SingleElement<ZplDataMatrix>("^XA^FO10,10^BXN,10,200^FDABC^FS^XZ");

            Assert.IsNull(dataMatrix.Columns);
            Assert.IsNull(dataMatrix.Rows);
            Assert.AreEqual(6, dataMatrix.Format);
            Assert.AreEqual('~', dataMatrix.EscapeSequence);
            Assert.IsNull(dataMatrix.AspectRatio);
        }

        [TestMethod]
        public void AztecAlternateCommandMatchesAztecCommand()
        {
            ZplAztecBarcode alternate = SingleElement<ZplAztecBarcode>("^XA^FO10,10^B0R,5,N,10,N,1^FDHELLO^FS^XZ");
            ZplAztecBarcode aztec = SingleElement<ZplAztecBarcode>("^XA^FO10,10^BOR,5,N,10,N,1^FDHELLO^FS^XZ");

            Assert.AreEqual(FieldOrientation.Rotated90, alternate.FieldOrientation);
            Assert.AreEqual(5, alternate.MagnificationFactor);
            Assert.AreEqual(10, alternate.ErrorControl);

            byte[] alternateImage = Render(alternate);
            Assert.IsNotEmpty(Decode(alternateImage), "aztec has ink");
            CollectionAssert.AreEqual(Render(aztec), alternateImage);
        }

        [TestMethod]
        public void QrCodeMagnificationDefaultsToTwo()
        {
            ZplQrCode qrCode = SingleElement<ZplQrCode>("^XA^FO10,10^BQN,2^FDQA,HELLO^FS^XZ");

            Assert.AreEqual(2, qrCode.MagnificationFactor);
        }

        [TestMethod]
        public void Industrial2of5ReadsParameters()
        {
            ZplBarcodeIndustrial2of5 barcode = SingleElement<ZplBarcodeIndustrial2of5>("^XA^BY3,2,40^FO10,10^BIR,,N,Y^FD123^FS^XZ");

            Assert.AreEqual(FieldOrientation.Rotated90, barcode.FieldOrientation);
            Assert.AreEqual(40, barcode.Height, "^BY height");
            Assert.AreEqual(3, barcode.ModuleWidth);
            Assert.AreEqual(2, barcode.WideBarToNarrowBarWidthRatio);
            Assert.IsFalse(barcode.PrintInterpretationLine);
            Assert.IsTrue(barcode.PrintInterpretationLineAboveCode);

            Assert.IsTrue(SingleElement<ZplBarcodeIndustrial2of5>("^XA^FO10,10^BIN,50^FD123^FS^XZ").PrintInterpretationLine, "interpretation line by default");
        }

        [TestMethod]
        public void Industrial2of5DrawsBarsAtTheFieldOrigin()
        {
            ZplBarcodeIndustrial2of5 barcode = SingleElement<ZplBarcodeIndustrial2of5>("^XA^BY2,3^FO10,20^BIN,50,N^FD123^FS^XZ");

            HashSet<(int X, int Y)> ink = Decode(Render(barcode));

            Assert.AreEqual(10, ink.Min(o => o.X), "first bar");
            Assert.AreEqual(20, ink.Min(o => o.Y), "bar top");
            Assert.AreEqual(20 + 50 - 1, ink.Max(o => o.Y), "bar bottom");
        }

        [TestMethod]
        public void Code93ReadsTheCheckDigitFlag()
        {
            Assert.IsTrue(SingleElement<ZplBarcode93>("^XA^FO10,10^BAN,50,Y,N,Y^FDABC^FS^XZ").CheckDigit);
            Assert.IsFalse(SingleElement<ZplBarcode93>("^XA^FO10,10^BAN,50,Y,N,N^FDABC^FS^XZ").CheckDigit);
        }

        [TestMethod]
        [DataRow("TEST93", false, "▯TEST93▯", DisplayName = "without check digits")]
        [DataRow("TEST93", true, "▯TEST93+6▯", DisplayName = "check digits C and K")]
        [DataRow("A", true, "▯AAU▯", DisplayName = "single character")]
        [DataRow("a", true, "▯a8P▯", DisplayName = "extended character (shift d)")]
        public void Code93InterpretationShowsTheCheckDigits(string content, bool includeCheckDigits, string expected)
        {
            Assert.AreEqual(expected, Barcode93ElementDrawer.BuildCode93Interpretation(content, includeCheckDigits));
        }

        private static T SingleElement<T>(string zpl) where T : ZplElementBase
        {
            AnalyzeInfo analyzeInfo = new ZplAnalyzer(new PrinterStorage()).Analyze(zpl);

            Assert.IsEmpty(analyzeInfo.Errors, string.Join(Environment.NewLine, analyzeInfo.Errors));
            Assert.IsEmpty(analyzeInfo.UnknownCommands, string.Join(" ", analyzeInfo.UnknownCommands));

            return analyzeInfo.LabelInfos.SelectMany(o => o.ZplElements).OfType<T>().Single();
        }

        private static byte[] Render(ZplElementBase element)
        {
            using PrinterStorage storage = new();
            ZplElementDrawer drawer = new(storage);
            return drawer.Draw([element], 100, 50, 8);
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
    }
}
