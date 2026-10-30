using System;
using ReceiptAI.Domain.Common;

namespace ReceiptAI.Domain.Entities;

public class DocumentChunk : BaseEntity
{
    public Guid DocumentId { get; private set; }

    public string Content { get; private set; }

    public int ChunkIndex { get; private set; }

    private DocumentChunk()
    {
        Content = string.Empty;
    }

    public DocumentChunk(
        Guid documentId,
        string content,
        int chunkIndex)
    {
        DocumentId = documentId;
        Content = content;
        ChunkIndex = chunkIndex;
    }
}
