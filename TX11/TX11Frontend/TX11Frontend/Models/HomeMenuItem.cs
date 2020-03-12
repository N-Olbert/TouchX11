namespace TX11Frontend.Models
{
    public enum MenuItemType
    {
        Canvas,
        About,
        Settings
    }

    public class HomeMenuItem
    {
        public MenuItemType Id { get; set; }

        public string Title { get; set; }
    }
}