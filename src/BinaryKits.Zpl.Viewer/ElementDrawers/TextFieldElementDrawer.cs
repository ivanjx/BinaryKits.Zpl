using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Helpers;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace BinaryKits.Zpl.Viewer.ElementDrawers
{
    public class TextFieldElementDrawer : ElementDrawerBase
    {
        ///<inheritdoc/>
        public override bool CanDraw(ZplElementBase element)
        {
            return element.GetType() == typeof(ZplTextField);
        }

        public override bool IsReverseDraw(ZplElementBase element)
        {
            if (element is ZplTextField textField)
            {
                return textField.ReversePrint;
            }

            return false;
        }

        ///<inheritdoc/>
        public override void Draw(ZplElementBase element, DrawerOptions options)
        {
            if (element is ZplTextField textField)
            {
                float x = textField.PositionX;
                float y = textField.PositionY;
                FieldJustification fieldJustification;

                var font = textField.Font;

                float fontSize = font.FontHeight > 0 ? font.FontHeight : font.FontWidth;
                var scaleX = 1.00f;
                if (font.FontWidth != 0 && font.FontWidth != fontSize)
                {
                    scaleX *= font.FontWidth / fontSize;
                }

                var typeface = options.FontLoader(font.FontName);

                var skFont = new SKFont(typeface, fontSize, scaleX);
                using var skPaint = new SKPaint()
                {
                    IsAntialias = options.Antialias
                };

                string displayText = textField.Text;
                if (textField.UseHexadecimalIndicator)
                {
                    displayText = displayText.ReplaceHexEscapes();
                }

                if (options.ReplaceDashWithEnDash)
                {
                    displayText = displayText.Replace("-", " \u2013 ");
                }

                skFont.MeasureText("X", out var textBoundBaseline);
                skFont.MeasureText(displayText, out var textBounds);

                using (new SKAutoCanvasRestore(this._skCanvas))
                {
                    SKMatrix matrix = SKMatrix.Empty;

                    if (textField.FieldOrigin != null)
                    {
                        switch (textField.Font.FieldOrientation)
                        {
                            case FieldOrientation.Rotated90:
                                matrix = SKMatrix.CreateRotationDegrees(90, x + fontSize / 2, y + fontSize / 2);
                                break;
                            case FieldOrientation.Rotated180:
                                matrix = SKMatrix.CreateRotationDegrees(180, x + textBounds.Width / 2, y + fontSize / 2);
                                break;
                            case FieldOrientation.Rotated270:
                                matrix = SKMatrix.CreateRotationDegrees(270, x + textBounds.Width / 2, y + textBounds.Width / 2);
                                break;
                            case FieldOrientation.Normal:
                                break;
                        }
                        fieldJustification = textField.FieldOrigin.FieldJustification;
                    }
                    else
                    {
                        switch (textField.Font.FieldOrientation)
                        {
                            case FieldOrientation.Rotated90:
                                matrix = SKMatrix.CreateRotationDegrees(90, x, y);
                                break;
                            case FieldOrientation.Rotated180:
                                matrix = SKMatrix.CreateRotationDegrees(180, x, y);
                                break;
                            case FieldOrientation.Rotated270:
                                matrix = SKMatrix.CreateRotationDegrees(270, x, y);
                                break;
                            case FieldOrientation.Normal:
                                break;
                        }
                        fieldJustification = textField.FieldTypeset.FieldJustification;
                    }

                    if (matrix != SKMatrix.Empty)
                    {
                        var currentMatrix = _skCanvas.TotalMatrix;
                        var concatMatrix = SKMatrix.Concat(currentMatrix, matrix);
                        this._skCanvas.SetMatrix(in concatMatrix);
                    }

                    if (textField.FieldTypeset == null)
                    {
                        y += textBoundBaseline.Height;
                    }

                    if (textField.ReversePrint)
                    {
                        skPaint.BlendMode = SKBlendMode.Xor;
                    }

                    SKTextAlign align = default;

                    if (fieldJustification == FieldJustification.Left)
                    {
                        align = SKTextAlign.Left;
                        
                    }
                    else if (fieldJustification == FieldJustification.Right)
                    {
                        align = SKTextAlign.Right;
                    }
                    else if (fieldJustification == FieldJustification.Auto)
                    {
                        var buffer = new HarfBuzzSharp.Buffer();
                        buffer.AddUtf16(displayText);
                        buffer.GuessSegmentProperties();
                        if (buffer.Direction == HarfBuzzSharp.Direction.RightToLeft)
                        {
                            align = SKTextAlign.Right;
                        }
                    }

                    this._skCanvas.DrawShapedText(displayText, x, y, align, skFont, skPaint);

                }
            }
        }
    }
}
