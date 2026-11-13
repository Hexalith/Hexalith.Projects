using System.Text.Json;

namespace Story65.ImageMemo.Harness;

/// <summary>Writes complete raw batch records only after all readers in that batch have joined.</summary>
internal sealed class RawSampleWriter : IDisposable
{
    private readonly StreamWriter _writer;

    internal RawSampleWriter(string path)
    {
        _writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read));
    }

    internal void Write(BatchMeasurement sample)
    {
        _writer.WriteLine(JsonSerializer.Serialize(sample));
        _writer.Flush();
    }

    public void Dispose() => _writer.Dispose();
}
