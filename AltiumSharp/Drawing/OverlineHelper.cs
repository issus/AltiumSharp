using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace OriginalCircuit.AltiumSharp.Drawing
{
    internal static class OverlineHelper
    {
        private enum State { NoBar, InBar, CheckBar }

        public static CharacterRange[] Parse(string text)
        {
            text = text.TrimStart('\\'); // remove leading backslashes

            var overlineRanges = new List<CharacterRange>();
            var state = State.NoBar;
            int charIndex = 0;
            int startIndex = 0;
            foreach (var c in text)
            {
                switch (state)
                {
                    case State.NoBar:
                        if (c != '\\')
                        {
                            charIndex++;
                        }
                        else
                        {
                            state = State.InBar;
                            startIndex = charIndex - 1;
                        }
                        break;
                    case State.InBar:
                        if (c != '\\')
                        {
                            state = State.CheckBar;
                            charIndex++;
                        }
                        break;
                    case State.CheckBar:
                        if (c != '\\')
                        {
                            var length = charIndex - 1 - startIndex;
                            overlineRanges.Add(new CharacterRange(startIndex, length));
                            startIndex = charIndex;
                            state = State.NoBar;
                            charIndex++;
                        }
                        else
                        {
                            state = State.InBar;
                        }
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            if (state != State.NoBar)
            {
                if (state == State.CheckBar) --charIndex; // last character isn't overlined
                if (startIndex < charIndex)
                {
                    overlineRanges.Add(new CharacterRange(startIndex, charIndex - startIndex));
                }
            }

            // limit size to 32 as it's the maximum allowed by SetMeasurableCharacterRanges
            return overlineRanges.Take(32).ToArray();
        }
    }
}
