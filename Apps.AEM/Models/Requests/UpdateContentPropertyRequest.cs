using Apps.AEM.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.SDK.Extensions.FileManagement.Models.FileDataSourceItems;

namespace Apps.AEM.Models.Requests
{
    public class UpdateContentPropertyRequest
    {
        [Display("Content path", Description = "Content path to be used in the request.")]
        [FileDataSource(typeof(ContentPickerDataSourceHandler))]
        public string ContentId { get; set; } = string.Empty;

        [Display("Property name")]
        public string PropertyName { get; set; }

        [Display("Property value")]
        public string PropertyValue { get; set; }
    }
}
