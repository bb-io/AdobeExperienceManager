using Apps.AEM.Actions;
using Apps.AEM.Api;
using Apps.AEM.Constants;
using Apps.AEM.Models.Dtos;
using Apps.AEM.Utils;
using Newtonsoft.Json.Linq;
using Apps.AEM.Models.Requests;
using Apps.AEM.Models.Responses;
using RestSharp;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json;

namespace Tests.AEM;

[TestClass]
[TestCategory("LiveReadOnly")]
public class ContentQueryBuilderLiveTests
{
    public TestContext TestContext { get; set; } = null!;

    private InvocationContext context = null!;
    private ContentActions actions = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        // The config is a credential-name/value dictionary outside the repository.
        var configPath = Environment.GetEnvironmentVariable("AEM_READONLY_CONFIG");
        if (string.IsNullOrWhiteSpace(configPath))
            Assert.Inconclusive("Set AEM_READONLY_CONFIG to a secure local credential config to run read-only WKND checks.");

        var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(await File.ReadAllTextAsync(configPath))!;
        context = new InvocationContext
        {
            AuthenticationCredentialsProviders = config.Select(entry => new AuthenticationCredentialsProvider(entry.Key, entry.Value))
        };
        actions = new ContentActions(context, null!);
    }

    [TestMethod]
    public async Task SearchContent_WkndRootLimitsDatesAndLegacyParity()
    {
        const string root = "/content/wknd/language-masters/en";
        const string page = root + "/about-us";

        var unrestricted = await actions.SearchContentWithQueryBuilder(new() { MaxItems = 1 });
        Assert.AreEqual(1, unrestricted.TotalCount);
        TestContext.WriteLine("Omitted root: one result with limit=1.");

        var included = await actions.SearchContentWithQueryBuilder(new() { RootPath = page });
        Assert.AreEqual(1, included.TotalCount);
        Assert.AreEqual(page, included.Items.Single().ContentId);
        TestContext.WriteLine($"About-us: {included.TotalCount}; title={included.Items[0].Title}; created={included.Items[0].Created:O}; modified={included.Items[0].Modified:O}");

        var excluded = await actions.SearchContentWithQueryBuilder(new() { RootPath = page, IncludeRoot = false });
        Assert.AreEqual(0, excluded.TotalCount);
        TestContext.WriteLine($"About-us with IncludeRoot=false: {excluded.TotalCount}");

        foreach (var limit in new[] { 1, 2, 100 })
        {
            var result = await actions.SearchContentWithQueryBuilder(new() { RootPath = root, MaxItems = limit });
            Assert.IsTrue(result.TotalCount > 0 && result.TotalCount <= limit);
            Assert.AreEqual(result.Items.Count, result.TotalCount);
            if (limit < 100) Assert.AreEqual(limit, result.TotalCount);
            TestContext.WriteLine($"Broad root limit={limit}: returned={result.TotalCount}");
        }

        var cutoff = new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc);
        var filtered = await actions.SearchContentWithQueryBuilder(new() { RootPath = page, StartDate = cutoff });
        Assert.AreEqual(0, filtered.TotalCount);
        TestContext.WriteLine($"About-us after 2026-09-29: {filtered.TotalCount}");

        var input = new SearchContentQueryBuilderRequest
        {
            RootPath = root, IncludeRoot = false, MaxItems = 100,
            StartDate = new DateTime(2000, 8, 1, 2, 30, 0, DateTimeKind.Utc), EndDate = cutoff
        };
        var native = await actions.SearchContentWithQueryBuilder(input);
        var legacy = await actions.SearchContent(new SearchContentRequest
        {
            RootPath = input.RootPath,
            MaxItems = input.MaxItems,
            StartDate = input.StartDate,
            EndDate = input.EndDate
        });
        CollectionAssert.AreEquivalent(legacy.Items.Select(x => x.ContentId).ToArray(), native.Items.Select(x => x.ContentId).ToArray());
        foreach (var item in native.Items)
        {
            var old = legacy.Items.Single(x => x.ContentId == item.ContentId);
            Assert.AreEqual(old.Title, item.Title, $"Title differs for {item.ContentId}");
            Assert.AreEqual(old.Created.Ticks / TimeSpan.TicksPerSecond, item.Created.Ticks / TimeSpan.TicksPerSecond, $"Created differs for {item.ContentId}: plugin={old.Created:O}, native={item.Created:O}");
            Assert.AreEqual(old.Modified.Ticks / TimeSpan.TicksPerSecond, item.Modified.Ticks / TimeSpan.TicksPerSecond, $"Modified differs for {item.ContentId}: plugin={old.Modified:O}, native={item.Modified:O}");
        }
        TestContext.WriteLine($"Subsecond differences: created={native.Items.Count(x => x.Created != legacy.Items.Single(y => y.ContentId == x.ContentId).Created)}, modified={native.Items.Count(x => x.Modified != legacy.Items.Single(y => y.ContentId == x.ContentId).Modified)}");
        TestContext.WriteLine($"Native/plugin parity: {native.TotalCount} items; paths and titles match exactly; dates match to native second precision.");

        var originalPage = legacy.Items.Single(x => x.ContentId == page);
        foreach (var delta in new[] { -1, 0, 1 })
        {
            var lower = await actions.SearchContentWithQueryBuilder(new()
            {
                RootPath = page, Events = ["created"], StartDate = originalPage.Created.AddMilliseconds(delta)
            });
            Assert.AreEqual(delta < 0 ? 1 : 0, lower.TotalCount, $"Exclusive lower bound at creation {delta:+0;-0;0} ms");
            var upper = await actions.SearchContentWithQueryBuilder(new()
            {
                RootPath = page, Events = ["created"], EndDate = originalPage.Created.AddMilliseconds(delta)
            });
            Assert.AreEqual(delta > 0 ? 1 : 0, upper.TotalCount, $"Exclusive upper bound at creation {delta:+0;-0;0} ms");
        }
        TestContext.WriteLine("Exclusive lower/upper date bounds verified at creation timestamp and +/-1 ms.");

        using var client = new ApiClient(context.AuthenticationCredentialsProviders);
        foreach (var type in new[] { ContentTypes.Asset, ContentTypes.File })
        {
            var sample = await client.ExecuteWithErrorHandling<GetPathByTagQueryBuilderResponseDto>(ContentSearch.BuildQueryBuilderRequest(new()
            {
                RootPath = "/content", ContentType = type, MaxItems = 2,
                Events = ["modified"], StartDate = input.StartDate
            }));
            Assert.IsTrue(sample.Success);
            TestContext.WriteLine($"{type}: {sample.Hits.Count} sample hits with jcr:content/jcr:lastModified after 2000-08-01.");
            foreach (var hit in sample.Hits)
            {
                var content = (JObject)hit.AdditionalData["jcr:content"];
                Assert.IsNotNull(content["jcr:lastModified"]);
                TestContext.WriteLine($"{type}: cq:lastModified present={content.ContainsKey("cq:lastModified")}; jcr:lastModified present={content.ContainsKey("jcr:lastModified")}");
            }
        }

    }
    [TestMethod]
    [DataRow("/content/wknd/language-masters/en")]
    [DataRow("/content/wknd/")]
    public async Task SearchContent_ParentRootReturnsOnlyItsPages(string root)
    {
        var result = await actions.SearchContentWithQueryBuilder(new() { RootPath = root });
        Assert.IsTrue(result.TotalCount > 0 && result.TotalCount <= 100);
        Assert.AreEqual(result.Items.Count, result.TotalCount);
        var normalizedRoot = root.TrimEnd('/');
        Assert.IsTrue(result.Items.All(item => item.ContentId == normalizedRoot || item.ContentId.StartsWith(normalizedRoot + "/", StringComparison.Ordinal)));
        if (root.EndsWith("/en"))
            Assert.IsTrue(result.Items.Any(item => item.ContentId == root + "/about-us"));

        var hits = await ReadHitPropertiesAsync(result.Items);
        Assert.IsTrue(hits.All(hit => hit.AdditionalData["jcr:primaryType"].Value<string>() == ContentTypes.Page));
        TestContext.WriteLine($"Root={root}; returned={result.TotalCount}; every hit is cq:Page within root.");
    }

    [TestMethod]
    [DataRow("/content/wknd/language-masters/en", "default:ready-for-translation")]
    [DataRow("/content/wknd/language-masters/en", "workflow:wcm/ready-for-translation")]
    [DataRow("/content/wknd/language-masters/en", "default:translation-in-progress")]
    [DataRow("/content/wknd/language-masters/en", "default:ready-for-translation,workflow:wcm/ready-for-translation,default:translation-in-progress")]
    [DataRow("/content/wknd/", "default:ready-for-translation")]
    [DataRow("/content/wknd/", "workflow:wcm/ready-for-translation")]
    [DataRow("/content/wknd/", "default:translation-in-progress")]
    [DataRow("/content/wknd/", "default:ready-for-translation,workflow:wcm/ready-for-translation,default:translation-in-progress")]
    public async Task SearchContent_TagsMatchAnySuppliedTag(string root, string tagList)
    {
        var tags = tagList.Split(',');
        var result = await actions.SearchContentWithQueryBuilder(new() { RootPath = root, Tags = tags });
        Assert.AreEqual(result.Items.Count, result.TotalCount);
        Assert.IsTrue(result.TotalCount <= 100);

        // Inspect stored tag IDs on returned nodes independently of the search predicate.
        var hits = await ReadHitPropertiesAsync(result.Items);
        foreach (var hit in hits)
        {
            var storedTags = hit.AdditionalData["jcr:content"]["cq:tags"]!;
            var values = storedTags is JArray array ? array.Values<string>() : [storedTags.Value<string>()];
            Assert.IsTrue(values.Any(value => tags.Contains(value)), $"No requested tag on {hit.Path}");
        }

        // Independently filter a bounded sample of all tagged pages, including zero-match cases.
        using var client = new ApiClient(context.AuthenticationCredentialsProviders);
        var baseline = await client.ExecuteWithErrorHandling<GetPathByTagQueryBuilderResponseDto>(new RestRequest("/bin/querybuilder.json")
            .AddQueryParameter("path", root)
            .AddQueryParameter("path.self", "true")
            .AddQueryParameter("type", ContentTypes.Page)
            .AddQueryParameter("property", "jcr:content/cq:tags")
            .AddQueryParameter("property.operation", "exists")
            .AddQueryParameter("p.hits", "selective")
            .AddQueryParameter("p.properties", "jcr:path jcr:content/cq:tags")
            .AddQueryParameter("p.limit", 1000)
            .AddQueryParameter("p.guessTotal", "true")
            .AddQueryParameter("p.excerpt", "false"));
        Assert.IsTrue(baseline.Success);
        Assert.IsFalse(baseline.More, "Tagged baseline exceeds 1000 pages; narrow the fixture root to verify complete tag coverage.");
        var expected = baseline.Hits.Where(hit =>
        {
            var storedTags = hit.AdditionalData["jcr:content"]["cq:tags"]!;
            var values = storedTags is JArray array ? array.Values<string>() : [storedTags.Value<string>()];
            return values.Any(value => tags.Contains(value));
        }).Select(hit => hit.Path).ToArray();
        Assert.AreEqual(Math.Min(expected.Length, 100), result.TotalCount);
        if (expected.Length <= 100)
            CollectionAssert.AreEquivalent(expected, result.Items.Select(item => item.ContentId).ToArray());
        else
            Assert.IsTrue(result.Items.All(item => expected.Contains(item.ContentId)));
        TestContext.WriteLine($"Root={root}; tags={tagList}; returned={result.TotalCount}; independently matched={expected.Length}; tagged baseline={baseline.Hits.Count}.");
    }

    [TestMethod]
    [DataRow(ContentTypes.Page, "/content/wknd/")]
    [DataRow(ContentTypes.Asset, "/content/dam")]
    [DataRow(ContentTypes.File, "/content")]
    public async Task SearchContent_EachContentTypeReturnsMatchingNodes(string type, string root)
    {
        var result = await actions.SearchContentWithQueryBuilder(new() { RootPath = root, ContentType = type, MaxItems = 20 });
        Assert.IsTrue(result.TotalCount > 0 && result.TotalCount <= 20);
        Assert.AreEqual(result.Items.Count, result.TotalCount);
        var normalizedRoot = root.TrimEnd('/');
        Assert.IsTrue(result.Items.All(item => item.ContentId == normalizedRoot || item.ContentId.StartsWith(normalizedRoot + "/", StringComparison.Ordinal)));
        var hits = await ReadHitPropertiesAsync(result.Items);
        Assert.IsTrue(hits.All(hit => hit.AdditionalData["jcr:primaryType"].Value<string>() == type));
        TestContext.WriteLine($"Type={type}; root={root}; returned={result.TotalCount}; every stored jcr:primaryType matches.");
    }

    [TestMethod]
    [DataRow("about-*", 1)]
    [DataRow("about-?s", 1)]
    [DataRow("about-[u]s", 1)]
    [DataRow("no-such-wknd-page-*", 0)]
    public async Task SearchContent_NamePatternMatchesPageName(string pattern, int expected)
    {
        var result = await actions.SearchContentWithQueryBuilder(new()
        {
            RootPath = "/content/wknd/language-masters/en", NamePattern = pattern
        });
        Assert.AreEqual(expected, result.TotalCount);
        if (expected > 0)
            Assert.AreEqual("/content/wknd/language-masters/en/about-us", result.Items.Single().ContentId);
        TestContext.WriteLine($"Name pattern={pattern}; returned={result.TotalCount}.");
    }

    [TestMethod]
    [DataRow("About Us", 1)]
    [DataRow("About", 0)]
    [DataRow("%About%", 0)]
    public async Task SearchContent_PropertyMatchesExactStoredTitle(string value, int expected)
    {
        var result = await actions.SearchContentWithQueryBuilder(new()
        {
            RootPath = "/content/wknd/language-masters/en", PropertyName = "jcr:content/jcr:title", PropertyValue = value
        });
        Assert.AreEqual(expected, result.TotalCount);
        if (expected > 0)
            Assert.AreEqual("/content/wknd/language-masters/en/about-us", result.Items.Single().ContentId);
        TestContext.WriteLine($"Property=jcr:content/jcr:title; value={value}; returned={result.TotalCount}.");
    }

    [TestMethod]
    [DataRow(".*/about-us", "/about-us")]
    [DataRow(".*/adventures(/.*)?", "/adventures")]
    public async Task SearchContent_ExcludePathRemovesMatchingNodes(string pattern, string excludedSuffix)
    {
        const string root = "/content/wknd/language-masters/en";
        var baseline = await actions.SearchContentWithQueryBuilder(new() { RootPath = root });
        Assert.IsTrue(baseline.TotalCount < 100, "Fixture must fit within one page for full comparison.");
        var excludedRoot = root + excludedSuffix;
        var expected = baseline.Items.Where(item => item.ContentId != excludedRoot && !item.ContentId.StartsWith(excludedRoot + "/", StringComparison.Ordinal))
            .Select(item => item.ContentId).ToArray();
        Assert.IsTrue(expected.Length > 0 && expected.Length < baseline.TotalCount);
        var result = await actions.SearchContentWithQueryBuilder(new() { RootPath = root, ExcludePath = pattern });
        CollectionAssert.AreEquivalent(expected, result.Items.Select(item => item.ContentId).ToArray());
        TestContext.WriteLine($"Exclude path={pattern}; before={baseline.TotalCount}; after={result.TotalCount}.");
    }

    [TestMethod]
    public async Task SearchContent_NewFiltersCombineAndCanExcludeRoot()
    {
        const string page = "/content/wknd/language-masters/en/about-us";
        var input = new SearchContentQueryBuilderRequest
        {
            RootPath = page, NamePattern = "about-*", PropertyName = "jcr:content/jcr:title", PropertyValue = "About Us",
            StartDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), ExcludePath = ".*/magazine(/.*)?"
        };
        var matched = await actions.SearchContentWithQueryBuilder(input);
        Assert.AreEqual(1, matched.TotalCount);
        Assert.AreEqual(page, matched.Items.Single().ContentId);
        input.ExcludePath = ".*/about-us";
        var excluded = await actions.SearchContentWithQueryBuilder(input);
        Assert.AreEqual(0, excluded.TotalCount);
        TestContext.WriteLine("Combined name/property/date filters: 1 matching root; excluding root: 0.");
    }

    private async Task<List<QueryBuilderPathHitResponseDto>> ReadHitPropertiesAsync(IEnumerable<ContentResponse> items)
    {
        var paths = items.Select(item => item.ContentId).ToArray();
        if (paths.Length == 0)
            return [];

        using var client = new ApiClient(context.AuthenticationCredentialsProviders);
        var hits = new List<QueryBuilderPathHitResponseDto>();
        // Keep verification URLs below the server's URI length limit for broad-root results.
        foreach (var batch in paths.Chunk(20))
        {
            var request = new RestRequest("/bin/querybuilder.json")
                .AddQueryParameter("group.p.or", "true")
                .AddQueryParameter("p.hits", "selective")
                .AddQueryParameter("p.properties", "jcr:path jcr:primaryType jcr:content/cq:tags")
                .AddQueryParameter("p.limit", batch.Length)
                .AddQueryParameter("p.guessTotal", "true")
                .AddQueryParameter("p.excerpt", "false");
            for (var index = 0; index < batch.Length; index++)
            {
                request.AddQueryParameter($"group.{index + 1}_path", batch[index]);
                request.AddQueryParameter($"group.{index + 1}_path.exact", "true");
            }
            var response = await client.ExecuteWithErrorHandling<GetPathByTagQueryBuilderResponseDto>(request);
            Assert.IsTrue(response.Success);
            hits.AddRange(response.Hits);
        }
        CollectionAssert.AreEquivalent(paths, hits.Select(hit => hit.Path).ToArray());
        return hits;
    }

}
