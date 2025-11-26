using Microsoft.AspNetCore.Components.Forms;
using System.Linq.Expressions;

namespace CreditCardsSystem.Web.Client.Components
{
    public static class EditContextExtension
    {
        public static void AddAndNotifyFieldError(this EditContext editContext, ValidationMessageStore store, Expression<Func<object>> field, string message, bool notify = true)
        {
            AddAndNotifyFieldError(editContext, store, FieldIdentifier.Create(field), message, notify);
        }

        public static void AddAndNotifyFieldError(this EditContext editContext, ValidationMessageStore store, FieldIdentifier fieldIdentifier, string message, bool notify = true)
        {
            if (store![fieldIdentifier].Any(x => x == message)) return;
            store.Add(fieldIdentifier, message);
            if (notify)
                editContext.NotifyValidationStateChanged();
        }
    }
}
