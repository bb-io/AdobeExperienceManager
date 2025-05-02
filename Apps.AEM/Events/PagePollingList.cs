using Apps.AEM.Events.Models;
using Apps.AEM.Models.Responses;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using RestSharp;

namespace Apps.AEM.Events;

[PollingEventList]
public class PagePollingList(InvocationContext invocationContext) : Invocable(invocationContext)
{
    [PollingEvent("On pages created or updated", Description = "Polling event that periodically checks for new or updated pages. If the any pages are found, the event is triggered.")]
    public async Task<PollingEventResponse<PagesMemory, SearchPagesResponse>> OnPagesCreatedOrUpdatedAsync(PollingEventRequest<PagesMemory> request,
        [PollingEventParameter] OnPagesCreatedOrUpdatedRequest optionalRequests)
    {
        if (request.Memory is null)
        {
            return new()
            {
                FlyBird = false,
                Result = null,
                Memory = new PagesMemory
                {
                    LastTriggeredTime = DateTime.UtcNow,
                }
            };
        }

        var parameters = new List<KeyValuePair<string, string>>
        {
            new("startDate", request.Memory.LastTriggeredTime.ToString("yyyy-MM-ddTHH:mm:ssZ")),
            new("endDate", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),
            new("events", "created"),
            new("events", "modified")
        };

        if (optionalRequests.RootPath != null)
        {
            parameters.Add(new("rootPath", optionalRequests.RootPath));
        }

        var createdAndUpdatedPages = await GetPagesAsync(parameters);
        if (optionalRequests.RootPathIncludes != null && optionalRequests.RootPathIncludes.Any())
        {
            createdAndUpdatedPages = createdAndUpdatedPages.Where(page => optionalRequests.RootPathIncludes.Any(include => page.Path.Contains(include))).ToList();
        }

        return new()
        {
            FlyBird = createdAndUpdatedPages.Count > 0,
            Result = new(createdAndUpdatedPages),
            Memory = new PagesMemory
            {
                LastTriggeredTime = DateTime.UtcNow
            }
        };
    }

    private async Task<List<PageResponse>> GetPagesAsync(List<KeyValuePair<string, string>> queryParams)
    {
        var request = new RestRequest("/content/services/bb-aem-connector/pages/events.json");
        foreach (var param in queryParams)
        {
            request.AddQueryParameter(param.Key, param.Value);
        }

        return await Client.Paginate<PageResponse>(request);
    }
}
