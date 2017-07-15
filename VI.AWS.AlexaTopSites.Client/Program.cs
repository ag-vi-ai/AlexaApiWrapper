using System;

namespace VI.AWS.AlexaTopSites.Client
{
    class Program
    {
        static void Main(string[] args)
        {

            string key = Environment.GetEnvironmentVariable("AWS_ALEXA_KEY");
            string secretKey = Environment.GetEnvironmentVariable("AWS_ALEXA_SECRET_KEY");

            var client = new VI.AWS.AlexaTopSites.AlexaClient(key, secretKey);            
            var result = client.GetUrlInfo("https://torf.tv", UrlInfoResponseGroup.All);

            Console.WriteLine(result.ToString());

            Console.ReadKey();
        }
    }
}
