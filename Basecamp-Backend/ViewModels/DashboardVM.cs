using Basecamp_Backend.Models;

namespace Basecamp_Backend.ViewModels
{
    public class DashboardVM
    {
        public List<Project> AllProjects { get; set; }
        public List<Project> CreatedByMe { get; set; }
        public List<Project> SharedWithMe { get; set; }
    }
}
