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

            bool hadLexicalErrors = lexemes.Any(t => t.IsError);

            // Для синтаксического анализа убираем только пробелы,
            // но НЕ выбрасываем ошибочные лексемы, чтобы не терять контекст.
            _tokens = lexemes
                .Where(t => t.Code != 23)
                .ToList();

            _position = 0;

            if (_tokens.Count == 0)
            {
                if (!hadLexicalErrors)
                {
                    _result.Message = "Ожидается строка для анализа.";
                    return _result;
                }

                SetFinalMessage();
                return _result;
            }

            DW();

            // Лишние ; после конца конструкции do-while
            ConsumeSemicolonSequence("Лишние символы ; после конца конструкции do-while", 3);

            if (Current != null && !Current.IsError)
            {
                AddError(Current, "Лишний текст после конца конструкции do-while", 0);
            }

            NormalizeErrors();
            SetFinalMessage();
            return _result;
        }

        private Lexeme? Current =>
            _position < _tokens.Count ? _tokens[_position] : null;

        private Lexeme? Peek(int offset = 1)
        {
            int index = _position + offset;
            return index < _tokens.Count ? _tokens[index] : null;
        }

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

        private bool PeekText(int offset, string text)
        {
            return Peek(offset)?.Text == text;
        }

        private bool CheckError()
        {
            return Current != null && Current.IsError;
        }

        private bool CheckBrokenKeyword(string keyword)
        {
            return Current != null &&
                   Current.IsError &&
                   Current.Type == $"ошибка: искажено ключевое слово {keyword}";
        }

        private bool PeekBrokenKeyword(int offset, string keyword)
        {
            var lex = Peek(offset);
            return lex != null &&
                   lex.IsError &&
                   lex.Type == $"ошибка: искажено ключевое слово {keyword}";
        }

        private bool CheckTextOrBrokenKeyword(string keyword)
        {
            return CheckText(keyword) || CheckBrokenKeyword(keyword);
        }

        private bool PeekTextOrBrokenKeyword(int offset, string keyword)
        {
            return PeekText(offset, keyword) || PeekBrokenKeyword(offset, keyword);
        }

        private bool CheckIdentifier()
        {
            return Current != null &&
                   !Current.IsError &&
                   Current.Type == "идентификатор";
        }

        private bool CheckNumber()
        {
            return Current != null &&
                   !Current.IsError &&
                   Current.Type == "целое без знака";
        }

        private bool CheckStatementStart()
        {
            return IsStatementStartAt(_position);
        }

        private bool IsStatementStartAt(int position)
        {
            if (position < 0 || position >= _tokens.Count)
                return false;

            var first = _tokens[position];

            if (first.IsError || first.Type != "идентификатор")
                return false;

            int nextPos = position + 1;

            if (nextPos >= _tokens.Count)
                return false;

            var second = _tokens[nextPos];

            if (second.IsError)
                return false;

            return second.Text == "++" ||
                   second.Text == "--" ||
                   second.Text == "=";
        }

        private bool CheckRelOp()
        {
            if (Current == null || Current.IsError)
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
            if (Current == null || Current.IsError)
                return false;

            return Current.Text == "and" ||
                   Current.Text == "or" ||
                   Current.Text == "&&" ||
                   Current.Text == "||";
        }

        private bool CheckBrokenLogicalOp()
        {
            return CheckBrokenKeyword("and") || CheckBrokenKeyword("or");
        }

        private bool CheckLogicalOpOrBroken()
        {
            return CheckLogicalOp() || CheckBrokenLogicalOp();
        }

        private bool IsStopToken(string stopToken)
        {
            return stopToken switch
            {
                "<stmt>" => CheckStatementStart(),
                "<relop>" => CheckRelOp(),
                "<logicop>" => CheckLogicalOpOrBroken(),
                "<whilepart>" => CheckWhilePartStart(),
                "id" => CheckIdentifier(),
                "num" => CheckNumber(),
                "while" => CheckTextOrBrokenKeyword("while"),
                "do" => CheckTextOrBrokenKeyword("do"),
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
                   CheckTextOrBrokenKeyword("while") ||
                   CheckRelOp() ||
                   CheckLogicalOpOrBroken();
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

        private void AddGroupedError(
            Lexeme firstLexeme,
            Lexeme lastLexeme,
            string invalidFragment,
            string description,
            int priority = 0)
        {
            _result.Errors.Add(new SyntaxErrorInfo
            {
                InvalidFragment = invalidFragment,
                Location = $"строка {firstLexeme.Line}, позиция {firstLexeme.ColumnFrom}",
                Description = description,
                StartIndex = firstLexeme.StartIndex,
                Length = (lastLexeme.StartIndex + lastLexeme.Length) - firstLexeme.StartIndex,
                Line = firstLexeme.Line,
                ColumnFrom = firstLexeme.ColumnFrom,
                ColumnTo = lastLexeme.ColumnTo,
                Priority = priority
            });
        }

        // Универсальная постановка ошибки о пропущенном токене
        // в позицию СРАЗУ ПОСЛЕ предыдущего токена.
        private void AddMissingTokenAfterPrevious(string expectedToken, string description, int priority = 1)
        {
            if (_tokens.Count == 0)
            {
                AddError(null, description, priority);
                return;
            }

            Lexeme anchor = _position > 0 ? _tokens[_position - 1] : _tokens[0];

            int startIndex = anchor.StartIndex + anchor.Length;
            int line = anchor.Line;
            int column = anchor.ColumnTo + 1;

            _result.Errors.Add(new SyntaxErrorInfo
            {
                InvalidFragment = expectedToken,
                Location = $"строка {line}, позиция {column}",
                Description = description,
                StartIndex = startIndex,
                Length = 0,
                Line = line,
                ColumnFrom = column,
                ColumnTo = column,
                Priority = priority
            });
        }

        private bool ConsumeSemicolonSequence(string description, int priority = 3)
        {
            if (!CheckText(";"))
                return false;

            var first = Current!;
            var last = Current!;
            string fragment = "";

            while (CheckText(";"))
            {
                fragment += Current!.Text;
                last = Current!;
                Next();
            }

            AddGroupedError(first, last, fragment, description, priority);
            return true;
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

                // Если ошибки стоят в одной и той же позиции,
                // то схлопываем только действительно дубли.
                // Разные ожидаемые символы в одной позиции (например ) и ;)
                // должны сохраняться обе.
                bool samePosition = last.StartIndex == err.StartIndex;
                bool sameFragment = last.InvalidFragment == err.InvalidFragment;
                bool sameDescription = last.Description == err.Description;

                if (samePosition && (sameFragment || sameDescription))
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

        private bool AcceptKeyword(string keyword)
        {
            if (CheckText(keyword))
            {
                Next();
                return true;
            }

            // Искажённое ключевое слово уже зафиксировано лексером,
            // здесь принимаем его как точку продолжения анализа.
            if (CheckBrokenKeyword(keyword))
            {
                Next();
                return true;
            }

            return false;
        }

        private bool AcceptLogicalOp()
        {
            if (CheckLogicalOp())
            {
                Next();
                return true;
            }

            if (CheckBrokenLogicalOp())
            {
                Next();
                return true;
            }

            return false;
        }

        private void MatchOrInsert(string text, string errorDescription, int priority = 2)
        {
            if (CheckText(text))
            {
                Next();
                return;
            }

            AddMissingTokenAfterPrevious(text, errorDescription, priority);
        }

        private void ExpectSemicolonAfter(string errorDescription)
        {
            if (CheckText(";"))
            {
                Next();
                return;
            }

            AddMissingTokenAfterPrevious(";", errorDescription, 1);

            RecoveryTo(";", "}", "while", "<stmt>");

            if (CheckText(";"))
                Next();
        }

        private void REL_OP()
        {
            if (CheckRelOp())
            {
                Next();
                return;
            }

            if (!CheckError())
                AddError(Current, "Ожидалась операция сравнения", 2);
        }

        private bool CheckWhilePartStart()
        {
            return CheckTextOrBrokenKeyword("while") ||
                   (CheckIdentifier() && PeekText(1, "("));
        }

        private bool HandleUnexpectedKeywordInsideBlock(string keyword, string description)
        {
            if (!CheckTextOrBrokenKeyword(keyword))
                return false;

            AddError(Current, description, 3);
            Next();

            if (Current == null)
                return true;

            // Если после лишнего ключевого слова сразу идёт корректный оператор,
            // разбираем его как обычный оператор блока.
            if (CheckStatementStart())
            {
                STMT();
                return true;
            }

            RecoveryTo(";", "<stmt>", "}", "while");

            if (CheckText(";"))
                Next();

            return true;
        }


        private void AddMissingTokenAfterLexeme(Lexeme anchor, string expectedToken, string description, int priority = 1)
        {
            int startIndex = anchor.StartIndex + anchor.Length;
            int line = anchor.Line;
            int column = anchor.ColumnTo + 1;

            _result.Errors.Add(new SyntaxErrorInfo
            {
                InvalidFragment = expectedToken,
                Location = $"строка {line}, позиция {column}",
                Description = description,
                StartIndex = startIndex,
                Length = 0,
                Line = line,
                ColumnFrom = column,
                ColumnTo = column,
                Priority = priority
            });
        }

        // ===== Нетерминалы =====

        private void DW()
        {
            if (AcceptKeyword("do"))
            {
                BODY();
                return;
            }

            if (!CheckError())
                AddError(Current, "Ожидалось ключевое слово do", 3);

            if (Current == null)
                return;

            if (CheckTextOrBrokenKeyword("while"))
            {
                Next();
                RecoveryTo("{", "<stmt>", "}", "while");

                if (Current == null)
                    return;

                BODY();
                return;
            }

            if (!CheckText("{") && !CheckText("}") && !CheckStatementStart())
            {
                RecoveryTo("{", "<stmt>", "}", "while");

                if (Current == null)
                    return;
            }

            BODY();
        }

        private void BODY()
        {
            if (ConsumeSemicolonSequence("Лишние символы ;: после do ожидалось тело цикла", 3))
            {
                if (Current != null)
                    BODY();
                return;
            }

            if (CheckText("{"))
            {
                BLOCK();
                WHILE_PART();
                return;
            }

            if (CheckIdentifier() || CheckStatementStart())
            {
                STMT();

                if (CheckText("}"))
                {
                    AddError(Current, "Лишний символ }", 3);
                    Next();
                    WHILE_PART();
                    return;
                }

                WHILE_PART();
                return;
            }

            if (CheckTextOrBrokenKeyword("while"))
            {
                AddError(Current, "Лишнее ключевое слово while: после do ожидалось тело цикла", 3);
                Next();

                RecoveryTo("{", "<stmt>", "}", "while");

                if (Current == null)
                    return;

                if (CheckText("{") || CheckStatementStart() || CheckText("}") || CheckText(";"))
                {
                    BODY();
                    return;
                }

                if (CheckTextOrBrokenKeyword("while"))
                {
                    WHILE_PART();
                    return;
                }

                return;
            }

            if (CheckText("}"))
            {
                AddError(Current, "Ожидалось тело цикла: блок { ... } или оператор", 3);
                Next();
                WHILE_PART();
                return;
            }

            if (CheckError())
            {
                RecoveryTo("{", "<stmt>", "}", "while", ";");

                if (Current == null)
                    return;

                BODY();
                return;
            }

            AddError(Current, "Ожидалось тело цикла: блок { ... } или оператор", 3);
            RecoveryTo("{", "<stmt>", "}", "while", ";");

            if (Current != null)
                BODY();
        }

        private void BLOCK()
        {
            MatchOrInsert("{", "Ожидался символ {");
            BLOCK_CONTENT();
        }

        private void BLOCK_CONTENT()
        {
            while (true)
            {
                // Пустой блок допустим
                if (CheckText("}"))
                {
                    Next();
                    return;
                }

                if (Current == null)
                {
                    AddError(null, "Ожидался символ }");
                    return;
                }

                // Лишние ; внутри блока
                if (ConsumeSemicolonSequence("Лишние символы ; внутри блока", 3))
                    continue;

                if (CheckError())
                {
                    RecoveryTo("<stmt>", "}", "while", ";");

                    if (CheckText("}"))
                    {
                        Next();
                        return;
                    }

                    if (ConsumeSemicolonSequence("Лишние символы ; внутри блока", 3))
                        continue;

                    if (Current == null)
                    {
                        AddError(null, "Ожидался символ }");
                        return;
                    }
                }

                STMT();

                if (Current == null)
                {
                    AddError(null, "Ожидался символ }");
                    return;
                }

                // Лишние ; после корректного оператора внутри блока
                if (ConsumeSemicolonSequence("Лишние символы ; внутри блока", 3))
                    continue;

                if (CheckText("}"))
                {
                    Next();
                    return;
                }

                // Разрешаем ещё один оператор в блоке
                if (CheckStatementStart() ||
                    CheckTextOrBrokenKeyword("do") ||
                    CheckTextOrBrokenKeyword("while") ||
                    CheckText(";") ||
                    CheckError())
                {
                    continue;
                }

                MatchOrInsert("}", "Ожидался символ }");
                return;
            }
        }

        private void STMT()
        {
            if (HandleUnexpectedKeywordInsideBlock("do", "Лишнее ключевое слово do внутри блока"))
                return;

            if (HandleUnexpectedKeywordInsideBlock("while", "Лишнее ключевое слово while внутри блока"))
                return;

            if (CheckIdentifier())
            {
                Next();
                STMT_TAIL();
                return;
            }

            if (CheckError())
            {
                RecoveryTo("<stmt>", "}", "while", ";");

                if (CheckStatementStart())
                {
                    STMT();
                    return;
                }

                return;
            }

            AddError(Current, "Ожидался идентификатор в начале оператора", 3);
            RecoveryTo(";", "<stmt>", "}", "while");

            if (CheckText(";"))
                Next();
        }

        private void STMT_TAIL()
        {
            if (CheckText("++"))
            {
                Next();
                ExpectSemicolonAfter("Ожидался символ ; после оператора ++");
                return;
            }

            if (CheckText("--"))
            {
                Next();
                ExpectSemicolonAfter("Ожидался символ ; после оператора --");
                return;
            }

            if (CheckText("="))
            {
                Next();
                EXPR();
                ExpectSemicolonAfter("Ожидался символ ; после оператора присваивания");
                return;
            }

            // Запоминаем место, после которого должен был закончиться оператор
            var anchor = Current ?? (_position > 0 ? _tokens[_position - 1] : null);

            if (CheckError())
            {
                RecoveryTo(";", "<stmt>", "}", "<whilepart>");

                if (CheckText(";"))
                {
                    Next();
                    return;
                }

                if (anchor != null)
                {
                    AddMissingTokenAfterLexeme(
                        anchor,
                        ";",
                        "Ожидался символ ; после незавершённого оператора",
                        1);
                }

                return;
            }

            AddError(Current, "Ожидались ++, -- или = после идентификатора", 3);
            RecoveryTo(";", "<stmt>", "}", "<whilepart>");

            if (CheckText(";"))
            {
                Next();
                return;
            }

            if (anchor != null)
            {
                AddMissingTokenAfterLexeme(
                    anchor,
                    ";",
                    "Ожидался символ ; после незавершённого оператора",
                    1);
            }
        }

        private void WHILE_PART()
        {
            // Лишние ; между телом цикла и частью while
            ConsumeSemicolonSequence("Лишние символы ; перед частью while", 3);

            if (AcceptKeyword("while"))
            {
                COND();
                return;
            }

            // Если вместо while стоит идентификатор, а дальше идёт '(',
            // считаем, что пользователь ошибся в написании while,
            // но условие всё же пытаемся разобрать.
            if (CheckIdentifier() && PeekText(1, "("))
            {
                AddError(Current, "Ожидалось ключевое слово while", 3);
                Next(); // пропускаем неверное слово, например whele

                if (CheckText("("))
                {
                    COND();
                    return;
                }
            }

            if (Current == null)
            {
                AddError(null, "Ожидалась часть while (...) ; в конце конструкции do-while");
                return;
            }

            if (!CheckError())
                AddError(Current, "Ожидалось ключевое слово while", 3);

            RecoveryTo("while", "(", ";");

            ConsumeSemicolonSequence("Лишние символы ; перед частью while", 3);

            if (AcceptKeyword("while"))
            {
                COND();
                return;
            }

            if (CheckText("("))
            {
                COND();
            }
        }

        private void COND()
        {
            if (CheckText("("))
            {
                Next();
            }
            else
            {
                if (ConsumeSemicolonSequence("Лишние символы ;: после while ожидался символ (", 3))
                {
                    if (CheckText("("))
                    {
                        Next();
                    }
                    else if (Current == null)
                    {
                        AddError(null, "Ожидалось условие после while");
                        return;
                    }
                }

                if (!CheckText("("))
                {
                    if (CheckTextOrBrokenKeyword("do"))
                    {
                        AddError(Current, "Лишнее ключевое слово do: после while ожидался символ (", 3);
                        Next();
                        RecoveryTo("(", "id", "num");
                    }
                    else if (CheckTextOrBrokenKeyword("while"))
                    {
                        AddError(Current, "Лишнее ключевое слово while: после while ожидался символ (", 3);
                        Next();
                        RecoveryTo("(", "id", "num");
                    }
                    else if (!CheckText("("))
                    {
                        AddError(Current, "Ожидался символ ( после while", 2);
                    }

                    if (CheckText("("))
                        Next();
                }
            }

            if (Current == null)
            {
                AddError(null, "Ожидалось условие после while");
                return;
            }

            REL_EXPR();

            // ВАЖНО:
            // COND_TAIL должен вызываться даже если дошли до конца строки,
            // чтобы зафиксировать пропущенные ) и ;
            COND_TAIL();
        }

        private void COND_TAIL()
        {
            while (CheckLogicalOpOrBroken())
            {
                LOGICAL_OP();
                REL_EXPR();

                if (Current == null)
                    return;
            }

            MatchOrInsert(")", "Ожидался символ ) после условия");
            MatchOrInsert(";", "Ожидался символ ; в конце конструкции do-while");

            // Лишние ; после завершающего ; конструкции
            ConsumeSemicolonSequence("Лишние символы ; после конца конструкции do-while", 3);
        }

        private void LOGICAL_OP()
        {
            if (AcceptLogicalOp())
                return;

            if (!CheckError())
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

            if (CheckError())
            {
                RecoveryTo("id", "num", "(", ")", ";", "+", "-", "*", "/", "}", "while", "<relop>", "<logicop>");

                if (CheckIdentifier() || CheckNumber() || CheckText("("))
                    FACTOR();

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
                FACTOR();
        }
    }
}