using Apps.AEM.Models.ApiPayloads;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.SDK.Extensions.FileManagement.Models.FileDataSourceItems;
using RestSharp;
using FileItem = Blackbird.Applications.SDK.Extensions.FileManagement.Models.FileDataSourceItems.File;

namespace Apps.AEM.Handlers;

public class AssetPickerDataSourceHandler(InvocationContext invocationContext) : Invocable(invocationContext), IAsyncFileDataSourceItemHandler
{
    private const string RootPath = "/content/dam";

    public Task<IEnumerable<FolderPathItem>> GetFolderPathAsync(FolderPathDataSourceContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context?.FileDataItemId))
            return Task.FromResult<IEnumerable<FolderPathItem>>(new List<FolderPathItem> { new() { DisplayName = "Assets", Id = RootPath } });

        var path = context.FileDataItemId;
        
        // Build breadcrumb path
        var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var breadcrumbs = new List<FolderPathItem>
        {
            new() { DisplayName = "Assets", Id = RootPath }
        };

        var currentPath = "";
        foreach (var part in pathParts.Skip(2)) // Skip "content" and "dam"
        {
            currentPath += "/" + part;
            breadcrumbs.Add(new FolderPathItem 
            { 
                DisplayName = part, 
                Id = "/content/dam" + currentPath 
            });
        }

        return Task.FromResult<IEnumerable<FolderPathItem>>(breadcrumbs);
    }

    public async Task<IEnumerable<FileDataItem>> GetFolderContentAsync(FolderContentDataSourceContext context, CancellationToken cancellationToken)
    {
        var folderId = string.IsNullOrEmpty(context.FolderId) ? RootPath : context.FolderId;

        var request = new RestRequest("/bin/querybuilder.json")
            .AddQueryParameter("path", folderId)
            .AddQueryParameter("type", "dam:Asset")
            .AddQueryParameter("p.limit", "-1")
            .AddQueryParameter("orderby", "@jcr:lastModified")
            .AddQueryParameter("orderby.sort", "desc");

        var response = await Client.ExecuteWithErrorHandling<QueryBuilderDto>(request);
        
        if (response?.Hits == null || response.Hits.Count == 0)
            return new List<FileDataItem>();

        return BuildFileDataItems(folderId, response.Hits);
    }

    private static IEnumerable<FileDataItem> BuildFileDataItems(string currentPath, List<QueryBuilderHitDto> assetItems)
    {
        var result = new List<FileDataItem>();
        var processedSegments = new HashSet<string>();

        var normalizedCurrentPath = currentPath.TrimEnd('/');

        // Now determine which direct children are folders vs files
        foreach (var item in assetItems)
        {
            var itemPath = item.Path.TrimEnd('/');
            if (!itemPath.StartsWith(normalizedCurrentPath + "/", StringComparison.OrdinalIgnoreCase))
                continue;

            var relativePath = itemPath.Substring(normalizedCurrentPath.Length + 1);
            var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
                continue;

            var firstSegment = segments[0];
            var segmentPath = normalizedCurrentPath + "/" + firstSegment;

            // Skip if already processed
            if (processedSegments.Contains(firstSegment))
                continue;

            processedSegments.Add(firstSegment);

            // Check if this path is a folder by seeing if any other items have paths that start with it
            var isFolder = assetItems.Any(otherItem =>
            {
                var otherPath = otherItem.Path.TrimEnd('/');
                return otherPath.StartsWith(segmentPath + "/", StringComparison.OrdinalIgnoreCase);
            });

            if (isFolder)
            {
                result.Add(new Folder
                {
                    Id = segmentPath,
                    DisplayName = firstSegment,
                    IsSelectable = false
                });
            }
            else
            {
                // This is a file (asset)
                result.Add(new FileItem
                {
                    Id = item.Path,
                    DisplayName = string.IsNullOrEmpty(item.Name) ? firstSegment : item.Name,
                    Date = item.LastModified,
                    IsSelectable = true
                });
            }
        }

        return result.OrderBy(x => x is FileItem ? 1 : 0).ThenBy(x => x.DisplayName);
    }
}