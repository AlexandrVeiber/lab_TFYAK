namespace GUI.Semantic
{
    public enum SemanticValueType
    {
        Unknown,
        Int,
        Bool
    }

    internal static class SemanticValueTypeExtensions
    {
        public static string ToDisplayString(this SemanticValueType type)
        {
            return type switch
            {
                SemanticValueType.Int => "Int",
                SemanticValueType.Bool => "Bool",
                _ => "Unknown"
            };
        }
    }
}