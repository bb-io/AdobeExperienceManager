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
    public async Task<PollingEventResponse<PagesMemory, SearchPagesResponse>> OnPagesCreatedOrUpdatedAsync(
        PollingEventRequest<PagesMemory> request,
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

        var triggerTime = DateTime.UtcNow;
        var searchRequest = ContentSearch.BuildRequest(new()
        {
            RootPath = optionalRequests.RootPath,
            StartDate = request.Memory.LastTriggeredTime,
            EndDate = triggerTime,
            Tags = optionalRequests.Tags,
            Keyword = optionalRequests.Keyword,
            ContentType = optionalRequests.ContentType,
            Events = optionalRequests.Events,
        });

        var createdAndUpdatedPages = await Client.Paginate<PageResponse>(searchRequest);

        if (optionalRequests.RootPathIncludes != null && optionalRequests.RootPathIncludes.Any())
        {
            createdAndUpdatedPages = createdAndUpdatedPages
                .Where(page => optionalRequests.RootPathIncludes.Any(include => page.Path.Contains(include)))
                .ToList();
        }

        return new()
        {
            FlyBird = createdAndUpdatedPages.Count > 0,
            Result = new(createdAndUpdatedPages),
            Memory = new PagesMemory { LastTriggeredTime = triggerTime }
        };
    }

    [PollingEvent("On tag added", Description = "Periodically checks for new content with any of the specified tags. If there is any content found, the event is triggered.")]
    public async Task<PollingEventResponse<TagsMemory, SearchPagesResponse>> OnTagAddedAsync(
        PollingEventRequest<TagsMemory> request,
        [PollingEventParameter] OnPagesCreatedOrUpdatedRequest optionalRequests)
    {
        if (request.Memory is null)
        {
            return new()
            {
                FlyBird = false,
                Result = null,
                Memory = new TagsMemory
                {
                    LastTriggeredTime = DateTime.UtcNow,
                    PagesWithTagsObserved = new HashSet<string>()
                }
            };
        }

        var triggerTime = DateTime.UtcNow;
        var searchRequest = ContentSearch.BuildRequest(new()
        {
            RootPath = optionalRequests.RootPath,
            StartDate = request.Memory.LastTriggeredTime,
            EndDate = triggerTime,
            Tags = optionalRequests.Tags,
            Keyword = optionalRequests.Keyword,
            ContentType = optionalRequests.ContentType,
            Events = optionalRequests.Events,
        });

        IEnumerable<PageResponse> pagesFound = await Client.Paginate<PageResponse>(searchRequest);

        if (optionalRequests.RootPathIncludes?.Any() == true)
        {
            pagesFound = pagesFound
                .Where(page => optionalRequests.RootPathIncludes.Any(include => page.Path.Contains(include)));
        }

        var pagesFoundSet = pagesFound.Select(page => page.Path).ToHashSet();
        var newPagesWithTags = pagesFoundSet.Except(request.Memory.PagesWithTagsObserved);

        return new()
        {
            FlyBird = newPagesWithTags.Any(),
            Result = new(pagesFound.Where(x => newPagesWithTags.Contains(x.Path))),
            Memory = new TagsMemory
            {
                LastTriggeredTime = triggerTime,
                PagesWithTagsObserved = pagesFoundSet
            }
        };
    }
}
