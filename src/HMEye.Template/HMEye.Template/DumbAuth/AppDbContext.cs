using HMEye.DumbAuth.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HMEye.DumbAuth;

public class DumbAuthDbContext : IdentityDbContext<CustomUser>
{
	public DumbAuthDbContext(DbContextOptions<DumbAuthDbContext> options)
		: base(options) { }
}
