using System.Globalization;
using System.Linq;
using Xamarin.Forms;

namespace TX11Frontend.Models
{
    public class DecimalInputBehaviour : Behavior<Entry>
    {
        protected override void OnAttachedTo(Entry entry)
        {
            if (entry != null)
            {
                entry.TextChanged += OnTextChanged;
            }

            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            if (entry != null)
            {
                entry.TextChanged -= OnTextChanged;
            }

            base.OnDetachingFrom(entry);
        }

        private static void OnTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args?.NewTextValue) && sender is Entry entry)
            {
                bool isValid = decimal.TryParse(args.NewTextValue, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out decimal dummy);                 
                entry.Text = isValid ? args.NewTextValue : args.OldTextValue;
            }
        }
    }
}