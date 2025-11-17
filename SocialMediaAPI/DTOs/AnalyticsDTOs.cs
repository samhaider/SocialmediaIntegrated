namespace SocialMediaAPI.DTOs;

public class AnalyticsResponse
{
    public string Platform { get; set; } = string.Empty;
    public int Likes { get; set; }
    public int Comments { get; set; }
    public int Shares { get; set; }
    public int Views { get; set; }
    public int Clicks { get; set; }
    public DateTime FetchedAt { get; set; }
}

public class AggregatedAnalyticsResponse
{
    public int TotalLikes { get; set; }
    public int TotalComments { get; set; }
    public int TotalShares { get; set; }
    public int TotalViews { get; set; }
    public int TotalClicks { get; set; }
    public List<AnalyticsResponse> ByPlatform { get; set; } = new();
}
