using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace SecureChatServer.util;

public static class Cryptography {
	public static bool Verify(string text, string signature, RsaKeyParameters publicKey) {
		Console.WriteLine(signature);
		byte[] textBytes = Encoding.UTF8.GetBytes(text);
		byte[] signatureBytes = Convert.FromBase64String(signature);

		ISigner verifier = new RsaDigestSigner(new Sha256Digest());
		verifier.Init(false, publicKey);
		
		verifier.BlockUpdate(textBytes, 0, textBytes.Length);

		return verifier.VerifySignature(signatureBytes);
	}
}