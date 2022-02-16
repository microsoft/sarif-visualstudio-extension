using Octokit;
using System;
using System.IO;
using System.Net;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            var result = GitHubPoC.InitializeGitHubClientAsync("SarifVsExtension").GetAwaiter().GetResult();
            var client = result.gitHubClient;

            string token = client.Credentials.GetToken();
            string url = "https://api.github.com/repos/microsoft/sarif-sdk/code-scanning/analyses";
            string query = $"tool_name=CodeQL&ref=ref/heads/main";
            string response = GetHttpGetResponseData(url, query, "application/vnd.github.v3+json", token);

            //var reposClient = client.Repository;
            //var repo = await reposClient.Get("microsoft", "sarif-sdk");


            #region Junk
            //StartWebServer();

            //state = Guid.NewGuid().ToString();
            //ProcessStartInfo processStartInfo = new()
            //{
            //    //FileName = $"https://github.com/login/oauth/authorize?client_id={clientId}&scope={scope}&redirect_uri=https%3A%2F%2Flocalhost%3A44378%2Fauth&state={state}",
            //    FileName = $"https://github.com/login/oauth/authorize?client_id={ClientId}&scope={Scope}&redirect_uri=http%3A%2F%2Flocalhost%3A{Port}%2Fauth&state={state}",
            //    UseShellExecute = true,
            //};
            //Process.Start(processStartInfo);

            //StartHttpListener();

            //using (var listener = new HttpListener())
            //{
            //    listener.Prefixes.Add("http://localhost:57789/");
            //    listener.Start();
            //    listener.BeginGetContext(ListenerCallback, listener);

            //    state = Guid.NewGuid().ToString();
            //    ProcessStartInfo processStartInfo = new ()
            //    {
            //        //FileName = $"https://github.com/login/oauth/authorize?client_id={clientId}&scope={scope}&redirect_uri=https%3A%2F%2Flocalhost%3A44378%2Fauth&state={state}",
            //        FileName = $"https://github.com/login/oauth/authorize?client_id={clientId}&scope={scope}&redirect_uri=http%3A%2F%2Flocalhost%3A57123%2Fauth&state={state}",
            //        UseShellExecute = true,
            //    };
            //    Process.Start(processStartInfo);

            //    while (true)
            //    {
            //        ConsoleKeyInfo key = Console.ReadKey();
            //        Console.WriteLine();

            //        if (key.Key != ConsoleKey.Enter)
            //        {
            //            return;
            //        }
            //        listener.Close();
            //        if (isReturningOk)
            //        {
            //            Console.WriteLine("\r\nCurrently returning success");
            //        }
            //        else
            //        {
            //            Console.WriteLine("\r\nCurrently returning error");
            //        }
            //        isReturningOk = !isReturningOk;
            //    }
            //}
            //Process.Start($"https://github.com/login/oauth/authorize?client_id={clientId}&scope={scope}&redirect_uri=http%3A%2F%2Flocalhost%3A20949%2Fauth&state={state}");
            //string response = GetHttpGetResponseData(
            //    "https://github.com/login/oauth/authorize",
            //    $"client_id={clientId}&scope={scope}&redirect_uri=http%3A%2F%2Flocalhost%3A20949%2Fauth&state={state}",
            //    accept: null);
            #endregion
        }

        //private static void StartWebServer()
        //{
        //    if (mainLoop != null && !mainLoop.IsCompleted)
        //    {
        //        // Already started
        //        return;
        //    }

        //    mainLoop = MainLoop();
        //}

        //private static void StopWebServer()
        //{
        //    serverActive = false;

        //    lock (Listener)
        //    {
        //        // Use a lock so we don't kill a request that's currently being processed
        //        Listener.Stop();
        //    }

        //    try
        //    {
        //        mainLoop.Wait();
        //        Console.WriteLine($"Received toke: {token}");
        //        Console.ReadKey();
        //    }
        //    catch { }
        //}

        //private static async Task MainLoop()
        //{
        //    Listener.Start();

        //    while (serverActive)
        //    {
        //        try
        //        {
        //            // GetContextAsync returns when a new request comes in
        //            var context = await Listener.GetContextAsync();
        //            lock (Listener)
        //            {
        //                if (serverActive)
        //                {
        //                    ProcessRequest(context);
        //                }
        //            }
        //        }
        //        catch (HttpListenerException)
        //        {
        //            // The listener has stopped
        //            return;
        //        }
        //        catch (Exception e)
        //        {
        //            throw;
        //        }
        //    }
        //}

        //private static void ProcessRequest(HttpListenerContext context)
        //{
        //    using (HttpListenerResponse response = context.Response)
        //    {
        //        try
        //        {
        //            bool handled = false;

        //            if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/auth")
        //            {
        //                string code = context.Request.QueryString["code"];
        //                string responseState = context.Request.QueryString["state"];

        //                if (!string.IsNullOrWhiteSpace(code) && state == responseState)
        //                {
        //                    string responseJson = GetHttpPostResponseData(
        //                        "https://github.com/login/oauth/access_token",
        //                        $"client_id={ClientId}&client_secret={OAuthSecret}&code={code}");

        //                    JObject jObject = JObject.Parse(responseJson);
        //                    token = jObject.Value<string>("access_token");

        //                    handled = true; 
        //                    StopWebServer();
        //                    response.StatusCode = 204;
        //                }
        //            }

        //            //switch (context.Request.Url.AbsolutePath)
        //            //{
        //            //    //This is where we do different things depending on the URL
        //            //    //TODO: Add cases for each URL we want to respond to
        //            //    case "/settings":
        //            //        switch (context.Request.HttpMethod)
        //            //        {
        //            //            case "GET":
        //            //                //Get the current settings
        //            //                response.ContentType = "application/json";

        //            //                //This is what we want to send back
        //            //                var responseBody = JsonConvert.SerializeObject(MyApplicationSettings);

        //            //                //Write it to the response stream
        //            //                var buffer = Encoding.UTF8.GetBytes(responseBody);
        //            //                response.ContentLength64 = buffer.Length;
        //            //                response.OutputStream.Write(buffer, 0, buffer.Length);
        //            //                handled = true;
        //            //                break;

        //            //            case "PUT":
        //            //                //Update the settings
        //            //                using (var body = context.Request.InputStream)
        //            //                using (var reader = new StreamReader(body, context.Request.ContentEncoding))
        //            //                {
        //            //                    //Get the data that was sent to us
        //            //                    var json = reader.ReadToEnd();

        //            //                    //Use it to update our settings
        //            //                    UpdateSettings(JsonConvert.DeserializeObject<MySettings>(json));

        //            //                    //Return 204 No Content to say we did it successfully
        //            //                    response.StatusCode = 204;
        //            //                    handled = true;
        //            //                }
        //            //                break;
        //            //        }
        //            //        break;
        //            //}

        //            if (!handled)
        //            {
        //                response.StatusCode = 404;
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            throw;
        //        }
        //    }
        //}

        //static void DoDeviceAuth()
        //{
        //    string response = GetHttpGetResponseData(
        //        "https://github.com/login/device/code",
        //        $"client_id={ClientId}&scope={Scope}");
        //    JObject jObject = JObject.Parse(response);
        //    string userCode = jObject.Value<string>("user_code");
        //    string deviceCode = jObject.Value<string>("device_code");
        //    string verificationUri = jObject.Value<string>("verification_uri");
        //    int expiresInSeconds = jObject.Value<int>("expires_in");
        //    int pollInteravl = jObject.Value<int>("interval");

        //    Console.WriteLine($"User code: {userCode}");
        //    Console.WriteLine($"Enter code at {verificationUri}");

        //    string accessToken = string.Empty;
        //    DateTime expireTime = DateTime.UtcNow.AddSeconds(expiresInSeconds);

        //    while (string.IsNullOrEmpty(accessToken))
        //    {
        //        Task.Delay(pollInteravl * 1000).GetAwaiter().GetResult();

        //        if (DateTime.UtcNow > expireTime)
        //        {
        //            break;
        //        }

        //        Console.WriteLine("Polling for access token...");

        //        response = GetHttpPostResponseData(
        //            "https://github.com/login/oauth/access_token",
        //            $"client_id={ClientId}&device_code={deviceCode}&grant_type=urn:ietf:params:oauth:grant-type:device_code");

        //        jObject = JObject.Parse(response);
        //        accessToken = jObject.Value<string>("access_token");
        //    }

        //    Console.WriteLine();

        //    if (!string.IsNullOrEmpty(accessToken))
        //    {
        //        Console.WriteLine("Received access token:");
        //        Console.WriteLine(accessToken);
        //        Console.WriteLine();
        //        Console.WriteLine("Querying for code scanning data...");
        //        Console.WriteLine();

        //        response = GetHttpGetResponseData(
        //            "https://api.github.com/repos/microsoft/sarif-sdk/code-scanning/analyses",
        //            "tool_name=CodeQL&ref=main&per_page=5",
        //            "application/vnd.github.v3+json",
        //            accessToken);

        //        Console.WriteLine("Received response:");
        //        Console.Write(response);
        //    }
        //    else
        //    {
        //        Console.WriteLine("Access token request expired.");
        //    }

        //    Console.WriteLine();
        //    Console.WriteLine("Press any key to exit...");
        //    Console.ReadKey();
        //}

        //static void StartHttpListener()
        //{
        //    var listener = new HttpListener();
        //    listener.Prefixes.Add("http://localhost:57123/");
        //    listener.Start();
        //    listener.BeginGetContext(ListenerCallback, listener);
        //}

        //static void ListenerCallback(IAsyncResult result)
        //{
        //    HttpListener listener = result.AsyncState as HttpListener;
        //    if (!listener.IsListening)
        //    {
        //        return;
        //    }
        //    HttpListenerContext context = listener.EndGetContext(result);
        //    HttpListenerResponse response = context.Response;

        //    response.Close();
        //    listener.BeginGetContext(ListenerCallback, listener);
        //}

        //static string GetHttpPostResponseData(string url, string query, string accept = "application/json", string token = null)
        //{
        //    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        //    byte[] data = Encoding.ASCII.GetBytes(query);

        //    request.Accept = accept;
        //    request.Method = "POST";
        //    request.ContentType = "application/x-www-form-urlencoded";
        //    request.ContentLength = data.Length;

        //    if (!string.IsNullOrWhiteSpace(token))
        //    {
        //        request.Headers.Add("Authorization", $"Bearer {token}");
        //    }

        //    using (var stream = request.GetRequestStream())
        //    {
        //        stream.Write(data, 0, data.Length);
        //    }

        //    using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        //    using Stream webStream = response.GetResponseStream();
        //    using StreamReader reader = new(webStream);

        //    return reader.ReadToEnd();
        //}

        static string GetHttpGetResponseData(string url, string query, string accept = "application/json", string token = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{url}?{query}");

            request.Accept = accept;
            request.Method = "GET";

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Add("Authorization", $"Bearer {token}");
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream webStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(webStream))
                    {
                        return reader.ReadToEnd();
                    }
        }
    }
}
