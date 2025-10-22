using Apps.AEM.Models.ApiPayloads;
using Apps.AEM.Models.Requests;
using Apps.AEM.Models.Responses;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;
using Blackbird.Applications.SDK.Extensions.FileManagement.Models;
using RestSharp;

namespace Apps.AEM.Actions;

[ActionList("Content")]
public class AssetActions(InvocationContext invocationContext, IFileManagementClient fileManagementClient) : Invocable(invocationContext)
{
    [Action("Search assets", Description = "Search main assets using specific criteria.")]
    public async Task<SearchAssetsResponse> SearchAssets([ActionParameter] SearchAssetsRequest input)
    {
        if (!input.RootPath.StartsWith("/content/dam/"))
            throw new PluginMisconfigurationException("Asset path must start with /content/dam/");

        var request = new RestRequest("/bin/querybuilder.json")
            .AddQueryParameter("path", input.RootPath)
            .AddQueryParameter("mainasset", "true")
            .AddQueryParameter("type", "dam:Asset")
            .AddQueryParameter("p.limit", "-1")             // todo add proper pagination
            .AddQueryParameter("orderby", "@jcr:lastModified")
            .AddQueryParameter("orderby.sort", "desc");

        if (!string.IsNullOrWhiteSpace(input.NodeName))
            request.AddQueryParameter("nodename", input.NodeName);

        var response = await Client.ExecuteWithErrorHandling<QueryBuilderDto>(request);

        return new SearchAssetsResponse
        {
            AssetsFound = response.Hits.Count,
            Assets = response.Hits.Select(hit => new AssetItem
            {
                Path = hit.Path,
                Name = hit.Name,
                LastModified = hit.LastModified
            })
        };
    }

    [Action("Download asset metadata", Description = "Download an asset metadata as JSON file.")]
    public async Task<DownloadAssetMetadataResponse> DownloadAssetMetadata([ActionParameter] AssetPathRequest input)
    {
        if (!input.Path.StartsWith("/content/dam/"))
            throw new PluginMisconfigurationException("Asset path must start with /content/dam/");

        var apiPath = input.Path.Replace("/content/dam/", "/api/assets/", StringComparison.OrdinalIgnoreCase);

        var request = new RestRequest($"{apiPath}.json", Method.Get);
        var response = await Client.ExecuteWithErrorHandling(request); // TODO : add proper error handling for non-existing assets

        return new DownloadAssetMetadataResponse
        {
            File = await fileManagementClient.UploadAsync(
                new MemoryStream(response.RawBytes ?? []),
                "application/json",
                $"{input.Path.Split('/').Last()}.json")
        };
    }

    [Action("Download asset", Description = "Download an asset from the path.")]
    public async Task<DownloadAssetResponse> DownloadAsset([ActionParameter] AssetPathRequest input)
    {
        if (!input.Path.StartsWith("/content/dam/"))
            throw new PluginMisconfigurationException("Asset path must start with /content/dam/");

        var request = new RestRequest(input.Path, Method.Get);
        var response = await Client.ExecuteWithErrorHandling(request); // TODO : add proper error handling for non-existing assets

        return new DownloadAssetResponse
        {
            File = await fileManagementClient.UploadAsync(
                new MemoryStream(response.RawBytes ?? []),
                response.ContentType ?? "application/octet-stream",
                input.Path.Split('/').Last())
        };
    }
}
