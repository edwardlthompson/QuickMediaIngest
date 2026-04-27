#nullable enable
using System;

namespace QuickMediaIngest.Core.Services
{
    /// <summary>Toolbar filter snapshot applied to visible shoot previews.</summary>
    public sealed class ShootFilterCriteria
    {
        public DateTime? FilterStartDate { get; init; }
        public DateTime? FilterEndDate { get; init; }
        public string FilterKeyword { get; init; } = string.Empty;
        public string FilterFileType { get; init; } = string.Empty;
    }
}
