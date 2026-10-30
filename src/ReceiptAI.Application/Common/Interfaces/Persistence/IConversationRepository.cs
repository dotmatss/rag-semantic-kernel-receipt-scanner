using System;
using ReceiptAI.Domain.Entities;

namespace ReceiptAI.Application.Common.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(
        Guid conversationId,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Conversation>> GetAllAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Conversation conversation,
        CancellationToken cancellationToken = default);
}
