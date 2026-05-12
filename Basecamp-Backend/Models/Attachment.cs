namespace Basecamp_Backend.Models
{
    public class Attachment : BaseEntity
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; } // .pdf, .png və s.

        public int ProjectId { get; set; }
        public Project Project { get; set; }
    }
}
