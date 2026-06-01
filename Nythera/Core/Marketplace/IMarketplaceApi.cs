using System.Collections.Generic;
using System.Threading.Tasks;
using Nythera.Core.Marketplace.Models;

namespace Nythera.Core.Marketplace;

public interface IMarketplaceApi
{
    Task<List<MarketplaceItem>> GetFeaturedWallpapersAsync();
    Task<List<MarketplaceItem>> SearchWallpapersAsync(string query);
}
