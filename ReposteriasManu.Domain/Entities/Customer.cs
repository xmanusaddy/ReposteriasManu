namespace ReposteriasManu.Domain.Entities
{
    public class Customer : Core.Person
    {
        public string Address { get; set; }
        public ICollection<Order> Orders { get; set; }

        public Customer()
        {
            Orders = new List<Order>();
        }

        public Customer(string name, string lastName, string phone, string email, string address)
        {
            Name = name;
            LastName = lastName;
            Phone = phone;
            Email = email;
            Address = address;
            Orders = new List<Order>();
        }
    }
}