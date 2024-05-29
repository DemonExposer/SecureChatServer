namespace SecureChatServer;

public class Message {
	public User Sender { get; set; }

	public User Receiver {  get; set; }
    
	public string Text { get; set; }

	public string SenderEncryptedKey {  get; set; }

	public string ReceiverEncryptedKey {  get; set; }

	public string Signature {  get; set; }
	
	public DateTime DateTime { get; set; }
}