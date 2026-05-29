namespace PythonWiki.Dtos
{
    public class ArticleNodeDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public bool IsUnlocked { get; set; }
        public int XPReward { get; set; }
    }

    public class ArticleEdgeDto
    {
        public int From { get; set; }
        public int To { get; set; }
    }

    public class ArticleGraphDto
    {
        public List<ArticleNodeDto> Nodes { get; set; } = new();
        public List<ArticleEdgeDto> Edges { get; set; } = new();
    }

}
