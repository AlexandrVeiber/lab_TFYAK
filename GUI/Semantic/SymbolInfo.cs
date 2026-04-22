namespace GUI.Semantic
{
    public sealed class SymbolInfo
    {
        public SymbolInfo(string name, SemanticValueType type, object? value = null)
        {
            Name = name;
            Type = type;
            Value = value;
        }

        public string Name { get; }
        public SemanticValueType Type { get; }
        public object? Value { get; }
    }
}