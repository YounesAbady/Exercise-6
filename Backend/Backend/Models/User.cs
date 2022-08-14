using Newtonsoft.Json;

namespace Backend.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public User()
        {
            Id = Guid.NewGuid();
        }
    }
}
