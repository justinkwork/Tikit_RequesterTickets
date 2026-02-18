using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Web;

[ApiController]
[Route("api/requesters/{username}/tickets")]
public class RequesterTicketsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public RequesterTicketsController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetRequesterTickets(string username)
    {
        var client = _httpClientFactory.CreateClient();
        var apiBase = _configuration["ThirdPartyApi:BaseUrl"];
        var apiKey = _configuration["ThirdPartyApi:ApiKey"];

        // 1) Look up user by username
        var userUrl = $"{apiBase}/PlatformUser?$filter=AadObjectId eq '{HttpUtility.UrlEncode(username)}'";
        var userRequest = new HttpRequestMessage(HttpMethod.Get, userUrl);
        userRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

        var userResponse = await client.SendAsync(userRequest);
        userResponse.EnsureSuccessStatusCode();

        var userJson = await userResponse.Content.ReadAsStringAsync();
        var userOdata = JsonSerializer.Deserialize<ODataResponse<UserDto>>(userJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var user = userOdata?.Value?.FirstOrDefault();
        if (user == null)
            return NotFound($"No user found for username '{username}'");

        // 2) Query tickets filtered by requesterId
        var ticketsUrl = $"{apiBase}/Ticket?$filter=RequesterId eq {user.Id} and Closed eq false";
        var ticketRequest = new HttpRequestMessage(HttpMethod.Get, ticketsUrl);
        ticketRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

        var ticketResponse = await client.SendAsync(ticketRequest);
        ticketResponse.EnsureSuccessStatusCode();

        var ticketJson = await ticketResponse.Content.ReadAsStringAsync();
        var ticketOdata = JsonSerializer.Deserialize<ODataResponse<TicketDto>>(ticketJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return Ok(ticketOdata?.Value ?? new List<TicketDto>());
    }
}

public class TicketDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime CreatedDate { get; set; }
    public int StatusId { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; }
}

public class ODataResponse<T>
{
    public List<T> Value { get; set; }
}