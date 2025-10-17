using Apps.AEM.Events;
using Apps.AEM.Events.Models;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using Newtonsoft.Json;
using System.Reflection;
using Tests.AEM.Base;

namespace Tests.AEM;

[TestClass]
public class PagePollingListTests : TestBase
{
    // can't use parent method directly in DynamicData decorator as studio can't see it and shows a warning
    public static string? GetConnectionTypeName(MethodInfo _, object[]? data) => GetConnectionTypeFromDynamicData(data);

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task OnPagesCreatedOrUpdatedAsync_WithNullMemory_ShouldReturnCorrectResponse(InvocationContext context)
    {
        var pollingList = new ContentPollingList(context);

        // Arrange
        var request = new PollingEventRequest<ContentMemory>
        {
            Memory = null,
            PollingTime = DateTime.UtcNow
        };
        var optionalRequest = new OnContentCreatedOrUpdatedRequest();

        // Act
        var response = await pollingList.OnContentCreatedOrUpdated(request, optionalRequest);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.Memory);
        Assert.IsNull(response.Result);
        
        // Log for debugging
        TestContext.WriteLine($"Response: {JsonConvert.SerializeObject(response, Formatting.Indented)}");
        TestContext.WriteLine($"Last triggered time: {response.Memory.LastTriggeredTime}");
        
        Assert.IsFalse(response.FlyBird, "FlyBird should be false for first run with null memory");
        
        // Now test with existing memory
        var memoryWithTime = new ContentMemory
        {
            LastTriggeredTime = DateTime.UtcNow.AddMinutes(-10)  // 10 minutes in the past
        };
        
        var secondRequest = new PollingEventRequest<ContentMemory>
        {
            Memory = memoryWithTime,
            PollingTime = DateTime.UtcNow
        };
        
        // Act again with memory
        var secondResponse = await pollingList.OnContentCreatedOrUpdated(secondRequest, optionalRequest);
        
        // Log second response
        TestContext.WriteLine($"Second Response: {JsonConvert.SerializeObject(secondResponse, Formatting.Indented)}");
        if (secondResponse.Result != null)
        {
            TestContext.WriteLine($"Found pages: {secondResponse.Result.TotalCount}");
        }
        
        // The FlyBird value will depend on whether any pages were created/updated during the test
        TestContext.WriteLine($"FlyBird: {secondResponse.FlyBird}");
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task OnTagAddedAsync_WithNullMemory_ShouldReturnCorrectResponse(InvocationContext context)
    {
        var pollingList = new ContentPollingList(context);

        // Arrange
        var request = new PollingEventRequest<TagsMemory>
        {
            Memory = null,
            PollingTime = DateTime.UtcNow
        };
        var optionalRequest = new OnTagsAddedRequest();

        // Act
        var response = await pollingList.OnTagAddedAsync(request, optionalRequest);

        // Assert
        Assert.IsFalse(response.FlyBird, "FlyBird should be false for first run with null memory");
        Assert.IsNotNull(response.Memory);
        Assert.IsNull(response.Result);
    }

    [TestMethod]
    [DynamicData(nameof(AllInvocationContexts), DynamicDataDisplayName = nameof(GetConnectionTypeName))]
    public async Task OnTagAddedAsync_WithInitialMemory_ShouldWork(InvocationContext context)
    {
        var pollingList = new ContentPollingList(context);

        // Arrange
        var testRunTime = DateTime.UtcNow;
        var request = new PollingEventRequest<TagsMemory>
        {
            Memory = new()
            {
                LastTriggeredTime = testRunTime.AddDays(-360), // 360 days in the past as test instance data might be old
                ContentWithTagsObserved = new HashSet<string>() // { "/content/test-site/en/Homepage" }
            },
            PollingTime = testRunTime
        };
        var optionalRequest = new OnTagsAddedRequest()
        {
            Tags = ["workflow:wcm/ready-for-translation"],
            RootPath = "/content/test-site/en/",
        };

        // Act
        var response = await pollingList.OnTagAddedAsync(request, optionalRequest);

        // Assert
        TestContext.WriteLine($"Response: {JsonConvert.SerializeObject(response.Result, Formatting.Indented)}");

        Assert.IsTrue(response.FlyBird);
        Assert.IsNotNull(response.Memory);
        Assert.IsNotNull(response.Result);
    }
}
