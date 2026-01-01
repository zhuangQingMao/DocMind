namespace DocMind
{
    public class HighlightMessage(List<string> spans)
    {
        public List<string> Spans { get; set; } = spans;
    }
}
