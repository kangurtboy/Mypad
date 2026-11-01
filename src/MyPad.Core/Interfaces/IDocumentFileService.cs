using MyPad.Core.Models;

namespace MyPad.Core.Interfaces;

public interface IDocumentFileService
{
    Task<TextDocument> OpenAsync(string filePath, CancellationToken ct = default);

    Task SaveAsync(TextDocument document, string? destinationPath = null, CancellationToken ct = default);

    bool CheckForExternalChanges(TextDocument document);
}
