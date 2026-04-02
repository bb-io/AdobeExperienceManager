namespace Apps.AEM.Events.Models;

public class ContentFragmentTagMemory
{
    public ISet<string> ObservedFragmentTags { get; set; } = new HashSet<string>();
}
