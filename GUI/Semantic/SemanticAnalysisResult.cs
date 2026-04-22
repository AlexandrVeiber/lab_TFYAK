using System.Collections.Generic;

namespace GUI.Semantic
{
    public class SemanticAnalysisResult
    {
        public bool Success => Errors.Count == 0;

        public List<SemanticErrorInfo> Errors { get; } = new();

        public AstNode? Root { get; set; }

        public string AstText { get; set; } = "";

        public string Message { get; set; } = "";
    }
}