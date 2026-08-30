#nullable enable
using System;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class DestinationFolderTemplateTests
    {
        [Fact]
        public void GetTargetFolderName_WithTemplateTokens_ResolvesCustomTokens()
        {
            var group = new ItemGroup
            {
                Title = "Wedding Reception",
                StartDate = new DateTime(2026, 8, 30, 14, 0, 0)
            };

            string folder = GroupFolderNaming.GetTargetFolderName(
                group,
                template: "[Date]_[Client]_[Job]_[Camera]_[ShootTitle]",
                job: "Event402",
                client: "Smith",
                camera: "A7IV");

            Assert.Equal("2026-08-30_Smith_Event402_A7IV_Wedding Reception", folder);
        }
    }
}
