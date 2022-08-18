using Backend.Models;
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
        public static User user = new User();
        private static List<User> s_users = new List<User>();
        private readonly IConfiguration _configuration;
        public UserController(IConfiguration config)
        {
            _configuration = config;
        }
        [HttpPost]
        [Route("api/create-user/{jsonUser}")]
        public async Task Register(string jsonUser)
        {
            if (!s_isLoaded)
            {
                LoadData();
            }
            UserDto newUser = JsonConvert.DeserializeObject<UserDto>(jsonUser);
            if (string.IsNullOrEmpty(newUser.Username) || string.IsNullOrEmpty(newUser.Password))
                throw new InvalidOperationException("Cant be empty");
            else
            {
                user.Name = newUser.Username;
                CreatePasswordHash(newUser.Password, out byte[] passwordHash, out byte[] passwordSalt);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
                s_users.Add(user);
                string fileName = RecipeController.PathCombine(Environment.CurrentDirectory, @"\Users.json");
                string jsonString = System.Text.Json.JsonSerializer.Serialize(s_users);
                await System.IO.File.WriteAllTextAsync(fileName, jsonString);
                s_isLoaded = false;
            }
        }
        [HttpPost]
        [Route("api/login/{jsonUser}")]
        public async Task<ActionResult> Login(string jsonUser)
        {
            if (!s_isLoaded)
            {
                LoadData();
            }
            UserDto user = JsonConvert.DeserializeObject<UserDto>(jsonUser);
            User loggedUser = s_users.SingleOrDefault(x => x.Name == user.Username);
            if (loggedUser == null)
                return BadRequest("User not found!");
            if (VerifyPassword(user.Password, loggedUser.PasswordHash, loggedUser.PasswordSalt))
            {
                string token = CreateToken(loggedUser);
                var refreshToken = CreateRefreshToken();
                SetRefreshToken(refreshToken, loggedUser.Id);
                string fileName = RecipeController.PathCombine(Environment.CurrentDirectory, @"\Users.json");
                string jsonString = System.Text.Json.JsonSerializer.Serialize(s_users);
                await System.IO.File.WriteAllTextAsync(fileName, jsonString);
                s_isLoaded = false;
                return Ok(token);
            }
            else
                return BadRequest("Wrond password!");
        }
        [HttpPost]
        [Route("api/refresh-token/{id}"),AllowAnonymous]
        public async Task<ActionResult<string>> RefreshToken(Guid id , RefreshToken rT)
        {
            if (!s_isLoaded)
            {
                LoadData();
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
                string fileName = RecipeController.PathCombine(Environment.CurrentDirectory, @"\Users.json");
                string jsonString = System.Text.Json.JsonSerializer.Serialize(s_users);
                await System.IO.File.WriteAllTextAsync(fileName, jsonString);
                s_isLoaded = false;
                return Ok(token);
            }
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
        private void SetRefreshToken(RefreshToken newRT, Guid id)
        {
            if (!s_isLoaded)
            {
                LoadData();
            }
            var cookiesOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = newRT.TimeExpires
            };
            Response.Cookies.Append("refreshToken", newRT.Token, cookiesOptions);
            User loggedUser = s_users.FirstOrDefault(x => x.Id == id);
            loggedUser.RefreshToken = newRT.Token;
            loggedUser.TimeCreated = newRT.TimeCreated;
            loggedUser.TimeExpires = newRT.TimeExpires;
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
        private async void LoadData()
        {
            string fileName = RecipeController.PathCombine(Environment.CurrentDirectory, @"\Users.json");
            string jsonString = await System.IO.File.ReadAllTextAsync(fileName);
            s_users = System.Text.Json.JsonSerializer.Deserialize<List<User>>(jsonString);
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
                expires: DateTime.Now.AddMinutes(1),
                signingCredentials: cred
                );
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }
}
