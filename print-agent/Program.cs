using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using PrintAgent.Models;
using PrintAgent.Services;

namespace PrintAgent;

internal static class Program
{
    [STAThread]
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.UseUrls("http://127.0.0.1:17777");
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        TryAskStartupRegistration();

        app.MapGet("/health", () => Results.Ok(new
        {
            status = "ok",
            startedAtUtc = DateTime.UtcNow,
            machine = Environment.MachineName
        }));

        app.MapGet("/printers", () =>
        {
            var printers = PrinterSettings.InstalledPrinters.Cast<string>().ToList();
            return Results.Ok(new
            {
                count = printers.Count,
                defaultPrinter = new PrinterSettings().PrinterName,
                printers
            });
        });

        app.MapPost("/print/text", (TextPrintRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return Results.BadRequest("Text é obrigatório.");

            PrintText(request);
            return Results.Ok(new { success = true });
        });

        app.MapPost("/print/file", (FilePrintRequest request) =>
        {
            if (!File.Exists(request.FilePath))
                return Results.NotFound($"Arquivo não encontrado: {request.FilePath}");

            var ext = Path.GetExtension(request.FilePath).ToLowerInvariant();
            if (ext == ".txt")
            {
                var text = File.ReadAllText(request.FilePath);
                PrintText(new TextPrintRequest(text, request.PrinterName, Path.GetFileName(request.FilePath), request.Copies));
            }
            else if (ext == ".pdf")
            {
                PrintPdf(new PdfPrintRequest(request.FilePath, request.PrinterName));
            }
            else
            {
                PrintByShellVerb(request.FilePath, request.PrinterName);
            }

            if (request.DeleteAfterPrint)
                File.Delete(request.FilePath);

            return Results.Ok(new { success = true });
        });

        app.MapPost("/print/pdf", (PdfPrintRequest request) =>
        {
            if (!File.Exists(request.FilePath))
                return Results.NotFound($"Arquivo não encontrado: {request.FilePath}");

            PrintPdf(request);

            if (request.DeleteAfterPrint)
                File.Delete(request.FilePath);

            return Results.Ok(new { success = true });
        });

        using var stopCts = new CancellationTokenSource();
        var webTask = app.RunAsync(stopCts.Token);

        if (Environment.UserInteractive)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var tray = new AgentTrayContext(async () =>
            {
                await app.StopAsync();
                stopCts.Cancel();
            });

            Application.Run(tray);

            if (!stopCts.IsCancellationRequested)
            {
                await app.StopAsync();
                stopCts.Cancel();
            }
        }

        try
        {
            await webTask;
        }
        catch (OperationCanceledException)
        {
            // Encerramento esperado ao fechar pelo tray icon.
        }
    }

    private static void TryAskStartupRegistration()
    {
        if (Environment.UserInteractive is false)
            return;

        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath))
            return;

        var answer = MessageBox.Show(
            "Deseja iniciar o Maker Print Agent junto com o Windows?",
            "Maker Print Agent",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer == DialogResult.Yes)
            StartupRegistration.Enable(exePath);
        else if (answer == DialogResult.No)
            StartupRegistration.Disable();
    }

    private static void PrintText(TextPrintRequest request)
    {
        var printerSettings = new PrinterSettings();
        if (!string.IsNullOrWhiteSpace(request.PrinterName))
            printerSettings.PrinterName = request.PrinterName;

        if (!printerSettings.IsValid)
            throw new InvalidOperationException($"Impressora inválida: {request.PrinterName}");

        using var doc = new PrintDocument();
        doc.PrinterSettings = printerSettings;
        doc.DocumentName = string.IsNullOrWhiteSpace(request.DocumentName) ? "Maker Text Job" : request.DocumentName;
        doc.PrinterSettings.Copies = (short)Math.Max(1, request.Copies);

        doc.PrintPage += (_, e) =>
        {
            using var font = new Font("Consolas", 10);
            var rect = e.MarginBounds;
            e.Graphics.DrawString(request.Text, font, Brushes.Black, rect);
        };

        doc.Print();
    }

    private static void PrintPdf(PdfPrintRequest request)
    {
        var sumatra = request.SumatraPdfPath;
        if (string.IsNullOrWhiteSpace(sumatra))
            sumatra = "SumatraPDF.exe";

        try
        {
            var printerPart = string.IsNullOrWhiteSpace(request.PrinterName)
                ? string.Empty
                : $"\"{request.PrinterName}\"";

            var args = string.IsNullOrWhiteSpace(printerPart)
                ? $"-silent -print-to-default \"{request.FilePath}\""
                : $"-silent -print-to {printerPart} \"{request.FilePath}\"";

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = sumatra,
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false
            });

            process?.WaitForExit(15000);

            if (process is null || process.ExitCode != 0)
                throw new InvalidOperationException("Falha ao imprimir PDF com SumatraPDF.");
        }
        catch
        {
            PrintByShellVerb(request.FilePath, request.PrinterName);
        }
    }

    private static void PrintByShellVerb(string filePath, string? printerName)
    {
        var psi = new ProcessStartInfo
        {
            FileName = filePath,
            Verb = string.IsNullOrWhiteSpace(printerName) ? "print" : "printto",
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = true
        };

        if (!string.IsNullOrWhiteSpace(printerName))
            psi.Arguments = $"\"{printerName}\"";

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Não foi possível abrir processo de impressão para {filePath}.");

        process.WaitForExit(15000);
    }
}
