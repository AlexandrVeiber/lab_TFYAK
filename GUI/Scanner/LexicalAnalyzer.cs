using System.Collections.Generic;
using System.Numerics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GUI.Scanner
{
    public class LexicalAnalyzer
    {
        // --- Уникальные коды лексем ---
        private const int CODE_NUMBER = 1;
        private const int CODE_IDENTIFIER = 2;

        private const int CODE_DO = 3;
        private const int CODE_WHILE = 4;

        private const int CODE_PLUS = 5;
        private const int CODE_INCREMENT = 6;
        private const int CODE_MINUS = 7;
        private const int CODE_DECREMENT = 8;
        private const int CODE_MULTIPLY = 9;
        private const int CODE_DIVIDE = 10;
        private const int CODE_ASSIGN = 11;
        private const int CODE_EQUAL = 12;

        private const int CODE_GREATER = 13;
        private const int CODE_GREATER_OR_EQUAL = 14;
        private const int CODE_LESS = 15;
        private const int CODE_LESS_OR_EQUAL = 16;
        private const int CODE_NOT_EQUAL = 17;

        private const int CODE_LBRACE = 18;
        private const int CODE_RBRACE = 19;
        private const int CODE_LPAREN = 20;
        private const int CODE_RPAREN = 21;
        private const int CODE_SEMICOLON = 22;

        private const int CODE_WHITESPACE = 23;
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

                // Игнорируем \r
                if (ch == '\r')
                {
                    i++;
                    continue;
                }

                // Перевод строки
                if (ch == '\n')
                {
                    i++;
                    line++;
                    col = 1;
                    continue;
                }

                // Пробелы / табы
                if (ch == ' ' || ch == '\t')
                {
                    int start = i;
                    int startLine = line;
                    int startCol = col;

                    bool hadNewLineInside = false;

                    while (i < text.Length)
                    {
                        if (text[i] == '\r')
                        {
                            i++;
                            continue;
                        }

                        if (text[i] == '\n')
                        {
                            i++;
                            line++;
                            col = 1;
                            hadNewLineInside = true;
                            continue;
                        }

                        if (text[i] == ' ')
                        {
                            i++;
                            col++;
                            continue;
                        }

                        if (text[i] == '\t')
                        {
                            i++;
                            col += 4;
                            continue;
                        }

                        break;
                    }

                    bool hasPreviousLexeme = result.Count > 0;
                    bool hasNextMeaningful = i < text.Length && IsMeaningfulStart(text[i]);

                    bool previousLexemeOnSameLine = hasPreviousLexeme && result[^1].Line == startLine;

                    if (hasPreviousLexeme && previousLexemeOnSameLine && hasNextMeaningful && !hadNewLineInside)
                    {
                        result.Add(MakeLexeme(
                            CODE_WHITESPACE,
                            "разделитель (пробел)",
                            "(пробел)",
                            startLine,
                            startCol,
                            startCol,
                            start,
                            1));
                    }

                    continue;
                }

                // Идентификаторы / ключевые слова
                if (IsLetter(ch) || ch == '_')
                {
                    int start = i;
                    int startCol = col;

                    i++;
                    col++;

                    while (i < text.Length &&
                           (IsLetter(text[i]) || IsDigit(text[i]) || text[i] == '_'))
                    {
                        i++;
                        col++;
                    }

                    string word = text.Substring(start, i - start);

                    if (word == "do")
                    {
                        result.Add(MakeLexeme(
                            CODE_DO,
                            "ключевое слово do",
                            word,
                            line,
                            startCol,
                            col - 1,
                            start,
                            word.Length));
                    }
                    else if (word == "while")
                    {
                        result.Add(MakeLexeme(
                            CODE_WHILE,
                            "ключевое слово while",
                            word,
                            line,
                            startCol,
                            col - 1,
                            start,
                            word.Length));
                    }
                    else
                    {
                        result.Add(MakeLexeme(
                            CODE_IDENTIFIER,
                            "идентификатор",
                            word,
                            line,
                            startCol,
                            col - 1,
                            start,
                            word.Length));
                    }

                    continue;
                }

                // Числа
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

                    result.Add(MakeLexeme(
                        CODE_NUMBER,
                        "целое без знака",
                        num,
                        line,
                        startCol,
                        col - 1,
                        start,
                        num.Length));

                    continue;
                }

                // Операторы и разделители
                int tokenStart = i;
                int tokenStartCol = col;

                switch (ch)
                {
                    case '+':
                        if (i + 1 < text.Length && text[i + 1] == '+')
                        {
                            i += 2;
                            col += 2;
                            result.Add(MakeLexeme(
                                CODE_INCREMENT,
                                "оператор инкремента",
                                "++",
                                line,
                                tokenStartCol,
                                col - 1,
                                tokenStart,
                                2));
                        }
                        else
                        {
                            i++;
                            col++;
                            result.Add(MakeLexeme(
                                CODE_PLUS,
                                "оператор сложения",
                                "+",
                                line,
                                tokenStartCol,
                                col - 1,
                                tokenStart,
                                1));
                        }
                        continue;

                    case '-':
                        if (i + 1 < text.Length && text[i + 1] == '-')
                        {
                            i += 2;
                            col += 2;
                            result.Add(MakeLexeme(
                                CODE_DECREMENT,
                                "оператор декремента",
                                "--",
                                line,
                                tokenStartCol,
                                col - 1,
                                tokenStart,
                                2));
                        }
                        else
                        {
                            i++;
                            col++;
                            result.Add(MakeLexeme(
                                CODE_MINUS,
                                "оператор вычитания",
                                "-",
                                line,
                                tokenStartCol,
                                col - 1,
                                tokenStart,
                                1));
                        }
                        continue;

                    case '*':
                        i++;
                        col++;
                        result.Add(MakeLexeme(
                            CODE_MULTIPLY,
                            "оператор умножения",
                            "*",
                            line,
                            tokenStartCol,
                            col - 1,
                            tokenStart,
                            1));
                        continue;

                    case '/':
                        i++;
                        col++;
                        result.Add(MakeLexeme(
                            CODE_DIVIDE,
                            "оператор деления",
                            "/",
                            line,
                            tokenStartCol,
                            col - 1,
                            tokenStart,
                            1));
                        continue;

                    case '=':
                        if (i + 1 < text.Length && text[i + 1] == '=')
                        {
                            i += 2;
                            col += 2;
                            result.Add(MakeLexeme(
                                CODE_EQUAL,
                                "оператор равенства",
                                "==",
                                line,
                                tokenStartCol,
                                col - 1,
                                tokenStart,
                                2));
                        }
                        else
                        {
                            i++;
                            col++;
                            result.Add(MakeLexeme(
                                CODE_ASSIGN,
                                "оператор присваивания",
                                "=",
                                line,
                                tokenStartCol,
                                col - 1,
                                tokenStart,
                                1));
                        }
                        continue;

                    case '>':
                        if (i + 1 < text.Length && text[i + 1] == '=')
                        {
                            i += 2;
                            col += 2;
                            result.Add(MakeLexeme(
                                CODE_GREATER_OR_EQUAL,
                                "оператор сравнения больше либо равно",
                                ">=",
                                line,
                                tokenStartCol,
                                col - 1,
                                tokenStart,
                                2));
                        }
                        else
                        {
                            i++;
                            col++;
                            result.Add(MakeLexeme(
                                CODE_GREATER,
                                "оператор сравнения больше",
                                ">",
                                line,
                                tokenStartCol,
                                col - 1,
                                tokenStart,
                                1));
                        }
                        continue;

                    case '<':
                        if (i + 1 < text.Length && text[i + 1] == '=')
                        {
                            i += 2;
                            col += 2;
                            result.Add(MakeLexeme(
                                CODE_LESS_OR_EQUAL,
                                "оператор сравнения меньше либо равно",
                                "<=",
                                line,
                                tokenStartCol,
                                col - 1,
                                tokenStart,
                                2));
                        }
                        else
                        {
                            i++;
                            col++;
                            result.Add(MakeLexeme(
                                CODE_LESS,
                                "оператор сравнения меньше",
                                "<",
                                line,
                                tokenStartCol,
                                col - 1,
                                tokenStart,
                                1));
                        }
                        continue;

                    case '!':
                        if (i + 1 < text.Length && text[i + 1] == '=')
                        {
                            i += 2;
                            col += 2;
                            result.Add(MakeLexeme(
                                CODE_NOT_EQUAL,
                                "оператор сравнения не равно",
                                "!=",
                                line,
                                tokenStartCol,
                                col - 1,
                                tokenStart,
                                2));
                        }
                        else
                        {
                            i++;
                            col++;
                            result.Add(MakeLexeme(
                                CODE_ERROR,
                                "ошибка: недопустимый символ",
                                "!",
                                line,
                                tokenStartCol,
                                tokenStartCol,
                                tokenStart,
                                1,
                                true));
                        }
                        continue;

                    case '{':
                        i++;
                        col++;
                        result.Add(MakeLexeme(
                            CODE_LBRACE,
                            "открывающая фигурная скобка",
                            "{",
                            line,
                            tokenStartCol,
                            col - 1,
                            tokenStart,
                            1));
                        continue;

                    case '}':
                        i++;
                        col++;
                        result.Add(MakeLexeme(
                            CODE_RBRACE,
                            "закрывающая фигурная скобка",
                            "}",
                            line,
                            tokenStartCol,
                            col - 1,
                            tokenStart,
                            1));
                        continue;

                    case '(':
                        i++;
                        col++;
                        result.Add(MakeLexeme(
                            CODE_LPAREN,
                            "открывающая круглая скобка",
                            "(",
                            line,
                            tokenStartCol,
                            col - 1,
                            tokenStart,
                            1));
                        continue;

                    case ')':
                        i++;
                        col++;
                        result.Add(MakeLexeme(
                            CODE_RPAREN,
                            "закрывающая круглая скобка",
                            ")",
                            line,
                            tokenStartCol,
                            col - 1,
                            tokenStart,
                            1));
                        continue;

                    case ';':
                        i++;
                        col++;
                        result.Add(MakeLexeme(
                            CODE_SEMICOLON,
                            "конец оператора",
                            ";",
                            line,
                            tokenStartCol,
                            col - 1,
                            tokenStart,
                            1));
                        continue;

                    default:
                        i++;
                        col++;
                        result.Add(MakeLexeme(
                            CODE_ERROR,
                            "ошибка: недопустимый символ",
                            ch.ToString(),
                            line,
                            tokenStartCol,
                            tokenStartCol,
                            tokenStart,
                            1,
                            true));
                        continue;
                }
            }

            return result;
        }

        private static Lexeme MakeLexeme(
            int code,
            string type,
            string text,
            int line,
            int colFrom,
            int colTo,
            int startIndex,
            int length,
            bool isError = false)
        {
            return new Lexeme
            {
                Code = code,
                Type = type,
                Text = text,
                Location = $"строка {line}, {colFrom}-{colTo}",
                StartIndex = startIndex,
                Length = length,
                IsError = isError,
                Line = line,
                ColumnFrom = colFrom,
                ColumnTo = colTo
            };
        }

        private static bool IsMeaningfulStart(char c)
        {
            return IsLetter(c) || IsDigit(c) || c == '_' ||
                   c == '+' || c == '-' || c == '*' || c == '/' ||
                   c == '=' || c == '>' || c == '<' || c == '!' ||
                   c == '{' || c == '}' || c == '(' || c == ')' || c == ';';
        }

        private static bool IsLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }
    }
    
}