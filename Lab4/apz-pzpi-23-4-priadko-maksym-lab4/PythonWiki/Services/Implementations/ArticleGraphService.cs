using Microsoft.EntityFrameworkCore;
using QuikGraph;
using QuikGraph.Graphviz;
using QuikGraph.Graphviz.Dot;
using QuikGraph.Serialization;
using PythonWiki.Persistence.DbContext;
using PythonWiki.Services.Interfaces;
using System.IO;
using System.Xml;

public class ArticleGraphService : IArticleGraphService
{
    private readonly PythonWikiDbContext _db;

    public ArticleGraphService(PythonWikiDbContext db)
    {
        _db = db;
    }

    public async Task<string> ExportDotAsync()
    {
        var graph = await BuildGraphAsync();

        var graphviz = new GraphvizAlgorithm<int, Edge<int>>(graph);
        graphviz.FormatVertex += (sender, args) =>
        {
            var article = _db.Articles.Find(args.Vertex);
            args.VertexFormat.Label = article?.Title ?? args.Vertex.ToString();
        };

        return graphviz.Generate();
    }

    private async Task<AdjacencyGraph<int, Edge<int>>> BuildGraphAsync()
    {
        var graph = new AdjacencyGraph<int, Edge<int>>(true);

        var articles = await _db.Articles.ToListAsync();
        var links = await _db.ArticleLinks.ToListAsync();

        foreach (var article in articles)
            graph.AddVertex(article.Id);

        foreach (var link in links)
            graph.AddEdge(new Edge<int>(link.FromArticleId, link.ToArticleId));

        return graph;
    }

    public async Task<string> ExportGraphMLAsync()
    {
        var graph = await BuildGraphAsync();

        using var sw = new StringWriter();
        using var xmlWriter = XmlWriter.Create(sw, new XmlWriterSettings { Indent = true });

        var serializer = new GraphMLSerializer<int, Edge<int>, AdjacencyGraph<int, Edge<int>>>();
        serializer.Serialize(
            xmlWriter,
            graph,
            vertex => vertex.ToString(),
            edge => null
        );

        xmlWriter.Flush();

        return sw.ToString();
    }
    public async Task<byte[]> ExportPngAsync()
    {
        var dot = await ExportDotAsync();
        var tempDot = Path.GetTempFileName();
        var tempPng = Path.ChangeExtension(tempDot, ".png");

        await File.WriteAllTextAsync(tempDot, dot);

        var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = "dot";
        process.StartInfo.Arguments = $"-Tpng \"{tempDot}\" -o \"{tempPng}\"";
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;

        process.Start();
        process.WaitForExit();

        var data = await File.ReadAllBytesAsync(tempPng);

        File.Delete(tempDot);
        File.Delete(tempPng);

        return data;
    }

}
