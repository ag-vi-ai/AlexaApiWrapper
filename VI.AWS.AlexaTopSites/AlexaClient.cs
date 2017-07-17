using RestSharp;
using RestSharp.Deserializers;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using VI.AWS.AlexaTopSites.ResponseModel;

namespace VI.AWS.AlexaTopSites
{
    public enum CategoryListingsSortBy
	{
		Popularity,
		Title,
		AverageReview
	}

    public class AlexaClient
    {
        const string BaseUrl = "http://awis.amazonaws.com";

        const string TimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		const string DateFormatForUrlInfo = "dd-MMM-yyyy";
		const string DateFormat = "yyyy-MM-dd";

        private readonly string _key;
        private readonly string _secretKey;

		/// <summary>
		/// Creates Alexa API client
		/// </summary>
		/// <param name="key">Access key Id</param>
		/// <param name="secretKey">Secret key</param>
        public AlexaClient(string key, string secretKey)
        {
            _key = key;
            _secretKey = secretKey;
        }

		/// <summary>
		/// Detailed information about error occured during last invoking
		/// <para>Alexa API methods. If you recieve an exception while invoking </para>
		/// API then it make sense to check this property for more detailed information
		/// </summary>
		public AlexaError LastError { get; private set; }

        private T Execute<T>(RestRequest request) where T : new()
        {
			LastError = null;
            var client = new RestClient(BaseUrl);
            var response = client.Execute<T>(request);

            if (response.ErrorException != null)
            {
                throw response.ErrorException;
            }

			if (response.StatusCode != HttpStatusCode.OK)
			{
				throw new WebException(
					string.Format("Alexa Error. Status code: {0}; description: {1}. Please review AlexaClient.LastError property for detailed information.",
					response.StatusCode, response.StatusDescription));
			}

            return response.Data;
        }

		/// <summary>
		/// The UrlInfo action provides information about a website, such as:
		/// <para> - how popular the site is;</para> 
		/// <para> - what sites are related; </para> 
		/// <para> - contact information for the owner of the site. </para> 
		/// To learn more about Alexa traffic data please see explanation on the Alexa website
		/// </summary>
		/// <param name="url">Any valid URL.</param>
		/// <param name="responseGroup">Could be combined as a Flags: 
		/// <para><c>UrlInfoResponseGroup.RelatedLinks | UrlInfoResponseGroup.Categories</c></para></param>
		/// <returns><c>UrlInfoResponse</c> POCO object</returns>
        public UrlInfoResponse GetUrlInfo(string url, UrlInfoResponseGroup responseGroup)
        {
            var request = new RestRequest(Method.GET);
			request.DateFormat = DateFormatForUrlInfo;
            var queryBuilder = new QueryBuilder(request)
            {
                {"Action", "UrlInfo"},
                {"AWSAccessKeyId", _key},
                {"SignatureMethod", "HmacSHA256"},
                {"SignatureVersion", "2"},
                {"Timestamp", Timestamp},
                {"Url", url},
                {"ResponseGroup", responseGroup.ToQuery()}
            };
            request.AddParameter("Signature", Signature(queryBuilder.Query));
			request.OnBeforeDeserialization = ErrorHandling;

            return Execute<UrlInfoResponse>(request);
        }

		/// <summary>
		/// The TrafficHistory action returns the daily Alexa Traffic Rank, Reach per Million Users, 
		/// <para>and Unique Page Views per Million Users for each day since August 2007. </para>
		/// <para>The same data is used to produce the traffic graphs found on alexa.com. </para>
		/// </summary>
		/// <param name="url">Any valid URL.</param>
		/// <param name="range">Number of days to return. 
		/// <para>Note that the response document may contain fewer results than this maximum if data is not available. </para>
		/// Default value is '31'. Maximum value is '31'.</param>
		/// <param name="start">Start date for results. The first start available date is 20070801 (August 1, 2007).</param>
		/// <returns>TrafficHistoryResponse POCO object</returns>
		public TrafficHistoryResponse GetTrafficHistory(string url, int? range = 31, DateTime? start = null)
		{
			var request = new RestRequest(Method.GET);
			request.DateFormat = DateFormat;
			var queryBuilder = new QueryBuilder(request);
			queryBuilder.Add("Action", "TrafficHistory");
			queryBuilder.Add("AWSAccessKeyId", _key);
			queryBuilder.Add("SignatureMethod", "HmacSHA256");
			queryBuilder.Add("SignatureVersion", "2");
			queryBuilder.Add("Timestamp", Timestamp);
			queryBuilder.Add("ResponseGroup", "History");
			queryBuilder.Add("Url", url);
			queryBuilder.Add("Range", range);
			queryBuilder.Add("Start", start.HasValue ? ((DateTime)start).ToString("yyyyMMdd") : null);
			request.AddParameter("Signature", Signature(queryBuilder.Query));
			request.OnBeforeDeserialization = ErrorHandling;

			return Execute<TrafficHistoryResponse>(request);
		}

        /// <summary>
        /// The CategoryBrowse action and CategoryListings actions together provide a directory service 
        /// <para> based on the Open Directory, www.dmoz.org, and enhanced with Alexa traffic data. </para>
        /// <para> For any given category, the CategoryBrowse action returns a list of sub-categories. </para>
        /// <para> Within a particular category you can use the CategoryListings action to get the documents within that category ordered by traffic. </para>
        /// </summary>
        /// <param name="responseGroup">Could be combined as a Flags: 
        /// <para>CategoryBrowseResponseGroup.Categories | CategoryBrowseResponseGroup.LanguageCategories</para></param>
        /// <param name="path">Valid category path ("Top/Games/Board_Games"). Important: 
        /// <para>Path parameter shouldn't contain space characters. </para>
        /// <para>Space inside category name should be replaced with underscore.</para></param>
        /// <param name="descriptions">Whether to return descriptions with categories</param>
        /// <returns>CategoryBrowseResponse POCO object</returns>
        public CategoryBrowseResponse GetCategoryBrowse(
			CategoryBrowseResponseGroup responseGroup, 
			string path, 
			bool? descriptions = null)
		{
			if (path.Contains(" "))
				throw new ArgumentException("Path parameter shouldn't contain space characters. Space inside category name should be replaced with underscore.");

			var request = new RestRequest(Method.GET);
		    var queryBuilder = new QueryBuilder(request)
		    {
		        {"Action", "CategoryBrowse"},
		        {"AWSAccessKeyId", _key},
		        {"SignatureMethod", "HmacSHA256"},
		        {"SignatureVersion", "2"},
		        {"Timestamp", Timestamp},
		        {"ResponseGroup", responseGroup.ToQuery()},
		        {"Path", path},
		        {"Descriptions", descriptions}
		    };
		    request.AddParameter("Signature", Signature(queryBuilder.Query));

			request.OnBeforeDeserialization = ErrorHandling;
			return Execute<CategoryBrowseResponse>(request);
			
		}

		/// <summary>
		/// The CategoryListings action is a directory service based on the Open Directory, www.dmoz.org. 
		/// For any given category, it returns a list of site listings contained within that category.
		/// </summary>
		/// <param name="path">Valid category path ("Top/Games/Board_Games"). Important: 
		/// Path parameter shouldn't contain space characters. 
		/// Space inside category name should be replaced with underscore.</param>
		/// <param name="sortBy">How to sort the results returned by this service</param>
		/// <param name="recursive">Whether to return listings for the current category only, or for the current category plus all subcategories</param>
		/// <param name="start">1-based index of result at which to start. Note: An empty document will be returned if this value exceeds the total number of available results.</param>
		/// <param name="count">Number of results to return for this request, beginning from specified Start number (maximum 20)</param>
		/// <param name="descriptions">Whether to return descriptions with categories</param>
		/// <returns>CategoryListingsResponse POCO object</returns>
		public CategoryListingsResponse GetCategoryListings(
			string path, 
			CategoryListingsSortBy? sortBy = null, 
			bool? recursive = null,
			int? start = null,
			int? count = null,
			bool? descriptions = null)
		{
			if (path.Contains(" "))
				throw new ArgumentException("Path parameter shouldn't contain space characters. Space inside category name should be replaced with underscore.");

			var sortByString = sortBy.HasValue ? sortBy.ToString() : null; 
			var request = new RestRequest(Method.GET);

		    var queryBuilder = new QueryBuilder(request)
		    {
		        {"Action", "CategoryListings"},
		        {"AWSAccessKeyId", _key},
		        {"SignatureMethod", "HmacSHA256"},
		        {"SignatureVersion", "2"},
		        {"Timestamp", Timestamp},
		        {"ResponseGroup", "Listings"},
		        {"Path", path},
		        {"SortBy", sortByString},
		        {"Recursive", recursive},
		        {"Count", count},
		        {"Start", start},
		        {"Descriptions", descriptions}
		    };
		    request.AddParameter("Signature", Signature(queryBuilder.Query));
			request.OnBeforeDeserialization = ErrorHandling;

			return Execute<CategoryListingsResponse>(request);
		}

		/// <summary>
		/// The SitesLinkingIn action returns a list of web sites linking to a given web site. 
		/// <para>Within each domain linking into the web site, only a single link - the one with </para>
		/// the highest page-level traffic - is returned.
		/// </summary>
		/// <param name="url">Any valid URL.</param>
		/// <param name="count">Maximum number of results per page to return. 
		/// <para>Note that the response document may contain fewer results than this maximum. </para>
		/// Default value is '10' (maximum 20).</param>
		/// <param name="start">Number of result at which to start. Used for paging through results. Default value is '0.'</param>
		/// <returns>SitesLinkingInResponse POCO object</returns>
		public SitesLinkingInResponse GetSitesLinkingIn(string url, int count = 10, int start = 0)
        {
            var request = new RestRequest(Method.GET);

            var queryBuilder = new QueryBuilder(request)
            {
                {"Action", "SitesLinkingIn"},
                {"AWSAccessKeyId", _key},
                {"SignatureMethod", "HmacSHA256"},
                {"SignatureVersion", "2"},
                {"Timestamp", Timestamp},
                {"Url", url},
                {"ResponseGroup", "SitesLinkingIn"},
                {"Count", count},
                {"Start", start}
            };

            request.AddParameter("Signature", Signature(queryBuilder.Query));
			request.OnBeforeDeserialization = ErrorHandling;

			return Execute<SitesLinkingInResponse>(request);
        }

		private void ErrorHandling(IRestResponse response)
		{
			var xml = new XmlDeserializer();
			var output = xml.Deserialize<AlexaError>(response);
			if (output != null && output.Errors.Count > 0 &&  !string.IsNullOrWhiteSpace(output.RequestID.Value))
				LastError = output;
		}

        private string Timestamp
        {
            get
            {
                return DateTime.UtcNow.ToString(TimeFormat, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private string Signature(string query)
        {
			const string ToSignFormat = "GET\nawis.amazonaws.com\n/\n{0}";
			var toSign = string.Format(ToSignFormat, query);

			// create the hash object
			var shaSignature = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));

			// calculate the hash
			var binarySignature = shaSignature.ComputeHash(Encoding.UTF8.GetBytes(toSign));

			// convert to hex
			var signature = Convert.ToBase64String(binarySignature);

			return signature;
        }
    }
}
