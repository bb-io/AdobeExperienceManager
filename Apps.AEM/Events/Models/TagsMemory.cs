namespace Apps.AEM.Events.Models;

public class TagsMemory
{
    public ISet<string> ContentWithTagsObserved { get; set; } = new HashSet<string>();
}
