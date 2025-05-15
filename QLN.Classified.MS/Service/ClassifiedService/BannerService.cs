using Dapr.Client;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.BannerService;
using QLN.Common.Infrastructure.Model;
using System.Text.Json;

namespace QLN.Classified.MS.Service.BannerService
{
    public class BannerService : IBannerService
    {
        private readonly DaprClient _dapr;
        private const string BannerStore = "classifiedbannerstatestore";
        private const string ImageStore = "bannerimagestore";
        private const string BannerIndexKey = "banner-index";
        private const string ImageIndexKey = "banner-image-index";


        public BannerService(DaprClient dapr)
        {
            _dapr = dapr;
        }

        public async Task<Banner> CreateBanner(BannerDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var existingKeys = await _dapr.GetStateAsync<List<string>>(BannerStore, BannerIndexKey) ?? new();

                foreach (var existingKey in existingKeys)
                {
                    var existingBanner = await _dapr.GetStateAsync<Banner>(BannerStore, existingKey);
                    if (existingBanner != null && existingBanner.Display.Equals(dto.Display, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception($"A banner with the display name '{dto.Display}' already exists.");
                    }
                }

                var banner = new Banner
                {
                    Id = Guid.NewGuid(),
                    Display = dto.Display,
                    WidthDesktop = dto.WidthDesktop,
                    HeightDesktop = dto.HeightDesktop,
                    WidthMobile = dto.WidthMobile,
                    HeightMobile = dto.HeightMobile,
                    Text = dto.Text,
                    Rotation = dto.Rotation,
                    Images = new()
                };

                var key = $"banner-{banner.Id}";

                // Save banner
                await _dapr.SaveStateAsync(BannerStore, key, banner);

                // Update index
                existingKeys.Add(key);
                await _dapr.SaveStateAsync(BannerStore, BannerIndexKey, existingKeys);

                return banner;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Banner> UpdateBanner(Guid id, BannerDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var banner = await GetBanner(id);

                if (banner == null)
                {
                    throw new Exception("Banner not found");
                }

                banner.Display = dto.Display;
                banner.WidthDesktop = dto.WidthDesktop;
                banner.HeightDesktop = dto.HeightDesktop;
                banner.WidthMobile = dto.WidthMobile;
                banner.HeightMobile = dto.HeightMobile;
                banner.Text = dto.Text;
                banner.Rotation = dto.Rotation;

                await _dapr.SaveStateAsync(BannerStore, $"banner-{id}", banner);
                return banner;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Banner?> GetBanner(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var bannerKey = $"banner-{id}";
                var banner = await _dapr.GetStateAsync<Banner>(BannerStore, bannerKey);

                if (banner == null)
                    throw new Exception("No Banner Found");

                // Fetch all images and filter those linked to this banner
                var images = await GetAllImages();
                banner.Images = images.Where(img => img.BannerId == id).ToList();

                return banner;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Banner>> GetAllBanners()
        {
            try
            {                
                var bannerKeys = await _dapr.GetStateAsync<List<string>>(BannerStore, BannerIndexKey) ?? new();

                if (bannerKeys.Count == 0) return Enumerable.Empty<Banner>();

                var bannerBulk = await _dapr.GetBulkStateAsync(BannerStore, bannerKeys, parallelism: 10);
                
                var imageList = (await GetAllImages() ?? Enumerable.Empty<BannerImage>()).ToList();

                var result = new List<Banner>();

                Console.WriteLine("== BANNER == Processing each banner...");

                foreach (var entry in bannerBulk)
                {
                    if (!string.IsNullOrWhiteSpace(entry.Value))
                    {
                        var banner = JsonSerializer.Deserialize<Banner>(entry.Value, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (banner != null)
                        {
                            banner.Images = imageList
                                .Where(img => img != null && img.BannerId == banner.Id)
                                .ToList();
                            result.Add(banner);
                        }                       
                    }
                }               
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> DeleteBanner(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var bannerKey = $"banner-{id}";

                await _dapr.DeleteStateAsync(BannerStore, bannerKey);

                var bannerKeys = await _dapr.GetStateAsync<List<string>>(BannerStore, BannerIndexKey);
                if (bannerKeys?.Remove(bannerKey) == true)
                {
                    await _dapr.SaveStateAsync(BannerStore, BannerIndexKey, bannerKeys);
                }

                var imageKeys = await _dapr.GetStateAsync<List<string>>(ImageStore, ImageIndexKey) ?? new();

                var imagesToDelete = new List<string>();

                foreach (var imageKey in imageKeys)
                {
                    var image = await _dapr.GetStateAsync<BannerImage>(ImageStore, imageKey);
                    if (image?.BannerId == id)
                    {
                        await _dapr.DeleteStateAsync(ImageStore, imageKey);
                        imagesToDelete.Add(imageKey);
                    }
                }

                if (imagesToDelete.Any())
                {
                    imageKeys.RemoveAll(k => imagesToDelete.Contains(k));
                    await _dapr.SaveStateAsync(ImageStore, ImageIndexKey, imageKeys);
                }

                return true;

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<BannerImage> SaveImage(BannerImage image)
        {
            try
            {
                var key = $"image-{image.Id}";
                Console.WriteLine("Saving to key: " + key);
                await _dapr.SaveStateAsync(ImageStore, key, image);

                Console.WriteLine("Saved image to Dapr");


                // Maintain key index
                var keys = await _dapr.GetStateAsync<List<string>>(ImageStore, ImageIndexKey) ?? new();
                if (!keys.Contains(key))
                {
                    keys.Add(key);
                    await _dapr.SaveStateAsync(ImageStore, ImageIndexKey, keys);
                }
                return image;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveImage Error: " + ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine("Inner: " + ex.InnerException.Message);
                throw;
            }
        }

        public async Task<IEnumerable<BannerImage>> GetAllImages()
        {
            try
            {             
                var keys = await _dapr.GetStateAsync<List<string>>(ImageStore, ImageIndexKey) ?? new();
                if (keys.Count == 0) return Enumerable.Empty<BannerImage>();                
                var bulk = await _dapr.GetBulkStateAsync(ImageStore, keys, parallelism: 10);                
                var result = new List<BannerImage>();

                foreach (var entry in bulk)
                {
                    if (!string.IsNullOrWhiteSpace(entry.Value))
                    {
                        var image = JsonSerializer.Deserialize<BannerImage>(entry.Value, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (image != null)
                            result.Add(image);
                    }
                }                
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<BannerImage>> UploadImage(BannerImageUploadRequest form, CancellationToken cancellationToken = default)
        {
            try
            {
                var banner = await GetBanner(form.BannerId);
                if (banner == null)
                    throw new Exception("Banner not found");

                if (form.File == null || form.File.Length == 0)
                    throw new Exception("No file was uploaded.");

                using var ms = new MemoryStream();
                await form.File.CopyToAsync(ms, cancellationToken);
                var base64 = Convert.ToBase64String(ms.ToArray());

                var image = new BannerImage
                {
                    Id = Guid.NewGuid(),
                    BannerId = form.BannerId,
                    Banner = banner,
                    AnalyticsSlot = form.AnalyticsSlot,
                    Alt = form.Alt,
                    ImageDesktop = base64,
                    ImageMobile = base64,
                    Duration = form.Duration,
                    Href = form.Href,
                    WidthDesktop = form.WidthDesktop,
                    HeightDesktop = form.HeightDesktop,
                    WidthMobile = form.WidthMobile,
                    HeightMobile = form.HeightMobile,
                    IsDesktop = form.IsDesktop,
                    IsMobile = form.IsMobile,
                    SortOrder = form.SortOrder,
                    Title = form.Title
                };

                await SaveImage(image);

                return new List<BannerImage> { image };
            }
            catch(Exception ex)
            {
                throw;
            }
        }


        public async Task<BannerImage?> GetImage(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var data = await _dapr.GetStateAsync<BannerImage>(ImageStore, $"image-{id}");
                return data;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<BannerImage> UpdateImage(Guid id, BannerImageUpdateDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = $"image-{id}";
                var image = await _dapr.GetStateAsync<BannerImage>(ImageStore, key);
                if (image == null)
                    throw new Exception("Image not found");

                // Update metadata
                if (dto.Title != null) image.Title = dto.Title;
                if (dto.Alt != null) image.Alt = dto.Alt;
                if (dto.Href != null) image.Href = dto.Href;
                if (dto.Duration.HasValue) image.Duration = dto.Duration.Value;
                if (dto.IsDesktop.HasValue) image.IsDesktop = dto.IsDesktop.Value;
                if (dto.IsMobile.HasValue) image.IsMobile = dto.IsMobile.Value;
                if (dto.SortOrder.HasValue) image.SortOrder = dto.SortOrder.Value;
                if (dto.AnalyticsSlot != null) image.AnalyticsSlot = dto.AnalyticsSlot;

                if (dto.File != null)
                {
                    using var ms = new MemoryStream();
                    await dto.File.CopyToAsync(ms);
                    var base64 = Convert.ToBase64String(ms.ToArray());
                    image.ImageDesktop = base64;
                    image.ImageMobile = base64;
                }
                await _dapr.SaveStateAsync(ImageStore, key, image);
                return image;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> DeleteImage(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = $"image-{id}";

                await _dapr.DeleteStateAsync(ImageStore, key);

                // Remove from index
                var keys = await _dapr.GetStateAsync<List<string>>(ImageStore, ImageIndexKey);
                if (keys?.Remove(key) == true)
                {
                    await _dapr.SaveStateAsync(ImageStore, ImageIndexKey, keys);
                }

                return true;
            }
            catch(Exception ex)
            {
                throw;
            }
        }
    }
}
