using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace POS.Desktop.Tests.Services.Sync;

/// <summary>
/// Static analysis tests verifying async safety and blocking task prevention
/// within the POS.Desktop Sync service codebase.
/// </summary>
public sealed class SyncStaticAnalysisTests
{
    private static string FindRepositoryRoot()
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null)
        {
            if (File.Exists(Path.Combine(currentDir.FullName, "POS.slnx")))
            {
                return currentDir.FullName;
            }
            currentDir = currentDir.Parent;
        }
        throw new DirectoryNotFoundException("Could not find the repository root directory containing POS.slnx.");
    }

    [Fact]
    public void SyncServices_DoNotContainSynchronousBlockingCalls()
    {
        // Arrange
        var root = FindRepositoryRoot();
        var syncServicesDir = Path.Combine(root, "POS.Desktop", "Services", "Sync");
        Assert.True(Directory.Exists(syncServicesDir), $"Sync services directory does not exist: {syncServicesDir}");

        var csFiles = Directory.GetFiles(syncServicesDir, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(csFiles);

        var forbiddenPatterns = new[]
        {
            ".Result",
            ".Wait(",
            "GetAwaiter().GetResult()"
        };

        var violations = new List<string>();

        // Act
        foreach (var file in csFiles)
        {
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Exclude comments to avoid false positives in documentation/XML summaries
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("///") || trimmed.StartsWith("/*") || trimmed.StartsWith("*"))
                {
                    continue;
                }

                foreach (var pattern in forbiddenPatterns)
                {
                    if (line.Contains(pattern, StringComparison.Ordinal))
                    {
                        var relativePath = Path.GetRelativePath(root, file);
                        violations.Add($"{relativePath}(L{i + 1}): Found forbidden blocking call pattern '{pattern}' in line: '{line.Trim()}'");
                    }
                }
            }
        }

        // Assert
        if (violations.Count > 0)
        {
            var errorDetails = string.Join(Environment.NewLine, violations);
            Assert.Fail($"Async-safety check failed with the following violations:{Environment.NewLine}{errorDetails}");
        }
    }
}
