using Backend.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Backend.Controllers
{
    public class UserController : Controller
    {
        private static bool s_isLoaded = false;
        private static List<User> s_users = new List<User>();
        private readonly IConfiguration _configuration;
        private readonly IAntiforgery _antiforgory;
        public UserController(IConfiguration config, IAntiforgery antiforgery)
        {
            _configuration = config;
            _antiforgory = antiforgery;
        }
        [HttpPost]
        [Route("api/create-user")]
        public async Task<ActionResult> Register([FromBody] UserDto newUser)
        {
            if (!s_isLoaded)
            {
                await LoadData();
            }
            User user = s_users.FirstOrDefault(x => x.Name == newUser.Username);
            if (string.IsNullOrEmpty(newUser.Username) || string.IsNullOrEmpty(newUser.Password))
                return BadRequest("Cant be empty");
            else if (user != null)
            {
                return BadRequest("Username already taken!");
            }
            else
            {
                user = new User();
                user.Name = newUser.Username;
                CreatePasswordHash(newUser.Password, out byte[] passwordHash, out byte[] passwordSalt);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
                s_users.Add(user);
                await SaveData();
                return Ok();
            }
        }
        [HttpPost]
        [Route("api/login")]
        public async Task<ActionResult> Login([FromBody] UserDto user)
        {
            //await _antiforgory.ValidateRequestAsync(HttpContext);
            if (!s_isLoaded)
            {
                await LoadData();
            }
            User loggedUser = s_users.SingleOrDefault(x => x.Name == user.Username);
            if (loggedUser == null)
                return BadRequest("User not found!");
            if (VerifyPassword(user.Password, loggedUser.PasswordHash, loggedUser.PasswordSalt))
            {
                string token = CreateToken(loggedUser);
                var refreshToken = CreateRefreshToken();
                SetRefreshToken(refreshToken, loggedUser.Id);
                await SaveData();
                return Ok(token);
            }
            else
                return BadRequest("Wrond password!");
        }
        [HttpPost]
        [Route("api/refresh-token/{id}"), AllowAnonymous]
        public async Task<ActionResult<string>> RefreshToken([FromBody] string rT, Guid id)
        {
            if (!s_isLoaded)
            {
                await LoadData();
            }
            var refreshToken = rT;
            User loggedUser = s_users.FirstOrDefault(user => user.Id == id);
            if (!loggedUser.RefreshToken.Equals(refreshToken))
                return Unauthorized("Invalid refresh token");
            else if (loggedUser.TimeExpires < DateTime.Now)
                return Unauthorized("Token expired");
            else
            {
                string token = CreateToken(loggedUser);
                var newRT = CreateRefreshToken();
                SetRefreshToken(newRT, loggedUser.Id);
                await SaveData();
                return Ok(token);
            }
        }
        [HttpPost]
        [Route("api/get-rt")]
        public async Task<ActionResult<RefreshToken>> GetRefreshToken([FromBody] UserDto user)
        {
            if (!s_isLoaded)
            {
                await LoadData();
            }
            User loggedUser = s_users.SingleOrDefault(x => x.Name == user.Username);
            if (loggedUser == null)
                return BadRequest("User not found!");
            if (VerifyPassword(user.Password, loggedUser.PasswordHash, loggedUser.PasswordSalt))
            {
                return Ok(loggedUser.RefreshToken);
            }
            else
                return BadRequest("Invalid user data!");
        }
        [HttpPost]
        [Route("api/get-id")]
        public async Task<ActionResult<RefreshToken>> GetUserId([FromBody] UserDto user)
        {
            if (!s_isLoaded)
            {
                await LoadData();
            }
            User loggedUser = s_users.SingleOrDefault(x => x.Name == user.Username);
            if (loggedUser == null)
                return BadRequest("User not found!");
            if (VerifyPassword(user.Password, loggedUser.PasswordHash, loggedUser.PasswordSalt))
            {
                return Ok(loggedUser.Id);
            }
            else
                return BadRequest("Invalid user data!");
        }
        private RefreshToken CreateRefreshToken()
        {
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                TimeExpires = DateTime.Now.AddDays(1),
                TimeCreated = DateTime.Now
            };
            return refreshToken;
        }
        async private void SetRefreshToken(RefreshToken newRT, Guid id)
        {
            if (!s_isLoaded)
            {
                await LoadData();
            }
            var cookiesOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = newRT.TimeExpires
            };
            User loggedUser = s_users.FirstOrDefault(x => x.Id == id);
            loggedUser.RefreshToken = newRT.Token;
            loggedUser.TimeCreated = newRT.TimeCreated;
            loggedUser.TimeExpires = newRT.TimeExpires;
            await SaveData();
        }
        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(ASCIIEncoding.UTF8.GetBytes(password));
            }
        }
        private bool VerifyPassword(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(ASCIIEncoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(passwordHash);
            }
        }
        private async Task LoadData()
        {
            while (!s_isLoaded)
            {
                try
                {
                    string fileName = RecipeController.PathCombine(Environment.CurrentDirectory, @"\Users.json");
                    string jsonString = await System.IO.File.ReadAllTextAsync(fileName);
                    s_users = System.Text.Json.JsonSerializer.Deserialize<List<User>>(jsonString);
                    s_isLoaded = true;
                }
                catch
                {
                    Thread.Sleep(100);
                }
            }
        }
        private async Task SaveData()
        {
            while (true)
            {
                try
                {
                    string fileName = RecipeController.PathCombine(Environment.CurrentDirectory, @"\Users.json");
                    string jsonString = System.Text.Json.JsonSerializer.Serialize(s_users);
                    await System.IO.File.WriteAllTextAsync(fileName, jsonString);
                    s_isLoaded = false;
                    break;
                }
                catch
                {
                    Thread.Sleep(100);
                }
            }
        }
        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString())
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("Token").Value));
            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddMinutes(2),
                signingCredentials: cred,
                issuer: "Younes Abady",
                audience: "https://localhost:7024/"
                );
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }
}
