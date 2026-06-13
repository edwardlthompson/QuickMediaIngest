#nullable enable
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    public sealed class IngestEngineFactory : IIngestEngineFactory
    {
        private readonly ILogger<IngestEngine> _logger;

        public IngestEngineFactory(ILogger<IngestEngine> logger)
        {
            _logger = logger;
        }

        public IngestEngine Create(IFileProvider provider) => new IngestEngine(provider, _logger);
    }
}
