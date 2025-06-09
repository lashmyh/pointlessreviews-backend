namespace backend.Models
{
    public enum ReactionType
    {
        Like,
        Dislike,
        Okay
    }

    public class Reaction 
    {
        public int Id { get; set; }
        public required string UserId { get; set; }
        public required User User { get; set; }
        public int ReviewId { get; set; }
        public required Review Review { get; set; }
        public ReactionType ReactionType { get; set; }

        
    }
    
}