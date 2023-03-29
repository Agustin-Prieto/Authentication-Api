using System.ComponentModel.DataAnnotations;

namespace AuthenticationServices.Models.DTOs;

public class RegisterRequestDto
{
    [Required]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }

    [Required]
    public string Name { get; set; }

}
