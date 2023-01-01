using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace OriginalCircuit.AltiumSharp.Drawing
{
    internal static class RectangleFExtensions
    {
        public static RectangleF Inflated(this RectangleF rect, float x, float y)
        {
            var result = rect;
            result.Inflate(x, y);
            return result;
        }
    }
}
