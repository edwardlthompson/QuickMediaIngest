#nullable enable
namespace QuickMediaIngest.Core
{
    public interface IIngestEngineFactory
    {
        IngestEngine Create(IFileProvider provider);
    }
}
