using System.ComponentModel.DataAnnotations;

namespace AuthenticationServices.Models;

public class LoginRequestDto
{
    [Required]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
}
