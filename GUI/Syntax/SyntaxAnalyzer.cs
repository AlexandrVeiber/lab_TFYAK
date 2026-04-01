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

        private bool _suppressTrailingTextError;

        public SyntaxParseResult Analyze(List<Lexeme> lexemes)
        {
            _result = new SyntaxParseResult();
            _suppressTrailingTextError = false;

            bool hadLexicalErrors = lexemes.Any(t => t.IsError);

            // Для синтаксического анализа берём только корректные непустые лексемы
            _tokens = lexemes
                .Where(t => t.Code != 23 && !t.IsError)
                .ToList();

            _position = 0;

            if (_tokens.Count == 0)
            {
                if (!hadLexicalErrors)
                {
                    AddError(null, "Ожидалось ключевое слово do");
                }

                SetFinalMessage();
                return _result;
            }

            DW();

            if (!_suppressTrailingTextError && Current != null)
            {
                AddError(Current, "Лишний текст после конца конструкции do-while", 0);
            }

            NormalizeErrors();
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
                _result.Message = "Синтаксический анализ завершён. Ошибок нет. Общее количество найденных ошибок: 0.";
            else
                _result.Message = $"Синтаксический анализ завершён. Общее количество найденных ошибок: {_result.Errors.Count}.";
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

        private bool CheckStatementStart()
        {
            return CheckIdentifier();
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

        private bool CheckLogicalOp()
        {
            if (Current == null)
                return false;

            return Current.Text == "and" ||
                   Current.Text == "or" ||
                   Current.Text == "&&" ||
                   Current.Text == "||";
        }

        private bool IsStopToken(string stopToken)
        {
            return stopToken switch
            {
                "<stmt>" => CheckStatementStart(),
                "<relop>" => CheckRelOp(),
                "<logicop>" => CheckLogicalOp(),
                "id" => CheckIdentifier(),
                "num" => CheckNumber(),
                _ => CheckText(stopToken)
            };
        }

        private void RecoveryTo(params string[] stopTokens)
        {
            while (Current != null && !stopTokens.Any(IsStopToken))
            {
                Next();
            }
        }

        private bool IsFactorFollow()
        {
            return Current == null ||
                   CheckText(")") ||
                   CheckText(";") ||
                   CheckText("+") ||
                   CheckText("-") ||
                   CheckText("*") ||
                   CheckText("/") ||
                   CheckText("}") ||
                   CheckText("while") ||
                   CheckRelOp() ||
                   CheckLogicalOp();
        }

        private void AddError(Lexeme? lexeme, string description, int priority = 0)
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
                    ColumnTo = column,
                    Priority = priority
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
                ColumnTo = lexeme.ColumnTo,
                Priority = priority
            });
        }

        private void NormalizeErrors()
        {
            var normalized = _result.Errors
                .OrderBy(e => e.Line)
                .ThenBy(e => e.ColumnFrom)
                .ThenByDescending(e => e.Priority)
                .ToList();

            var unique = new List<SyntaxErrorInfo>();

            foreach (var err in normalized)
            {
                if (unique.Count == 0)
                {
                    unique.Add(err);
                    continue;
                }

                var last = unique[^1];

                if (last.StartIndex == err.StartIndex)
                {
                    if (err.Priority > last.Priority)
                        unique[^1] = err;

                    continue;
                }

                unique.Add(err);
            }

            _result.Errors.Clear();
            _result.Errors.AddRange(unique);
        }

        /// <summary>
        /// Пытается сопоставить ожидаемый терминал.
        /// Если терминал отсутствует, фиксирует ошибку и
        /// считает, что он был виртуально вставлен.
        /// Текущая лексема при этом не потребляется.
        /// </summary>
        private void MatchOrInsert(string text, string errorDescription, int priority = 2)
        {
            if (CheckText(text))
            {
                Next();
                return;
            }

            AddError(Current, errorDescription, priority);
        }

        /// <summary>
        /// Проверка операции отношения.
        /// Если её нет, считаем, что она была виртуально вставлена.
        /// </summary>
        private void REL_OP()
        {
            if (CheckRelOp())
            {
                Next();
                return;
            }

            AddError(Current, "Ожидалась операция сравнения", 2);
        }

        // ===== Нетерминалы =====

        private void DW()
        {
            if (CheckText("do"))
            {
                Next();
                BODY();
                return;
            }

            AddError(Current, "Ожидалось ключевое слово do");
            RecoveryTo("do", "{", "<stmt>", "while");

            if (CheckText("do"))
            {
                Next();
                BODY();
                return;
            }

            if (CheckText("while") || Current == null)
            {
                _suppressTrailingTextError = true;
                return;
            }

            BODY();
        }

        private void BODY()
        {
            if (CheckText("{"))
            {
                BLOCK();
                WHILE_PART();
                return;
            }

            if (CheckStatementStart())
            {
                STMT();
                WHILE_PART();
                return;
            }

            if (CheckText("while"))
            {
                AddError(Current, "Ожидалось тело цикла: блок { ... } или оператор");
                WHILE_PART();
                return;
            }

            AddError(Current, "Ожидалось тело цикла: блок { ... } или оператор");
            RecoveryTo("{", "<stmt>", "while");

            if (CheckText("{"))
            {
                BLOCK();
                WHILE_PART();
                return;
            }

            if (CheckStatementStart())
            {
                STMT();
                WHILE_PART();
                return;
            }

            if (CheckText("while"))
            {
                WHILE_PART();
            }
        }

        private void BLOCK()
        {
            MatchOrInsert("{", "Ожидался символ {");
            BLOCK_CONTENT();
        }

        private void BLOCK_CONTENT()
        {
            if (CheckText("}"))
            {
                Next();
                return;
            }

            if (CheckText("while"))
            {
                AddError(Current, "Ожидался символ }");
                return;
            }

            STMT_LIST();
            MatchOrInsert("}", "Ожидался символ }");
        }

        private void STMT_LIST()
        {
            STMT();
            STMT_LIST_TAIL();
        }

        private void STMT_LIST_TAIL()
        {
            while (CheckStatementStart())
            {
                STMT();
            }

            if (Current != null && !CheckText("}") && !CheckText("while"))
            {
                AddError(Current, "Ожидался следующий оператор или символ }");
                RecoveryTo("<stmt>", "}", "while");

                while (CheckStatementStart())
                {
                    STMT();
                }
            }
        }

        private void STMT()
        {
            if (CheckIdentifier())
            {
                Next();
                STMT_TAIL();
                return;
            }

            AddError(Current, "Ожидался идентификатор в начале оператора", 3);

            if (CheckText(";"))
            {
                Next();
                return;
            }

            RecoveryTo(";", "<stmt>", "}", "while");

            if (CheckText(";"))
                Next();
        }

        private void STMT_TAIL()
        {
            if (CheckText("++"))
            {
                Next();
                MatchOrInsert(";", "Ожидался символ ; после оператора ++");
                return;
            }

            if (CheckText("--"))
            {
                Next();
                MatchOrInsert(";", "Ожидался символ ; после оператора --");
                return;
            }

            if (CheckText("="))
            {
                Next();
                EXPR();
                MatchOrInsert(";", "Ожидался символ ; после оператора присваивания", 1);
                return;
            }

            AddError(Current, "Ожидались ++, -- или = после идентификатора", 3);
            RecoveryTo(";", "<stmt>", "}", "while");

            if (CheckText(";"))
                Next();
        }

        private void WHILE_PART()
        {
            MatchOrInsert("while", "Ожидалось ключевое слово while");
            COND();
        }

        private void COND()
        {
            MatchOrInsert("(", "Ожидался символ ( после while");

            if (Current == null)
            {
                AddError(null, "Ожидалось условие после while");
                return;
            }

            REL_EXPR();

            if (Current == null)
                return;

            COND_TAIL();
        }

        private void COND_TAIL()
        {
            while (CheckLogicalOp())
            {
                LOGICAL_OP();
                REL_EXPR();

                if (Current == null)
                    return;
            }

            MatchOrInsert(")", "Ожидался символ ) после условия");
            MatchOrInsert(";", "Ожидался символ ; в конце конструкции do-while");
        }

        private void LOGICAL_OP()
        {
            if (CheckLogicalOp())
            {
                Next();
                return;
            }

            AddError(Current, "Ожидалась логическая операция");
        }

        private void REL_EXPR()
        {
            EXPR();
            REL_OP();
            EXPR();
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
                MatchOrInsert(")", "Ожидался символ )");
                return;
            }

            if (IsFactorFollow())
            {
                AddError(Current, "Ожидались идентификатор, число или выражение в скобках", 3);
                return;
            }

            AddError(Current, "Ожидались идентификатор, число или выражение в скобках", 3);
            RecoveryTo("id", "num", "(", ")", ";", "+", "-", "*", "/", "}", "while", "<relop>", "<logicop>");

            if (CheckIdentifier() || CheckNumber() || CheckText("("))
            {
                FACTOR();
            }
        }
    }
}