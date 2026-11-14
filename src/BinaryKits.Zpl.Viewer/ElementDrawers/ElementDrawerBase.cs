using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;

using SkiaSharp;

namespace BinaryKits.Zpl.Viewer.ElementDrawers
{
    public abstract class ElementDrawerBase : IElementDrawer
    {
        internal IPrinterStorage printerStorage;
        internal SKCanvas skCanvas;

        ///<inheritdoc/>
        public void Prepare(
            IPrinterStorage printerStorage,
            SKCanvas skCanvas)
        {
            this.printerStorage = printerStorage;
            this.skCanvas = skCanvas;
        }

        ///<inheritdoc/>
        public abstract bool CanDraw(ZplElementBase element);

        ///<inheritdoc/>
        public virtual bool IsReverseDraw(ZplElementBase element)
        {
            return false;
        }

        ///<inheritdoc/>
        public virtual bool IsWhiteDraw(ZplElementBase element)
        {
            return false;
        }

        ///<inheritdoc/>
        public virtual bool ForceBitmapDraw(ZplElementBase element)
        {
            return false;
        }

        ///<inheritdoc/>
        public virtual SKPoint Draw(ZplElementBase element, DrawerOptions options, SKPoint currentPosition)
        {
            return currentPosition;
        }

        ///<inheritdoc/>
        public virtual SKPoint Draw(ZplElementBase element, DrawerOptions options, SKPoint currentPosition, InternationalFont internationalFont)
        {
            return this.Draw(element, options, currentPosition);
        }

        ///<inheritdoc/>
        public virtual SKPoint Draw(ZplElementBase element, DrawerOptions options, SKPoint currentPosition, InternationalFont internationalFont, int printDensityDpmm)
        {
            return this.Draw(element, options, currentPosition, internationalFont);
        }

        protected virtual SKPoint CalculateNextDefaultPosition(float x, float y, float elementWidth, float elementHeight, bool useFieldOrigin, Label.FieldOrientation fieldOrientation, SKPoint currentPosition)
        {
            if (useFieldOrigin)
            {
                switch (fieldOrientation)
                {
                    case Label.FieldOrientation.Normal:
                        return new SKPoint(x + elementWidth, y + elementHeight);
                    case Label.FieldOrientation.Rotated90:
                        return new SKPoint(x, y + elementHeight);
                    case Label.FieldOrientation.Rotated180:
                        return new SKPoint(x - elementWidth, y);
                    case Label.FieldOrientation.Rotated270:
                        return new SKPoint(x, y - elementHeight);
                }
            }
            else
            {
                switch (fieldOrientation)
                {
                    case Label.FieldOrientation.Normal:
                        return new SKPoint(x + elementWidth, y);
                    case Label.FieldOrientation.Rotated90:
                        return new SKPoint(x, y + elementWidth);
                    case Label.FieldOrientation.Rotated180:
                        return new SKPoint(x - elementWidth, y);
                    case Label.FieldOrientation.Rotated270:
                        return new SKPoint(x, y - elementWidth);
                }
            }

            return currentPosition;
        }

        /// <summary>
        /// Position of a right-justified ^FO field drawn like a left-justified one: the rotated field box ends at x.
        /// The box is <paramref name="boxWidth"/> along the text and <paramref name="boxHeight"/> across it, so N and I
        /// move left by the width, R and B by the height (Labelary; ^FT justifies along the text instead).
        /// </summary>
        protected static float GetRightJustifiedFieldOriginX(float x, float boxWidth, float boxHeight, Label.FieldOrientation fieldOrientation)
        {
            return fieldOrientation is Label.FieldOrientation.Rotated90 or Label.FieldOrientation.Rotated270 ?
                x - boxHeight :
                x - boxWidth;
        }

    }
}
