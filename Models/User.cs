using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace backend.Models
{
    public class User : IdentityUser {

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

    }
}