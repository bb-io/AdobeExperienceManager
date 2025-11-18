using Apps.AEM.Models.Responses;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.SDK.Extensions.FileManagement.Models.FileDataSourceItems;
using Newtonsoft.Json.Linq;
using RestSharp;
using FileItem = Blackbird.Applications.SDK.Extensions.FileManagement.Models.FileDataSourceItems.File;

namespace Apps.AEM.Handlers;

public class ContentPickerDataSourceHandler(InvocationContext invocationContext) : Invocable(invocationContext), IAsyncFileDataSourceItemHandler
{
    private const string RootPath = "/content";

    public Task<IEnumerable<FolderPathItem>> GetFolderPathAsync(FolderPathDataSourceContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context?.FileDataItemId))
            return Task.FromResult<IEnumerable<FolderPathItem>>(new List<FolderPathItem> { new() { DisplayName = "Content", Id = RootPath } });

        var path = context.FileDataItemId;
        
        // Build breadcrumb path
        var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var breadcrumbs = new List<FolderPathItem>
        {
            new() { DisplayName = "Content", Id = RootPath }
        };

        var currentPath = "";
        foreach (var part in pathParts.Skip(1)) // Skip "content"
        {
            currentPath += "/" + part;
            breadcrumbs.Add(new FolderPathItem 
            { 
                DisplayName = part, 
                Id = "/content" + currentPath 
            });
        }

        return Task.FromResult<IEnumerable<FolderPathItem>>(breadcrumbs);
    }

    public async Task<IEnumerable<FileDataItem>> GetFolderContentAsync(FolderContentDataSourceContext context, CancellationToken cancellationToken)
    {
        var folderId = string.IsNullOrEmpty(context.FolderId) ? RootPath : context.FolderId;

        var request = new RestRequest("/content/services/bb-aem-connector/content/events.json")
            .AddQueryParameter("rootPath", folderId);

        var response = await Client.ExecuteWithErrorHandling(request);
        
        if (string.IsNullOrEmpty(response.Content))
            return new List<FileDataItem>();

        var json = JObject.Parse(response.Content);
        var contentItems = json["content"]?.ToObject<List<ContentResponse>>() ?? new List<ContentResponse>();

        return BuildFileDataItems(folderId, contentItems);
    }

    private static IEnumerable<FileDataItem> BuildFileDataItems(string currentPath, List<ContentResponse> contentItems)
    {
        var result = new List<FileDataItem>();
        var pathSegments = new HashSet<string>();

        var normalizedCurrentPath = currentPath.TrimEnd('/');
        var depth = normalizedCurrentPath.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;

        foreach (var item in contentItems)
        {
            var itemPath = item.ContentId.TrimEnd('/');
            if (!itemPath.StartsWith(normalizedCurrentPath + "/", StringComparison.OrdinalIgnoreCase))
                continue;

            var relativePath = itemPath.Substring(normalizedCurrentPath.Length + 1);
            var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
                continue;

            var firstSegment = segments[0];
            var segmentPath = normalizedCurrentPath + "/" + firstSegment;

            if (segments.Length > 1)
            {
                if (!pathSegments.Contains(firstSegment))
                {
                    pathSegments.Add(firstSegment);
                    result.Add(new Folder
                    {
                        Id = segmentPath,
                        DisplayName = firstSegment,
                        IsSelectable = false
                    });
                }
            }
            else
            {
                if (!pathSegments.Contains(firstSegment))
                {
                    pathSegments.Add(firstSegment);
                    result.Add(new FileItem
                    {
                        Id = item.ContentId,
                        DisplayName = string.IsNullOrEmpty(item.Title) ? firstSegment : item.Title,
                        Date = item.Created,
                        IsSelectable = true
                    });
                }
            }
        }

        return result.OrderBy(x => x is FileItem ? 1 : 0).ThenBy(x => x.DisplayName);
    }
}