using System;
using ReceiptAI.Domain.Common;

namespace ReceiptAI.Domain.Entities;

public class Conversation : BaseEntity
{
    public Guid OwnerId { get; private set; }

    public string Title { get; private set; }

    private Conversation()
    {
        Title = string.Empty;
    }

    public Conversation(Guid ownerId, string title)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Owner ID is required.");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.");

        OwnerId = ownerId;
        Title = title;
    }

    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.");

        Title = title;
        SetUpdatedAt();
    }

    public static Conversation Reconstitute(
        Guid id,
        Guid ownerId,
        string title,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        return new Conversation
        {
            Id = id,
            OwnerId = ownerId,
            Title = title,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}
