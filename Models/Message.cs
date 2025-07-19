namespace CSharpStream.Models
{
    public class Message
    {
        private static int _lastId = 0;

        public int Id { get; private set; }
        public string Content { get; set; }
        public string Sender { get; set; }
        public DateTime Timestamp { get; set; }

        public Message(string content, string sender)
        {
            Id = System.Threading.Interlocked.Increment(ref _lastId);
            Content = content;
            Sender = sender;
            Timestamp = DateTime.Now;
        }
    }
}