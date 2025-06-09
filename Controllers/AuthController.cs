using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using backend.Helpers;
using backend.DTOs;

namespace backend.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReviewController> _logger;

        public AuthController( UserManager<User> userManager, IConfiguration configuration, ILogger<ReviewController> logger)
        {
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDTO model)
        {   
            try {
                var existingUser = await _userManager.FindByNameAsync(model.Username);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed: Username already taken: {Username}", model.Username);
                    return BadRequest("Username already taken");
                }
                var user = new User { UserName = model.Username };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    return Ok();
                }

                _logger.LogWarning("User registration failed for {Username}: {@Errors}", model.Username, result.Errors);
                return BadRequest(result.Errors);
            } catch (Exception ex) {
                _logger.LogError(ex, "Exception occurred during registration for {Username}", model.Username);
                return StatusCode(500, "An error occurred while registering the user.");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserDTO model)
        {
            try {
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    var token = GenerateJwt.GenerateJwtToken(user, _configuration);
                    return Ok(new { token, user });
                }

                return Unauthorized();
            } catch (Exception ex) {
                _logger.LogError(ex, "Exception occurred during login for {Username}", model.Username);
                return StatusCode(500, "An error occurred while logging in.");
            }
        }



    }
}
