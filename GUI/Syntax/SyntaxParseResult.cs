using System.Collections.Generic;

namespace GUI.Syntax
{
    public class SyntaxParseResult
    {
        public bool Success => Errors.Count == 0;

        public List<SyntaxErrorInfo> Errors { get; } = new();

        public string Message { get; set; } = "";
    }
}