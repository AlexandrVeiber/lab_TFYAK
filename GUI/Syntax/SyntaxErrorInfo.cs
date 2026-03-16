namespace GUI.Syntax
{
    public class SyntaxErrorInfo
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
}