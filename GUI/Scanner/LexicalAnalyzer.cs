using System;
using System.Collections.Generic;

namespace GUI.Scanner
{
    public class LexicalAnalyzer
    {
        private const int CODE_NUMBER = 1;
        private const int CODE_ID = 2;
        private const int CODE_KEYWORD = 3;
        private const int CODE_OPERATOR = 4;
        private const int CODE_SEPARATOR = 5;
        private const int CODE_WHITESPACE = 6;
        private const int CODE_ERROR = 99;

        public List<Lexeme> Analyze(string text)
        {
            var result = new List<Lexeme>();

            int i = 0;
            int line = 1;
            int col = 1;

            while (i < text.Length)
            {
                char ch = text[i];

                // --- 1) Перевод строки ---
                if (ch == '\n')
                {
                    i++;
                    line++;
                    col = 1;
                    continue;
                }

                // --- 2) Пробелы/табы/CR ---
                if (ch == ' ' || ch == '\t' || ch == '\r')
                {
                    while (i < text.Length && (text[i] == ' ' || text[i] == '\t' || text[i] == '\r'))
                    {
                        i++;
                        col++;
                    }
                    continue;
                }

                // --- 3) Идентификатор или ключевое слово ---
                if (IsLetter(ch) || ch == '_')
                {
                    int start = i;
                    int startCol = col;

                    i++;
                    col++;

                    while (i < text.Length && (IsLetter(text[i]) || IsDigit(text[i]) || text[i] == '_'))
                    {
                        i++;
                        col++;
                    }

                    string word = text.Substring(start, i - start);
                    if (word == "do" || word == "while")
                    {
                        result.Add(MakeLexeme(CODE_KEYWORD, "ключевое слово", word, line, startCol, col - 1, start, word.Length));
                    }
                    else
                    {
                        result.Add(MakeLexeme(CODE_ID, "идентификатор", word, line, startCol, col - 1, start, word.Length));
                    }
                    continue;
                }

                // --- 4) Число (целое без знака) ---
                if (IsDigit(ch))
                {
                    int start = i;
                    int startCol = col;

                    i++;
                    col++;

                    while (i < text.Length && IsDigit(text[i]))
                    {
                        i++;
                        col++;
                    }

                    string num = text.Substring(start, i - start);
                    result.Add(MakeLexeme(CODE_NUMBER, "целое без знака", num, line, startCol, col - 1, start, num.Length));
                    continue;
                }

                // --- 5) Операторы/разделители (по одному или два символа) ---
                // 5.1 -- (декремент)
                if (ch == '-')
                {
                    int start = i;
                    int startCol = col;

                    if (i + 1 < text.Length && text[i + 1] == '-')
                    {
                        i += 2;
                        col += 2;
                        result.Add(MakeLexeme(CODE_OPERATOR, "оператор", "--", line, startCol, col - 1, start, 2));
                        continue;
                    }

                    // одиночный '-'
                    i++;
                    col++;
                    result.Add(MakeLexeme(CODE_OPERATOR, "оператор", "-", line, startCol, col - 1, start, 1));
                    continue;
                }

                // 5.2 >=
                if (ch == '>')
                {
                    int start = i;
                    int startCol = col;

                    if (i + 1 < text.Length && text[i + 1] == '=')
                    {
                        i += 2;
                        col += 2;
                        result.Add(MakeLexeme(CODE_OPERATOR, "оператор", ">=", line, startCol, col - 1, start, 2));
                        continue;
                    }

                    i++;
                    col++;
                    result.Add(MakeLexeme(CODE_OPERATOR, "оператор", ">", line, startCol, col - 1, start, 1));
                    continue;
                }

                // 5.3 Односимвольные операторы/разделители
                if (IsSingleCharToken(ch, out int code, out string type))
                {
                    int start = i;
                    int startCol = col;

                    i++;
                    col++;

                    result.Add(MakeLexeme(code, type, ch.ToString(), line, startCol, col - 1, start, 1));
                    continue;
                }

                // --- 6) Иначе — ошибка (недопустимый символ) ---
                {
                    int start = i;
                    int startCol = col;

                    i++;
                    col++;

                    string bad = ch.ToString();
                    var lex = MakeLexeme(CODE_ERROR, "ошибка", bad, line, startCol, startCol, start, 1);
                    lex.IsError = true;
                    lex.Type = "ошибка: недопустимый символ";
                    result.Add(lex);
                }
            }

            return result;
        }

        private static Lexeme MakeLexeme(int code, string type, string text, int line, int colFrom, int colTo, int startIndex, int length)
        {
            return new Lexeme
            {
                Code = code,
                Type = type,
                Text = text,
                Location = $"строка {line}, {colFrom}-{colTo}",
                StartIndex = startIndex,
                Length = length,
                IsError = false
            };
        }

        private static bool IsLetter(char c) =>
            (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

        private static bool IsDigit(char c) =>
            (c >= '0' && c <= '9');

        private static bool IsSingleCharToken(char ch, out int code, out string type)
        {
            code = 0;
            type = "";
            
            switch (ch)
            {
                case '{':
                case '}':
                case '(':
                case ')':
                case ';':
                    code = 5;
                    type = "разделитель";
                    return true;

                case '=':
                case '+':
                case '*':
                case '/':
                    code = 4;
                    type = "оператор";
                    return true;
            }

            return false;
        }
    }
}