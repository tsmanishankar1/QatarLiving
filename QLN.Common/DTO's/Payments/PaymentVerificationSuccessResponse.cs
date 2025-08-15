using System.Text.Json.Serialization;

public class PaymentVerificationSuccessResponse
{
    [JsonPropertyName("invoice_url")]
    public string InvoiceUrl { get; set; } = string.Empty;

    [JsonPropertyName("transfer_details")]
    public TransferDetails? TransferDetails { get; set; } 

    [JsonPropertyName("refund_details")]
    public RefundDetails? RefundDetails { get; set; } 

    [JsonPropertyName("exchange_details")]
    public ExchangeDetails? ExchangeDetails { get; set; } 

    [JsonPropertyName("card_details")]
    public CardDetails CardDetails { get; set; } = new();

    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("note")]
    public string Note { get; set; } = string.Empty;

    [JsonPropertyName("payment_date")]
    public string PaymentDate { get; set; } = string.Empty; 

    [JsonPropertyName("payment_status")]
    public string PaymentStatus { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("transaction_id")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty; 

    [JsonPropertyName("auth_code")]
    public string AuthCode { get; set; } = string.Empty;

    [JsonPropertyName("card_token")]
    public string CardToken { get; set; } = string.Empty;

    [JsonPropertyName("client")]
    public ClientInfo Client { get; set; } = new();
}
