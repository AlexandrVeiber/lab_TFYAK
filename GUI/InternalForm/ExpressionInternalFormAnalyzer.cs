using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace GUI.InternalForm
{
    public enum ExpressionTokenType
    {
        Number,
        Identifier,
        Plus,
        Minus,
        Multiply,
        Divide,
        Modulo,
        LeftParenthesis,
        RightParenthesis,
        Error
    }

    public sealed class ExpressionToken
    {
        public int Code { get; set; }
        public string Type { get; set; } = "";
        public string Text { get; set; } = "";
        public string Location { get; set; } = "";

        public ExpressionTokenType TokenType { get; set; }

        public int StartIndex { get; set; }
        public int Length { get; set; }
        public bool IsError { get; set; }

        public int Line { get; set; }
        public int ColumnFrom { get; set; }
        public int ColumnTo { get; set; }
    }

    public sealed class ExpressionErrorInfo
    {
        public string InvalidFragment { get; set; } = "";
        public string Location { get; set; } = "";
        public string Description { get; set; } = "";

        public int StartIndex { get; set; }
        public int Length { get; set; }

        public int Line { get; set; }
        public int ColumnFrom { get; set; }
        public int ColumnTo { get; set; }
    }

    public sealed class TetradRow
    {
        public int Number { get; set; }
        public string Operation { get; set; } = "";
        public string Argument1 { get; set; } = "";
        public string Argument2 { get; set; } = "";
        public string Result { get; set; } = "";
    }

    public sealed class InternalFormResult
    {
        public List<ExpressionToken> Lexemes { get; } = new();
        public List<ExpressionErrorInfo> LexicalErrors { get; } = new();
        public List<ExpressionErrorInfo> SyntaxErrors { get; } = new();

        public List<TetradRow> Tetrads { get; } = new();

        public List<string> PolizTokens { get; } = new();
        public string PolizText { get; set; } = "";
        public string EvaluationText { get; set; } = "";

        public bool ContainsIdentifier { get; set; }

        public bool HasLexicalErrors => LexicalErrors.Count > 0;
        public bool HasSyntaxErrors => SyntaxErrors.Count > 0;
        public bool Success => !HasLexicalErrors && !HasSyntaxErrors;

        public string DetailsText { get; set; } = "";
    }

    public sealed class ExpressionInternalFormAnalyzer
    {
        public List<ExpressionToken> Scan(string text)
        {
            return ExpressionLexer.Analyze(text);
        }

        public InternalFormResult Analyze(string text)
        {
            var result = new InternalFormResult();

            result.Lexemes.AddRange(ExpressionLexer.Analyze(text));

            foreach (var lexeme in result.Lexemes.Where(t => t.IsError))
            {
                result.LexicalErrors.Add(new ExpressionErrorInfo
                {
                    InvalidFragment = lexeme.Text,
                    Location = $"строка {lexeme.Line}, позиция {lexeme.ColumnFrom}",
                    Description = lexeme.Type,
                    StartIndex = lexeme.StartIndex,
                    Length = lexeme.Length,
                    Line = lexeme.Line,
                    ColumnFrom = lexeme.ColumnFrom,
                    ColumnTo = lexeme.ColumnTo
                });
            }

            if (result.HasLexicalErrors)
            {
                result.DetailsText =
                    "Внутренняя форма программы не построена, так как обнаружены лексические ошибки.\n" +
                    "Сначала исправьте недопустимые символы или фрагменты.";
                return result;
            }

            var parser = new ExpressionParser(result);
            ExprNode? root = parser.Parse();

            if (result.HasSyntaxErrors || root == null)
            {
                result.Tetrads.Clear();
                result.PolizTokens.Clear();
                result.PolizText = "";
                result.EvaluationText = "";
                result.DetailsText =
                    "Внутренняя форма программы не построена, так как обнаружены синтаксические ошибки.\n" +
                    "Согласно ТЗ, тетрады и ПОЛИЗ формируются только для корректных цепочек.";
                return result;
            }

            result.ContainsIdentifier = root.ContainsIdentifier;

            int tempCounter = 1;
            BuildTetrads(root, result.Tetrads, ref tempCounter);

            if (result.ContainsIdentifier)
            {
                result.DetailsText =
                    "Синтаксический анализ завершён успешно.\n\n" +
                    "Тетрады построены.\n\n" +
                    "ПОЛИЗ не формируется и значение не вычисляется, потому что выражение содержит идентификаторы.\n" +
                    "По ТЗ ПОЛИЗ и вычисление выполняются только для арифметического выражения, состоящего исключительно из целых чисел.";
                return result;
            }

            BuildPoliz(root, result.PolizTokens);
            result.PolizText = string.Join(" ", result.PolizTokens);

            if (TryEvaluate(root, out double value, out string evaluationError))
            {
                result.EvaluationText = FormatNumber(value);
            }
            else
            {
                result.EvaluationText = "Ошибка вычисления: " + evaluationError;
            }

            result.DetailsText = BuildDetailsText(result);
            return result;
        }

        private static string BuildDetailsText(InternalFormResult result)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Грамматика варианта:");
            sb.AppendLine("E → T A");
            sb.AppendLine("A → ε | + T A | - T A");
            sb.AppendLine("T → F B");
            sb.AppendLine("B → ε | * F B | / F B | % F B");
            sb.AppendLine("F → num | id | (E)");
            sb.AppendLine();

            sb.AppendLine("ПОЛИЗ:");
            sb.AppendLine(string.IsNullOrWhiteSpace(result.PolizText) ? "ПОЛИЗ пуст." : result.PolizText);
            sb.AppendLine();

            sb.AppendLine("Вычисление:");
            sb.AppendLine(result.EvaluationText);

            return sb.ToString();
        }

        private static string BuildTetrads(ExprNode node, List<TetradRow> tetrads, ref int tempCounter)
        {
            if (node is OperandNode operand)
                return operand.Text;

            if (node is BinaryExprNode binary)
            {
                string left = BuildTetrads(binary.Left, tetrads, ref tempCounter);
                string right = BuildTetrads(binary.Right, tetrads, ref tempCounter);

                string temp = "t" + tempCounter++;

                tetrads.Add(new TetradRow
                {
                    Number = tetrads.Count + 1,
                    Operation = binary.Operation,
                    Argument1 = left,
                    Argument2 = right,
                    Result = temp
                });

                return temp;
            }

            return "?";
        }

        private static void BuildPoliz(ExprNode node, List<string> poliz)
        {
            if (node is OperandNode operand)
            {
                poliz.Add(operand.Text);
                return;
            }

            if (node is BinaryExprNode binary)
            {
                BuildPoliz(binary.Left, poliz);
                BuildPoliz(binary.Right, poliz);
                poliz.Add(binary.Operation);
            }
        }

        private static bool TryEvaluate(ExprNode node, out double value, out string error)
        {
            value = 0;
            error = "";

            if (node is OperandNode operand)
            {
                if (double.TryParse(
                        operand.Text,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out double number))
                {
                    value = number;
                    return true;
                }

                error = $"не удалось преобразовать операнд \"{operand.Text}\" в число";
                return false;
            }

            if (node is BinaryExprNode binary)
            {
                if (!TryEvaluate(binary.Left, out double left, out error))
                    return false;

                if (!TryEvaluate(binary.Right, out double right, out error))
                    return false;

                switch (binary.Operation)
                {
                    case "+":
                        value = left + right;
                        return true;

                    case "-":
                        value = left - right;
                        return true;

                    case "*":
                        value = left * right;
                        return true;

                    case "/":
                        if (Math.Abs(right) < double.Epsilon)
                        {
                            error = "деление на ноль";
                            return false;
                        }

                        value = left / right;
                        return true;

                    case "%":
                        if (Math.Abs(right) < double.Epsilon)
                        {
                            error = "остаток от деления на ноль";
                            return false;
                        }

                        value = left % right;
                        return true;

                    default:
                        error = $"неизвестная операция \"{binary.Operation}\"";
                        return false;
                }
            }

            error = "неизвестный узел выражения";
            return false;
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.##########", CultureInfo.InvariantCulture);
        }

        private abstract class ExprNode
        {
            public abstract bool ContainsIdentifier { get; }
        }

        private sealed class OperandNode : ExprNode
        {
            public OperandNode(string text, bool isIdentifier)
            {
                Text = text;
                IsIdentifier = isIdentifier;
            }

            public string Text { get; }
            public bool IsIdentifier { get; }

            public override bool ContainsIdentifier => IsIdentifier;
        }

        private sealed class BinaryExprNode : ExprNode
        {
            public BinaryExprNode(string operation, ExprNode left, ExprNode right)
            {
                Operation = operation;
                Left = left;
                Right = right;
            }

            public string Operation { get; }
            public ExprNode Left { get; }
            public ExprNode Right { get; }

            public override bool ContainsIdentifier =>
                Left.ContainsIdentifier || Right.ContainsIdentifier;
        }

        private static class ExpressionLexer
        {
            public static List<ExpressionToken> Analyze(string text)
            {
                var tokens = new List<ExpressionToken>();

                int i = 0;
                int line = 1;
                int column = 1;

                while (i < text.Length)
                {
                    char ch = text[i];

                    if (ch == '\r')
                    {
                        i++;
                        continue;
                    }

                    if (ch == '\n')
                    {
                        i++;
                        line++;
                        column = 1;
                        continue;
                    }

                    if (ch == ' ' || ch == '\t')
                    {
                        i++;
                        column += ch == '\t' ? 4 : 1;
                        continue;
                    }

                    if (IsLetter(ch))
                    {
                        int start = i;
                        int startColumn = column;

                        i++;
                        column++;

                        while (i < text.Length && IsIdentifierPart(text[i]))
                        {
                            i++;
                            column++;
                        }

                        string textValue = text.Substring(start, i - start);

                        tokens.Add(MakeToken(
                            2,
                            "идентификатор",
                            textValue,
                            ExpressionTokenType.Identifier,
                            line,
                            startColumn,
                            column - 1,
                            start,
                            textValue.Length));

                        continue;
                    }

                    if (IsDigit(ch))
                    {
                        int start = i;
                        int startColumn = column;

                        i++;
                        column++;

                        while (i < text.Length && IsDigit(text[i]))
                        {
                            i++;
                            column++;
                        }

                        string textValue = text.Substring(start, i - start);

                        tokens.Add(MakeToken(
                            1,
                            "целое число",
                            textValue,
                            ExpressionTokenType.Number,
                            line,
                            startColumn,
                            column - 1,
                            start,
                            textValue.Length));

                        continue;
                    }

                    int tokenStart = i;
                    int tokenStartColumn = column;

                    switch (ch)
                    {
                        case '+':
                            tokens.Add(MakeSingleCharToken(3, "оператор сложения", "+", ExpressionTokenType.Plus, line, tokenStartColumn, tokenStart));
                            i++;
                            column++;
                            break;

                        case '-':
                            tokens.Add(MakeSingleCharToken(4, "оператор вычитания", "-", ExpressionTokenType.Minus, line, tokenStartColumn, tokenStart));
                            i++;
                            column++;
                            break;

                        case '*':
                            tokens.Add(MakeSingleCharToken(5, "оператор умножения", "*", ExpressionTokenType.Multiply, line, tokenStartColumn, tokenStart));
                            i++;
                            column++;
                            break;

                        case '/':
                            tokens.Add(MakeSingleCharToken(6, "оператор деления", "/", ExpressionTokenType.Divide, line, tokenStartColumn, tokenStart));
                            i++;
                            column++;
                            break;

                        case '%':
                            tokens.Add(MakeSingleCharToken(7, "оператор остатка от деления", "%", ExpressionTokenType.Modulo, line, tokenStartColumn, tokenStart));
                            i++;
                            column++;
                            break;

                        case '(':
                            tokens.Add(MakeSingleCharToken(8, "открывающая круглая скобка", "(", ExpressionTokenType.LeftParenthesis, line, tokenStartColumn, tokenStart));
                            i++;
                            column++;
                            break;

                        case ')':
                            tokens.Add(MakeSingleCharToken(9, "закрывающая круглая скобка", ")", ExpressionTokenType.RightParenthesis, line, tokenStartColumn, tokenStart));
                            i++;
                            column++;
                            break;

                        default:
                            int errorStart = i;
                            int errorStartColumn = column;

                            while (i < text.Length &&
                                   text[i] != '\r' &&
                                   text[i] != '\n' &&
                                   text[i] != ' ' &&
                                   text[i] != '\t' &&
                                   !IsKnownStart(text[i]))
                            {
                                i++;
                                column++;
                            }

                            if (i == errorStart)
                            {
                                i++;
                                column++;
                            }

                            string fragment = text.Substring(errorStart, i - errorStart);

                            tokens.Add(MakeToken(
                                99,
                                "ошибка: недопустимый символ или фрагмент",
                                fragment,
                                ExpressionTokenType.Error,
                                line,
                                errorStartColumn,
                                column - 1,
                                errorStart,
                                fragment.Length,
                                true));

                            break;
                    }
                }

                return tokens;
            }

            private static ExpressionToken MakeSingleCharToken(
                int code,
                string type,
                string text,
                ExpressionTokenType tokenType,
                int line,
                int column,
                int startIndex)
            {
                return MakeToken(
                    code,
                    type,
                    text,
                    tokenType,
                    line,
                    column,
                    column,
                    startIndex,
                    1);
            }

            private static ExpressionToken MakeToken(
                int code,
                string type,
                string text,
                ExpressionTokenType tokenType,
                int line,
                int columnFrom,
                int columnTo,
                int startIndex,
                int length,
                bool isError = false)
            {
                return new ExpressionToken
                {
                    Code = code,
                    Type = type,
                    Text = text,
                    TokenType = tokenType,
                    Location = $"строка {line}, {columnFrom}-{columnTo}",
                    StartIndex = startIndex,
                    Length = length,
                    IsError = isError,
                    Line = line,
                    ColumnFrom = columnFrom,
                    ColumnTo = columnTo
                };
            }

            private static bool IsKnownStart(char ch)
            {
                return IsLetter(ch) ||
                       IsDigit(ch) ||
                       ch == '+' ||
                       ch == '-' ||
                       ch == '*' ||
                       ch == '/' ||
                       ch == '%' ||
                       ch == '(' ||
                       ch == ')';
            }

            private static bool IsIdentifierPart(char ch)
            {
                return IsLetter(ch) || IsDigit(ch) || ch == '_';
            }

            private static bool IsLetter(char ch)
            {
                return (ch >= 'A' && ch <= 'Z') ||
                       (ch >= 'a' && ch <= 'z');
            }

            private static bool IsDigit(char ch)
            {
                return ch >= '0' && ch <= '9';
            }
        }

        private sealed class ExpressionParser
        {
            private readonly InternalFormResult _result;
            private readonly List<ExpressionToken> _tokens;
            private int _position;

            public ExpressionParser(InternalFormResult result)
            {
                _result = result;
                _tokens = result.Lexemes
                    .Where(t => !t.IsError)
                    .ToList();
            }

            public ExprNode? Parse()
            {
                _position = 0;

                if (_tokens.Count == 0)
                {
                    AddEndError("Ожидается арифметическое выражение.");
                    return null;
                }

                ExprNode root = ParseE();

                while (Current != null)
                {
                    if (Current.TokenType == ExpressionTokenType.RightParenthesis)
                    {
                        AddError(Current, "Лишняя закрывающая скобка \")\".");
                        Next();
                        continue;
                    }

                    AddError(Current, "Лишний фрагмент после окончания выражения.");
                    Next();
                }

                return root;
            }

            // E → T A
            // A → ε | + T A | - T A
            private ExprNode ParseE()
            {
                ExprNode left = ParseT();

                while (Current != null &&
                       (Current.TokenType == ExpressionTokenType.Plus ||
                        Current.TokenType == ExpressionTokenType.Minus))
                {
                    string operation = Current.Text;
                    Next();

                    ExprNode right = ParseT();
                    left = new BinaryExprNode(operation, left, right);
                }

                return left;
            }

            // T → F B
            // B → ε | * F B | / F B | % F B
            private ExprNode ParseT()
            {
                ExprNode left = ParseF();

                while (Current != null &&
                       (Current.TokenType == ExpressionTokenType.Multiply ||
                        Current.TokenType == ExpressionTokenType.Divide ||
                        Current.TokenType == ExpressionTokenType.Modulo))
                {
                    string operation = Current.Text;
                    Next();

                    ExprNode right = ParseF();
                    left = new BinaryExprNode(operation, left, right);
                }

                return left;
            }

            // F → num | id | (E)
            private ExprNode ParseF()
            {
                if (Current == null)
                {
                    AddEndError("Пропущен операнд в конце выражения.");
                    return new OperandNode("?", false);
                }

                if (Current.TokenType == ExpressionTokenType.Number)
                {
                    string value = Current.Text;
                    Next();
                    return new OperandNode(value, false);
                }

                if (Current.TokenType == ExpressionTokenType.Identifier)
                {
                    string name = Current.Text;
                    Next();
                    return new OperandNode(name, true);
                }

                if (Current.TokenType == ExpressionTokenType.LeftParenthesis)
                {
                    Next();

                    ExprNode inner = ParseE();

                    if (Current != null &&
                        Current.TokenType == ExpressionTokenType.RightParenthesis)
                    {
                        Next();
                    }
                    else
                    {
                        AddErrorAtCurrentOrEnd("Пропущена закрывающая скобка \")\".");
                    }

                    return inner;
                }

                if (Current.TokenType == ExpressionTokenType.RightParenthesis)
                {
                    AddError(Current, "Пропущен операнд перед закрывающей скобкой \")\".");
                    return new OperandNode("?", false);
                }

                if (IsOperator(Current))
                {
                    var badOperator = Current;
                    AddError(badOperator, $"Пропущен операнд рядом с оператором \"{badOperator.Text}\".");
                    Next();

                    if (Current != null && IsFactorStart(Current))
                        return ParseF();

                    return new OperandNode("?", false);
                }

                AddError(Current, "Ожидался операнд: число, идентификатор или выражение в скобках.");
                Next();

                return new OperandNode("?", false);
            }

            private ExpressionToken? Current =>
                _position < _tokens.Count ? _tokens[_position] : null;

            private void Next()
            {
                if (_position < _tokens.Count)
                    _position++;
            }

            private static bool IsOperator(ExpressionToken token)
            {
                return token.TokenType == ExpressionTokenType.Plus ||
                       token.TokenType == ExpressionTokenType.Minus ||
                       token.TokenType == ExpressionTokenType.Multiply ||
                       token.TokenType == ExpressionTokenType.Divide ||
                       token.TokenType == ExpressionTokenType.Modulo;
            }

            private static bool IsFactorStart(ExpressionToken token)
            {
                return token.TokenType == ExpressionTokenType.Number ||
                       token.TokenType == ExpressionTokenType.Identifier ||
                       token.TokenType == ExpressionTokenType.LeftParenthesis;
            }

            private void AddErrorAtCurrentOrEnd(string description)
            {
                if (Current != null)
                    AddError(Current, description);
                else
                    AddEndError(description);
            }

            private void AddError(ExpressionToken token, string description)
            {
                _result.SyntaxErrors.Add(new ExpressionErrorInfo
                {
                    InvalidFragment = token.Text,
                    Location = $"строка {token.Line}, позиция {token.ColumnFrom}",
                    Description = description,
                    StartIndex = token.StartIndex,
                    Length = token.Length,
                    Line = token.Line,
                    ColumnFrom = token.ColumnFrom,
                    ColumnTo = token.ColumnTo
                });
            }

            private void AddEndError(string description)
            {
                ExpressionToken? last = _tokens.LastOrDefault();

                int line = last?.Line ?? 1;
                int column = last == null ? 1 : last.ColumnTo + 1;
                int startIndex = last == null ? 0 : last.StartIndex + last.Length;

                _result.SyntaxErrors.Add(new ExpressionErrorInfo
                {
                    InvalidFragment = "<конец строки>",
                    Location = $"строка {line}, позиция {column}",
                    Description = description,
                    StartIndex = startIndex,
                    Length = 1,
                    Line = line,
                    ColumnFrom = column,
                    ColumnTo = column
                });
            }
        }
    }
}