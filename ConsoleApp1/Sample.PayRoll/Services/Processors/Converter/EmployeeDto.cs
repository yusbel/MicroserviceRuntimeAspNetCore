using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Sample.PayRoll.Services.Processors.Converter
{
    public class EmployeeDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
}