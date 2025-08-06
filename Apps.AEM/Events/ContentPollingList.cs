using Apps.AEM.Events.Models;
using Apps.AEM.Handlers;
using Apps.AEM.Models.Responses;
using Apps.AEM.Utils;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using Blackbird.Applications.SDK.Blueprints;

namespace Apps.AEM.Events;

[PollingEventList]
public class ContentPollingList(InvocationContext invocationContext) : Invocable(invocationContext)
{
    [PollingEvent("On content created or updated", Description = "Polling event that periodically checks for new or updated content. If the any content are found, the event is triggered.")]
    [BlueprintEventDefinition(BlueprintEvent.ContentCreatedOrUpdatedMultiple)]
    public async Task<PollingEventResponse<ContentMemory, SearchContentResponse>> OnContentCreatedOrUpdated(
        PollingEventRequest<ContentMemory> request,
        [PollingEventParameter] OnContentCreatedOrUpdatedRequest optionalRequests)
    {
        if (request.Memory is null)
        {
            return new()
            {
                FlyBird = false,
                Result = null,
                Memory = new ContentMemory { LastTriggeredTime = DateTime.UtcNow }
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

        var createdAndUpdatedContent = await Client.Paginate<ContentResponse>(searchRequest);

        if (optionalRequests.ContentIdIncludes != null && optionalRequests.ContentIdIncludes.Any())
        {
            createdAndUpdatedContent = createdAndUpdatedContent
                .Where(content => optionalRequests.ContentIdIncludes.Any(include => content.ContentId.Contains(include)))
                .ToList();
        }

        return new()
        {
            FlyBird = createdAndUpdatedContent.Count > 0,
            Result = new(createdAndUpdatedContent),
            Memory = new ContentMemory { LastTriggeredTime = triggerTime }
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

        IEnumerable<ContentResponse> contentFound = await Client.Paginate<ContentResponse>(searchRequest);

        if (input.RootPathIncludes?.Any() == true)
        {
            contentFound = contentFound
                .Where(content => input.RootPathIncludes.Any(include => content.ContentId.Contains(include)));
        }

        var previoslyObservedContent = request.Memory?.ContentWithTagsObserved ?? new HashSet<string>();
        var recentlyChangedContent = contentFound.Select(content => content.ContentId).ToHashSet();

        var newContentWithTags = recentlyChangedContent.Except(previoslyObservedContent);

        var response = new PollingEventResponse<TagsMemory, SearchContentResponse>
        {
            Memory = new TagsMemory
            {
                LastTriggeredTime = triggerTime,
                ContentWithTagsObserved = previoslyObservedContent.Union(newContentWithTags).ToHashSet(),
            }
        };

        if (request.Memory == null)
        {
            response.FlyBird = false;
            response.Result = null;
        }
        else
        {
            response.FlyBird = newContentWithTags.Any();
            response.Result = new SearchContentResponse(contentFound.Where(content => newContentWithTags.Contains(content.ContentId)));
        }

        return response;
    }
}
