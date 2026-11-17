using BinaryKits.Zpl.Label.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO.Compression;

namespace BinaryKits.Zpl.Label.UnitTest
{
    [TestClass]
    public class ZebraZ64CompressionHelperTest
    {
        [TestMethod]
        public void CompressUncompress_Flow_Successful()
        {
            var originalData = "FFFFFFFFFFFFFFFFFFFF8000FFFF0000FFFF00018000FFFF0000FFFF00018000FFFF0000FFFF0001FFFF0000FFFF0000FFFFFFFF0000FFFF0000FFFFFFFF0000FFFF0000FFFFFFFFFFFFFFFFFFFFFFFF";

            string compressed = ZebraZ64CompressionHelper.Compress(originalData);
            string uncompressed = ZebraZ64CompressionHelper.Uncompress(compressed).ToHexFromBytes();
            Assert.AreEqual(originalData, uncompressed);
        }
        [TestMethod]
        public void Uncompress_VerifyCrc_AcceptsMatchingCrcInAnyCase()
        {
            var compressed = ZebraZ64CompressionHelper.Compress(new byte[] { 0xFF, 0x81, 0x00 });
            var crcIndex = compressed.LastIndexOf(':') + 1;
            var upperCaseCrc = compressed.Substring(0, crcIndex) + compressed.Substring(crcIndex).ToUpperInvariant();

            CollectionAssert.AreEqual(new byte[] { 0xFF, 0x81, 0x00 }, ZebraZ64CompressionHelper.Uncompress(compressed, true));
            CollectionAssert.AreEqual(new byte[] { 0xFF, 0x81, 0x00 }, ZebraZ64CompressionHelper.Uncompress(upperCaseCrc, true));
        }
        [TestMethod]
        public void Uncompress_VerifyCrc_RejectsWrongCrc()
        {
            var compressed = ZebraZ64CompressionHelper.Compress(new byte[] { 0xFF, 0x81, 0x00 });
            var crc = compressed.Substring(compressed.LastIndexOf(':') + 1);
            var data = compressed.Substring(0, compressed.Length - crc.Length);
            var wrongCrc = ((Convert.ToUInt16(crc, 16) + 1) & 0xFFFF).ToString("x4");

            Assert.ThrowsExactly<FormatException>(() => ZebraZ64CompressionHelper.Uncompress(data + wrongCrc, true));
            Assert.ThrowsExactly<FormatException>(() => ZebraZ64CompressionHelper.Uncompress(data + "0" + crc, true), "more than 4 digits");
            CollectionAssert.AreEqual(new byte[] { 0xFF, 0x81, 0x00 }, ZebraZ64CompressionHelper.Uncompress(data + wrongCrc));
        }
#if NET5_0_OR_GREATER
        [TestMethod]
        public void Compress_ValidData1_Successful()
        {
            var compressed = ZebraZ64CompressionHelper.Compress("FFFFFFFFFFFFFFFFFFFF8000FFFF0000FFFF00018000FFFF0000FFFF00018000FFFF0000FFFF0001FFFF0000FFFF0000FFFFFFFF0000FFFF0000FFFFFFFF0000FFFF0000FFFFFFFFFFFFFFFFFFFFFFFF");
            Assert.AreEqual(":Z64:eNr7/x8GGhj+/2cAYUZsLCjNAFKJjQUDAOLDM1I=:8f39", compressed);
            //^XA
            //~DGR:SAMPLE.GRF,00006,02,JFGFH0GFJF
            //^FO20,20
            //^XGR:SAMPLE.GRF,1,1^FS
            //^XZ
        }
        [TestMethod]
        public void DeflateNetStandardSameAsNetCore_Optimal_Successful()
        {
            var originalData = "FFFFFFFFFFFFFFFFFFFF8000FFFF0000FFFF00018000FFFF0000FFFF00018000FFFF0000FFFF0001FFFF0000FFFF0000FFFFFFFF0000FFFF0000FFFFFFFF0000FFFF0000FFFFFFFFFFFFFFFFFFFFFFFF";

            var compressionLevel = CompressionLevel.Optimal;
            string compressed = ZebraZ64CompressionHelper.Deflate(originalData.ToBytesFromHex(), compressionLevel).ToHexFromBytes();
            string compressedCore = ZebraZ64CompressionHelper.DeflateCore(originalData.ToBytesFromHex(), compressionLevel).ToHexFromBytes();

            Assert.AreEqual(compressed, compressedCore);
        }
        [TestMethod]
        public void DeflateNetStandardSameAsNetCore_Fastest_Successful()
        {
            var originalData = "FFFFFFFFFFFFFFFFFFFF8000FFFF0000FFFF00018000FFFF0000FFFF00018000FFFF0000FFFF0001FFFF0000FFFF0000FFFFFFFF0000FFFF0000FFFFFFFF0000FFFF0000FFFFFFFFFFFFFFFFFFFFFFFF";

            var compressionLevel = CompressionLevel.Fastest;
            string compressed = ZebraZ64CompressionHelper.Deflate(originalData.ToBytesFromHex(), compressionLevel).ToHexFromBytes();
            string compressedCore = ZebraZ64CompressionHelper.DeflateCore(originalData.ToBytesFromHex(), compressionLevel).ToHexFromBytes();

            Assert.AreEqual(compressed, compressedCore);
        }
        [TestMethod]
        public void DeflateNetStandardSameAsNetCore_SmallestSize_Successful()
        {
            var originalData = "FFFFFFFFFFFFFFFFFFFF8000FFFF0000FFFF00018000FFFF0000FFFF00018000FFFF0000FFFF0001FFFF0000FFFF0000FFFFFFFF0000FFFF0000FFFFFFFF0000FFFF0000FFFFFFFFFFFFFFFFFFFFFFFF";

            var compressionLevel = CompressionLevel.SmallestSize;
            string compressed = ZebraZ64CompressionHelper.Deflate(originalData.ToBytesFromHex(), compressionLevel).ToHexFromBytes();
            string compressedCore = ZebraZ64CompressionHelper.DeflateCore(originalData.ToBytesFromHex(), compressionLevel).ToHexFromBytes();

            Assert.AreEqual(compressed, compressedCore);
        }
        [TestMethod]
        public void DeflateNetStandardSameAsNetCore_NoCompression_Successful()
        {
            var originalData = "FFFFFFFFFFFFFFFFFFFF8000FFFF0000FFFF00018000FFFF0000FFFF00018000FFFF0000FFFF0001FFFF0000FFFF0000FFFFFFFF0000FFFF0000FFFFFFFF0000FFFF0000FFFFFFFFFFFFFFFFFFFFFFFF";
            
            var compressionLevel = CompressionLevel.NoCompression;
            string compressed = ZebraZ64CompressionHelper.Deflate(originalData.ToBytesFromHex(), compressionLevel).ToHexFromBytes();
            string compressedCore = ZebraZ64CompressionHelper.DeflateCore(originalData.ToBytesFromHex(), compressionLevel).ToHexFromBytes();
            
            Assert.AreEqual(compressed, compressedCore);
        }
        [TestMethod]
        public void InflateNetStandardSameAsNetCore_Successful()
        {
            var originalData = "eJz7/x8GGhj+/2cAYUZsLCgNxNhZMAAA4sMzUg==";

            string compressed = ZebraZ64CompressionHelper.Inflate(originalData.FromBase64()).ToHexFromBytes();
            string compressedCore = ZebraZ64CompressionHelper.InflateCore(originalData.FromBase64()).ToHexFromBytes();
            
            Assert.AreEqual(compressed, compressedCore);
        }
#endif
    }
}
