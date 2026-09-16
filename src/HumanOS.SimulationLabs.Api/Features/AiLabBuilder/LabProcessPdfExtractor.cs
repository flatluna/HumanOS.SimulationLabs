using System.Text;
using UglyToad.PdfPig;

namespace HumanOS.SimulationLabs.Api.Features.AiLabBuilder;

/// <summary>
/// Extracts plain text from a source-process PDF uploaded to the AI Lab
/// Builder (see GenerateLabDraftRequest.ProcessFileName/ProcessContentBase64),
/// mirroring the HumanOS.Storage.PdfTextExtractor pattern used by the main
/// HumanOS backend. Uses UglyToad.PdfPig — a pure-.NET PDF parser — kept
/// local to this project rather than referencing the main backend project.
/// </summary>
public static class LabProcessPdfExtractor
{
    public static string ExtractText(byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        using var document = PdfDocument.Open(stream);
        var builder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return builder.ToString();
    }
}
