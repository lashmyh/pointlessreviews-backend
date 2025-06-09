using backend.Models;

namespace backend.DTOs
{
    public class ReviewDTO //review creation
    {   
        public int? Rating { get; set; } 
        public required string Title { get; set; }
        public required string Description { get; set; }
        public IFormFile? Image { get; set; }

    }

    public class ReviewResponseDTO //response
    {   
        public int Id { get; set; }
        public int? Rating { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        //reaction counts
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        public int OkayCount { get; set; }
        //users own reaction
        public ReactionType? UserReaction { get; set; } 

        public string UserId { get; set; } = null!;
        public string Username { get; set; } = "Anonymous";

    }


}