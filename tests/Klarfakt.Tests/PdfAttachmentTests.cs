using System.IO.Compression;
using System.Text;

namespace Klarfakt.Tests;

/// <summary>
/// Which embedded file a hybrid PDF's invoice actually is. The name tree is walked before the
/// annotations, so two files under one name used to be settled by whichever collector ran second —
/// a document could carry a clean factur-x.xml where the specification puts it and be judged on a
/// different one, which is the question a validator cannot afford to answer by accident.
/// </summary>
public class PdfAttachmentTests
{
    private const string Invoice = """<Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"/>""";

    private const string Other = """<Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"><!-- not the same --></Invoice>""";

    [Fact]
    public void Refuses_two_different_files_under_one_name()
    {
        var pdf = Build(
            ("factur-x.xml", Deflate(Bytes(Invoice)), true, null, false),
            ("factur-x.xml", Deflate(Bytes(Other)), true, null, false));

        var exception = Assert.Throws<UnsupportedDocumentException>(
            () => InvoiceDocument.Load(new MemoryStream(pdf)));

        Assert.Contains("two different files named 'factur-x.xml'", exception.Message);
    }

    [Fact]
    public void Accepts_the_same_file_listed_twice()
    {
        // Producers do list one attachment in both the name tree and an annotation. Identical bytes
        // are not a conflict, and refusing them would lose readable invoices.
        var pdf = Build(
            ("factur-x.xml", Deflate(Bytes(Invoice)), true, null, false),
            ("factur-x.xml", Deflate(Bytes(Invoice)), true, null, false));

        var document = InvoiceDocument.Load(new MemoryStream(pdf));

        Assert.Equal("factur-x.xml", document.EmbeddedFileName);
    }

    [Fact]
    public void Blames_the_attachment_rather_than_the_document_for_broken_flate()
    {
        var pdf = Build(("factur-x.xml", "this is not deflate data at all"u8.ToArray(), true, null, false));

        var exception = Assert.Throws<UnsupportedDocumentException>(
            () => InvoiceDocument.Load(new MemoryStream(pdf)));

        Assert.Contains("factur-x.xml", exception.Message);
        Assert.Contains("neither zlib nor raw deflate", exception.Message);
        Assert.DoesNotContain("The PDF could not be read", exception.Message);
    }

    [Fact]
    public void Reads_a_raw_deflate_attachment()
    {
        // Some producers write bare deflate where the specification says zlib — /FlateDecode is
        // declared, the zlib header is absent. The fallback that handles them has to survive the
        // rewrite of the error path above.
        var pdf = Build(("factur-x.xml", RawDeflate(Bytes(Invoice)), true, null, false));

        var document = InvoiceDocument.Load(new MemoryStream(pdf));

        Assert.Equal("factur-x.xml", document.EmbeddedFileName);
    }

    [Fact]
    public void Stops_inflating_at_the_remaining_document_budget()
    {
        // Two 4 MB attachments against a 6 MB document total. The second cannot fit in what is
        // left, and the point is that it is refused during inflation rather than after.
        var pdf = Build(
            ("factur-x.xml", Deflate(new byte[4 * 1024 * 1024]), true, null, false),
            ("extra.xml", Deflate(new byte[4 * 1024 * 1024]), true, null, false));

        var exception = Assert.Throws<UnsupportedDocumentException>(() => InvoiceDocument.Load(
            new MemoryStream(pdf),
            new DocumentLimits { MaxAttachmentBytes = 8 * 1024 * 1024, MaxTotalAttachmentBytes = 6 * 1024 * 1024 }));

        Assert.Contains("total attachment limit", exception.Message);
        Assert.Contains("MaxTotalAttachmentBytes", exception.Message);
    }

    [Fact]
    public void Bounds_a_flate_stream_that_declares_a_no_op_predictor()
    {
        // /Predictor 1 is "no prediction", so these are ordinary Flate bytes. Treating the presence
        // of /DecodeParms as exotic sent them down the unbounded path, which let the sender decide
        // whether the limit applied: refusing afterwards still pays for the memory first.
        var pdf = Build(("factur-x.xml", Deflate(new byte[64 * 1024 * 1024]), true, "<< /Predictor 1 >>", false));

        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<UnsupportedDocumentException>(() => InvoiceDocument.Load(
            new MemoryStream(pdf), new DocumentLimits { MaxAttachmentBytes = 1024 }));
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated < 16 * 1024 * 1024,
            $"refusing a 64 MB payload against a 1 KB limit allocated {allocated:N0} bytes");
    }

    [Fact]
    public void Reads_a_single_element_filter_array()
    {
        // /Filter [/FlateDecode] is valid PDF and means exactly /Filter /FlateDecode. Reading it as
        // a name threw, and the broad catch reported the whole document as unreadable.
        var pdf = Build(("factur-x.xml", Deflate(Bytes(Invoice)), true, null, true));

        var document = InvoiceDocument.Load(new MemoryStream(pdf));

        Assert.Equal("factur-x.xml", document.EmbeddedFileName);
    }

    [Fact]
    public void Refuses_a_filter_it_will_not_decode_rather_than_decoding_it_unbounded()
    {
        // A predictor Klarfakt does not implement. PDFsharp would expand the whole attachment
        // before anything could measure it, and the sender writes the dictionary.
        var pdf = Build(("factur-x.xml", Deflate(Bytes(Invoice)), true, "<< /Predictor 12 /Columns 4 >>", false));

        var exception = Assert.Throws<UnsupportedDocumentException>(
            () => InvoiceDocument.Load(new MemoryStream(pdf)));

        Assert.Contains("factur-x.xml", exception.Message);
        Assert.Contains("does not decode", exception.Message);
    }

    [Fact]
    public void Reads_an_uncompressed_attachment()
    {
        // No filter at all: nothing to expand, so the bytes are bounded by the file on disk.
        var pdf = Build(("factur-x.xml", Bytes(Invoice), false, null, false));

        var document = InvoiceDocument.Load(new MemoryStream(pdf));

        Assert.Equal("factur-x.xml", document.EmbeddedFileName);
    }

    private static byte[] Bytes(string xml) => Encoding.UTF8.GetBytes(xml);

    private static byte[] Deflate(byte[] content) => Compress(content, zlib: true);

    private static byte[] RawDeflate(byte[] content) => Compress(content, zlib: false);

    private static byte[] Compress(byte[] content, bool zlib)
    {
        using var compressed = new MemoryStream();
        using (Stream stream = zlib
                   ? new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true)
                   : new DeflateStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            stream.Write(content);
        }

        return compressed.ToArray();
    }

    /// <summary>
    /// A PDF whose /Names /EmbeddedFiles lists one file specification per attachment given.
    /// Objects 1-3 are the catalogue and page, 4 is the name tree, then a specification and a
    /// stream for each attachment. <c>Flate</c> controls only the declared /Filter, so a test can
    /// declare compression the bytes do not have.
    /// </summary>
    private static byte[] Build(params (string Name, byte[] Bytes, bool Flate, string? DecodeParms, bool FilterArray)[] attachments)
    {
        var first = 5;
        var names = string.Join(" ", attachments.Select((a, i) => $"({a.Name}) {first + i * 2} 0 R"));

        var objects = new List<byte[]?>
        {
            "<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles 4 0 R >> >>"u8.ToArray(),
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>"u8.ToArray(),
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>"u8.ToArray(),
            Encoding.ASCII.GetBytes($"<< /Names [{names}] >>"),
        };

        for (var index = 0; index < attachments.Length; index++)
        {
            var name = attachments[index].Name;
            objects.Add(Encoding.ASCII.GetBytes(
                $"<< /Type /Filespec /F ({name}) /UF ({name}) /EF << /F {first + index * 2 + 1} 0 R >> >>"));
            objects.Add(null);
        }

        var pdf = new MemoryStream();
        Append(pdf, "%PDF-1.7\n");

        var offsets = new List<long>();
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(pdf.Position);
            Append(pdf, $"{index + 1} 0 obj\n");

            if (objects[index] is { } body)
            {
                pdf.Write(body);
            }
            else
            {
                var (_, bytes, flate, parms, asArray) = attachments[(index - first) / 2];
                var name = asArray ? "[/FlateDecode]" : "/FlateDecode";
                var filter = flate ? $" /Filter {name}" : string.Empty;
                if (parms is not null) filter += $" /DecodeParms {parms}";
                Append(pdf, $"<< /Type /EmbeddedFile{filter} /Length {bytes.Length} >>\nstream\n");
                pdf.Write(bytes);
                Append(pdf, "\nendstream");
            }

            Append(pdf, "\nendobj\n");
        }

        var startXref = pdf.Position;
        Append(pdf, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            Append(pdf, $"{offset:D10} 00000 n \n");
        }

        Append(pdf, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{startXref}\n%%EOF\n");
        return pdf.ToArray();
    }

    private static void Append(MemoryStream stream, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }
}
