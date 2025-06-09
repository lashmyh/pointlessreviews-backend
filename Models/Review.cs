using System;
using System.Collections.Generic;

namespace backend.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int? Rating { get; set; }  
        public required string Title { get; set; }
        public required string Description { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        // Relationships
        public required string UserId { get; set; }
        public required User User { get; set; }

        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

    }
}
