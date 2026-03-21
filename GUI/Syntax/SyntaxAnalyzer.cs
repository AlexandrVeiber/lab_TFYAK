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

            // Пробелы для синтаксического анализа не нужны
            _tokens = lexemes
                .Where(t => t.Code != 23)
                .ToList();

            _position = 0;

            if (_tokens.Count == 0)
            {
                AddError(null, "Ожидалось ключевое слово do");
                SetFinalMessage();
                return _result;
            }

            DW();

            if (Current != null)
            {
                AddError(Current, "Лишний текст после конца конструкции do-while");
            }

            SetFinalMessage();
            return _result;
        }

        private Lexeme? Current =>
            _position < _tokens.Count ? _tokens[_position] : null;

        private void Next()
        {
            if (_position < _tokens.Count)
                _position++;
        }

        private void SetFinalMessage()
        {
            if (_result.Success)
                _result.Message = "Синтаксический анализ завершён. Ошибок нет.";
            else
                _result.Message = $"Синтаксический анализ завершён. Найдено ошибок: {_result.Errors.Count}.";
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

        private bool CheckRelOp()
        {
            if (Current == null)
                return false;

            return Current.Text == "<" ||
                   Current.Text == "<=" ||
                   Current.Text == ">" ||
                   Current.Text == ">=" ||
                   Current.Text == "==" ||
                   Current.Text == "!=";
        }

        private void AddError(Lexeme? lexeme, string description)
        {
            if (lexeme == null)
            {
                int startIndex = 0;
                int line = 1;
                int column = 1;

                if (_tokens.Count > 0)
                {
                    var last = _tokens[^1];
                    startIndex = last.StartIndex + last.Length;
                    line = last.Line;
                    column = last.ColumnTo + 1;
                }

                _result.Errors.Add(new SyntaxErrorInfo
                {
                    InvalidFragment = "(конец строки)",
                    Location = $"строка {line}, позиция {column}",
                    Description = description,
                    StartIndex = startIndex,
                    Length = 0,
                    Line = line,
                    ColumnFrom = column,
                    ColumnTo = column
                });

                return;
            }

            var lastError = _result.Errors.LastOrDefault();
            if (lastError != null &&
                lastError.StartIndex == lexeme.StartIndex &&
                lastError.Description == description)
            {
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

        // Нейтрализация ошибки: переходим к ближайшему допустимому символу
        private void RecoveryTo(params string[] stopTokens)
        {
            while (Current != null && !stopTokens.Contains(Current.Text))
            {
                Next();
            }
        }

        // ===== Нетерминалы =====

        private void DW()
        {
            if (!MatchText("do", "Ожидалось ключевое слово do"))
            {
                RecoveryTo("do", "{", "while");
                if (CheckText("do"))
                    Next();
            }

            BODY();
        }

        private void BODY()
        {
            if (CheckText("{"))
            {
                Next();
                STMT_LIST();
                return;
            }

            AddError(Current, "Ожидался символ { после do");

            // Если после do уже идет начало оператора,
            // то считаем, что { просто пропущена,
            // и продолжаем разбор тела без грубого перескока.
            if (CheckIdentifier())
            {
                STMT_LIST();
                return;
            }

            // Если сразу встретили }, while или конец,
            // то оставляем текущий токен на месте —
            // STMT_LIST / следующие правила сами обработают ситуацию.
            if (CheckText("}") || CheckText("while") || Current == null)
            {
                STMT_LIST();
                return;
            }

            // Только если встретился совсем посторонний токен,
            // тогда уже переходим к восстановлению.
            RecoveryTo("{", "}", "while");

            if (CheckText("{"))
                Next();

            STMT_LIST();
        }

        private void STMT_LIST()
        {
            if (!CheckIdentifier())
            {
                AddError(Current, "Ожидался оператор в теле цикла");
                RecoveryTo("}", "while");
                if (CheckText("}"))
                {
                    Next();
                    WHILE_PART();
                }

                return;
            }

            STMT();
            STMT_LIST_TAIL();
        }

        private void STMT_LIST_TAIL()
        {
            while (CheckIdentifier())
            {
                STMT();
            }

            if (CheckText("}"))
            {
                Next();
                WHILE_PART();
                return;
            }

            AddError(Current, "Ожидался символ } или следующий оператор");
            RecoveryTo("}", "while");

            if (CheckText("}"))
                Next();

            WHILE_PART();
        }

        private void STMT()
        {
            if (!CheckIdentifier())
            {
                AddError(Current, "Ожидался идентификатор в начале оператора");
                RecoveryTo(";", "}", "while");
                if (CheckText(";"))
                    Next();
                return;
            }

            Next();
            STMT_TAIL();
        }

        private void STMT_TAIL()
        {
            if (CheckText("++"))
            {
                Next();

                if (!MatchText(";", "Ожидался символ ; после оператора ++"))
                {
                    RecoveryTo(";", "}", "while");
                    if (CheckText(";"))
                        Next();
                }

                return;
            }

            if (CheckText("--"))
            {
                Next();

                if (!MatchText(";", "Ожидался символ ; после оператора --"))
                {
                    RecoveryTo(";", "}", "while");
                    if (CheckText(";"))
                        Next();
                }

                return;
            }

            if (CheckText("="))
            {
                Next();
                EXPR();

                if (!MatchText(";", "Ожидался символ ; после оператора присваивания"))
                {
                    RecoveryTo(";", "}", "while");
                    if (CheckText(";"))
                        Next();
                }

                return;
            }

            AddError(Current, "Ожидались ++, -- или = после идентификатора");
            RecoveryTo(";", "}", "while");

            if (CheckText(";"))
                Next();
        }

        private void WHILE_PART()
        {
            if (!MatchText("while", "Ожидалось ключевое слово while"))
            {
                RecoveryTo("while", "(", ";");
                if (CheckText("while"))
                    Next();
            }

            COND();
        }

        private void COND()
        {
            if (!MatchText("(", "Ожидался символ ( после while"))
            {
                RecoveryTo("(", ")", ";", "<", "<=", ">", ">=", "==", "!=");
                if (CheckText("("))
                    Next();
            }

            EXPR();
            COND_TAIL();
        }

        private void COND_TAIL()
        {
            REL_OP();
            EXPR();

            if (!MatchText(")", "Ожидался символ ) после условия"))
            {
                RecoveryTo(")", ";");
                if (CheckText(")"))
                    Next();
            }

            if (!MatchText(";", "Ожидался символ ; в конце конструкции do-while"))
            {
                RecoveryTo(";");
                if (CheckText(";"))
                    Next();
            }
        }

        private void REL_OP()
        {
            if (CheckRelOp())
            {
                Next();
                return;
            }

            AddError(Current, "Ожидалась операция сравнения");
            RecoveryTo("<", "<=", ">", ">=", "==", "!=", ")", ";");

            if (CheckRelOp())
                Next();
        }

        private void EXPR()
        {
            TERM();
            EXPR_TAIL();
        }

        private void EXPR_TAIL()
        {
            while (CheckText("+") || CheckText("-"))
            {
                Next();
                TERM();
            }
        }

        private void TERM()
        {
            FACTOR();
            TERM_TAIL();
        }

        private void TERM_TAIL()
        {
            while (CheckText("*") || CheckText("/"))
            {
                Next();
                FACTOR();
            }
        }

        private void FACTOR()
        {
            if (CheckIdentifier() || CheckNumber())
            {
                Next();
                return;
            }

            if (CheckText("("))
            {
                Next();
                EXPR();

                if (!MatchText(")", "Ожидался символ )"))
                {
                    RecoveryTo(")", ";", "+", "-", "*", "/", "<", "<=", ">", ">=", "==", "!=");
                    if (CheckText(")"))
                        Next();
                }

                return;
            }

            AddError(Current, "Ожидались идентификатор, число или выражение в скобках");
            RecoveryTo(";", ")", "+", "-", "*", "/", "<", "<=", ">", ">=", "==", "!=");
        }
    }
}