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
    public class ReactionController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ReviewController> _logger;

        public ReactionController(AppDbContext context, ILogger<ReviewController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ReactToReview([FromBody] ReactionDTO reactionDto)
        {
            try {
                // Check for existing reaction
                var existing = await _context.Reactions
                    .FirstOrDefaultAsync(r => r.UserId == UserId && r.ReviewId == reactionDto.ReviewId);

                if (existing != null)
                {
                    // If new reaction is the same as the existing one, remove it
                    if (existing.ReactionType == reactionDto.ReactionType)
                    {
                        _context.Reactions.Remove(existing); 
                    }
                    else
                    {
                        existing.ReactionType = reactionDto.ReactionType; // update the reaction
                    }
                }
                else
                {
                    var user = await _context.Users.FindAsync(UserId);
                    var review = await _context.Reviews.FindAsync(reactionDto.ReviewId);

                    if (user == null || review == null)
                    {
                        return NotFound();
                    }

                    var reaction = new Reaction
                    {
                        UserId = UserId,
                        ReviewId = reactionDto.ReviewId,
                        ReactionType = reactionDto.ReactionType,
                        User = user,
                        Review = review
                    };

                    _context.Reactions.Add(reaction);
                }

                await _context.SaveChangesAsync();
                return Ok();
            } catch (Exception ex) {
                _logger.LogError(ex, "Error reacting to review. UserId: {UserId}, ReviewId: {ReviewId}", UserId, reactionDto.ReviewId);
                return StatusCode(500, "An unexpected error occurred while processing your reaction.");
            }
        }
    }
}
