using JaMoveo.Infrastructure.Enums;
using System.ComponentModel.DataAnnotations;

namespace JaMoveo.Core.DTOs.Auth
{
    public class SignUpRequestDto
    {
        [Required(ErrorMessage = "שם המשתמש נדרש")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "שם המשתמש חייב להיות בין 3 ל-50 תווים")]
        public string Username { get; set; }

        [Required(ErrorMessage = "סיסמה נדרשת")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "הסיסמה חייבת להיות בין 6 ל-100 תווים")]
        public string Password { get; set; }

        [Required(ErrorMessage = "כלי נגינה נדרש")]
        public EInstrument Instrument { get; set; }
    }
}
