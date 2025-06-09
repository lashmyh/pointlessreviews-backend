using backend.Models;

namespace backend.DTOs
{
    public class ReactionDTO
    {
        public ReactionType ReactionType { get; set; }
        public int ReviewId { get; set; }
    }
}