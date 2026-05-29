using PythonWiki.Dtos;

namespace PythonWiki.Services.Interfaces;

public interface IArticleGraphService
{
    Task<string> ExportDotAsync();
    Task<string> ExportGraphMLAsync();
    Task<byte[]> ExportPngAsync();
}
