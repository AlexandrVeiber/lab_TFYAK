using System.Collections.Generic;
using System.Linq;
using GUI.Scanner;

namespace GUI.Syntax
{
    public class SyntaxAnalyzer
    {
        private List<Lexeme> _tokens = new();
        private int _position;
        private SyntaxParseResult _result = new();

        public SyntaxParseResult Analyze(List<Lexeme> lexemes)
        {
            _result = new SyntaxParseResult();

            // Убираем пробелы, но оставляем все значимые лексемы
            _tokens = lexemes
                .Where(t => t.Code != 23)
                .ToList();

            _position = 0;

            if (_tokens.Count == 0)
            {
                AddError(null, "Ожидалось ключевое слово do");
                _result.Message = "Синтаксический анализ завершён. Найдено ошибок: 1.";
                return _result;
            }

            DW();

            if (_result.Success)
                _result.Message = "Синтаксический анализ завершён. Ошибок нет.";
            else
                _result.Message = $"Синтаксический анализ завершён. Найдено ошибок: {_result.Errors.Count}.";

            return _result;
        }

        private Lexeme? Current =>
            _position < _tokens.Count ? _tokens[_position] : null;

        private void Next()
        {
            if (_position < _tokens.Count)
                _position++;
        }

        private bool CheckText(string text)
        {
            return Current != null && Current.Text == text;
        }

        private bool CheckIdentifier()
        {
            return Current != null && Current.Type == "идентификатор";
        }

        private bool CheckNumber()
        {
            return Current != null && Current.Type == "целое без знака";
        }

        private void AddError(Lexeme? lexeme, string description)
        {
            if (lexeme == null)
            {
                _result.Errors.Add(new SyntaxErrorInfo
                {
                    InvalidFragment = "(конец строки)",
                    Location = "конец строки",
                    Description = description,
                    StartIndex = _tokens.Count > 0
                        ? _tokens[^1].StartIndex + _tokens[^1].Length
                        : 0,
                    Length = 0,
                    Line = _tokens.Count > 0 ? _tokens[^1].Line : 1,
                    ColumnFrom = _tokens.Count > 0 ? _tokens[^1].ColumnTo + 1 : 1,
                    ColumnTo = _tokens.Count > 0 ? _tokens[^1].ColumnTo + 1 : 1
                });

                return;
            }

            _result.Errors.Add(new SyntaxErrorInfo
            {
                InvalidFragment = lexeme.Text,
                Location = $"строка {lexeme.Line}, позиция {lexeme.ColumnFrom}",
                Description = description,
                StartIndex = lexeme.StartIndex,
                Length = lexeme.Length,
                Line = lexeme.Line,
                ColumnFrom = lexeme.ColumnFrom,
                ColumnTo = lexeme.ColumnTo
            });
        }

        private bool MatchText(string text, string errorDescription)
        {
            if (CheckText(text))
            {
                Next();
                return true;
            }

            AddError(Current, errorDescription);
            return false;
        }

        // ===== Нетерминалы =====

        private void DW()
        {
            // пока только каркас
            MatchText("do", "Ожидалось ключевое слово do");
            // дальше BODY();
        }
    }
}