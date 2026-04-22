using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using GUI.Scanner;

namespace GUI.Semantic
{
    public sealed class SemanticAnalyzer
    {
        private static readonly string[] DefaultDeclaredIdentifiers =
        {
            "number",
            "counter",
            "sum",
            "step",
            "index",
            "limit",
            "total",
            "value",
            "result",
            "i",
            "j",
            "k"
        };

        private readonly string[] _declaredIdentifiers;

        private List<Lexeme> _tokens = new();
        private int _position;
        private SemanticAnalysisResult _result = new();
        private SymbolTable _symbolTable = new();

        public SemanticAnalyzer(IEnumerable<string>? declaredIdentifiers = null)
        {
            _declaredIdentifiers = declaredIdentifiers?.ToArray()
                                   ?? DefaultDeclaredIdentifiers;
        }

        public SemanticAnalysisResult Analyze(List<Lexeme> lexemes)
        {
            _result = new SemanticAnalysisResult();

            _tokens = lexemes
                .Where(t => t.Code != 23 && !t.IsError)
                .ToList();

            _position = 0;
            _symbolTable = CreateDefaultSymbolTable();

            if (_tokens.Count == 0)
            {
                _result.Message = "Ожидается строка для анализа.";
                _result.AstText = "AST не построено.";
                return _result;
            }

            var root = ParseDoWhile();
            _result.Root = root;
            _result.AstText = AstPrinter.Print(root);

            if (Current != null)
            {
                AddError(Current, "Лишний текст после конца конструкции do-while.");
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

        private bool CheckText(string text)
        {
            return Current != null && Current.Text == text;
        }

        private bool CheckIdentifier()
        {
            return Current != null &&
                   Current.Type == "идентификатор";
        }

        private bool CheckNumber()
        {
            return Current != null &&
                   Current.Type == "целое без знака";
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

        private Lexeme RequireText(string text)
        {
            if (!CheckText(text))
                throw new InvalidOperationException($"Ожидался символ или ключевое слово \"{text}\".");

            var lexeme = Current!;
            Next();
            return lexeme;
        }

        private Lexeme RequireIdentifier()
        {
            if (!CheckIdentifier())
                throw new InvalidOperationException("Ожидался идентификатор.");

            var lexeme = Current!;
            Next();
            return lexeme;
        }

        private Lexeme RequireNumber()
        {
            if (!CheckNumber())
                throw new InvalidOperationException("Ожидалось число.");

            var lexeme = Current!;
            Next();
            return lexeme;
        }

        private string RequireRelOp(out Lexeme operatorLexeme)
        {
            if (!CheckRelOp())
                throw new InvalidOperationException("Ожидалась операция сравнения.");

            operatorLexeme = Current!;
            string op = Current!.Text;
            Next();
            return op;
        }

        private string RequireLogicalOp(out Lexeme operatorLexeme)
        {
            if (!CheckLogicalOp())
                throw new InvalidOperationException("Ожидалась логическая операция.");

            operatorLexeme = Current!;
            string op = Current!.Text;
            Next();
            return op;
        }

        private SymbolTable CreateDefaultSymbolTable()
        {
            var table = new SymbolTable();

            foreach (var name in _declaredIdentifiers)
            {
                table.Declare(name, SemanticValueType.Int);
            }

            return table;
        }

        private void SetFinalMessage()
        {
            if (_result.Success)
            {
                _result.Message =
                    "Семантический анализ завершён. Ошибок нет. Общее количество найденных ошибок: 0.";
            }
            else
            {
                _result.Message =
                    $"Семантический анализ завершён. Общее количество найденных ошибок: {_result.Errors.Count}.";
            }
        }

        private void AddError(Lexeme lexeme, string description)
        {
            _result.Errors.Add(new SemanticErrorInfo
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

        private DoWhileNode ParseDoWhile()
        {
            RequireText("do");
            AstNode body = ParseBody();
            RequireText("while");
            ExpressionNode condition = ParseCondition();
            return new DoWhileNode(body, condition);
        }

        private AstNode ParseBody()
        {
            if (CheckText("{"))
                return ParseBlock();

            return ParseStatement();
        }

        private BlockNode ParseBlock()
        {
            RequireText("{");

            var statements = new List<StatementNode>();

            while (Current != null && !CheckText("}"))
            {
                statements.Add(ParseStatement());
            }

            RequireText("}");

            return new BlockNode(statements);
        }

        private StatementNode ParseStatement()
        {
            Lexeme identifierLexeme = RequireIdentifier();
            VariableNode target = BuildVariableNode(identifierLexeme);

            if (CheckText("++"))
            {
                Lexeme opLexeme = RequireText("++");
                RequireText(";");
                return CreateUpdateStatementNode(target, opLexeme.Text, opLexeme);
            }

            if (CheckText("--"))
            {
                Lexeme opLexeme = RequireText("--");
                RequireText(";");
                return CreateUpdateStatementNode(target, opLexeme.Text, opLexeme);
            }

            Lexeme assignLexeme = RequireText("=");
            ExpressionNode value = ParseExpression();
            RequireText(";");

            return CreateAssignmentStatementNode(target, value, assignLexeme);
        }

        private ExpressionNode ParseCondition()
        {
            RequireText("(");

            ExpressionNode condition = ParseRelExpression();

            while (CheckLogicalOp())
            {
                string op = RequireLogicalOp(out Lexeme logicalOpLexeme);
                ExpressionNode right = ParseRelExpression();
                condition = CreateBinaryOpNode(condition, op, right, logicalOpLexeme);
            }

            RequireText(")");
            RequireText(";");

            return condition;
        }

        private ExpressionNode ParseRelExpression()
        {
            ExpressionNode left = ParseExpression();
            string op = RequireRelOp(out Lexeme relOpLexeme);
            ExpressionNode right = ParseExpression();
            return CreateBinaryOpNode(left, op, right, relOpLexeme);
        }

        private ExpressionNode ParseExpression()
        {
            ExpressionNode left = ParseTerm();

            while (CheckText("+") || CheckText("-"))
            {
                Lexeme opLexeme = Current!;
                string op = Current!.Text;
                Next();

                ExpressionNode right = ParseTerm();
                left = CreateBinaryOpNode(left, op, right, opLexeme);
            }

            return left;
        }

        private ExpressionNode ParseTerm()
        {
            ExpressionNode left = ParseFactor();

            while (CheckText("*") || CheckText("/"))
            {
                Lexeme opLexeme = Current!;
                string op = Current!.Text;
                Next();

                ExpressionNode right = ParseFactor();
                left = CreateBinaryOpNode(left, op, right, opLexeme);
            }

            return left;
        }

        private ExpressionNode ParseFactor()
        {
            if (CheckIdentifier())
            {
                Lexeme identifierLexeme = RequireIdentifier();
                return BuildVariableNode(identifierLexeme);
            }

            if (CheckNumber())
            {
                Lexeme numberLexeme = RequireNumber();
                return BuildLiteralNode(numberLexeme);
            }

            RequireText("(");
            ExpressionNode expr = ParseExpression();
            RequireText(")");
            return expr;
        }

        private VariableNode BuildVariableNode(Lexeme identifierLexeme)
        {
            var symbol = _symbolTable.Lookup(identifierLexeme.Text);

            if (symbol == null)
            {
                AddError(
                    identifierLexeme,
                    $"Ошибка: идентификатор \"{identifierLexeme.Text}\" не объявлен ранее.");

                return new VariableNode(identifierLexeme.Text, SemanticValueType.Unknown);
            }

            return new VariableNode(identifierLexeme.Text, symbol.Type);
        }

        private LiteralNode BuildLiteralNode(Lexeme numberLexeme)
        {
            if (BigInteger.TryParse(numberLexeme.Text, out BigInteger value))
            {
                if (value > int.MaxValue)
                {
                    AddError(
                        numberLexeme,
                        $"Ошибка: число \"{numberLexeme.Text}\" выходит за пределы типа Int32.");
                }
            }

            return new LiteralNode(numberLexeme.Text, SemanticValueType.Int);
        }

        private UpdateStatementNode CreateUpdateStatementNode(
            VariableNode target,
            string operation,
            Lexeme operationLexeme)
        {
            if (target.ValueType != SemanticValueType.Unknown &&
                target.ValueType != SemanticValueType.Int)
            {
                AddError(
                    operationLexeme,
                    $"Ошибка: операция \"{operation}\" применима только к значениям типа Int.");
            }

            return new UpdateStatementNode(target, operation);
        }

        private AssignmentStatementNode CreateAssignmentStatementNode(
            VariableNode target,
            ExpressionNode value,
            Lexeme assignLexeme)
        {
            if (target.ValueType != SemanticValueType.Unknown &&
                value.ValueType != SemanticValueType.Unknown &&
                target.ValueType != value.ValueType)
            {
                AddError(
                    assignLexeme,
                    $"Ошибка: нельзя присвоить значение типа {value.ValueType.ToDisplayString()} переменной типа {target.ValueType.ToDisplayString()}.");
            }

            return new AssignmentStatementNode(target, value);
        }

        private BinaryOpNode CreateBinaryOpNode(
            ExpressionNode left,
            string operation,
            ExpressionNode right,
            Lexeme operationLexeme)
        {
            var node = new BinaryOpNode(
                operation,
                left,
                right,
                SemanticValueType.Unknown);

            if (left.ValueType == SemanticValueType.Unknown ||
                right.ValueType == SemanticValueType.Unknown)
            {
                return node;
            }

            switch (operation)
            {
                case "+":
                case "-":
                case "*":
                case "/":
                    if (left.ValueType != SemanticValueType.Int ||
                        right.ValueType != SemanticValueType.Int)
                    {
                        AddError(
                            operationLexeme,
                            $"Ошибка: оператор \"{operation}\" применим только к значениям типа Int.");
                    }
                    else
                    {
                        node.ValueType = SemanticValueType.Int;
                    }
                    break;

                case "<":
                case "<=":
                case ">":
                case ">=":
                    if (left.ValueType != SemanticValueType.Int ||
                        right.ValueType != SemanticValueType.Int)
                    {
                        AddError(
                            operationLexeme,
                            $"Ошибка: оператор \"{operation}\" требует операнды типа Int.");
                    }
                    else
                    {
                        node.ValueType = SemanticValueType.Bool;
                    }
                    break;

                case "==":
                case "!=":
                    if (left.ValueType != right.ValueType)
                    {
                        AddError(
                            operationLexeme,
                            $"Ошибка: оператор \"{operation}\" требует операнды одинакового типа.");
                    }
                    else
                    {
                        node.ValueType = SemanticValueType.Bool;
                    }
                    break;

                case "and":
                case "or":
                case "&&":
                case "||":
                    if (left.ValueType != SemanticValueType.Bool ||
                        right.ValueType != SemanticValueType.Bool)
                    {
                        AddError(
                            operationLexeme,
                            $"Ошибка: оператор \"{operation}\" требует логические операнды типа Bool.");
                    }
                    else
                    {
                        node.ValueType = SemanticValueType.Bool;
                    }
                    break;
            }

            return node;
        }
    }
}