using System.Collections.Generic;
using System.Text;

namespace SKO.Bounty.Utils
{
    public static class TableFormatting
    {
        public static Dictionary<char, int> FontD = new Dictionary<char, int>
        {
            { ' ', 8 }, { '!', 8 }, { '"', 10 }, { '#', 19 }, { '$', 20 }, { '%', 24 }, { '&', 20 }, { '(', 9 },
            { ')', 9 }, { '*', 11 }, { '+', 18 }, { ',', 9 },
            { '-', 10 }, { '.', 9 }, { '/', 14 }, { '0', 19 }, { '1', 9 }, { '2', 19 }, { '3', 17 }, { '4', 19 },
            { '5', 19 }, { '6', 19 }, { '7', 16 }, { '8', 19 },
            { '9', 19 }, { ':', 9 }, { ';', 9 }, { '<', 18 }, { '=', 18 }, { '>', 18 }, { '?', 16 }, { '@', 25 },
            { 'A', 21 }, { 'B', 21 }, { 'C', 19 }, { 'D', 21 },
            { 'E', 18 }, { 'F', 17 }, { 'G', 20 }, { 'H', 20 }, { 'I', 8 }, { 'J', 16 }, { 'K', 17 }, { 'L', 15 },
            { 'M', 26 }, { 'N', 21 }, { 'O', 21 }, { 'P', 20 },
            { 'Q', 21 }, { 'R', 21 }, { 'S', 21 }, { 'T', 17 }, { 'U', 20 }, { 'V', 20 }, { 'W', 31 }, { 'X', 19 },
            { 'Y', 20 }, { 'Z', 19 }, { '[', 9 }, { ']', 9 },
            { '^', 18 }, { '_', 15 }, { '`', 8 }, { 'a', 17 }, { 'b', 17 }, { 'c', 16 }, { 'd', 17 }, { 'e', 17 },
            { 'f', 9 }, { 'g', 17 }, { 'h', 17 }, { 'i', 8 },
            { 'j', 8 }, { 'k', 17 }, { 'l', 8 }, { 'm', 27 }, { 'n', 17 }, { 'o', 17 }, { 'p', 17 }, { 'q', 17 },
            { 'r', 10 }, { 's', 17 }, { 't', 9 }, { 'u', 17 },
            { 'v', 15 }, { 'w', 27 }, { 'x', 15 }, { 'y', 17 }, { 'z', 16 }, { '{', 9 }, { '|', 6 }, { '}', 9 },
            { '~', 18 }, { '\\', 12 }, { '\'', 6 }
        };

        public static int GetWordTotalWidth(string word)
        {
            var width = 0;
            foreach (var ch in word)
                width += FontD[ch] + 1;
            return width;
        }

        public static float AlignWord(StringBuilder addTo, string word, float maxChars, char fillChar = ' ')
        {
            var fillCharWidth = FontD[fillChar];
            var maxWidth = fillCharWidth * maxChars;
            var width = GetWordTotalWidth(word);

            var leftOver = maxWidth - width;
            var numToAdd = (int)(leftOver / fillCharWidth);
            var error = maxWidth - width - numToAdd * fillCharWidth;

            addTo.Append(word);

            if (numToAdd > 0)
                addTo.Append(fillChar, numToAdd);

            return error / fillCharWidth;
        }
    }
}