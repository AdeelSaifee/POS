using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Data.Seeding;

public interface ILocalCatalogSeeder
{
    Task SeedAsync(int tenantId, CancellationToken cancellationToken = default);
}
