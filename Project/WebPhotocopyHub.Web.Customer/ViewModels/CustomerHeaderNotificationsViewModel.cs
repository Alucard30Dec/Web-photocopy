namespace WebPhotocopyHub.Web.Customer.Models;

public sealed class CustomerHeaderNotificationsViewModel
{
    public int AttentionCount { get; set; }
    public string AllNotificationsUrl { get; set; } = string.Empty;
    public List<CustomerHeaderNotificationItemViewModel> Items { get; set; } = new();
}

public sealed class CustomerHeaderNotificationItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = "notifications";
    public string Tone { get; set; } = "blue";
    public bool NeedsAttention { get; set; }
    public DateTime SortTime { get; set; }
}
