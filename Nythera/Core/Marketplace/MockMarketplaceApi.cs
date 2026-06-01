using System.Collections.Generic;
using System.Threading.Tasks;
using Nythera.Core.Marketplace.Models;

namespace Nythera.Core.Marketplace;

public class MockMarketplaceApi : IMarketplaceApi
{
    public async Task<List<MarketplaceItem>> GetFeaturedWallpapersAsync()
    {
        await Task.Delay(1000); // Simulate network latency
        return new List<MarketplaceItem>
        {
            new MarketplaceItem
            {
                Id = "1",
                Title = "Cyberpunk Cityscape",
                Author = "Nythera Team",
                ThumbnailUrl = "https://via.placeholder.com/300x169/8A2BE2/FFFFFF?text=Cyberpunk+City",
                VideoUrl = "https://test-videos.co.uk/vids/bigbuckbunny/mp4/h264/720/Big_Buck_Bunny_720_10s_1MB.mp4",
                Description = "A futuristic cyberpunk city with flying cars.",
                SizeBytes = 1048576,
                Downloads = 15420
            },
            new MarketplaceItem
            {
                Id = "2",
                Title = "Relaxing Rain",
                Author = "NatureLover",
                ThumbnailUrl = "https://via.placeholder.com/300x169/4682B4/FFFFFF?text=Relaxing+Rain",
                VideoUrl = "https://test-videos.co.uk/vids/bigbuckbunny/mp4/h264/720/Big_Buck_Bunny_720_10s_1MB.mp4",
                Description = "High quality rain dropping on a window pane.",
                SizeBytes = 2048576,
                Downloads = 8400
            },
            new MarketplaceItem
            {
                Id = "3",
                Title = "Anime Sakura",
                Author = "WeebLord",
                ThumbnailUrl = "https://via.placeholder.com/300x169/FF69B4/FFFFFF?text=Anime+Sakura",
                VideoUrl = "https://test-videos.co.uk/vids/bigbuckbunny/mp4/h264/720/Big_Buck_Bunny_720_10s_1MB.mp4",
                Description = "Beautiful sakura trees falling in 4K 60FPS.",
                SizeBytes = 5048576,
                Downloads = 35200
            }
        };
    }

    public async Task<List<MarketplaceItem>> SearchWallpapersAsync(string query)
    {
        return await GetFeaturedWallpapersAsync();
    }
}
