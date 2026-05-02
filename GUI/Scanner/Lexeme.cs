namespace GUI.Scanner
{
    public class Lexeme
    {
        public int Code { get; set; }
        public string Type { get; set; } = "";
        public string Text { get; set; } = "";
        public string Location { get; set; } = "";

        // Для навигации по ошибкам (и вообще по токенам)
        public int StartIndex { get; set; }     // индекс в общей строке EditorTextBox.Text
        public int Length { get; set; }         // длина лексемы
        public bool IsError { get; set; }
        public int Line { get; set; }
        public int ColumnFrom { get; set; }
        public int ColumnTo { get; set; }   
    }
}