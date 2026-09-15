#nullable enable
using System;

namespace QuickMediaIngest.Core.AppModel
{
    public readonly struct IngestBenchCopy
    {
        public IngestBenchCopy(Func<string, string> get, Func<string, object[], string> format)
        {
            Get = get;
            Format = format;
        }

        public Func<string, string> Get { get; }

        public Func<string, object[], string> Format { get; }

        public static IngestBenchCopy Neutral { get; } = new(key => key, (_, args) => string.Join(" ", args));
    }
}
