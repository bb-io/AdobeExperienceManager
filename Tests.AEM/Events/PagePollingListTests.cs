using Apps.AEM.Events;
using Apps.AEM.Events.Models;
using Blackbird.Applications.Sdk.Common.Polling;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Tests.AEM.Base;

namespace Tests.AEM.Events;

[TestClass]
public class PagePollingListTests : TestBase
{
    private PagePollingList _pollingList => new PagePollingList(InvocationContext);

    [TestMethod]
    public async Task OnPagesCreatedOrUpdatedAsync_WithNullMemory_ShouldReturnCorrectResponse()
    {
        // Arrange
        var request = new PollingEventRequest<PagesMemory>
        {
            Memory = null,
            PollingTime = DateTime.UtcNow
        };
        var optionalRequest = new OnPagesCreatedOrUpdatedRequest();

        // Act
        var response = await _pollingList.OnPagesCreatedOrUpdatedAsync(request, optionalRequest);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.Memory);
        Assert.IsNull(response.Result);
        
        // Log for debugging
        Console.WriteLine($"Response: {JsonConvert.SerializeObject(response, Formatting.Indented)}");
        Console.WriteLine($"Last triggered time: {response.Memory.LastTriggeredTime}");
        
        Assert.IsFalse(response.FlyBird, "FlyBird should be false for first run with null memory");
        
        // Now test with existing memory
        var memoryWithTime = new PagesMemory
        {
            LastTriggeredTime = DateTime.UtcNow.AddMinutes(-10)  // 10 minutes in the past
        };
        
        var secondRequest = new PollingEventRequest<PagesMemory>
        {
            Memory = memoryWithTime,
            PollingTime = DateTime.UtcNow
        };
        
        // Act again with memory
        var secondResponse = await _pollingList.OnPagesCreatedOrUpdatedAsync(secondRequest, optionalRequest);
        
        // Log second response
        Console.WriteLine($"Second Response: {JsonConvert.SerializeObject(secondResponse, Formatting.Indented)}");
        if (secondResponse.Result != null)
        {
            Console.WriteLine($"Found pages: {secondResponse.Result.TotalCount}");
        }
        
        // The FlyBird value will depend on whether any pages were created/updated during the test
        Console.WriteLine($"FlyBird: {secondResponse.FlyBird}");
    }

    [TestMethod]
    public async Task OnTagAddedAsync_WithNullMemory_ShouldReturnCorrectResponse()
    {
        // Arrange
        var request = new PollingEventRequest<TagsMemory>
        {
            Memory = null,
            PollingTime = DateTime.UtcNow
        };
        var optionalRequest = new OnTagsAddedRequest();

        // Act
        var response = await _pollingList.OnTagAddedAsync(request, optionalRequest);

        // Assert
        Assert.IsFalse(response.FlyBird, "FlyBird should be false for first run with null memory");
        Assert.IsNotNull(response.Memory);
        Assert.IsNull(response.Result);
    }

    [TestMethod]
    public async Task OnTagAddedAsync_WithInitialMemory_ShouldWork()
    {
        // Arrange
        var testRunTime = DateTime.UtcNow;
        var request = new PollingEventRequest<TagsMemory>
        {
            Memory = new()
            {
                LastTriggeredTime = testRunTime.AddDays(-10),
                PagesWithTagsObserved = new HashSet<string>() // { "/content/test-site/en/Homepage" }
            },
            PollingTime = testRunTime
        };
        var optionalRequest = new OnTagsAddedRequest()
        {
            Tags = ["workflow:wcm/ready-for-translation"],
            RootPath = "/content/test-site/en/",
        };

        // Act
        var response = await _pollingList.OnTagAddedAsync(request, optionalRequest);

        // Assert
        Console.WriteLine($"Response: {JsonConvert.SerializeObject(response.Result, Formatting.Indented)}");

        Assert.IsTrue(response.FlyBird);
        Assert.IsNotNull(response.Memory);
        Assert.IsNotNull(response.Result);
    }
}
