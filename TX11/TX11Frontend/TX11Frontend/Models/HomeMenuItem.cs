namespace TX11Frontend.Models
{
    public enum MenuItemType
    {
        Canvas,
        About
    }

    public class HomeMenuItem
    {
        public MenuItemType Id { get; set; }

        public string Title { get; set; }
    }
}