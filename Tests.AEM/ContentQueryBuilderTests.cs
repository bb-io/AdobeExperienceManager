using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Apps.AEM.Actions;
using Apps.AEM.Constants;
using Apps.AEM.Models.Requests;
using Apps.AEM.Models.Responses;
using Apps.AEM.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Blueprints;
using Blackbird.Applications.SDK.Blueprints.Interfaces.CMS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tests.AEM;

[TestClass]
public class ContentQueryBuilderTests
{
    [TestMethod]
    [DataRow(null, 100)]
    [DataRow(1, 1)]
    [DataRow(2, 2)]
    [DataRow(250, 250)]
    public void Request_IsBoundedAndSelective(int? limit, int expected)
    {
        var request = ContentSearch.BuildQueryBuilderRequest(new() { RootPath = " /content/test ", MaxItems = limit });
        var query = request.Parameters.ToDictionary(p => p.Name!, p => p.Value?.ToString());
        Assert.AreEqual("/bin/querybuilder.json", request.Resource);
        Assert.AreEqual("/content/test", query["path"]);
        Assert.AreEqual("true", query["path.self"]);
        Assert.AreEqual("cq:Page", query["type"]);
        Assert.AreEqual(expected.ToString(), query["p.limit"]);
        Assert.AreEqual("0", query["p.offset"]);
        Assert.AreEqual("true", query["p.guessTotal"]);
        Assert.AreEqual("false", query["p.excerpt"]);
        Assert.AreEqual("selective", query["p.hits"]);
        CollectionAssert.AreEquivalent(new[] { "jcr:path", "jcr:created", "jcr:content/pageTitle", "jcr:content/jcr:title", "jcr:content/cq:lastModified", "jcr:content/jcr:lastModified" }, query["p.properties"]!.Split(' '));
        Assert.IsFalse(query.Keys.Any(k => k.Contains("daterange") || k.Contains("group") || k.Contains("orderby")));
        Assert.IsFalse(query.ContainsKey("limit"));
        Assert.IsFalse(query.ContainsKey("offset"));
    }

    [TestMethod]
    public void Request_ExplicitRootExclusionAndOmittedRoot()
    {
        var query = ContentSearch.BuildQueryBuilderRequest(new() { IncludeRoot = false, Events = ["modified"] })
            .Parameters.ToDictionary(p => p.Name!, p => p.Value?.ToString());
        Assert.AreEqual("false", query["path.self"]);
        Assert.AreEqual(string.Empty, query["path"]);
        Assert.IsFalse(query.Keys.Any(k => k.Contains("daterange")));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(-100)]
    public void Request_RejectsNonpositiveLimit(int limit)
    {
        Assert.ThrowsExactly<PluginMisconfigurationException>(() => ContentSearch.BuildQueryBuilderRequest(new() { MaxItems = limit }));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Request_RejectsContradictoryBounds(int days)
    {
        var start = new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc);
        Assert.ThrowsExactly<PluginMisconfigurationException>(() => ContentSearch.BuildQueryBuilderRequest(new() { StartDate = start, EndDate = start.AddDays(days) }));
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public void Request_DateBoundsHaveOwnOrGroup(bool lower, bool upper)
    {
        var start = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddDays(28);
        var query = ContentSearch.BuildQueryBuilderRequest(new()
        {
            StartDate = lower ? start : null, EndDate = upper ? end : null,
            Tags = ["tag:first", "tag:second"], Keyword = "test phrase"
        }).Parameters.ToDictionary(p => p.Name!, p => p.Value?.ToString());
        Assert.AreEqual("true", query["1_group.p.or"]);
        Assert.AreEqual("jcr:created", query["1_group.1_daterange.property"]);
        Assert.AreEqual("jcr:content/cq:lastModified", query["1_group.2_daterange.property"]);
        foreach (var index in new[] { 1, 2 })
        {
            var prefix = $"1_group.{index}_daterange";
            Assert.AreEqual(lower, query.ContainsKey($"{prefix}.lowerBound"));
            Assert.AreEqual(upper, query.ContainsKey($"{prefix}.upperBound"));
            if (lower)
            {
                Assert.AreEqual(start.ToString("O", CultureInfo.InvariantCulture), query[$"{prefix}.lowerBound"]);
                Assert.AreEqual(">", query[$"{prefix}.lowerOperation"]);
            }
            if (upper)
            {
                Assert.AreEqual(end.ToString("O", CultureInfo.InvariantCulture), query[$"{prefix}.upperBound"]);
                Assert.AreEqual("<", query[$"{prefix}.upperOperation"]);
            }
        }
        Assert.IsFalse(query.ContainsKey("p.or"));
        Assert.AreEqual("jcr:content/cq:tags", query["property"]);
        Assert.AreEqual("false", query["property.and"]);
        Assert.AreEqual("tag:first", query["property.1_value"]);
        Assert.AreEqual("tag:second", query["property.2_value"]);
        Assert.AreEqual("test phrase", query["fulltext"]);
    }

    [TestMethod]
    [DataRow("created", "cq:Page", "jcr:created", "jcr:content/cq:tags")]
    [DataRow("modified", "cq:Page", "jcr:content/cq:lastModified", "jcr:content/cq:tags")]
    [DataRow("modified", "dam:Asset", "jcr:content/jcr:lastModified", "jcr:content/metadata/cq:tags")]
    [DataRow("modified", "nt:file", "jcr:content/jcr:lastModified", "jcr:content/cq:tags")]
    public void Request_UsesEventAndContentTypeProperties(string eventType, string type, string dateProperty, string tagProperty)
    {
        var query = ContentSearch.BuildQueryBuilderRequest(new()
        {
            Events = [eventType], ContentType = type, Tags = ["tag:test"], StartDate = DateTime.UtcNow
        }).Parameters.ToDictionary(p => p.Name!, p => p.Value?.ToString());
        Assert.AreEqual(dateProperty, query["1_group.1_daterange.property"]);
        Assert.IsFalse(query.ContainsKey("1_group.2_daterange.property"));
        Assert.AreEqual(tagProperty, query["property"]);
        if (type != "cq:Page")
        {
            StringAssert.Contains(query["p.properties"], "jcr:content/fmditaTitle");
            Assert.IsFalse(query["p.properties"]!.Contains("pageTitle"));
        }
    }

    [TestMethod]
    public void Request_ExplicitBothEventsAndEmptySelectionUseOr()
    {
        foreach (var events in new[] { new[] { "created", "modified" }, Array.Empty<string>() })
        {
            var query = ContentSearch.BuildQueryBuilderRequest(new() { Events = events, StartDate = DateTime.UtcNow })
                .Parameters.ToDictionary(p => p.Name!, p => p.Value?.ToString());
            Assert.AreEqual("true", query["1_group.p.or"]);
            Assert.IsTrue(query.ContainsKey("1_group.2_daterange.property"));
        }
    }

    [TestMethod]
    public void Request_DatesUseInvariantUtcAndPreserveFractionalSeconds()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            var utc = new DateTime(2026, 9, 29, 12, 30, 45, DateTimeKind.Utc).AddTicks(1234567);
            foreach (var date in new[] { utc, utc.ToLocalTime(), DateTime.SpecifyKind(utc, DateTimeKind.Unspecified) })
            {
                var query = ContentSearch.BuildQueryBuilderRequest(new() { StartDate = date })
                    .Parameters.ToDictionary(p => p.Name!, p => p.Value?.ToString());
                Assert.AreEqual("2026-09-29T12:30:45.1234567Z", query["1_group.1_daterange.lowerBound"]);
            }
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    [TestMethod]
    public void Request_NewFiltersCombineWithTagsKeywordAndDateGroup()
    {
        var query = ContentSearch.BuildQueryBuilderRequest(new()
        {
            NamePattern = " about-* ", PropertyName = " jcr:content/jcr:title ", PropertyValue = "About Us",
            ExcludePath = ".*/archive(/.*)?", Tags = ["workflow:wcm/ready-for-translation"],
            Keyword = "About", StartDate = new DateTime(2000, 1, 1)
        }).Parameters.ToDictionary(p => p.Name!, p => p.Value?.ToString());
        Assert.AreEqual("about-*", query["nodename"]);
        Assert.AreEqual("jcr:content/jcr:title", query["2_property"]);
        Assert.AreEqual("About Us", query["2_property.value"]);
        Assert.AreEqual("equals", query["2_property.operation"]);
        Assert.AreEqual(".*/archive(/.*)?", query["excludepaths"]);
        Assert.AreEqual("jcr:content/cq:tags", query["property"]);
        Assert.AreEqual("workflow:wcm/ready-for-translation", query["property.1_value"]);
        Assert.AreEqual("About", query["fulltext"]);
        Assert.AreEqual("true", query["1_group.p.or"]);
        Assert.IsFalse(query.ContainsKey("p.or"));
        Assert.AreEqual("100", query["p.limit"]);
        Assert.IsFalse(query["p.properties"]!.Contains("cq:tags"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    public void Request_OmittedNewFiltersDoNotAddPredicates(string? empty)
    {
        var query = ContentSearch.BuildQueryBuilderRequest(new() { NamePattern = empty, ExcludePath = empty, PropertyName = empty })
            .Parameters.Select(p => p.Name).ToArray();
        Assert.IsFalse(query.Any(name => name is "nodename" or "excludepaths" || name!.StartsWith("2_property")));
    }

    [TestMethod]
    [DataRow("jcr:content/jcr:title", null)]
    [DataRow(null, "About Us")]
    [DataRow(" ", "About Us")]
    [DataRow(null, "")]
    public void Request_RejectsIncompletePropertyPair(string? name, string? value)
    {
        Assert.ThrowsExactly<PluginMisconfigurationException>(() => ContentSearch.BuildQueryBuilderRequest(new() { PropertyName = name, PropertyValue = value }));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" About Us ")]
    [DataRow("%About*")]
    public void Request_PropertyValuesRemainExact(string value)
    {
        var query = ContentSearch.BuildQueryBuilderRequest(new() { PropertyName = "jcr:content/jcr:title", PropertyValue = value })
            .Parameters.ToDictionary(p => p.Name!, p => p.Value?.ToString());
        Assert.AreEqual(value, query["2_property.value"]);
        Assert.AreEqual("equals", query["2_property.operation"]);
    }

    [TestMethod]
    public async Task Action_NewFiltersReachServerWithCorrectEncoding()
    {
        var (_, requests) = await ExecuteSearchAsync(new()
        {
            NamePattern = "about-?[us]", PropertyName = "jcr:content/jcr:title", PropertyValue = "About & Us = + %",
            ExcludePath = @".*/archive\.old(/.*)?"
        }, "{\"success\":true,\"hits\":[]}");
        Assert.AreEqual(1, requests.Count);
        var query = System.Web.HttpUtility.ParseQueryString(requests[0].Query);
        Assert.AreEqual("about-?[us]", query["nodename"]);
        Assert.AreEqual("About & Us = + %", query["2_property.value"]);
        Assert.AreEqual(@".*/archive\.old(/.*)?", query["excludepaths"]);
    }

    [TestMethod]
    public void LegacyBuilder_PreservesPluginParametersAndDefaults()
    {
        var request = ContentSearch.BuildRequest(new()
        {
            RootPath = " /content/test ", StartDate = new DateTime(2000, 8, 1, 2, 30, 0),
            EndDate = new DateTime(2026, 9, 29), Tags = ["tag:a", "tag:b"], Keyword = "text", Offset = 50, Limit = 100
        });
        Assert.AreEqual("/content/services/bb-aem-connector/content/events.json", request.Resource);
        Assert.AreEqual(" /content/test ", request.Parameters.Single(p => p.Name == "rootPath").Value);
        Assert.AreEqual("2000-08-01T02:30:00Z", request.Parameters.Single(p => p.Name == "startDate").Value);
        Assert.AreEqual("2026-09-29T00:00:00Z", request.Parameters.Single(p => p.Name == "endDate").Value);
        Assert.AreEqual("cq:Page", request.Parameters.Single(p => p.Name == "type").Value);
        CollectionAssert.AreEqual(new[] { "created", "modified" }, request.Parameters.Where(p => p.Name == "events").Select(p => p.Value!.ToString()).ToArray());
        CollectionAssert.AreEqual(new[] { "tag:a", "tag:b" }, request.Parameters.Where(p => p.Name == "tags").Select(p => p.Value!.ToString()).ToArray());
        Assert.AreEqual("100", request.Parameters.Single(p => p.Name == "limit").Value?.ToString());
        Assert.AreEqual("50", request.Parameters.Single(p => p.Name == "offset").Value?.ToString());
        Assert.IsFalse(request.Parameters.Any(p => p.Name!.StartsWith("p.")));
        var omitted = ContentSearch.BuildRequest(new());
        Assert.IsFalse(omitted.Parameters.Any(p => p.Name is "limit" or "offset"));
        Assert.AreEqual("0001-01-01T00:00:00Z", omitted.Parameters.Single(p => p.Name == "startDate").Value);
        Assert.AreEqual("0001-01-01T00:00:00Z", omitted.Parameters.Single(p => p.Name == "endDate").Value);
    }

    [TestMethod]
    public void Contract_PreservesLegacySignatureInputsAndOutputs()
    {
        var legacy = typeof(ContentActions).GetMethod("SearchContent")!;
        Assert.AreEqual(typeof(Task<SearchContentResponse>), legacy.ReturnType);
        Assert.AreEqual(typeof(SearchContentRequest), legacy.GetParameters().Single().ParameterType);
        Assert.AreEqual("Search content (deprecated)", legacy.GetCustomAttributesData().Single(a => a.AttributeType == typeof(ActionAttribute)).ConstructorArguments[0].Value);
        Assert.IsNull(legacy.GetCustomAttribute<BlueprintActionDefinitionAttribute>());
        var current = typeof(ContentActions).GetMethod("SearchContentWithQueryBuilder")!;
        Assert.IsNotNull(current.GetCustomAttribute<BlueprintActionDefinitionAttribute>());
        Assert.AreEqual("Search content", current.GetCustomAttributesData().Single(a => a.AttributeType == typeof(ActionAttribute)).ConstructorArguments[0].Value);
        var properties = typeof(SearchContentQueryBuilderRequest).GetProperties();
        CollectionAssert.AreEquivalent(typeof(SearchContentRequest).GetProperties().Select(p => p.Name).Concat(new[] { "IncludeRoot", "NamePattern", "PropertyName", "PropertyValue", "ExcludePath" }).ToArray(), properties.Select(p => p.Name).ToArray());
        var input = JsonConvert.DeserializeObject<SearchContentQueryBuilderRequest>("{\"StartDate\":\"2026-09-01T00:00:00Z\",\"EndDate\":\"2026-09-29T00:00:00Z\"}")!;
        Assert.AreEqual(typeof(object), typeof(SearchContentQueryBuilderRequest).BaseType);
        Assert.IsTrue(properties.All(property => property.DeclaringType == typeof(SearchContentQueryBuilderRequest)));
        Assert.AreEqual(new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc), input.EndDate);
        Assert.AreEqual(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), input.StartDate);
        Assert.IsFalse(properties.Single(p => p.Name == "EndDate").GetCustomAttribute<DisplayAttribute>()!.Description!.Contains("5 years"));
        StringAssert.Contains(typeof(SearchContentRequest).GetProperty("EndDate")!.GetCustomAttribute<DisplayAttribute>()!.Description, "5 years");
        Assert.IsTrue(typeof(IDownloadContentInput).IsAssignableFrom(typeof(ContentResponse)));
        Assert.IsTrue(typeof(IMultiDownloadableContentOutput<ContentResponse>).IsAssignableFrom(typeof(SearchContentResponse)));
        CollectionAssert.AreEquivalent(new[] { "Items", "TotalCount" }, typeof(SearchContentResponse).GetProperties().Select(p => p.Name).ToArray());
        CollectionAssert.AreEquivalent(new[] { "ContentId", "Title", "Created", "Modified" }, typeof(ContentResponse).GetProperties().Select(p => p.Name).ToArray());
        CollectionAssert.AreEquivalent(new[] { "path", "title", "created", "modified" }, JObject.FromObject(new ContentResponse()).Properties().Select(p => p.Name).ToArray());
    }

    [TestMethod]
    public async Task Action_MapsNestedPropertiesAndReturnsActualCountWithoutPagination()
    {
        var (response, requests) = await ExecuteSearchAsync(new() { MaxItems = 2 }, """
            {"success":true,"total":999,"more":true,"hits":[
              {"jcr:path":"/content/about-us","jcr:created":"2020-01-01T00:00:00Z","jcr:content":{"pageTitle":"About Us","jcr:title":"Other title","cq:lastModified":"2026-09-23T12:30:00Z","jcr:lastModified":"2026-09-22T00:00:00Z"}},
              {"jcr:path":"/content/second","jcr:content":{"pageTitle":" ","jcr:title":"Second"}}
            ]}
            """);
        Assert.AreEqual(1, requests.Count);
        StringAssert.Contains(requests[0].Query, "p.limit=2");
        Assert.AreEqual("/bin/querybuilder.json", requests[0].AbsolutePath);
        Assert.AreEqual(2, response.TotalCount);
        Assert.AreEqual("/content/about-us", response.Items[0].ContentId);
        Assert.AreEqual("About Us", response.Items[0].Title);
        Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), response.Items[0].Created.ToUniversalTime());
        Assert.AreEqual(new DateTime(2026, 9, 23, 12, 30, 0, DateTimeKind.Utc), response.Items[0].Modified.ToUniversalTime());
        Assert.AreEqual("Second", response.Items[1].Title);
    }

    [TestMethod]
    public async Task Action_HandlesMissingPropertiesAndSameSecondDates()
    {
        var (response, _) = await ExecuteSearchAsync(new(), """
            {"success":true,"hits":[
              {"jcr:path":"/content/name-fallback"},
              {"jcr:path":"/content/equal","jcr:created":"2026-09-23T12:30:00.100Z","jcr:content":{"cq:lastModified":"2026-09-23T12:30:00.999Z"}},
              {"jcr:path":"/content/fallback","jcr:content":{"jcr:lastModified":"2026-09-24T00:00:00Z"}}
            ]}
            """);
        Assert.AreEqual("name-fallback", response.Items[0].Title);
        Assert.AreEqual(default(DateTime), response.Items[0].Created);
        Assert.AreEqual(default(DateTime), response.Items[0].Modified);
        Assert.AreEqual(default(DateTime), response.Items[1].Modified);
        Assert.AreEqual(new DateTime(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc), response.Items[2].Modified.ToUniversalTime());
    }

    [TestMethod]
    [DataRow("dam:Asset")]
    [DataRow("nt:file")]
    public async Task Action_NonPageTitlesMatchPluginFallback(string contentType)
    {
        var (response, _) = await ExecuteSearchAsync(new() { ContentType = contentType }, """
            {"success":true,"hits":[
              {"jcr:path":"/content/dam/first","jcr:content":{"jcr:title":"Title","fmditaTitle":"DITA"}},
              {"jcr:path":"/content/dam/second","jcr:content":{"jcr:title":" ","fmditaTitle":"DITA fallback"}},
              {"jcr:path":"/content/dam/missing","jcr:created":null,"jcr:content":{"jcr:title":null,"cq:lastModified":null}}
            ]}
            """);
        Assert.AreEqual("Title", response.Items[0].Title);
        Assert.AreEqual("DITA fallback", response.Items[1].Title);
        Assert.AreEqual(string.Empty, response.Items[2].Title);
        Assert.AreEqual(default(DateTime), response.Items[2].Created);
        Assert.AreEqual(default(DateTime), response.Items[2].Modified);
    }

    [TestMethod]
    [DataRow("Thu Jun 19 2025 08:37:16 GMT+0000", "2025-06-19T08:37:16Z")]
    [DataRow("Thu Jun 19 2025 08:37:16 GMT+0530", "2025-06-19T03:07:16Z")]
    [DataRow("Thu Jun 19 2025 08:37:16 GMT-0230", "2025-06-19T11:07:16Z")]
    [DataRow("2025-06-19T08:37:16.123+05:30", "2025-06-19T03:07:16.123Z")]
    public void NativeDates_ParseInvariantOffsets(string value, string expected)
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var result = ContentSearch.ParseQueryBuilderDate(new JValue(value));
            Assert.AreEqual(DateTime.Parse(expected, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal), result);
            Assert.AreEqual(DateTimeKind.Utc, result!.Value.Kind);
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    [TestMethod]
    public async Task Action_RejectsQueryBuilderFailureOnHttp200()
    {
        await Assert.ThrowsExactlyAsync<PluginApplicationException>(() => ExecuteSearchAsync(new(), "{\"success\":false,\"hits\":[],\"error\":\"Invalid query\"}"));
    }

    [TestMethod]
    public async Task Action_EmptyResultHasZeroCount()
    {
        var (response, requests) = await ExecuteSearchAsync(new(), "{\"success\":true,\"total\":100,\"hits\":[]}");
        Assert.AreEqual(0, response.TotalCount);
        Assert.AreEqual(0, response.Items.Count);
        Assert.AreEqual(1, requests.Count);
    }

    private static async Task<(SearchContentResponse Response, List<Uri> Requests)> ExecuteSearchAsync(SearchContentQueryBuilderRequest input, string json)
    {
        using var portReservation = new TcpListener(IPAddress.Loopback, 0);
        portReservation.Start();
        var port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
        portReservation.Stop();
        using var listener = new HttpListener();
        var address = $"http://127.0.0.1:{port}/";
        listener.Prefixes.Add(address);
        listener.Start();
        var requests = new List<Uri>();
        var server = Task.Run(async () =>
        {
            try
            {
                while (listener.IsListening)
                {
                    var context = await listener.GetContextAsync();
                    requests.Add(context.Request.Url!);
                    context.Response.ContentType = "application/json";
                    var bytes = Encoding.UTF8.GetBytes(json);
                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes);
                    context.Response.Close();
                }
            }
            catch (HttpListenerException) when (!listener.IsListening) { }
            catch (ObjectDisposedException) when (!listener.IsListening) { }
        });
        try
        {
            var actions = new ContentActions(new InvocationContext
            {
                AuthenticationCredentialsProviders = new[]
                {
                    new AuthenticationCredentialsProvider(CredNames.BaseUrl, address),
                    new AuthenticationCredentialsProvider(CredNames.ConnectionType, ConnectionTypes.OnPremise),
                    new AuthenticationCredentialsProvider(CredNames.Username, "test"),
                    new AuthenticationCredentialsProvider(CredNames.Password, "test")
                }
            }, null!);
            var response = await actions.SearchContentWithQueryBuilder(input).WaitAsync(TimeSpan.FromSeconds(10));
            return (response, requests);
        }
        finally
        {
            listener.Stop();
            await server;
        }
    }
}
