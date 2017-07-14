using RestSharp;
using RestSharp.Deserializers;
using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using VI.AWS.AlexaTopSites.ResponseModel;
using Xunit;

namespace VI.AWS.AlexaTopSites.Tests
{
    public class ServiceCommunicationTest
    {
        private readonly string _alexaKey;
        private readonly string _alexaSecretKey;

        public ServiceCommunicationTest()
        {
            _alexaKey = Environment.GetEnvironmentVariable("AWS_ALEXA_KEY");
            _alexaSecretKey = Environment.GetEnvironmentVariable("AWS_ALEXA_SECRET_KEY");
        }

        private readonly Type[] _typesToCompare = new[]
        {
            typeof(int), typeof(double), typeof(string), typeof(DateTime),
            typeof(int?), typeof(double?), typeof(DateTime?)
        };

        private static string PathFor(string sampleFile)
        {
            var typeInfo = typeof(ServiceCommunicationTest).GetTypeInfo();
            var sampleDataPath = Path.Combine(GetParentDirectory(typeInfo.Assembly.Location, 4), "Sample.TestData");
            return Path.Combine(sampleDataPath, sampleFile);
        }

        private static string GetParentDirectory(string path, int i)
        {
            i--;
            var parent = Directory.GetParent(path);
            path = parent.FullName;

            if (i != 0)
            {
                return GetParentDirectory(path, i);
            }

            return path;

        }

        private T LoadSample<T>(string filename)
        {
            var xmlpath = PathFor(filename);
            var doc = XDocument.Load(xmlpath);

            var xml = new XmlDeserializer();
            var output = xml.Deserialize<T>(new RestResponse { Content = doc.ToString() });
            return output;
        }

        private void TestTemplate<T>(string filename, Func<T> endpointMethod)
        {
            try
            {
                var expected = LoadSample<T>(filename);
                var actual = endpointMethod();
                Assert.Equal(expected, actual);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Fact]
        public void UrlInfoMS()
        {
            var client = new AlexaClient(_alexaKey, _alexaSecretKey);
            TestTemplate<UrlInfoResponse>("UrlInfo-microsoft.xml", () => client.GetUrlInfo("http://microsoft.com", UrlInfoResponseGroup.All));
        }

        [Fact]
        public void TrafficHistoryYahoo()
        {
            var client = new AlexaClient(_alexaKey, _alexaSecretKey);
            TestTemplate<TrafficHistoryResponse>("TrafficHistory-yahoo.xml",
                () => client.GetTrafficHistory("http://yahoo.com", 31, new DateTime(2007, 8, 1)));
        }

        [Fact]
        public void CategoryBrowseBoard_Games()
        {
            var client = new AlexaClient(_alexaKey, _alexaSecretKey);
            TestTemplate<CategoryBrowseResponse>("CategoryBrowse_Board_Games.xml",
                () => client.GetCategoryBrowse(CategoryBrowseResponseGroup.All, "Top/Games/Board_Games"));
        }

        [Fact]
        public void CategoryListingsTop_Arts_Literature_Authors()
        {
            var client = new AlexaClient(_alexaKey, _alexaSecretKey);
            TestTemplate<CategoryListingsResponse>("CategoryListings_Top_Arts_Literature_Authors.xml",
                () => client.GetCategoryListings("Top/Arts/Literature/Authors", CategoryListingsSortBy.AverageReview,
                    true, descriptions: true));
        }

        [Fact]
        public void SitesLinkingInYahoo()
        {
            var client = new AlexaClient(_alexaKey, _alexaSecretKey);
            TestTemplate<SitesLinkingInResponse>("SitesLinkingIn-yahoo.xml",
                () => client.GetSitesLinkingIn("yahoo.com"));
        }

    }
}
