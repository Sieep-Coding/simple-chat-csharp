namespace CSharpStream.Models
{
    public class Message(string content, string sender)
    {
        public int Id { get; set; }
        public string Content { get; set; } = content;
        public string Sender { get; set; } = sender;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}