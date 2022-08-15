using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Backend.Controllers
{
    public class UserController : Controller
    {
        public static User user = new User();
        private static List<User> s_users = new List<User>();
        public UserController()
        {
            string fileName = RecipeController.PathCombine(Environment.CurrentDirectory, @"\Users.json");
            string jsonString = System.IO.File.ReadAllText(fileName);
            s_users = System.Text.Json.JsonSerializer.Deserialize<List<User>>(jsonString);
        }
        [HttpPost]
        [Route("api/create-user/{jsonUser}")]
        public async Task Register(string jsonUser)
        {
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
                System.IO.File.WriteAllText(fileName, jsonString);
            }
        }
        [HttpPost]
        [Route("api/login/{jsonUser}")]
        public async Task<ActionResult> Login(string jsonUser)
        {
            UserDto user = JsonConvert.DeserializeObject<UserDto>(jsonUser);
            User loggedUser = s_users.SingleOrDefault(x => x.Name == user.Username);
            if (loggedUser == null)
                return BadRequest("User not found!");
            if (VerifyPassword(user.Password, loggedUser.PasswordHash, loggedUser.PasswordSalt))
                return Ok(loggedUser);
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
    }
}
