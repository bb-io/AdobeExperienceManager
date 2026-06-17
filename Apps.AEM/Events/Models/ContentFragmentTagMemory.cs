namespace Apps.AEM.Events.Models;

public class ContentFragmentTagMemory
{
    public IEnumerable<string> ObservedFragmentTags { get; set; } = new HashSet<string>();
}
