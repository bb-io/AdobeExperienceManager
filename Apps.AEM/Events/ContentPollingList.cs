using Apps.AEM.Constants;
using Apps.AEM.Events.Models;
using Apps.AEM.Models.Dtos;
using Apps.AEM.Models.Responses;
using Apps.AEM.Utils;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using Blackbird.Applications.SDK.Blueprints;
using RestSharp;

namespace Apps.AEM.Events;

[PollingEventList]
public class ContentPollingList(InvocationContext invocationContext) : Invocable(invocationContext)
{
    [PollingEvent("On content created or updated", Description = "A polling event that periodically checks for new or updated content. If any content is found, the event is triggered.")]
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

    [PollingEvent("On tag added", Description = "A polling event that for new content with any of the specified tags. If any content is found, the event is triggered.")]
    public async Task<PollingEventResponse<TagsMemory, OnTagAddedContentResponse>> OnTagAddedAsync(
        PollingEventRequest<TagsMemory> request,
        [PollingEventParameter] OnTagsAddedRequest input)
    {
        input.ContentType ??= ContentTypes.Page;
        var property = input.ContentType == ContentTypes.Page
            ? "jcr:content/cq:tags"
            : "jcr:content/metadata/cq:tags";

        var queryBuilderRequest = new RestRequest("/bin/querybuilder.json")
                .AddQueryParameter("path", input.RootPathPrefix)
                .AddQueryParameter("type", input.ContentType ?? ContentTypes.Page)
                .AddQueryParameter("p.limit", -1)                   // TODO Implement pagination
                .AddQueryParameter("p.guessTotal", "true")
                .AddQueryParameter("p.hits", "selective")
                .AddQueryParameter("p.properties", "jcr:path")    // important: this limits what we receive
                .AddQueryParameter("property", property)
                .AddQueryParameter("property.or", "true");

        var index = 1;
        foreach (var tag in input.Tags)
        {
            queryBuilderRequest.AddQueryParameter($"property.{index}_value", tag);
            index++;
        }

        var queryBuilderResponse = await Client.ExecuteWithErrorHandling<GetPathByTagQueryBuilderResponseDto>(queryBuilderRequest);
        var contentFound = queryBuilderResponse.Hits
            .Where(hit => !string.IsNullOrWhiteSpace(hit.Path))
            .Select(hit => hit.Path)
            .ToHashSet();

        if (input.RootPathIncludes?.Any() == true)
        {
            contentFound = contentFound
                .Where(content => input.RootPathIncludes.Any(include => content.Contains(include)))
                .ToHashSet();
        }

        var previoslyObservedContent = request.Memory?.ContentWithTagsObserved ?? new HashSet<string>();

        var newContentWithTags = contentFound.Except(previoslyObservedContent);

        var response = new PollingEventResponse<TagsMemory, OnTagAddedContentResponse>
        {
            Memory = new TagsMemory
            {
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
            response.Result = new OnTagAddedContentResponse(newContentWithTags);
        }

        return response;
    }
}
