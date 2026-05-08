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

        if (string.IsNullOrWhiteSpace(rootPath) || !rootPath.StartsWith(ContentDamRoot, StringComparison.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("Content fragment path must start with /content/dam.");

        var statuses = input.Statuses ?? ContentFragmentStatuses.All;
        var fieldForTags = input.FieldForTags?.Trim();

        var fragmentStates = string.IsNullOrWhiteSpace(fieldForTags)
            ? await GetContentFragmentObservedStatesAsync(rootPath, watchedTags, statuses)
            : await GetContentFragmentObservedStatesFromFieldAsync(rootPath, watchedTags, statuses, fieldForTags);
        var currentSnapshot = fragmentStates
            .SelectMany(state => state.Tags.Select(tag => BuildObservedFragmentTagKey(state.Path, tag)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var response = new PollingEventResponse<ContentFragmentTagMemory, OnContentFragmentTagAddedResponse>
        {
            Memory = new ContentFragmentTagMemory { ObservedFragmentTags = currentSnapshot }
        };

        if (request.Memory == null)
        {
            response.FlyBird = false;
            response.Result = null;
            return response;
        }

        var previousSnapshot = new HashSet<string>(
            request.Memory.ObservedFragmentTags ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase),
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

    [PollingEvent("On property updated", Description = "Triggered when a page under the root path has a specific property updated to match a provided value.")]
    public async Task<PollingEventResponse<PropertyUpdateMemory, OnPropertyUpdatedResponse>> OnPropertyUpdatedAsync(
    PollingEventRequest<PropertyUpdateMemory> request,
    [PollingEventParameter] OnPropertyUpdatedRequest input)
    {
        var queryBuilderRequest = new RestRequest("/bin/querybuilder.json")
            .AddQueryParameter("path", input.RootPath)
            .AddQueryParameter("type", "cq:PageContent")
            .AddQueryParameter("p.limit", "-1")
            .AddQueryParameter("p.guessTotal", "true")
            .AddQueryParameter("p.hits", "selective")
            .AddQueryParameter("p.properties", "jcr:path")
            .AddQueryParameter("1_property", input.PropertyName)
            .AddQueryParameter("1_property.value", input.PropertyValue);

        var queryBuilderResponse = await Client.ExecuteWithErrorHandling<QueryBuilderPathResponseDto>(queryBuilderRequest);

        var contentFound = queryBuilderResponse.Hits
            .Where(hit => !string.IsNullOrWhiteSpace(hit.Path))
            .Select(hit => hit.Path.Replace("/jcr:content", ""))
            .ToHashSet();

        var previouslyObserved = request.Memory?.ObservedPaths ?? new HashSet<string>();

        var newlyMatchedContent = contentFound.Except(previouslyObserved).ToList();

        var response = new PollingEventResponse<PropertyUpdateMemory, OnPropertyUpdatedResponse>
        {
            Memory = new PropertyUpdateMemory
            {
                ObservedPaths = contentFound
            }
        };

        if (request.Memory == null)
        {
            response.FlyBird = false;
            response.Result = null;
        }
        else
        {
            response.FlyBird = newlyMatchedContent.Any();
            response.Result = new OnPropertyUpdatedResponse
            {
                ContentPaths = newlyMatchedContent
            };
        }

        return response;
    }

    private async Task<List<ObservedContentFragmentState>> GetContentFragmentObservedStatesAsync(
        string rootPath,
        HashSet<string> watchedTags,
        IEnumerable<string> statuses)
    {
        var filter = new JObject
        {
            ["path"] = rootPath,
            ["tags"] = new JArray(watchedTags)
        };

        if (statuses.Any() == true)
            filter["status"] = new JArray(statuses);

        var query = new JObject
        {
            ["filter"] = filter
        };

        var searchRequest = new RestRequest($"{FragmentsEndpoint}/search")
            .AddQueryParameter("projection", "summary")
            .AddQueryParameter("query", query.ToString(Formatting.None));

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

    private async Task<List<ObservedContentFragmentState>> GetContentFragmentObservedStatesFromFieldAsync(
        string rootPath,
        HashSet<string> watchedTags,
        IEnumerable<string> statuses,
        string fieldForTags)
    {
        var fieldTagProperty = $"jcr:content/data/master/{fieldForTags}";
        var selectedProperties = string.Join(" ", [
            "jcr:path",
            "jcr:uuid",
            "jcr:content/jcr:title",
            "jcr:content/metadata/dc:title",
            fieldTagProperty
        ]);
        var queryBuilderRequest = new RestRequest("/bin/querybuilder.json")
            .AddQueryParameter("path", rootPath)
            .AddQueryParameter("type", "dam:Asset")
            .AddQueryParameter("p.limit", -1)
            .AddQueryParameter("p.guessTotal", "true")
            .AddQueryParameter("p.hits", "selective")
            .AddQueryParameter("p.properties", selectedProperties)
            .AddQueryParameter("1_property", "jcr:content/contentFragment")
            .AddQueryParameter("1_property.value", "true")
            .AddQueryParameter("2_property", fieldTagProperty)
            .AddQueryParameter("2_property.or", "true");

        var index = 1;
        foreach (var tag in watchedTags.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase))
        {
            queryBuilderRequest.AddQueryParameter($"2_property.{index}_value", tag);
            index++;
        }

        var queryBuilderResponse = await Client.ExecuteWithErrorHandling<GetPathByTagQueryBuilderResponseDto>(queryBuilderRequest);
        return queryBuilderResponse.Hits
            .Where(hit => !string.IsNullOrWhiteSpace(hit.Path))
            .Select(hit => new
            {
                Hit = hit,
                Tags = GetMatchingQueryBuilderFieldTags(hit, fieldTagProperty, watchedTags)
            })
            .Where(item => item.Tags.Count > 0)
            .GroupBy(item => item.Hit.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.Hit.Path, StringComparer.OrdinalIgnoreCase)
            .Select(item => new ObservedContentFragmentState(
                GetQueryBuilderTitle(item.Hit),
                item.Hit.Path,
                item.Hit.Id,
                item.Tags))
            .ToList();
    }

    private static List<string> GetMatchingQueryBuilderFieldTags(
        QueryBuilderPathHitResponseDto hit,
        string fieldTagProperty,
        HashSet<string> watchedTags)
    {
        if (!hit.AdditionalData.TryGetValue(fieldTagProperty, out var fieldValue))
            return watchedTags.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToList();

        var matchingTags = GetQueryBuilderStringValues(fieldValue)
            .Where(tagId => !string.IsNullOrWhiteSpace(tagId) && watchedTags.Contains(tagId))
            .Select(tagId => tagId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return matchingTags.Count > 0
            ? matchingTags
            : watchedTags.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IEnumerable<string> GetQueryBuilderStringValues(JToken token)
    {
        return token switch
        {
            JArray array => array.Values<string>().Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!),
            JValue value when value.Type == JTokenType.String && !string.IsNullOrWhiteSpace(value.ToString()) => [value.ToString()],
            _ => []
        };
    }

    private static string GetQueryBuilderTitle(QueryBuilderPathHitResponseDto hit)
    {
        if (!string.IsNullOrWhiteSpace(hit.Title))
            return hit.Title;

        if (!string.IsNullOrWhiteSpace(hit.MetadataTitle))
            return hit.MetadataTitle;

        return hit.Path.TrimEnd('/').Split('/').LastOrDefault() ?? string.Empty;
    }

    private static string BuildObservedFragmentTagKey(string contentId, string tagId)
        => $"{contentId}|{tagId}";

    private sealed record ObservedContentFragmentState(string Title, string Path, string FragmentId, IReadOnlyCollection<string> Tags);
}
