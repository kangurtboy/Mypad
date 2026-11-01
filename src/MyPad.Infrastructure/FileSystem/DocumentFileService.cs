using System.Security.Cryptography;
using System.Text;
using MyPad.Core.Enums;
using MyPad.Core.Interfaces;
using MyPad.Core.Models;

namespace MyPad.Infrastructure.FileSystem;

public class DocumentFileService : IDocumentFileService
{
    private readonly IEncodingDetector _encodingDetector;
    private readonly ILineEndingDetector _lineEndingDetector;

    public DocumentFileService(IEncodingDetector encodingDetector, ILineEndingDetector lineEndingDetector)
    {
        _encodingDetector = encodingDetector ?? throw new ArgumentNullException(nameof(encodingDetector));
        _lineEndingDetector = lineEndingDetector ?? throw new ArgumentNullException(nameof(lineEndingDetector));
    }

    public async Task<TextDocument> OpenAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found.", filePath);
        }

        byte[] bytes = await File.ReadAllBytesAsync(filePath, ct);
        var detectedEncoding = _encodingDetector.Detect(bytes, bytes.Length);

        string content = detectedEncoding.Encoding.GetString(bytes);
        if (detectedEncoding.HasBom && content.Length > 0 && content[0] == '\uFEFF')
        {
            content = content[1..];
        }

        var detectedLineEnding = _lineEndingDetector.Detect(content);
        var fileInfo = new FileInfo(filePath);
        string fingerprint = ComputeFingerprint(fileInfo, bytes);

        var kind = DetermineDocumentKind(filePath);

        return new TextDocument
        {
            Id = Guid.NewGuid(),
            FilePath = Path.GetFullPath(filePath),
            Title = Path.GetFileName(filePath),
            Content = content,
            Encoding = detectedEncoding.Encoding,
            HasBom = detectedEncoding.HasBom,
            LineEnding = detectedLineEnding,
            IsModified = false,
            Kind = kind,
            FileFingerprint = fingerprint,
            LastSavedUtc = fileInfo.LastWriteTimeUtc
        };
    }

    public async Task SaveAsync(TextDocument document, string? destinationPath = null, CancellationToken ct = default)
    {
        string targetPath = destinationPath ?? document.FilePath
            ?? throw new InvalidOperationException("No target path specified for document save.");

        targetPath = Path.GetFullPath(targetPath);
        string? targetDir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // Normalize line endings according to document's configured line ending mode
        string contentToWrite = _lineEndingDetector.NormalizeLineEndings(document.Content, document.LineEnding);

        // Prepare bytes with or without BOM
        byte[] contentBytes = document.Encoding.GetBytes(contentToWrite);
        byte[] finalBytes;

        if (document.HasBom)
        {
            byte[] bom = document.Encoding.GetPreamble();
            if (bom.Length > 0)
            {
                finalBytes = new byte[bom.Length + contentBytes.Length];
                Buffer.BlockCopy(bom, 0, finalBytes, 0, bom.Length);
                Buffer.BlockCopy(contentBytes, 0, finalBytes, bom.Length, contentBytes.Length);
            }
            else
            {
                finalBytes = contentBytes;
            }
        }
        else
        {
            finalBytes = contentBytes;
        }

        // Atomic write pattern: write to .tmp in same folder, flush, then replace
        string tempFilePath = Path.Combine(targetDir ?? ".", $"{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await fileStream.WriteAsync(finalBytes, ct);
                await fileStream.FlushAsync(ct);
            }

            if (File.Exists(targetPath))
            {
                File.Replace(tempFilePath, targetPath, null);
            }
            else
            {
                File.Move(tempFilePath, targetPath);
            }
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                try { File.Delete(tempFilePath); } catch { }
            }
        }

        var newFileInfo = new FileInfo(targetPath);
        document.FilePath = targetPath;
        document.Title = Path.GetFileName(targetPath);
        document.FileFingerprint = ComputeFingerprint(newFileInfo, finalBytes);
        document.LastSavedUtc = newFileInfo.LastWriteTimeUtc;
        document.IsModified = false;
        document.Kind = DetermineDocumentKind(targetPath);
    }

    public bool CheckForExternalChanges(TextDocument document)
    {
        if (string.IsNullOrEmpty(document.FilePath) || !File.Exists(document.FilePath))
        {
            return false;
        }

        if (string.IsNullOrEmpty(document.FileFingerprint))
        {
            return false;
        }

        var fileInfo = new FileInfo(document.FilePath);
        string currentHeader = $"{fileInfo.LastWriteTimeUtc.Ticks}_{fileInfo.Length}";

        return !document.FileFingerprint.StartsWith(currentHeader);
    }

    private static string ComputeFingerprint(FileInfo fileInfo, byte[] contentBytes)
    {
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(contentBytes);
        return $"{fileInfo.LastWriteTimeUtc.Ticks}_{fileInfo.Length}_{Convert.ToHexString(hash)}";
    }

    private static DocumentKind DetermineDocumentKind(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".md" or ".markdown" or ".mdown" or ".mkd" => DocumentKind.Markdown,
            _ => DocumentKind.PlainText
        };
    }
}
