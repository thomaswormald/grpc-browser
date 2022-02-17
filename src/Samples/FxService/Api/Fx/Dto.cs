using System.Collections.Immutable;
using System.Runtime.Serialization;

[DataContract]
public class FxRateRequest
{
    [DataMember(Order = 1)]
    public string FromCurrency { get; set; }

    [DataMember(Order = 2)]
    public string ToCurrency { get; set; }

    [DataMember(Order = 3)]
    public bool Show { get; set; }
}

[DataContract]
public class FxRateUpdate
{
    [DataMember(Order = 1)]
    public string FromCurrency { get; set; }

    [DataMember(Order = 2)]
    public string ToCurrency { get; set; }

    [DataMember(Order = 3)]
    public decimal ConversionRate { get; set; }

    [DataMember(Order = 4)]
    public string Timestamp { get; set; }
}

[DataContract]
public class FxOrder
{
    [DataMember(Order = 1)]
    public string FromCurrency { get; set; }

    [DataMember(Order = 2)]
    public string ToCurrency { get; set; }

    [DataMember(Order = 3)]
    public decimal MinimumAcceptableConversionRate { get; set; }

    [DataMember(Order = 4)]
    public decimal AmountInFromCurrency { get; set; }
}

[DataContract]
public class FxOrderResult
{
    [DataMember(Order = 1)]
    public bool Success { get; set; }

    [DataMember(Order = 2)]
    public string? ErrorMessage { get; set; }

    [DataMember(Order = 3)]
    public Guid? OrderId { get; set; }
}

[DataContract]
public class SetFxRateRequest
{
    [DataMember(Order = 1)]
    public string FromCurrency { get; set; }

    [DataMember(Order = 2)]
    public string ToCurrency { get; set; }

    [DataMember(Order = 3)]
    public decimal ConversionRate { get; set; }
}

[DataContract]
public class SetFxRateResult
{
    public ImmutableDictionary<(string, string), decimal> FxRates { get; set; }
}