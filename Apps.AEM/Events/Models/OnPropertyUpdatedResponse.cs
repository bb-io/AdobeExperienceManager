using Blackbird.Applications.Sdk.Common;

namespace Apps.AEM.Events.Models
{
    public class OnPropertyUpdatedResponse
    {
        [Display("Content paths")]
        public List<string> ContentPaths { get; set; }
    }
}
