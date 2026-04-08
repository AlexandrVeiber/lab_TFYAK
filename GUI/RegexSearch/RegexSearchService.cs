using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GUI.RegexSearch
{
    public sealed class RegexSearchService
    {
        public string GetTaskTitle(RegexTaskType taskType) => taskType switch
        {
            RegexTaskType.Numbers => "Целые и вещественные числа",
            RegexTaskType.FileNames => "Корректные названия файлов",
            RegexTaskType.MacAddresses => "MAC-адреса",
            _ => "Регулярные выражения"
        };

        public string GetOriginalPattern(RegexTaskType taskType) => taskType switch
        {
            RegexTaskType.Numbers => @"^[+-]?\d+(\.\d+)?$",
            RegexTaskType.FileNames => @"^[A-Za-zА-Яа-я0-9_-]+\.[A-Za-z0-9]+$",
            RegexTaskType.MacAddresses => @"^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$",
            _ => ""
        };

        public List<RegexSearchResult> Search(string text, RegexTaskType taskType)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<RegexSearchResult>();

            return taskType switch
            {
                RegexTaskType.Numbers => SearchByPattern(text, GetSearchPatternForNumbers()),
                RegexTaskType.FileNames => SearchByPattern(text, GetSearchPatternForFileNames()),
                RegexTaskType.MacAddresses => SearchByPattern(text, GetSearchPatternForMac()),
                _ => new List<RegexSearchResult>()
            };
        }

        private static string GetSearchPatternForNumbers()
        {
            return @"(?<![\w\.,+\-])[+-]?\d+(?:\.\d+)?(?![\w\.,])";
        }

        private static string GetSearchPatternForFileNames()
        {
            return @"(?<![\w.@])[A-Za-zА-Яа-я0-9_-]+\.[A-Za-z0-9]+(?![\w.])";
        }

        private static string GetSearchPatternForMac()
        {
            // Ищем MAC-адрес как отдельный корректный фрагмент,
            // не выдёргивая его из:
            // x01:23:45:67:89:ABy
            // 01:23:45:67:89:AB:CD
            // и похожих строк.
            return @"(?<![A-Za-z0-9_:\-])(?:[0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}(?![A-Za-z0-9_:\-])";
        }

        private static List<RegexSearchResult> SearchByPattern(string text, string pattern)
        {
            var results = new List<RegexSearchResult>();
            var matches = Regex.Matches(text, pattern, RegexOptions.CultureInvariant);

            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;

                var (line, column) = GetLineAndColumn(text, match.Index);

                results.Add(new RegexSearchResult
                {
                    MatchedText = match.Value,
                    StartPosition = $"строка {line}, символ {column}",
                    Length = match.Length,
                    StartIndex = match.Index,
                    Line = line,
                    Column = column
                });
            }

            return results;
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