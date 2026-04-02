using Apps.AEM.Constants;
using Apps.AEM.Events.Models;
using Apps.AEM.Models.Dtos;
using Apps.AEM.Models.Responses;
using Apps.AEM.Utils;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Polling;
using Blackbird.Applications.SDK.Blueprints;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.AEM.Events;

[PollingEventList]
public class ContentPollingList(InvocationContext invocationContext) : Invocable(invocationContext)
{
    private const string ContentDamRoot = "/content/dam";
    private const string FragmentsEndpoint = "/adobe/sites/cf/fragments";

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
                .AddQueryParameter("type", input.ContentType)
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

    [PollingEvent("On tag added to content fragment", Description = "A polling event that checks for watched tags newly added to content fragments under the specified DAM path.")]
    public async Task<PollingEventResponse<ContentFragmentTagMemory, OnContentFragmentTagAddedResponse>> OnContentFragmentTagAddedAsync(
        PollingEventRequest<ContentFragmentTagMemory> request,
        [PollingEventParameter] OnContentFragmentTagAddedRequest input)
    {
        var watchedTags = input.Tags?
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        if (watchedTags.Count == 0)
            throw new PluginMisconfigurationException("At least one tag must be provided.");

        var rootPath = string.IsNullOrWhiteSpace(input.RootPath)
            ? ContentDamRoot
            : input.RootPath.Trim();

        ValidateDamPath(rootPath);

        var fragmentStates = await GetContentFragmentObservedStatesAsync(rootPath, watchedTags);
        var currentSnapshot = fragmentStates
            .SelectMany(state => state.Tags.Select(tag => BuildObservedFragmentTagKey(state.Path, tag)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var response = new PollingEventResponse<ContentFragmentTagMemory, OnContentFragmentTagAddedResponse>
        {
            Memory = new ContentFragmentTagMemory
            {
                ObservedFragmentTags = currentSnapshot
            }
        };

        if (request.Memory == null)
        {
            response.FlyBird = false;
            response.Result = null;
            return response;
        }

        var previousSnapshot = new HashSet<string>(
            request.Memory.ObservedFragmentTags ?? (IEnumerable<string>)Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        var addedItems = fragmentStates
            .Select(state => new ContentFragmentTagAddedItemResponse
            {
                Title = state.Title,
                ContentId = state.Path,
                FragmentId = state.FragmentId,
                AddedTags = state.Tags
                    .Where(tag => !previousSnapshot.Contains(BuildObservedFragmentTagKey(state.Path, tag)))
                    .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            })
            .Where(item => item.AddedTags.Any())
            .OrderBy(item => item.ContentId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        response.FlyBird = addedItems.Count > 0;
        response.Result = new OnContentFragmentTagAddedResponse(addedItems);

        return response;
    }

    private async Task<List<ObservedContentFragmentState>> GetContentFragmentObservedStatesAsync(
        string rootPath,
        HashSet<string> watchedTags)
    {
        var searchRequest = new RestRequest($"{FragmentsEndpoint}/search")
            .AddQueryParameter("projection", "summary")
            .AddQueryParameter("query", BuildContentFragmentTagSearchQuery(rootPath, watchedTags));

        var fragments = await Client.PaginateByCursor<ContentFragmentDto>(searchRequest);
        var fragmentStates = new List<ObservedContentFragmentState>();

        foreach (var fragment in fragments)
        {
            var tagsRequest = new RestRequest($"{FragmentsEndpoint}/{Uri.EscapeDataString(fragment.Id)}/tags");
            var tagsResponse = await Client.ExecuteWithErrorHandling<ContentFragmentTagListDto>(tagsRequest);

            var matchingTags = tagsResponse.Items
                .Select(tag => tag.Id)
                .Where(tagId => !string.IsNullOrWhiteSpace(tagId) && watchedTags.Contains(tagId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (matchingTags.Count == 0)
                continue;

            fragmentStates.Add(new ObservedContentFragmentState(
                fragment.Title,
                fragment.Path,
                fragment.Id,
                matchingTags));
        }

        return fragmentStates;
    }

    private static string BuildContentFragmentTagSearchQuery(string rootPath, IEnumerable<string> watchedTags)
    {
        var query = new JObject
        {
            ["filter"] = new JObject
            {
                ["path"] = rootPath,
                ["tags"] = new JArray(watchedTags)
            }
        };

        return query.ToString(Formatting.None);
    }

    private static void ValidateDamPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith(ContentDamRoot, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("Content fragment path must start with /content/dam.");
    }

    private static string BuildObservedFragmentTagKey(string contentId, string tagId)
        => $"{contentId}|{tagId}";

    private sealed record ObservedContentFragmentState(string Title, string Path, string FragmentId, IReadOnlyCollection<string> Tags);
}
