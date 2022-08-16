using Backend.Models;
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
                return Ok(token);
            }
            else
                return BadRequest("Wrond password!");
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
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: cred
                );
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }
}
