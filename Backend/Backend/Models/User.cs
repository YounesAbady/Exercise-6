using Newtonsoft.Json;

namespace Backend.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PasswordHash { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TimeCreated { get; set; }
        public DateTime TimeExpires { get; set; }
        public User()
        {
            Id = Guid.NewGuid();
        }
    }
}
