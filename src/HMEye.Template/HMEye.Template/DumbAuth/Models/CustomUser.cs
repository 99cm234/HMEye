using Microsoft.AspNetCore.Identity;

namespace HMEye.DumbAuth.Models;

public class CustomUser : IdentityUser
{
	public bool DarkMode { get; set; }
	public string? Theme { get; set; }
	public int ExpireTimeSpanMinutes { get; set; }
}
