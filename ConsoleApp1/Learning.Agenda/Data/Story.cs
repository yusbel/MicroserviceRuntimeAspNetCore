using System.ComponentModel.DataAnnotations;

namespace Learning.Agenda.Data
{
    public class Story
    {
        [Key]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string AgendaId { get; set; }
    }
}