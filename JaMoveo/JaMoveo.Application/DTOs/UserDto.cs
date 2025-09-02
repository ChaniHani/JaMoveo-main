using JaMoveo.Infrastructure.Enums;

namespace JaMoveo.Core.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public EInstrument Instrument { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
