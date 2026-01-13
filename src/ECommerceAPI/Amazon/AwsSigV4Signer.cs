using System.Security.Cryptography;
using System.Text;

namespace ECommerceAPI.Amazon;

public class AwsSigV4Signer
{
	private const string Algorithm = "AWS4-HMAC-SHA256";
	private const string Service = "execute-api";

	public void Sign(
		HttpRequestMessage request,
		string accessKey,
		string secretKey,
		string region)
	{
		var utcNow = DateTime.UtcNow;
		var amzDate = utcNow.ToString("yyyyMMddTHHmmssZ");
		var dateStamp = utcNow.ToString("yyyyMMdd");

		request.Headers.Remove("x-amz-date");
		request.Headers.Add("x-amz-date", amzDate);

		var canonicalRequest = BuildCanonicalRequest(request);
		var credentialScope = $"{dateStamp}/{region}/{Service}/aws4_request";

		var stringToSign = $"{Algorithm}\n" +
						   $"{amzDate}\n" +
						   $"{credentialScope}\n" +
						   Hash(canonicalRequest);

		var signingKey = GetSignatureKey(secretKey, dateStamp, region, Service);
		var signature = ToHexString(HmacSHA256(signingKey, stringToSign));

		var authorizationHeader =
			$"{Algorithm} " +
			$"Credential={accessKey}/{credentialScope}, " +
			$"SignedHeaders={GetSignedHeaders(request)}, " +
			$"Signature={signature}";

		request.Headers.Remove("Authorization");
		request.Headers.Add("Authorization", authorizationHeader);
	}

	// ---------------- HELPERS ----------------

	private static string BuildCanonicalRequest(HttpRequestMessage request)
	{
		var canonicalUri = request.RequestUri!.AbsolutePath;
		var canonicalQueryString = request.RequestUri.Query.TrimStart('?');

		var canonicalHeaders = GetCanonicalHeaders(request);
		var signedHeaders = GetSignedHeaders(request);
		var payloadHash = Hash("");

		return $"{request.Method.Method}\n" +
			   $"{canonicalUri}\n" +
			   $"{canonicalQueryString}\n" +
			   $"{canonicalHeaders}\n\n" +
			   $"{signedHeaders}\n" +
			   $"{payloadHash}";
	}

	private static string GetCanonicalHeaders(HttpRequestMessage request)
	{
		var headers = request.Headers
			.OrderBy(h => h.Key.ToLowerInvariant())
			.Select(h =>
				$"{h.Key.ToLowerInvariant()}:{string.Join(",", h.Value).Trim()}");

		return string.Join("\n", headers);
	}

	private static string GetSignedHeaders(HttpRequestMessage request)
	{
		return string.Join(";", request.Headers
			.Select(h => h.Key.ToLowerInvariant())
			.OrderBy(k => k));
	}

	private static byte[] GetSignatureKey(
		string key, string dateStamp, string region, string service)
	{
		var kDate = HmacSHA256(Encoding.UTF8.GetBytes("AWS4" + key), dateStamp);
		var kRegion = HmacSHA256(kDate, region);
		var kService = HmacSHA256(kRegion, service);
		return HmacSHA256(kService, "aws4_request");
	}

	private static byte[] HmacSHA256(byte[] key, string data)
	{
		using var hmac = new HMACSHA256(key);
		return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
	}

	private static string Hash(string data)
	{
		using var sha256 = SHA256.Create();
		return ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(data)));
	}

	private static string ToHexString(byte[] bytes)
	{
		var sb = new StringBuilder(bytes.Length * 2);
		foreach (var b in bytes)
			sb.AppendFormat("{0:x2}", b);
		return sb.ToString();
	}
}
