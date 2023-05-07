using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using AuthorizeNet.Api.Contracts.V1;
using AuthorizeNet.Api.Controllers.Bases;

namespace AuthorizeNet.Util
{
#pragma warning disable 1591
    public static class HttpUtility
    {
        // Max response size allowed: 64 MB
        const int MaxResponseLength = 67108864;
        static readonly Log Logger = LogFactory.getLog(typeof(HttpUtility));
        static bool _proxySet;// = false;
        static readonly HttpClient HttpClient;

        static readonly bool UseProxy = Environment.getBooleanProperty(Constants.HttpsUseProxy);
        static readonly string ProxyHost = Environment.GetProperty(Constants.HttpsProxyHost);
        static readonly int ProxyPort = Environment.getIntProperty(Constants.HttpsProxyPort);

        static HttpUtility()
        {
            var httpClientHandler = new HttpClientHandler();

            if (UseProxy)
            {
                httpClientHandler.Proxy = SetProxyIfRequested(httpClientHandler.Proxy);
            }

            var httpConnectionTimeout = Environment.getIntProperty(Constants.HttpConnectionTimeout);
            HttpClient = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromMilliseconds(httpConnectionTimeout != 0 ? httpConnectionTimeout : Constants.HttpConnectionDefaultTimeout)
            };
        }

        static Uri GetPostUrl(Environment env)
        {
            var postUrl = new Uri(env.getXmlBaseUrl() + "/xml/v1/request.api");
            Logger.debug($"Creating PostRequest Url: '{postUrl}'");

            return postUrl;
        }

        public static async Task<ANetApiResponse> PostDataAsync<TQ, TS>(Environment env, TQ request)
            where TQ : ANetApiRequest
            where TS : ANetApiResponse
        {
            ANetApiResponse response = null;
            if (null == request)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var postUrl = GetPostUrl(env);

            var requestType = typeof(TQ);
            var serializer = new XmlSerializer(requestType);

            using (var memoryStream = new MemoryStream())
            using (var writer = new XmlTextWriter(memoryStream, Encoding.UTF8))
            {
                serializer.Serialize(writer, request);

                memoryStream.Position = 0;
                using (var content = new StreamContent(memoryStream))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");

                    using (var httpResponse = await HttpClient.PostAsync(postUrl, content).ConfigureAwait(false))
                    {
                        string responseAsString = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                        if (responseAsString.Length >= MaxResponseLength)
                        {
                            throw new Exception("response is too long.");
                        }

                        if (!string.IsNullOrEmpty(responseAsString))
                        {
                            using (var memoryStreamForResponseAsString = new MemoryStream(Encoding.UTF8.GetBytes(responseAsString)))
                            {
                                var responseType = typeof(TS);
                                var deSerializer = new XmlSerializer(responseType);

                                object deSerializedObject;
                                try
                                {
                                    // try deserializing to the expected response type
                                    deSerializedObject = deSerializer.Deserialize(memoryStreamForResponseAsString);
                                }
                                catch (Exception)
                                {
                                    // probably a bad response, try if this is an error response
                                    memoryStreamForResponseAsString.Seek(0, SeekOrigin.Begin); //start from beginning of stream
                                    var genericDeserializer = new XmlSerializer(typeof(ANetApiResponse));
                                    deSerializedObject = genericDeserializer.Deserialize(memoryStreamForResponseAsString);
                                }

                                // if error response
                                if (deSerializedObject is ErrorResponse)
                                {
                                    response = deSerializedObject as ErrorResponse;
                                }
                                else
                                {
                                    // actual response of type expected
                                    if (deSerializedObject is TS)
                                    {
                                        response = deSerializedObject as TS;
                                    }
                                    else if (deSerializedObject is ANetApiResponse) // generic response
                                    {
                                        response = deSerializedObject as ANetApiResponse;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return response;
        }

        public static IWebProxy SetProxyIfRequested(IWebProxy proxy)
        {
            var newProxy = proxy as WebProxy;

            if (UseProxy)
            {
                var proxyUri = new Uri($"{Constants.ProxyProtocol}://{ProxyHost}:{ProxyPort}");
                if (!_proxySet)
                {
                    Logger.info(string.Format("Setting up proxy to URL: '{0}'", proxyUri));
                    _proxySet = true;
                }
                if (null == proxy || null == newProxy)
                {
                    newProxy = new WebProxy(proxyUri);
                }

                newProxy.UseDefaultCredentials = true;
                newProxy.BypassProxyOnLocal = true;
            }
            return (newProxy ?? proxy);
        }
    }
#pragma warning restore 1591
}
//http://ecommerce.shopify.com/c/shopify-apis-and-technology/t/c-webrequest-put-and-xml-49458
//http://www.808.dk/?code-csharp-httpwebrequest

