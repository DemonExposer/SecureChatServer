namespace SecureChatServer;

public class Message {
    public long Id { get; set; }

	public User Sender { get; set; }

	public User Receiver {  get; set; }
    
	public string Text { get; set; }

	public string SenderEncryptedKey { get; set; }

	public string ReceiverEncryptedKey { get; set; }
	
	public bool IsRead { get; set; }

	public string Signature { get; set; }
	
	public DateTime DateTime { get; set; }
	
	public long Timestamp { get; set; }
}