using System.Collections.Generic;
using GUI.Semantic;   // чтобы видеть DoWhileNode, VariableNode и т.д.

namespace GUI.InternalForm
{
    // Одна инструкция трехадресного кода
    public class TacInstruction
    {
        public int Line { get; set; }
        public string Operation { get; set; } = ""; // +, -, *, /, =, <, >, ==, !=, jump, jfalse, nop
        public string Arg1 { get; set; } = "";
        public string Arg2 { get; set; } = "";
        public string Result { get; set; } = "";
        public string Label { get; set; } = "";   // метка для jump/jfalse

        public override string ToString()
        {
            if (Operation == "nop") return $"{Result}";
            if (Operation == "jump") return $"goto {Label}";
            if (Operation == "jfalse") return $"if {Arg1} == 0 goto {Label}";
            if (Operation == "=") return $"{Result} = {Arg1}";
            if (Operation == "++") return $"{Result} = {Result} + 1";
            if (Operation == "--") return $"{Result} = {Result} - 1";
            if (Operation == "unary-") return $"{Result} = -{Arg1}";
            return $"{Result} = {Arg1} {Operation} {Arg2}";
        }
    }

    public class TacGenerator
    {
        private int tempCounter;
        private int labelCounter;
        private List<TacInstruction> instructions;
        private int currentLine;

        public List<TacInstruction> GenerateFromDoWhile(DoWhileNode doWhileNode)
        {
            instructions = new List<TacInstruction>();
            tempCounter = 1;
            labelCounter = 1;
            currentLine = 1;

            string bodyLabel = NewLabel();
            string afterBody = NewLabel();
            string endLabel = NewLabel();

            // Метка начала тела (нужна для обратного перехода из условия)
            EmitLabel(bodyLabel);

            // Тело do-while
            GenerateStatementOrBlock(doWhileNode.Body);

            // Метка сразу после тела (здесь будет проверка условия)
            EmitLabel(afterBody);

            // Вычисляем условие, результат -> временная переменная
            string condResult = GenerateExpression(doWhileNode.Condition);

            // Если условие ложно (==0), выходим из цикла
            Emit(new TacInstruction { Operation = "jfalse", Arg1 = condResult, Label = endLabel });
            // Иначе возвращаемся в начало тела
            Emit(new TacInstruction { Operation = "jump", Label = bodyLabel });

            // Конец цикла
            EmitLabel(endLabel);

            // Присваиваем Line номера
            for (int i = 0; i < instructions.Count; i++)
                instructions[i].Line = i + 1;

            return instructions;
        }

        private void GenerateStatementOrBlock(AstNode node)
        {
            if (node is BlockNode block)
            {
                foreach (var stmt in block.Statements)
                    GenerateStatement(stmt);
            }
            else if (node is StatementNode stmt)
            {
                GenerateStatement(stmt);
            }
        }

        private void GenerateStatement(StatementNode stmt)
        {
            if (stmt is UpdateStatementNode update)
            {
                string varName = update.Target.Name;
                if (update.Operation == "++")
                    Emit(new TacInstruction { Operation = "++", Result = varName });
                else if (update.Operation == "--")
                    Emit(new TacInstruction { Operation = "--", Result = varName });
            }
            else if (stmt is AssignmentStatementNode assign)
            {
                string target = assign.Target.Name;
                string value = GenerateExpression(assign.Value);
                Emit(new TacInstruction { Operation = "=", Result = target, Arg1 = value });
            }
        }

        private string GenerateExpression(ExpressionNode expr)
        {
            if (expr is LiteralNode lit)
                return lit.ValueText;

            if (expr is VariableNode var)
                return var.Name;

            if (expr is BinaryOpNode bin)
            {
                string left = GenerateExpression(bin.Left);
                string right = GenerateExpression(bin.Right);
                string temp = NewTemp();
                Emit(new TacInstruction
                {
                    Operation = bin.Operation,
                    Result = temp,
                    Arg1 = left,
                    Arg2 = right
                });
                return temp;
            }

            return "???";
        }

        private string NewTemp() => $"t{tempCounter++}";
        private string NewLabel() => $"L{labelCounter++}";

        private void EmitLabel(string label)
        {
            Emit(new TacInstruction { Operation = "nop", Result = $"{label}:" });
        }

        private void Emit(TacInstruction instr)
        {
            instructions.Add(instr);
        }
    }
}