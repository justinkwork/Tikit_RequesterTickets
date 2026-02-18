using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Web;

[ApiController]
[Route("api/status")]
public class StatusController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public StatusController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetStatus()
    {
        var client = _httpClientFactory.CreateClient();
        var apiBase = _configuration["ThirdPartyApi:BaseUrl"];
        var apiKey = _configuration["ThirdPartyApi:ApiKey"];

        
        var statusUrl = $"{apiBase}/Status";
        var statusRequest = new HttpRequestMessage(HttpMethod.Get, statusUrl);
        statusRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

        var statusResponse = await client.SendAsync(statusRequest);
        statusResponse.EnsureSuccessStatusCode();

        var statusJson = await statusResponse.Content.ReadAsStringAsync();
        var statusOdata = JsonSerializer.Deserialize<ODataResponse<StatusDto>>(statusJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return Ok(statusOdata?.Value ?? new List<StatusDto>());
    }
}

public class StatusDto
{
    public int Id { get; set; }
    public string ?Value { get; set; }
}