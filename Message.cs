namespace SecureChatServer;

public class Message {
	public User User { get; set; }
    
	public string Text { get; set; }
	
	public DateTime DateTime { get; set; }
}