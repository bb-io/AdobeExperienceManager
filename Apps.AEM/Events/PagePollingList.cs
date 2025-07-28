using Apps.AEM.Events.Models;
using Apps.AEM.Models.Responses;
using Apps.AEM.Utils;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;

namespace Apps.AEM.Events;

[PollingEventList]
public class PagePollingList(InvocationContext invocationContext) : Invocable(invocationContext)
{
    [PollingEvent("On content created or updated", Description = "Polling event that periodically checks for new or updated content. If the any content are found, the event is triggered.")]
    public async Task<PollingEventResponse<PagesMemory, SearchPagesResponse>> OnPagesCreatedOrUpdatedAsync(PollingEventRequest<PagesMemory> request,
        [PollingEventParameter] OnPagesCreatedOrUpdatedRequest optionalRequests)
    {
        if (request.Memory is null)
        {
            return new()
            {
                FlyBird = false,
                Result = null,
                Memory = new PagesMemory { LastTriggeredTime = DateTime.UtcNow }
            };
        }

        var searchRequest = ContentSearch.BuildRequest(new()
        {
            RootPath = optionalRequests.RootPath,
            StartDate = request.Memory.LastTriggeredTime,
            EndDate = DateTime.UtcNow,
            Tags = optionalRequests.Tags,
            Keyword = optionalRequests.Keyword,
            ContentType = optionalRequests.ContentType,
            Events = optionalRequests.Events,
        });

        var createdAndUpdatedPages = await Client.Paginate<PageResponse>(searchRequest);

        if (optionalRequests.RootPathIncludes != null && optionalRequests.RootPathIncludes.Any())
        {
            createdAndUpdatedPages = createdAndUpdatedPages.Where(page => optionalRequests.RootPathIncludes.Any(include => page.Path.Contains(include))).ToList();
        }

        return new()
        {
            FlyBird = createdAndUpdatedPages.Count > 0,
            Result = new(createdAndUpdatedPages),
            Memory = new PagesMemory { LastTriggeredTime = DateTime.UtcNow }
        };
    }
}
