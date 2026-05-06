using System.Collections.Generic;
using GUI.RegexSearch;

namespace GUI.AutomatonSearch
{
    public sealed class MacAddressAutomatonSearcher
    {
        private const int AcceptState = 17;

        public List<RegexSearchResult> Search(string text)
        {
            var results = new List<RegexSearchResult>();

            if (string.IsNullOrWhiteSpace(text))
                return results;

            for (int i = 0; i < text.Length; i++)
            {
                if (!IsStartBoundary(text, i))
                    continue;

                if (!TryMatchMac(text, i, out int length))
                    continue;

                int endIndex = i + length;

                if (!IsEndBoundary(text, endIndex))
                    continue;

                var (line, column) = GetLineAndColumn(text, i);

                results.Add(new RegexSearchResult
                {
                    MatchedText = text.Substring(i, length),
                    StartPosition = $"строка {line}, символ {column}",
                    Length = length,
                    StartIndex = i,
                    Line = line,
                    Column = column
                });

                i = endIndex - 1;
            }

            return results;
        }

        private static bool TryMatchMac(string text, int startIndex, out int length)
        {
            int state = 0;
            int i = startIndex;

            while (i < text.Length)
            {
                int nextState = GetNextState(state, text[i]);

                if (nextState == -1)
                    break;

                state = nextState;
                i++;

                if (state == AcceptState)
                {
                    length = i - startIndex;
                    return true;
                }
            }

            length = 0;
            return false;
        }

        private static int GetNextState(int state, char ch)
        {
            return state switch
            {
                0 => IsHex(ch) ? 1 : -1,
                1 => IsHex(ch) ? 2 : -1,
                2 => ch == ':' ? 3 : -1,

                3 => IsHex(ch) ? 4 : -1,
                4 => IsHex(ch) ? 5 : -1,
                5 => ch == ':' ? 6 : -1,

                6 => IsHex(ch) ? 7 : -1,
                7 => IsHex(ch) ? 8 : -1,
                8 => ch == ':' ? 9 : -1,

                9 => IsHex(ch) ? 10 : -1,
                10 => IsHex(ch) ? 11 : -1,
                11 => ch == ':' ? 12 : -1,

                12 => IsHex(ch) ? 13 : -1,
                13 => IsHex(ch) ? 14 : -1,
                14 => ch == ':' ? 15 : -1,

                15 => IsHex(ch) ? 16 : -1,
                16 => IsHex(ch) ? 17 : -1,

                _ => -1
            };
        }

        private static bool IsHex(char ch)
        {
            return (ch >= '0' && ch <= '9') ||
                   (ch >= 'A' && ch <= 'F') ||
                   (ch >= 'a' && ch <= 'f');
        }

        private static bool IsStartBoundary(string text, int index) 
        {
            if (index <= 0)
                return true;

            return !IsMacContextChar(text[index - 1]);
        }

        private static bool IsEndBoundary(string text, int index)
        {
            if (index >= text.Length)
                return true;

            return !IsMacContextChar(text[index]);
        }

        private static bool IsMacContextChar(char ch)
        {
            return (ch >= '0' && ch <= '9') ||
                   (ch >= 'A' && ch <= 'Z') ||
                   (ch >= 'a' && ch <= 'z') ||
                   ch == '_' ||
                   ch == ':' ||
                   ch == '-';
        }

        private static (int line, int column) GetLineAndColumn(string text, int index)
        {
            int line = 1;
            int column = 1;

            for (int i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\r')
                    continue;

                if (text[i] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }

            return (line, column);
        }
    }
}