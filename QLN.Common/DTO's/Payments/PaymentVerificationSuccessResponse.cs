public class PaymentVerificationSuccessResponse
{
    public string OrderId { get; set; }
    public string TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public ClientInfo Client { get; set; }
    public string? Note { get; set; }
    public string PaymentDate { get; set; } // Format: YYYY-MM-DDThh:mm:ss:ms
    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMode Mode { get; set; }
    public string AuthCode { get; set; }
    public ExchangeDetails? ExchangeDetails { get; set; }
    public TransferDetails? TransferDetails { get; set; }
    public string? CardToken { get; set; }
    public RefundDetails? RefundDetails { get; set; }
    public CardDetails CardDetails { get; set; }
}
