using System.Security.Claims;
using backend.Models;
using backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ReviewController> _logger;

        public ProfileController(AppDbContext context, ILogger<ReviewController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetProfile([FromQuery] string? id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {   
            try {
                // use provided id or fallback to current user
                var userId = string.IsNullOrEmpty(id) 
                    ? User.FindFirstValue(ClaimTypes.NameIdentifier)! 
                    : id;

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                // total review count for user
                var totalReviewCount = await _context.Reviews
                    .Where(r => r.UserId == userId)
                    .CountAsync();

                var reviews = await _context.Reviews
                    .Where(r => r.UserId == userId)
                    .Include(r => r.Reactions)
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var reviewDtos = reviews.Select(r => new ReviewResponseDTO
                {
                    Id = r.Id,
                    Title = r.Title,
                    Description = r.Description,
                    ImageUrl = r.ImageUrl,
                    Rating = r.Rating,
                    LikeCount = r.Reactions.Count(rx => rx.ReactionType == ReactionType.Like),
                    DislikeCount = r.Reactions.Count(rx => rx.ReactionType == ReactionType.Dislike),
                    OkayCount = r.Reactions.Count(rx => rx.ReactionType == ReactionType.Okay),
                    CreatedAt = r.CreatedAt,
                    UserReaction = r.Reactions.FirstOrDefault()?.ReactionType
                }).ToList();

                var profileDto = new 
                {
                    Username = user.UserName ?? "Anonymous",
                    Reviews = reviewDtos,
                    Pagination = new
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalCount = totalReviewCount,
                        TotalPages = (int)Math.Ceiling(totalReviewCount / (double)pageSize),
                        HasPrevious = page > 1,
                        HasNext = page < (int)Math.Ceiling(totalReviewCount / (double)pageSize)
                    }
                };

                return Ok(profileDto);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error fetching profile. user id: {id}", id);
                return StatusCode(500, "An unexpected error occurred while fetching this profile.");
            }
        }

    }
}
