namespace ReposteriasManu.Domain.Entities
{
    public class Decoration : Core.BaseEntity
    {
        public string Type { get; set; }
        public string Color { get; set; }
        public string Message { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public Decoration()
        {
        }

        public Decoration(string type, string color, string message, int orderId, int productId)
        {
            Type = type;
            Color = color;
            Message = message;
            OrderId = orderId;
            ProductId = productId;
        }
    }
}