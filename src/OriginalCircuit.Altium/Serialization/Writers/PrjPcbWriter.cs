using System.Text;
using OriginalCircuit.Altium.Models.Project;

namespace OriginalCircuit.Altium.Serialization.Writers;

/// <summary>
/// Writes Altium project (<c>.PrjPcb</c>) files. The project file is plain UTF-8 INI-style text;
/// sections are emitted in <see cref="AltiumProject.Sections"/> order, each followed by a single
/// blank line, matching Altium's layout so a read/write round-trip is byte-for-byte.
/// </summary>
/// <remarks>Writers are stateless and thread-safe.</remarks>
public sealed class PrjPcbWriter
{
    private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };

    /// <summary>
    /// Writes <paramref name="project"/> to <paramref name="path"/>. When
    /// <see cref="AltiumProject.Structure"/> is set, the sibling <c>.PrjPcbStructure</c> file is
    /// written alongside it.
    /// </summary>
    public async ValueTask WriteAsync(AltiumProject project, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        await File.WriteAllBytesAsync(path, SerializeToBytes(project), cancellationToken).ConfigureAwait(false);

        if (project.Structure != null)
        {
            var structurePath = Path.ChangeExtension(path, ".PrjPcbStructure");
            // The structure file has no BOM; UTF-8 of its ASCII content is identical to the original.
            var structureBytes = Encoding.UTF8.GetBytes(project.Structure.Serialize(NewLineOf(project)));
            await File.WriteAllBytesAsync(structurePath, structureBytes, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Writes <paramref name="project"/>'s <c>.PrjPcb</c> bytes to a stream. The structure file is not written.</summary>
    public async ValueTask WriteAsync(AltiumProject project, Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(stream);
        var bytes = SerializeToBytes(project);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Writes <paramref name="project"/>'s <c>.PrjPcb</c> bytes to a stream synchronously.</summary>
    public void Write(AltiumProject project, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(stream);
        var bytes = SerializeToBytes(project);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>Serialises the project's <c>.PrjPcb</c> file to bytes (including the BOM when enabled).</summary>
    public byte[] SerializeToBytes(AltiumProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var body = Encoding.UTF8.GetBytes(SerializeText(project));
        if (!project.HasByteOrderMark)
            return body;

        var result = new byte[Utf8Bom.Length + body.Length];
        Buffer.BlockCopy(Utf8Bom, 0, result, 0, Utf8Bom.Length);
        Buffer.BlockCopy(body, 0, result, Utf8Bom.Length, body.Length);
        return result;
    }

    /// <summary>Serialises the project's <c>.PrjPcb</c> file to text (without any BOM).</summary>
    public string SerializeText(AltiumProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var newLine = NewLineOf(project);
        var sb = new StringBuilder();

        foreach (var section in project.Sections)
        {
            sb.Append('[').Append(section.Name).Append(']').Append(newLine);
            foreach (var (key, value) in section.Entries)
                sb.Append(key).Append('=').Append(value).Append(newLine);
            sb.Append(newLine); // blank separator line after every section, including the last
        }

        return sb.ToString();
    }

    private static string NewLineOf(AltiumProject project) =>
        string.IsNullOrEmpty(project.NewLine) ? "\r\n" : project.NewLine;
}
