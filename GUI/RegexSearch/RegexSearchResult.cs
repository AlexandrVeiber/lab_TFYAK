namespace GUI.RegexSearch
{
    public class RegexSearchResult
    {
        public string MatchedText { get; set; } = "";
        public string StartPosition { get; set; } = "";
        public int Length { get; set; }

        public int StartIndex { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}