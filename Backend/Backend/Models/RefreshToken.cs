namespace Backend.Models
{
    public class RefreshToken
    {
        public string Token { get; set; } = string.Empty;
        public DateTime TimeCreated { get; set; } = DateTime.Now;
        public DateTime TimeExpires { get; set; }
    }
}
