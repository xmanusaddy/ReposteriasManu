namespace ReposteriasManu.Domain.Entities
{
    public class Product : Core.BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Flavor { get; set; }
        public string Size { get; set; }

        public Product()
        {
        }

        public Product(string name, string description, decimal price, string flavor, string size)
        {
            Name = name;
            Description = description;
            Price = price;
            Flavor = flavor;
            Size = size;
        }
    }
}