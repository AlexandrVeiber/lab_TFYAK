using System.Collections.Generic;

namespace GUI.Semantic
{
    public sealed class SymbolTable
    {
        private readonly Dictionary<string, SymbolInfo> _symbols =
            new Dictionary<string, SymbolInfo>();

        public bool Declare(string name, SemanticValueType type, object? value = null)
        {
            if (CheckDuplicate(name))
                return false;

            _symbols[name] = new SymbolInfo(name, type, value);
            return true;
        }

        public SymbolInfo? Lookup(string name)
        {
            return _symbols.TryGetValue(name, out var symbol)
                ? symbol
                : null;
        }

        public bool CheckDuplicate(string name)
        {
            return _symbols.ContainsKey(name);
        }
    }
}