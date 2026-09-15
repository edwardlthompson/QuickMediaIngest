#nullable enable
using System;
using System.Collections.Generic;

namespace QuickMediaIngest.Core.Models
{
    /// <summary>Queued or crash-resume import snapshot (no passwords).</summary>
    public sealed class PendingImportPlan
    {
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string SourceId { get; set; } = string.Empty;

        public string SourceDisplay { get; set; } = string.Empty;

        public string DestinationRoot { get; set; } = string.Empty;

        public string NamingTemplate { get; set; } = string.Empty;

        public List<string> SelectedSourcePaths { get; set; } = new();
    }
}
