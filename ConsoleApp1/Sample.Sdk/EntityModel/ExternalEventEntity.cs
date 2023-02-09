using System.ComponentModel.DataAnnotations.Schema;

namespace Sample.Sdk.EntityModel
{
    [Table("ExternalEvents")]
    public class ExternalEventEntity : Entity
    {
        public string Scheme { get; set; }
        public string Type { get; set; }
        public string Version { get; set; }
        public string Body { get; set; }
        public long CreationTime { get; set; }
        public bool IsDeleted { get; set; }
    }
}