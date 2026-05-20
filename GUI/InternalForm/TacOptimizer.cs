using System;
using System.Collections.Generic;
using System.Linq;

namespace GUI.InternalForm
{
    public static class TacOptimizer
    {
        // Оптимизация 1: свёртка констант
        public static List<TacInstruction> FoldConstants(List<TacInstruction> input)
        {
            // Работаем со списком, заменяя инструкции
            var output = new List<TacInstruction>();
            foreach (var instr in input)
            {
                // Если присваивание и аргумент — число
                if (instr.Operation == "=" && int.TryParse(instr.Arg1, out int constVal))
                {
                    output.Add(new TacInstruction
                    {
                        Operation = "=",
                        Result = instr.Result,
                        Arg1 = constVal.ToString()
                    });
                    continue;
                }

                // Арифметика с двумя числами
                if (IsArithmeticOp(instr.Operation) &&
                    int.TryParse(instr.Arg1, out int a) &&
                    int.TryParse(instr.Arg2, out int b))
                {
                    int result = ComputeOperation(instr.Operation, a, b);
                    output.Add(new TacInstruction
                    {
                        Operation = "=",
                        Result = instr.Result,
                        Arg1 = result.ToString()
                    });
                    continue;
                }

                // jfalse с константой
                if (instr.Operation == "jfalse" && int.TryParse(instr.Arg1, out int cond))
                {
                    if (cond == 0)
                    {
                        // Всегда переход
                        output.Add(new TacInstruction { Operation = "jump", Label = instr.Label });
                    }
                    else
                    {
                        // Никогда не переход – удаляем инструкцию
                        // (заменяем nop, можно просто пропустить)
                        output.Add(new TacInstruction { Operation = "nop", Result = $"; folded jfalse (always true)" });
                    }
                    continue;
                }

                // Остальные инструкции без изменений
                output.Add(instr);
            }
            return output;
        }

        // Оптимизация 2: распространение копий и удаление цепочек
        public static List<TacInstruction> EliminateCopyChains(List<TacInstruction> input)
        {
            var output = new List<TacInstruction>();
            var lastDef = new Dictionary<string, string>(); // переменная -> её текущее значение (имя или константа)

            foreach (var instr in input)
            {
                // Подставляем в аргументы, если это не метка и не переход
                if (instr.Operation != "nop" && instr.Operation != "jump" && instr.Operation != "jfalse")
                {
                    if (instr.Arg1 != null && lastDef.ContainsKey(instr.Arg1))
                        instr.Arg1 = lastDef[instr.Arg1];
                    if (instr.Arg2 != null && lastDef.ContainsKey(instr.Arg2))
                        instr.Arg2 = lastDef[instr.Arg2];
                }
                if (instr.Operation == "jfalse" && lastDef.ContainsKey(instr.Arg1))
                    instr.Arg1 = lastDef[instr.Arg1];

                // Обновляем информацию о определениях
                if (instr.Operation == "=")
                {
                    // x = y  -> запоминаем, что x теперь ссылается на y
                    lastDef[instr.Result] = instr.Arg1;
                }
                else if (instr.Operation == "++" || instr.Operation == "--")
                {
                    // x = x + 1, инвалидируем старое значение
                    lastDef.Remove(instr.Result);
                }
                else if (instr.Operation != "nop" && instr.Operation != "jump" && instr.Operation != "jfalse")
                {
                    // Любая другая операция порождает новое значение, удаляем старую связь
                    lastDef[instr.Result] = instr.Result;
                }

                output.Add(instr);
            }
            return output;
        }

        private static bool IsArithmeticOp(string op) =>
            op == "+" || op == "-" || op == "*" || op == "/" || op == "%";

        private static int ComputeOperation(string op, int a, int b) => op switch
        {
            "+" => a + b,
            "-" => a - b,
            "*" => a * b,
            "/" => b != 0 ? a / b : 0,
            "%" => b != 0 ? a % b : 0,
            _ => 0
        };

        // Утилита для форматирования списка инструкций в строку
        public static string FormatInstructions(List<TacInstruction> list)
        {
            return string.Join(Environment.NewLine, list.Select(i => i.ToString()));
        }
    }
}