using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

public class YouTubeApiService
{
    private readonly YouTubeService _youtubeService;

    public YouTubeApiService(string apiKey)
    {
        _youtubeService = new YouTubeService(new BaseClientService.Initializer
        {
            ApiKey = apiKey,
            ApplicationName = "YourAppName"
        });
    }

    /// <summary>
    /// Fetches the list of videos from a given channel ID.
    /// </summary>
    public async Task<IList<Video>> GetVideosByChannelIdAsync(string channelId, int maxResults = 10)
    {
        var channelRequest = _youtubeService.Channels.List("contentDetails");
        channelRequest.Id = channelId;
        var channelResponse = await channelRequest.ExecuteAsync();

        var uploadsPlaylistId = channelResponse.Items?.FirstOrDefault()?.ContentDetails?.RelatedPlaylists?.Uploads; 
        if (string.IsNullOrWhiteSpace(uploadsPlaylistId))
            return new List<Video>();
        var playlistItemsRequest = _youtubeService.PlaylistItems.List("snippet");
        playlistItemsRequest.PlaylistId = uploadsPlaylistId;
        playlistItemsRequest.MaxResults = maxResults;
        var playlistItemsResponse = await playlistItemsRequest.ExecuteAsync();
        var videoIds = new List<string>();
        foreach (var item in playlistItemsResponse.Items)
        {
            var videoId = item.Snippet?.ResourceId?.VideoId;
            if (!string.IsNullOrWhiteSpace(videoId))
                videoIds.Add(videoId);
        }
        var videosRequest = _youtubeService.Videos.List("snippet,statistics");
        videosRequest.Id = string.Join(",", videoIds);
        var videosResponse = await videosRequest.ExecuteAsync();

        return videosResponse.Items;
    }
    private string? ExtractVideoId(string url)
    {
        try
        {
            var uri = new Uri(url);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            if (query.TryGetValue("v", out var videoId))
            {
                return videoId;
            }
            if (uri.Host.Contains("youtu.be"))
            {
                return uri.AbsolutePath.Trim('/');
            }
        }
        catch
        {
            // Invalid URL
        }
        return null;
    }
    public async Task<Video?> GetVideoDetailsFromUrlAsync(string videoUrl)
    {
        var videoId = ExtractVideoId(videoUrl);
        if (string.IsNullOrWhiteSpace(videoId))
            return null;
        var request = _youtubeService.Videos.List("snippet,statistics");
        request.Id = videoId;
        var response = await request.ExecuteAsync();
        return response.Items.FirstOrDefault();
    }
}