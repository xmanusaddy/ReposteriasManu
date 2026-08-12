namespace ReposteriasManu.Domain.Entities
{
    public class Order : Core.BaseEntity
    {
        public DateTime OrderDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }
        public ICollection<Decoration> Decorations { get; set; }

        public Order()
        {
            Decorations = new List<Decoration>();
        }

        public Order(DateTime orderDate, DateTime deliveryDate, string status, string notes, int customerId)
        {
            OrderDate = orderDate;
            DeliveryDate = deliveryDate;
            Status = status;
            Notes = notes;
            CustomerId = customerId;
            Decorations = new List<Decoration>();
        }
    }
}