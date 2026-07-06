using SkiaSharp;
using SkiaSharp.HarfBuzz;

using System;

namespace BinaryKits.Zpl.Viewer.Helpers
{
    public static class SafeTextDrawing
    {
        public static void DrawShapedTextSafe(
            this SKCanvas canvas,
            string text,
            float x,
            float y,
            SKTextAlign textAlign,
            SKFont font,
            SKPaint paint)
        {
            try
            {
                canvas.DrawShapedText(text, x, y, textAlign, font, paint);
            }
            catch (ArgumentNullException ex) when (ex.ParamName == "asset")
            {
                canvas.DrawText(text, x, y, textAlign, font, paint);
            }
        }
    }
}
