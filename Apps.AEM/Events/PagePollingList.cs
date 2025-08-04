using Apps.AEM.Events.Models;
using Apps.AEM.Handlers;
using Apps.AEM.Models.Responses;
using Apps.AEM.Utils;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using Blackbird.Applications.SDK.Blueprints;

namespace Apps.AEM.Events;

[PollingEventList]
public class PagePollingList(InvocationContext invocationContext) : Invocable(invocationContext)
{
    [PollingEvent("On content created or updated", Description = "Polling event that periodically checks for new or updated content. If the any content are found, the event is triggered.")]
    [BlueprintEventDefinition(BlueprintEvent.ContentCreatedOrUpdatedMultiple)]
    public async Task<PollingEventResponse<PagesMemory, SearchContentResponse>> OnPagesCreatedOrUpdatedAsync(
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
            RootPath = optionalRequests.ContentId,
            StartDate = request.Memory.LastTriggeredTime,
            EndDate = triggerTime,
            Tags = optionalRequests.Tags,
            Keyword = optionalRequests.Keyword,
            ContentType = optionalRequests.ContentType,
            Events = optionalRequests.Events,
        });

        var createdAndUpdatedPages = await Client.Paginate<ContentResponse>(searchRequest);

        if (optionalRequests.ContentIdIncludes != null && optionalRequests.ContentIdIncludes.Any())
        {
            createdAndUpdatedPages = createdAndUpdatedPages
                .Where(page => optionalRequests.ContentIdIncludes.Any(include => page.ContentId.Contains(include)))
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
    public async Task<PollingEventResponse<TagsMemory, SearchContentResponse>> OnTagAddedAsync(
        PollingEventRequest<TagsMemory> request,
        [PollingEventParameter] OnTagsAddedRequest input)
    {
        var triggerTime = DateTime.UtcNow;
        var lastTriggeredTime = request.Memory?.LastTriggeredTime ?? triggerTime.AddDays(-365);

        var searchRequest = ContentSearch.BuildRequest(new()
        {
            RootPath = input.RootPath,
            StartDate = lastTriggeredTime,
            EndDate = triggerTime,
            Tags = input.Tags,
            Keyword = input.Keyword,
            ContentType = input.ContentType,
            Events = new EventsDataHandler().GetData().Select(x => x.Value),
        });

        IEnumerable<ContentResponse> pagesFound = await Client.Paginate<ContentResponse>(searchRequest);

        if (input.RootPathIncludes?.Any() == true)
        {
            pagesFound = pagesFound
                .Where(page => input.RootPathIncludes.Any(include => page.ContentId.Contains(include)));
        }

        var previoslyObservedPages = request.Memory?.PagesWithTagsObserved ?? new HashSet<string>();
        var recentlyChangedPages = pagesFound.Select(page => page.ContentId).ToHashSet();

        var newPagesWithTags = recentlyChangedPages.Except(previoslyObservedPages);

        var response = new PollingEventResponse<TagsMemory, SearchContentResponse>
        {
            Memory = new TagsMemory
            {
                LastTriggeredTime = triggerTime,
                PagesWithTagsObserved = previoslyObservedPages.Union(newPagesWithTags).ToHashSet(),
            }
        };

        if (request.Memory == null)
        {
            response.FlyBird = false;
            response.Result = null;
        }
        else
        {
            response.FlyBird = newPagesWithTags.Any();
            response.Result = new SearchContentResponse(pagesFound.Where(x => newPagesWithTags.Contains(x.ContentId)));
        }

        return response;
    }
}
