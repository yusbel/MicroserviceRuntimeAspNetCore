using System.ComponentModel.DataAnnotations;

namespace Learning.Agenda.Data
{
    public class Agenda
    {
        [Key]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public IEnumerable<Story> Stories { get; set; }
    }
}
