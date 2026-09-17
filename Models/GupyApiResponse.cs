using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WorkIT.App.Models;

public class GupyApiResponse
{
    [JsonPropertyName("data")]
    public List<GupyJobItem> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public GupyPagination? Pagination { get; set; }
}

public class GupyJobItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("careerPageName")]
    public string? CareerPageName { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("publishedDate")]
    public string? PublishedDate { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("jobUrl")]
    public string? JobUrl { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class GupyPagination
{
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
