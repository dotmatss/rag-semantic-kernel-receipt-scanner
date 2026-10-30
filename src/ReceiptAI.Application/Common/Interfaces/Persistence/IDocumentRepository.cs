using System;
using ReceiptAI.Domain.Entities;

namespace ReceiptAI.Application.Common.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(
        Guid documentId,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Document>> GetAllAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Document document,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid documentId,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}
