using System;
using ReceiptAI.Domain.Common;

namespace ReceiptAI.Domain.Entities;

public class Document : BaseEntity
{
    public string Title { get; private set; }

    public string FileName { get; private set; }

    public string ContentType { get; private set; }

    public string? FilePath { get; private set; }

    public ICollection<DocumentChunk> Chunks { get; private set; }
        = new List<DocumentChunk>();

    private Document()
    {
        Title = string.Empty;
        FileName = string.Empty;
        ContentType = string.Empty;
    }

    public Document(
        string title,
        string fileName,
        string contentType,
        string? filePath = null)
    {
        Title = title;
        FileName = fileName;
        ContentType = contentType;
        FilePath = filePath;
    }

    public void UpdateTitle(string title)
    {
        Title = title;
        SetUpdatedAt();
    }
}
