using Microsoft.Alm.Authentication;
using Octokit;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ConsoleApp2
{
    internal static class GitHubPoC
    {
        // Update this string to force generation of new tokens
        private const string SecretsNamespace = "SarifVsExtension2.1";
        private const string ClientId = "23c8243801d898f93624";
        private const string ClientCode = "14552a72ecc6086d4a699fe9b7fd81bb2cef6e4f";
        private const string Scope = "security_events";

        private const string GitHubBaseUrl = "https://github.com";
        private const string GitHubAuthUrl = "https://github.com/login/oauth/authorize";
        private const string GitHubAuthTokenUrl = "https://github.com/login/oauth/access_token";

        private static readonly TargetUri gitHubBaseUri = new TargetUri(GitHubBaseUrl);

        private const string authSuccessPageHtml = @"
<html>
    <head>
        <title>SARIF VS Extension Authorization for GitHub</title>
    </head>
    <body>
        Thank you for authorizing the Microsoft SARIF Viewer extension to access GitHub.
        <br/><br/>
        You may close this tab and return to Visual Studio.
    </body>
</html>";

        private static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string Base64urlencodeNoPadding(byte[] buffer)
        {
            string base64 = Convert.ToBase64String(buffer);

            // Convert base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");

            // Strip padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        private static string GetRandomDataBase64url(uint length)
        {
            var bytes = new byte[length];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }

            return Base64urlencodeNoPadding(bytes);
        }

        public static Task<(GitHubClient gitHubClient, string errorMessage)> InitializeGitHubClientAsync(string productName)
        {
            return Task.Run<(GitHubClient gitHubClient, string errorMessage)>(async () =>
            {
                #region Get Cached Authorization Token
                var secrets = new SecretStore(RuntimeContext.Default, SecretsNamespace);

                Token gitHubAccessToken = await secrets.ReadToken(gitHubBaseUri);

                if (gitHubAccessToken != null)
                {

                    Connection connection = new Connection(new ProductHeaderValue(productName));
                    connection.Credentials = new Credentials(gitHubAccessToken.Value, AuthenticationType.Bearer);
                    var gitHubClient = new GitHubClient(connection);

                    try
                    {
                        // Validate the connection.
                        User user = await gitHubClient.User.Current();
                        return (gitHubClient, null);
                    }
                    catch (Exception)
                    {
                        // Cached token is invalid, delete it.
                        _ = await secrets.DeleteToken(gitHubBaseUri);
                    }
                }
                #endregion

                // Creates a redirect URI using an available port on the loopback address.
                string redirectUri = $"http://localhost:{GetRandomUnusedPort()}/";

                var http = new HttpListener();
                http.Prefixes.Add(redirectUri);
                http.Start();

                string state = GetRandomDataBase64url(32);
                string authRequestUrl = $"{GitHubAuthUrl}?client_id={ClientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Scope}&state={state}&allow_signup=false";

                _ = System.Diagnostics.Process.Start(authRequestUrl);

                HttpListenerContext context = await http.GetContextAsync();

                // Sends an HTTP response to the browser.
                HttpListenerResponse response = context.Response;

                byte[] buffer = Encoding.UTF8.GetBytes(authSuccessPageHtml);
                response.ContentLength64 = buffer.Length;
                Stream responseOutput = response.OutputStream;
                Task responseTask = responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
                {
                    responseOutput.Close();
                    http.Stop();
                },
                TaskScheduler.Default);

                string authError = context.Request.QueryString.Get("error");
                if (!string.IsNullOrEmpty(authError))
                {
                    return (null, authError);
                }

                string authResponseCode = context.Request.QueryString.Get("code");
                string authResponseState = context.Request.QueryString.Get("state");

                if (string.IsNullOrEmpty(authResponseCode) ||
                    authResponseState != state)
                {
                    string errorMessage = "Invalid authorization response: ";
                    if (authResponseState != state)
                    {
                        errorMessage += "state mismatch - possible attack detected";
                    }
                    else
                    {
                        errorMessage += "response code not returned";
                    }

                    return (null, errorMessage);
                }

                string tokenRequestBody = $"client_id={ClientId}&client_secret={ClientCode}&code={authResponseCode}&state={state}";
                byte[] _byteVersion = Encoding.ASCII.GetBytes(tokenRequestBody);

                HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create(GitHubAuthTokenUrl);
                tokenRequest.Method = "POST";
                tokenRequest.ContentType = "application/x-www-form-urlencoded";
                tokenRequest.ContentLength = _byteVersion.Length;

                Stream stream = await tokenRequest.GetRequestStreamAsync();
                await stream.WriteAsync(_byteVersion, 0, _byteVersion.Length);
                stream.Close();

                try
                {
                    WebResponse tokenResponse = await tokenRequest.GetResponseAsync();

                    using (var reader = new StreamReader(tokenResponse.GetResponseStream()))
                    {
                        string responseRawText = await reader.ReadToEndAsync();
                        string responseDecodedText = HttpUtility.UrlDecode(responseRawText);
                        NameValueCollection responseValues = HttpUtility.ParseQueryString(responseDecodedText);

                        string token = responseValues.Get("access_token");
                        string grantedScopeString = responseValues.Get("scope");
                        string[] grantedScopes = grantedScopeString.Split(new char[] { ',' });

                        if (!string.IsNullOrWhiteSpace(token)
                            && grantedScopes.Length == 1
                            && Array.IndexOf(grantedScopes, "security_events") >= 0)
                        {
                            try
                            {
                                _ = secrets.WriteToken(gitHubBaseUri, new Token(token, TokenType.Personal));
                            }
                            catch (Exception)
                            {
                                // This is a non-fatal error, keep going with the token
                            }

                            try
                            {
                                Connection connection = new Connection(new ProductHeaderValue(productName));
                                connection.Credentials = new Credentials(token, AuthenticationType.Bearer);
                                return (new GitHubClient(connection), null);
                            }
                            catch (Exception e)
                            {
                                string errorMessage = "error creating GitHubClient: " + e.ToString();
                                return (null, errorMessage);
                            }
                        }
                        else
                        {
                            string errorMessage = "error parsing token response or not all requested access scopes were granted";
                            return (null, errorMessage);
                        }
                    }
                }
                catch (WebException) { }

                return (null, "Could not acquire authorization token from GitHub");
            });
        }
    }
}
