namespace FitControlWeb.ViewModels.Shared;

public class CalendarEventViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public object ExtendedProps { get; set; } = new();
}
