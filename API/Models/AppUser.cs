namespace API.Models
{
    public class AppUser
    {
        public string Name { get; set; }
        public string Login { get; set; }
        public string PassHash { get; set; }
        public string Salt { get; set; }
    }
}
