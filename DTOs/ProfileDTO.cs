namespace backend.DTOs
{
    public class ProfileDTO 
    {
        public required string Username { get; set; }
        public List<ReviewResponseDTO>? Reviews { get; set; }
    }
}