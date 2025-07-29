using Apps.AEM.Models.Dtos;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using RestSharp;

namespace Apps.AEM.Handlers;

public class TagDataHandler(InvocationContext invocationContext) : Invocable(invocationContext), IAsyncDataSourceItemHandler
{
    public async Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        var request = new RestRequest("/content/cq:tags.infinity.json");

        var response = await Client.ExecuteWithErrorHandling<TagsApiPayloadDto>(request);

        var tags = new List<DataSourceItem>();

        foreach (var tag in response.Tags)
        {
            tags.AddRange(
                BuildTagDictionaryRecursive(tag, string.Empty, string.Empty, 0));
        }

        return context.SearchString == null
            ? tags
            : tags.Where(x => x.DisplayName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase));
    }

    // See more info about tags: https://developer.adobe.com/experience-manager/reference-materials/6-5/javadoc/com/day/cq/tagging/Tag.html
    private static List<DataSourceItem> BuildTagDictionaryRecursive(
        TagNodeDto tag,
        string parentPath,
        string parentTitle,
        int level)
    {
        var currentPath = string.IsNullOrEmpty(parentPath)
            ? tag.TagId + ":" // Namespace (root level) always ends with ":", even if that's the selected tag itself
            : level == 1 
                ? $"{parentPath}{tag.TagId}"  // First level will reuse root's colon
                : $"{parentPath}/{tag.TagId}"; // Subsequent levels use "/"

        var currentTitle = string.IsNullOrEmpty(parentTitle) 
            ? tag.Title 
            : level == 1 
                ? $"{parentTitle} : {tag.Title}"  // First level uses " : "
                : $"{parentTitle} / {tag.Title}"; // Subsequent levels use " / "

        var allTags = new List<DataSourceItem> { new(currentPath, currentTitle) };

        foreach (var childTag in tag.Tags)
        {
            allTags.AddRange(
                BuildTagDictionaryRecursive(childTag, currentPath, currentTitle, level + 1));
        }

        return allTags;
    }
}
