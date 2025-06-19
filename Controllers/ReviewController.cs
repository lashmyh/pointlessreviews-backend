using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly SupabaseStorageService _storageService;
        private readonly AppDbContext _context;

        private readonly ILogger<ReviewController> _logger;

        public ReviewController(SupabaseStorageService storageService, AppDbContext context, ILogger<ReviewController> logger)
        {
            _storageService = storageService;
            _context = context;
            _logger = logger;
        }

        private string? UserId => User.Identity?.IsAuthenticated == true 
        ? User.FindFirstValue(ClaimTypes.NameIdentifier) 
        : null;

        // Create a review
        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateReview([FromForm] ReviewDTO reviewDto)
        {   

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for CreateReview: {@ModelState}", ModelState);
                return BadRequest(ModelState); 
            }

            var user = await _context.Users.FindAsync(UserId);
            if (user == null) {
                _logger.LogWarning("Unauthorised access attempt. User {UserId} not found.", UserId);
                return Unauthorized();
            }
            
            string imageUrl = "";

            try {

                if (reviewDto.Image != null) {
                    imageUrl = await _storageService.UploadImageAsync(reviewDto.Image);
                }

            
                var review = new Review{
                    Rating = reviewDto.Rating,
                    Title = reviewDto.Title,
                    Description = reviewDto.Description,
                    ImageUrl = imageUrl,
                    UserId = UserId!,
                    CreatedAt = DateTime.UtcNow,
                    User = user
                    
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                var response = new ReviewResponseDTO
                {
                    Id = review.Id,
                    Rating = review.Rating,
                    Title = review.Title,
                    Description = review.Description,
                    ImageUrl = review.ImageUrl,
                    CreatedAt = review.CreatedAt
                };

                return Ok(response);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error occured while creating review for user {UserId}", UserId);
                return StatusCode(500, "An error occurred while creating the review.");
            }
        }

        // Get all reviews
        [HttpGet]
        public async Task<IActionResult> GetReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {   
            try {
                var totalCount = await _context.Reviews.CountAsync();

                var reviews = await _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Reactions)
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var response = new 
                {
                    Items = reviews.Select(r => new ReviewResponseDTO
                    {
                        Id = r.Id,
                        Username = r.User?.UserName ?? "Anonymous",
                        UserId = r.User!.Id,
                        Rating = r.Rating,
                        Title = r.Title,
                        Description = r.Description,
                        ImageUrl = r.ImageUrl,
                        CreatedAt = r.CreatedAt,
                        // reaction counts
                        LikeCount = r.Reactions.Count(rx => rx.ReactionType == ReactionType.Like),
                        DislikeCount = r.Reactions.Count(rx => rx.ReactionType == ReactionType.Dislike),
                        OkayCount = r.Reactions.Count(rx => rx.ReactionType == ReactionType.Okay),
                        // users reaction
                        UserReaction = r.Reactions
                            .FirstOrDefault(x => x.UserId == UserId)?.ReactionType
                    }),
                    Pagination = new
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                        HasPrevious = page > 1,
                        HasNext = page < (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                };
                return Ok(response);
            } catch(Exception ex) {
                _logger.LogError(ex, "An error occurred while retrieving reviews. Page: {Page}, PageSize: {PageSize}", page, pageSize);
                return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
            }
        }

        // Get single review
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReview(int id)
        {   
            try {
                var review = await _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Reactions)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (review == null) return NotFound();

                var response = new ReviewResponseDTO
                {
                    Id = review.Id,
                    Rating = review.Rating,
                    Title = review.Title,
                    Description = review.Description,
                    ImageUrl = review.ImageUrl,
                    CreatedAt = review.CreatedAt,
                    LikeCount = review.Reactions.Count(rx => rx.ReactionType == ReactionType.Like),
                    DislikeCount = review.Reactions.Count(rx => rx.ReactionType == ReactionType.Dislike),
                    OkayCount = review.Reactions.Count(rx => rx.ReactionType == ReactionType.Okay),
                    UserReaction = review.Reactions
                        .FirstOrDefault(x => x.UserId == UserId)?.ReactionType
                };
                return Ok(response);
            } catch(Exception ex) {
                _logger.LogError(ex, "An error occurred while retrieving the review. id: {id}", id);
                return StatusCode(500, "An unexpected error occurred while fetching reviews.");
            }
        }

        // Update a review
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateReview(int id, [FromForm] ReviewDTO updatedReviewDto)
        {       
            try {
                var review = await _context.Reviews.FindAsync(id);
                if (review == null) return NotFound();

                if (review.UserId != UserId) return Forbid();

                review.Title = updatedReviewDto.Title;
                review.Description = updatedReviewDto.Description;
                review.Rating = updatedReviewDto.Rating;

                //image provided
                if (updatedReviewDto.Image != null) {
                    string imageUrl = await _storageService.UploadImageAsync(updatedReviewDto.Image);
                    review.ImageUrl = imageUrl;
                }

                await _context.SaveChangesAsync();

                var response = new ReviewResponseDTO
                {
                    Id = review.Id,
                    Rating = review.Rating,
                    Title = review.Title,
                    Description = review.Description,
                    ImageUrl = review.ImageUrl,
                    CreatedAt = review.CreatedAt
                };
                return Ok(response);
            } catch(Exception ex) {
                _logger.LogError(ex, "Error occurred while updating review with id: {id}", id);
                return StatusCode(500, "An error occurred while updating a review.");
            }
        }

        // Delete a review
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int id)
        {   
            try {
                var review = await _context.Reviews.FindAsync(id);
                if (review == null) return NotFound();

                if (review.UserId != UserId) return Forbid();

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                return NoContent();
            } catch (Exception ex) {
                _logger.LogError(ex, "Error occurred while deleting review with id: {id}", id);
                return StatusCode(500, "An error occurred while deleting a review.");
            }
        }
        
    }
}
