using System.Net;
using Org.BouncyCastle.Crypto.Parameters;

namespace SecureChatServer.util;

public class VoiceChats {
	private class VoiceConnection {
		public required RsaKeyParameters PersonalKey;
		public required RsaKeyParameters ForeignKey;
	}
	
	private static readonly Dictionary<IPEndPoint, VoiceConnection> Connections = new ();
	private static readonly Dictionary<RsaKeyParameters, IPEndPoint> Addresses = new ();

	public static void Add(IPEndPoint endPoint, RsaKeyParameters personalKey, RsaKeyParameters foreignKey) {
		Connections[endPoint] = new VoiceConnection { PersonalKey = personalKey, ForeignKey = foreignKey };
		Addresses[personalKey] = endPoint;
	}

	public static void AddForeignEndpoint(IPEndPoint endPoint, RsaKeyParameters foreignKey) => Addresses[foreignKey] = endPoint;
	
	public static bool Exists(RsaKeyParameters key) => Addresses.ContainsKey(key);

	public static bool TryGet(IPEndPoint personalEndPoint, out IPEndPoint? foreignEndPoint) {
		if (Connections.TryGetValue(personalEndPoint, out VoiceConnection? voiceConnection)) {
			if (Addresses.TryGetValue(voiceConnection.ForeignKey, out IPEndPoint? address)) {
				foreignEndPoint = address;
				return true;
			}
		}

		foreignEndPoint = null;
		return false;
	}
}