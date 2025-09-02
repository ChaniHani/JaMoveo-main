using System.ComponentModel.DataAnnotations;

namespace JaMoveo.Core.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "שם המשתמש נדרש")]
        [StringLength(50, ErrorMessage = "שם המשתמש לא יכול להיות יותר מ-50 תווים")]
        public string Username { get; set; }

        [Required(ErrorMessage = "סיסמה נדרשת")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "הסיסמה חייבת להיות בין 6 ל-100 תווים")]
        public string Password { get; set; }
    }
}
