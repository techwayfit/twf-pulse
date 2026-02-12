using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TechWayFit.Pulse.Contracts.Requests;
using TechWayFit.Pulse.Contracts.Responses;

namespace TechWayFit.Pulse.Web.Services;

public interface IPulseApiService
{
    Task<CreateSessionResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<SessionSummaryResponse> GetSessionAsync(string code, CancellationToken cancellationToken = default);
    Task<int> GetParticipantCountAsync(string code, CancellationToken cancellationToken = default);
    Task<JoinFacilitatorResponse> JoinAsFacilitatorAsync(string code, JoinFacilitatorRequest request, CancellationToken cancellationToken = default);
    Task<JoinParticipantResponse> JoinAsParticipantAsync(string code, JoinParticipantRequest request, CancellationToken cancellationToken = default);
    Task<SessionSummaryResponse> StartSessionAsync(string code, string facilitatorToken, CancellationToken cancellationToken = default);
    Task<SessionSummaryResponse> EndSessionAsync(string code, string facilitatorToken, CancellationToken cancellationToken = default);
    Task<ActivityResponse> AddActivityAsync(string code, AddActivityRequest request, string facilitatorToken, CancellationToken cancellationToken = default);
    Task<ActivityResponse> CreateActivityAsync(string code, CreateActivityRequest request, string facilitatorToken, CancellationToken cancellationToken = default);
    Task<ActivityResponse> OpenActivityAsync(string code, Guid activityId, string facilitatorToken, CancellationToken cancellationToken = default);
    Task<ActivityResponse> ReopenActivityAsync(string code, Guid activityId, string facilitatorToken, CancellationToken cancellationToken = default);
    Task<ActivityResponse> CloseActivityAsync(string code, Guid activityId, string facilitatorToken, CancellationToken cancellationToken = default);
    Task<ActivityResponse> UpdateActivityAsync(string code, Guid activityId, UpdateActivityRequest request, string facilitatorToken, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgendaActivityResponse>> GetAgendaAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgendaActivityResponse>> GenerateSessionActivitiesAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<SubmitResponseResponse> SubmitResponseAsync(string code, Guid activityId, SubmitResponseRequest request, string participantToken, CancellationToken cancellationToken = default);
    Task<PollDashboardResponse> GetPollDashboardAsync(string code, Guid activityId, Dictionary<string, string?>? filters = null, CancellationToken cancellationToken = default);
    Task<WordCloudDashboardResponse> GetWordCloudDashboardAsync(string code, Guid activityId, Dictionary<string, string?>? filters = null, CancellationToken cancellationToken = default);
    Task<RatingDashboardResponse> GetRatingDashboardAsync(string code, Guid activityId, Dictionary<string, string?>? filters = null, CancellationToken cancellationToken = default);
    Task<GeneralFeedbackDashboardResponse> GetGeneralFeedbackDashboardAsync(string code, Guid activityId, Dictionary<string, string?>? filters = null, CancellationToken cancellationToken = default);
    Task<int> GetParticipantActivityResponseCountAsync(string code, Guid activityId, Guid participantId, CancellationToken cancellationToken = default);
    
    // Generic methods for custom requests
    Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default);
}

public class PulseApiService : IPulseApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public PulseApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    public async Task<CreateSessionResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/sessions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to create session: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<CreateSessionResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<SessionSummaryResponse> GetSessionAsync(string code, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/sessions/{Uri.EscapeDataString(code)}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to get session: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<SessionSummaryResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<int> GetParticipantCountAsync(string code, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/sessions/{Uri.EscapeDataString(code)}/participants/count", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to get participant count: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<int>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<JoinFacilitatorResponse> JoinAsFacilitatorAsync(string code, JoinFacilitatorRequest request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/api/sessions/{Uri.EscapeDataString(code)}/facilitators/join", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to join as facilitator: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<JoinFacilitatorResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<JoinParticipantResponse> JoinAsParticipantAsync(string code, JoinParticipantRequest request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/api/sessions/{Uri.EscapeDataString(code)}/participants/join", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to join as participant: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<JoinParticipantResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<SessionSummaryResponse> StartSessionAsync(string code, string facilitatorToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/sessions/{Uri.EscapeDataString(code)}/start");
        request.Headers.Add("X-Facilitator-Token", facilitatorToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to start session: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<SessionSummaryResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<SessionSummaryResponse> EndSessionAsync(string code, string facilitatorToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/sessions/{Uri.EscapeDataString(code)}/end");
        request.Headers.Add("X-Facilitator-Token", facilitatorToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to end session: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<SessionSummaryResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<ActivityResponse> AddActivityAsync(string code, AddActivityRequest request, string facilitatorToken, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/sessions/{Uri.EscapeDataString(code)}/activities");
        httpRequest.Content = content;
        httpRequest.Headers.Add("X-Facilitator-Token", facilitatorToken);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to add activity: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ActivityResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<IReadOnlyList<AgendaActivityResponse>> GenerateSessionActivitiesAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/sessions/generate", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to generate session activities: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<IReadOnlyList<AgendaActivityResponse>>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? new List<AgendaActivityResponse>();
    }

    public async Task<ActivityResponse> CreateActivityAsync(string code, CreateActivityRequest request, string facilitatorToken, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/sessions/{Uri.EscapeDataString(code)}/activities")
        {
            Content = content
        };
        
        httpRequest.Headers.Add("X-Facilitator-Token", facilitatorToken);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to create activity: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ActivityResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<ActivityResponse> OpenActivityAsync(string code, Guid activityId, string facilitatorToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/sessions/{Uri.EscapeDataString(code)}/activities/{activityId}/open");
        request.Headers.Add("X-Facilitator-Token", facilitatorToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to open activity: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ActivityResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<ActivityResponse> ReopenActivityAsync(string code, Guid activityId, string facilitatorToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/sessions/{Uri.EscapeDataString(code)}/activities/{activityId}/reopen");
        request.Headers.Add("X-Facilitator-Token", facilitatorToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to reopen activity: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ActivityResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<ActivityResponse> CloseActivityAsync(string code, Guid activityId, string facilitatorToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/sessions/{Uri.EscapeDataString(code)}/activities/{activityId}/close");
        request.Headers.Add("X-Facilitator-Token", facilitatorToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to close activity: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ActivityResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<ActivityResponse> UpdateActivityAsync(string code, Guid activityId, UpdateActivityRequest request, string facilitatorToken, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/sessions/{Uri.EscapeDataString(code)}/activities/{activityId}")
        {
            Content = content
        };
        httpRequest.Headers.Add("X-Facilitator-Token", facilitatorToken);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to update activity: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<ActivityResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<IReadOnlyList<AgendaActivityResponse>> GetAgendaAsync(string code, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/sessions/{Uri.EscapeDataString(code)}/activities", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to get agenda: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<IReadOnlyList<AgendaActivityResponse>>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? new List<AgendaActivityResponse>();
    }

    public async Task<PollDashboardResponse> GetPollDashboardAsync(string code, Guid activityId, Dictionary<string, string?>? filters = null, CancellationToken cancellationToken = default)
    {
        var queryParameters = new List<string>();
        if (filters != null)
        {
            foreach (var filter in filters)
            {
                if (!string.IsNullOrEmpty(filter.Value))
                {
                    queryParameters.Add($"{Uri.EscapeDataString(filter.Key)}={Uri.EscapeDataString(filter.Value)}");
                }
            }
        }

        var queryString = queryParameters.Count > 0 ? "?" + string.Join("&", queryParameters) : "";
        var response = await _httpClient.GetAsync($"/api/sessions/{Uri.EscapeDataString(code)}/activities/{activityId}/dashboard/poll{queryString}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to get poll dashboard: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<PollDashboardResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<WordCloudDashboardResponse> GetWordCloudDashboardAsync(string code, Guid activityId, Dictionary<string, string?>? filters = null, CancellationToken cancellationToken = default)
    {
        var queryParameters = new List<string>();
        if (filters != null)
        {
            foreach (var filter in filters)
            {
                if (!string.IsNullOrEmpty(filter.Value))
                {
                    queryParameters.Add($"{Uri.EscapeDataString(filter.Key)}={Uri.EscapeDataString(filter.Value)}");
                }
            }
        }

        var queryString = queryParameters.Count > 0 ? "?" + string.Join("&", queryParameters) : "";
        var response = await _httpClient.GetAsync($"/api/sessions/{Uri.EscapeDataString(code)}/activities/{activityId}/dashboard/wordcloud{queryString}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to get word cloud dashboard: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<WordCloudDashboardResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<RatingDashboardResponse> GetRatingDashboardAsync(string code, Guid activityId, Dictionary<string, string?>? filters = null, CancellationToken cancellationToken = default)
    {
        var queryParameters = new List<string>();
        if (filters != null)
        {
            foreach (var filter in filters)
            {
                if (!string.IsNullOrEmpty(filter.Value))
                {
                    queryParameters.Add($"{Uri.EscapeDataString(filter.Key)}={Uri.EscapeDataString(filter.Value)}");
                }
            }
        }

        var queryString = queryParameters.Count > 0 ? "?" + string.Join("&", queryParameters) : "";
        var response = await _httpClient.GetAsync($"/api/sessions/{Uri.EscapeDataString(code)}/activities/{activityId}/dashboard/rating{queryString}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to get rating dashboard: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<RatingDashboardResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<GeneralFeedbackDashboardResponse> GetGeneralFeedbackDashboardAsync(string code, Guid activityId, Dictionary<string, string?>? filters = null, CancellationToken cancellationToken = default)
    {
        var queryParameters = new List<string>();
        if (filters != null)
        {
            foreach (var filter in filters)
            {
                if (!string.IsNullOrEmpty(filter.Value))
                {
                    queryParameters.Add($"{Uri.EscapeDataString(filter.Key)}={Uri.EscapeDataString(filter.Value)}");
                }
            }
        }

        var queryString = queryParameters.Count > 0 ? "?" + string.Join("&", queryParameters) : "";
        var response = await _httpClient.GetAsync($"/api/sessions/{Uri.EscapeDataString(code)}/activities/{activityId}/dashboard/generalfeedback{queryString}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to get general feedback dashboard: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<GeneralFeedbackDashboardResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<int> GetParticipantActivityResponseCountAsync(string code, Guid activityId, Guid participantId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/sessions/{Uri.EscapeDataString(code)}/activities/{activityId}/participants/{participantId}/responses/count", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to get participant activity response count: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<int>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? 0;
    }

    public async Task<SubmitResponseResponse> SubmitResponseAsync(string code, Guid activityId, SubmitResponseRequest request, string participantToken, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/sessions/{Uri.EscapeDataString(code)}/activities/{activityId}/responses")
        {
            Content = content
        };
        requestMessage.Headers.Add("X-Participant-Token", participantToken);

        var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to submit response: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<SubmitResponseResponse>>(responseJson, _jsonOptions);

        if (apiResponse?.Errors?.Any() == true)
        {
            throw new InvalidOperationException(string.Join(", ", apiResponse.Errors.Select(e => e.Message)));
        }

        return apiResponse?.Data ?? throw new InvalidOperationException("Invalid response from server");
    }

    public async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(endpoint, content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<TResponse>>(responseJson, _jsonOptions);

        return apiResponse ?? new ApiResponse<TResponse> 
        { 
            Success = false,
            Errors = new List<ApiError> { new ApiError { Code = "deserialization_error", Message = "Failed to deserialize response" } }
        };
    }

    public async Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<TResponse>>(responseJson, _jsonOptions);

        return apiResponse ?? new ApiResponse<TResponse> 
        { 
            Success = false,
            Errors = new List<ApiError> { new ApiError { Code = "deserialization_error", Message = "Failed to deserialize response" } }
        };
    }
}

// Helper class for API responses
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public List<ApiError>? Errors { get; set; }
}

public class ApiError
{
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
}