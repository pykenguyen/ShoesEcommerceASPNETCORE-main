namespace ShoesEcommerce.Models.Orders
{
    public class Payment
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        public string Method { get; set; }      // e.g. COD, CreditCard, VNPay, PayPal
        public string Status { get; set; }      // e.g. Pending, Paid, Failed, Cancelled
        public DateTime? PaidAt { get; set; }
        public string? TransactionId { get; set; }  // PayPal transaction ID, VNPay transaction number, etc.
    }
}
