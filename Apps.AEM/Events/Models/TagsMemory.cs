namespace Apps.AEM.Events.Models;

public class TagsMemory
{
    public DateTime LastTriggeredTime { get; set; }

    public ISet<string> ContentWithTagsObserved { get; set; } = new HashSet<string>();
}
