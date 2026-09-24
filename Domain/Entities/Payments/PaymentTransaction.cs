using Common;
using Domain.Entities.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Payments
{
    [Table("payment_transactions")]
    public class PaymentTransaction : BaseEntity
    {
        public string OrderId { get; set; }
        public long Amount { get; set; }
        public string TerminalId { get; set; }
        public string? MerchantId { get; set; }
        public string? GatewayToken { get; set; }
        public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Created;
        public string? ResultDescription { get; set; }
        public string? RetrievalReferenceNumber { get; set; }
        public string? SystemTraceAuditNumber { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public long? BasketId { get; set; }
    }
}
