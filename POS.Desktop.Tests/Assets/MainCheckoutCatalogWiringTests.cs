using System;
using System.IO;
using Xunit;

namespace POS.Desktop.Tests.Assets
{
    public sealed class MainCheckoutCatalogWiringTests
    {
        [Fact]
        public void MainCheckout_Html_MatchesCatalogWiringExpectations()
        {
            var baseDir = AppContext.BaseDirectory;
            var solutionDir = FindSolutionDirectory(baseDir);
            var htmlPath = Path.Combine(solutionDir, "POS.Desktop", "Assets", "ui", "main_checkout.html");

            Assert.True(File.Exists(htmlPath), $"main_checkout.html not found at: {htmlPath}");

            var htmlContent = File.ReadAllText(htmlPath);

            // Assert removal of legacy items/categories static arrays
            Assert.DoesNotContain("const ITEMS", htmlContent);
            Assert.DoesNotContain("const CATEGORIES", htmlContent);

            // Assert presence of bridge message invocations
            Assert.Contains("catalog.listCategories", htmlContent);
            Assert.Contains("catalog.listItems", htmlContent);
            Assert.Contains("catalog.searchItems", htmlContent);
            Assert.Contains("catalog.lookupByIdentifier", htmlContent);

            // Assert presence of escapeHtml helper and usages
            Assert.Contains("function escapeHtml", htmlContent);
            Assert.Contains("escapeHtml(", htmlContent);

            // Assert absence of old default demo cart ids
            Assert.DoesNotContain("I1001", htmlContent);
            Assert.DoesNotContain("I1002", htmlContent);
            Assert.DoesNotContain("I1008", htmlContent);

            // Assert addToCart(item) or equivalent CatalogItemDto add signature
            Assert.Contains("function addToCart(item)", htmlContent);
        }

        private static string FindSolutionDirectory(string startDir)
        {
            var current = new DirectoryInfo(startDir);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "POS.slnx")))
                {
                    return current.FullName;
                }
                current = current.Parent;
            }
            throw new DirectoryNotFoundException("Could not find solution directory starting from: " + startDir);
        }
    }
}
