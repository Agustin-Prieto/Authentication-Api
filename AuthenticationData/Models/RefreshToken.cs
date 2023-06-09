﻿namespace AuthenticationServices.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Token { get; set; }
    public string JwtId { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoke { get; set; }
    public DateTime AddedDate { get; set; }
    public DateTime ExpiryDates { get; set; }
}
