using Microsoft.Extensions.Options;
using System.Web;

namespace ECommerceAPI.Amazon
{
	public class AmazonOAuthService
	{
		private readonly AmazonSettings _settings;
		private const string AuthorizationEndpoint = "https://sellercentral.amazon.in/apps/authorize/consent";

		public AmazonOAuthService(IOptions<AmazonSettings> settings)
		{
			_settings = settings.Value;
		}

		public string GetAuthorizationUrl(string state)
		{
			var queryParams = HttpUtility.ParseQueryString(string.Empty);
			queryParams["application_id"] = _settings.ApplicationId;
			queryParams["state"] = state;
			queryParams["version"] = "beta";

			return $"{AuthorizationEndpoint}?{queryParams}";
		}
	}
}