namespace PrintAgent.Models;

public sealed record TextPrintRequest(
    string Text,
    string? PrinterName = null,
    string? DocumentName = null,
    int Copies = 1);

public sealed record FilePrintRequest(
    string FilePath,
    string? PrinterName = null,
    int Copies = 1,
    bool DeleteAfterPrint = false);

public sealed record PdfPrintRequest(
    string FilePath,
    string? PrinterName = null,
    string? SumatraPdfPath = null,
    bool DeleteAfterPrint = false);
