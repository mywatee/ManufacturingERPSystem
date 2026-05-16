using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ManufacturingERP.Core;

public class InvoiceCreatedMessage : ValueChangedMessage<string>
{
    public InvoiceCreatedMessage(string value) : base(value)
    {
    }
}
