using System.Collections.Generic;
using System.Linq;

namespace GUI.Semantic
{
    public abstract class StatementNode : AstNode
    {
    }

    public abstract class ExpressionNode : AstNode
    {
        protected ExpressionNode(SemanticValueType valueType)
        {
            ValueType = valueType;
        }

        public SemanticValueType ValueType { get; set; }
    }

    public sealed class DoWhileNode : AstNode
    {
        public DoWhileNode(AstNode body, ExpressionNode condition)
        {
            Body = body;
            Condition = condition;
        }

        public AstNode Body { get; }
        public ExpressionNode Condition { get; }

        public override string NodeType => "DoWhileNode";

        public override IReadOnlyList<AstChild> GetChildren()
        {
            return new List<AstChild>
            {
                new AstChild("body", Body),
                new AstChild("condition", Condition)
            };
        }
    }

    public sealed class BlockNode : AstNode
    {
        public BlockNode(IReadOnlyList<StatementNode> statements)
        {
            Statements = statements;
        }

        public IReadOnlyList<StatementNode> Statements { get; }

        public override string NodeType => "BlockNode";

        public override IReadOnlyList<AstProperty> GetProperties()
        {
            return new List<AstProperty>
            {
                new AstProperty("statementCount", Statements.Count.ToString())
            };
        }

        public override IReadOnlyList<AstChild> GetChildren()
        {
            return Statements
                .Select((statement, index) => new AstChild($"statement[{index}]", statement))
                .ToList();
        }
    }

    public sealed class UpdateStatementNode : StatementNode
    {
        public UpdateStatementNode(VariableNode target, string operation)
        {
            Target = target;
            Operation = operation;
        }

        public VariableNode Target { get; }
        public string Operation { get; }

        public override string NodeType => "UpdateStatementNode";

        public override IReadOnlyList<AstProperty> GetProperties()
        {
            return new List<AstProperty>
            {
                new AstProperty("operation", $"\"{Operation}\"")
            };
        }

        public override IReadOnlyList<AstChild> GetChildren()
        {
            return new List<AstChild>
            {
                new AstChild("target", Target)
            };
        }
    }

    public sealed class AssignmentStatementNode : StatementNode
    {
        public AssignmentStatementNode(VariableNode target, ExpressionNode value)
        {
            Target = target;
            Value = value;
        }

        public VariableNode Target { get; }
        public ExpressionNode Value { get; }

        public override string NodeType => "AssignmentStatementNode";

        public override IReadOnlyList<AstChild> GetChildren()
        {
            return new List<AstChild>
            {
                new AstChild("target", Target),
                new AstChild("value", Value)
            };
        }
    }

    public sealed class VariableNode : ExpressionNode
    {
        public VariableNode(string name, SemanticValueType valueType)
            : base(valueType)
        {
            Name = name;
        }

        public string Name { get; }

        public override string NodeType => "VariableNode";

        public override IReadOnlyList<AstProperty> GetProperties()
        {
            return new List<AstProperty>
            {
                new AstProperty("name", $"\"{Name}\""),
                new AstProperty("type", ValueType.ToDisplayString())
            };
        }
    }

    public sealed class LiteralNode : ExpressionNode
    {
        public LiteralNode(string valueText, SemanticValueType valueType)
            : base(valueType)
        {
            ValueText = valueText;
        }

        public string ValueText { get; }

        public override string NodeType => "LiteralNode";

        public override IReadOnlyList<AstProperty> GetProperties()
        {
            return new List<AstProperty>
            {
                new AstProperty("value", ValueText),
                new AstProperty("type", ValueType.ToDisplayString())
            };
        }
    }

    public sealed class BinaryOpNode : ExpressionNode
    {
        public BinaryOpNode(string operation, ExpressionNode left, ExpressionNode right, SemanticValueType valueType)
            : base(valueType)
        {
            Operation = operation;
            Left = left;
            Right = right;
        }

        public string Operation { get; }
        public ExpressionNode Left { get; }
        public ExpressionNode Right { get; }

        public override string NodeType => "BinaryOpNode";

        public override IReadOnlyList<AstProperty> GetProperties()
        {
            return new List<AstProperty>
            {
                new AstProperty("operation", $"\"{Operation}\""),
                new AstProperty("type", ValueType.ToDisplayString())
            };
        }

        public override IReadOnlyList<AstChild> GetChildren()
        {
            return new List<AstChild>
            {
                new AstChild("left", Left),
                new AstChild("right", Right)
            };
        }
    }
}