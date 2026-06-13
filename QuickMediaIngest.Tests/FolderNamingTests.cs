using System;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class FolderNamingTests
    {
        [Fact]
        public void GroupFolderNaming_UsesTimestampAndSanitizedTitle()
        {
            var group = new ItemGroup
            {
                Title = "Beach Shoot",
                StartDate = new DateTime(2024, 6, 15, 14, 30, 45),
                EndDate = new DateTime(2024, 6, 15, 18, 0, 0),
            };

            string folderName = GroupFolderNaming.GetTargetFolderName(group);

            Assert.Equal("20240615_143045_Beach Shoot", folderName);
        }

        [Fact]
        public void GroupBuilder_And_GroupFolderNaming_Agree()
        {
            var builder = new GroupBuilder();
            var group = new ItemGroup
            {
                Title = "Test/Folder:Name",
                StartDate = new DateTime(2025, 1, 2, 8, 9, 10),
                EndDate = new DateTime(2025, 1, 2, 9, 0, 0),
            };

            Assert.Equal(GroupFolderNaming.GetTargetFolderName(group), builder.GetTargetFolderName(group));
        }

        [Fact]
        public void GroupFolderNaming_FallsBackWhenTitleEmpty()
        {
            var group = new ItemGroup
            {
                Title = "   ",
                StartDate = new DateTime(2025, 3, 1, 12, 0, 0),
                EndDate = new DateTime(2025, 3, 1, 13, 0, 0),
            };

            Assert.Equal("20250301_120000_Group", GroupFolderNaming.GetTargetFolderName(group));
        }
    }
}
