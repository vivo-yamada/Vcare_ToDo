namespace VcareTodo.Models.ViewModels
{
    public class TodoViewModel
    {
        public List<TodoTask> UnscheduledTasks { get; set; } = new List<TodoTask>();
        public List<TodoTask> ScheduledTasks { get; set; } = new List<TodoTask>();
        public List<Employee> Employees { get; set; } = new List<Employee>();
        public List<WorkCategory> WorkCategories { get; set; } = new List<WorkCategory>();
        public List<ReportCategory> ReportCategories { get; set; } = new List<ReportCategory>();
        public string? SelectedUserCode { get; set; }
        public string CurrentUserCode { get; set; } = string.Empty;
        public string CurrentUserName { get; set; } = string.Empty;
        public string ViewMode { get; set; } = "week";
        public DateTime CurrentDate { get; set; } = DateTime.Today;
    }
}