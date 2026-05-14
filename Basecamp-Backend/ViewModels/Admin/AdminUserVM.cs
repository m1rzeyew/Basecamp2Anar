namespace Basecamp_Backend.ViewModels.Admin
{
    public class AdminUserVM
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool IsAdmin { get; set; }
    }
}
