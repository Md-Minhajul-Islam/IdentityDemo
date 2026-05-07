using Microsoft.AspNetCore.Identity;

namespace IdentityDemo.Models;

public class AppUser : IdentityUser
{

    // We can add custom properties
    public string FullName {get; set;} = string.Empty;
}
