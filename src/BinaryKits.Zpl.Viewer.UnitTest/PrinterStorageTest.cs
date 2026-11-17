using BinaryKits.Zpl.Viewer.DownloadedFonts;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Linq;

namespace BinaryKits.Zpl.Viewer.UnitTest
{
    [TestClass]
    public sealed class PrinterStorageTest
    {
        [TestMethod]
        public void FileKeyIgnoresCaseAndSurroundingSpaces()
        {
            using PrinterStorage storage = new();
            byte[] data = [0x01, 0x02, 0x03];

            storage.AddFile('r', " logo.dat ", data);

            CollectionAssert.AreEqual(data, storage.GetFile('R', "LOGO.DAT"));
        }

        [TestMethod]
        public void MissingFileIsEmpty()
        {
            using PrinterStorage storage = new();

            Assert.IsEmpty(storage.GetFile('R', "MISSING.DAT"));
        }

        [TestMethod]
        public void FileReplacesAndDisposesTheFontWithTheSameName()
        {
            using PrinterStorage storage = new();
            ZplDownloadedOutlineFont font = CreateFont();

            storage.AddFont('E', "X.TTF", font);
            storage.AddFile('E', "X.TTF", [0x01]);

            Assert.IsFalse(storage.TryGetFont('E', "X.TTF", out _));
            Assert.IsTrue(IsDisposed(font));
        }

        [TestMethod]
        public void FontReplacesTheFileWithTheSameName()
        {
            using PrinterStorage storage = new();

            storage.AddFile('E', "X.TTF", [0x01]);
            storage.AddFont('E', "X.TTF", CreateFont());

            Assert.IsEmpty(storage.GetFile('E', "X.TTF"));
            Assert.IsTrue(storage.TryGetFont('E', "X.TTF", out _));
        }

        [TestMethod]
        public void ReplacingAFontDisposesItOnlyWhenOwned()
        {
            using PrinterStorage storage = new();
            ZplDownloadedOutlineFont owned = CreateFont();
            using ZplDownloadedOutlineFont shared = CreateFont();

            storage.AddFont('E', "X.TTF", owned);
            storage.AddFont('E', "X.TTF", owned);
            Assert.IsFalse(IsDisposed(owned), "adding the same font again keeps it");

            storage.AddFont('E', "X.TTF", shared, ownsFont: false);
            Assert.IsTrue(IsDisposed(owned), "owned font replaced");

            storage.AddFont('E', "X.TTF", CreateFont());
            Assert.IsFalse(IsDisposed(shared), "shared font replaced");
        }

        [TestMethod]
        public void DisposeDisposesOwnedFontsOnce()
        {
            ZplDownloadedOutlineFont owned = CreateFont();
            using ZplDownloadedOutlineFont shared = CreateFont();
            PrinterStorage storage = new();

            storage.AddFont('E', "OWNED.TTF", owned);
            storage.AddFont('E', "SHARED.TTF", shared, ownsFont: false);

            storage.Dispose();
            storage.Dispose();

            Assert.IsTrue(IsDisposed(owned));
            Assert.IsFalse(IsDisposed(shared));
            Assert.ThrowsExactly<ObjectDisposedException>(() => storage.AddFont('E', "X.TTF", CreateFont()));
        }

        [TestMethod]
        public void FontLinksKeepTheLastFive()
        {
            using PrinterStorage storage = new();

            for (int i = 1; i <= 6; i++)
            {
                storage.SetFontLink($"E:EXT{i}.TTF", "E:BASE.TTF", true);
            }

            CollectionAssert.AreEqual(
                new[] { "E:EXT2.TTF", "E:EXT3.TTF", "E:EXT4.TTF", "E:EXT5.TTF", "E:EXT6.TTF" },
                storage.GetFontLinks("E:BASE.TTF").ToArray());
        }

        [TestMethod]
        public void RelinkingMovesTheLinkToTheEnd()
        {
            using PrinterStorage storage = new();

            storage.SetFontLink("E:ONE.TTF", "E:BASE.TTF", true);
            storage.SetFontLink("E:TWO.TTF", "E:BASE.TTF", true);
            storage.SetFontLink("e:one.ttf", "e:base.ttf", true);

            CollectionAssert.AreEqual(new[] { "E:TWO.TTF", "E:ONE.TTF" }, storage.GetFontLinks("E:BASE.TTF").ToArray());
        }

        [TestMethod]
        public void ExactLinksComeBeforeWildcardLinksWithoutDuplicates()
        {
            using PrinterStorage storage = new();

            storage.SetFontLink("E:WILD.TTF", "E:*.TTF", true);
            storage.SetFontLink("E:BOTH.TTF", "E:*.TTF", true);
            storage.SetFontLink("E:EXACT.TTF", "E:BASE.TTF", true);
            storage.SetFontLink("E:BOTH.TTF", "E:BASE.TTF", true);

            CollectionAssert.AreEqual(
                new[] { "E:EXACT.TTF", "E:BOTH.TTF", "E:WILD.TTF" },
                storage.GetFontLinks("E:BASE.TTF").ToArray());
        }

        [TestMethod]
        public void QuestionMarkMatchesOneCharacter()
        {
            using PrinterStorage storage = new();

            storage.SetFontLink("E:EXT.TTF", "E:BAS?.TTF", true);

            CollectionAssert.AreEqual(new[] { "E:EXT.TTF" }, storage.GetFontLinks("E:BASE.TTF").ToArray());
            Assert.IsEmpty(storage.GetFontLinks("E:BASES.TTF"));
        }

        [TestMethod]
        public void FontIsNotItsOwnLink()
        {
            using PrinterStorage storage = new();

            storage.SetFontLink("E:BASE.TTF", "E:*.TTF", true);

            Assert.IsEmpty(storage.GetFontLinks("E:BASE.TTF"));
            CollectionAssert.AreEqual(new[] { "E:BASE.TTF" }, storage.GetFontLinks("E:OTHER.TTF").ToArray());
        }

        /// <summary>
        /// Outline font with only the sfnt magic: enough for storage, Skia cannot load it.
        /// </summary>
        private static ZplDownloadedOutlineFont CreateFont()
        {
            return new ZplDownloadedOutlineFont([0x00, 0x01, 0x00, 0x00]);
        }

        private static bool IsDisposed(ZplDownloadedOutlineFont font)
        {
            try
            {
                _ = font.Typeface;
                return false;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
        }
    }
}
