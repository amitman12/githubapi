// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

string secret = "<ADD SECRET HERE>"; //I removed the token~
string path = "C:\\Users\\AMITTR\\source\\repos\\ConsoleApp3\\ConsoleApp3\\github_queries.txt";
const int parallelism = 3;

using (StreamReader reader = new StreamReader(path)) 
{
    string line;

    using (HttpClient client = new HttpClient()) 
    {
        //request.Headers.Add("Authorization", secret);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DotNet", "1.0"));
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(+http://example.com)"));

        int totalCount = 0;
        SemaphoreSlim semaphore = new SemaphoreSlim(parallelism);
        var taskList = new List<Task>();

        while ((line = reader.ReadLine()) != null)
        {
            await semaphore.WaitAsync();
            taskList.Add(Task.Run(async () =>
            {
                try
                {
                    int current = await HandleLineAsync(line, client);
                    totalCount += current;
                }
                catch (Exception e)
                {
                    //TODO: log
                }
                finally 
                {
                    semaphore.Release();
                }

            }));
        }
        await Task.WhenAll(taskList);
        Console.WriteLine(totalCount);
    }
}

async Task<int> HandleLineAsync(string url2, HttpClient client) 
{
    int totalCount = 0;
    try
    {
        string urlToUse = url2;
        while (urlToUse != null)
        {
            // Send a GET request
            HttpResponseMessage response = await client.GetAsync(urlToUse);

            // Ensure the request was successful
            response.EnsureSuccessStatusCode();

            string responseBodyAsString = await response.Content.ReadAsStringAsync();

            // Read the response content as a string
            JObject responseBodyAsJson = JObject.Parse(responseBodyAsString);

            int count = (int)responseBodyAsJson["total_count"];
            totalCount += count;

            Console.WriteLine(responseBodyAsJson.ToString(Newtonsoft.Json.Formatting.Indented));

            if (response.Headers.TryGetValues("Link", out var linkValues))
            {
                string linkHeader = linkValues.FirstOrDefault();
                urlToUse = GetNextPageUrl(linkHeader);
            }
            else
            {
                urlToUse = null;
            }
        }
    }
    catch (HttpRequestException e)
    {
        // Handle the exception
        Console.WriteLine($"Request error: {e.Message}");
    }
    return totalCount;
}

static string GetNextPageUrl(string linkHeader)
{
    if (string.IsNullOrEmpty(linkHeader))
        return null;

    // Split the link header into parts
    var links = linkHeader.Split(',');

    foreach (var link in links)
    {
        var segments = link.Split(';');
        if (segments.Length < 2)
            continue;

        // Extract the URL and rel value
        var urlPart = segments[0].Trim();
        var relPart = segments[1].Trim();

        // Check if this is the 'next' link
        if (relPart.Equals("rel=\"next\"", StringComparison.OrdinalIgnoreCase))
        {
            // Extract the URL and remove angle brackets
            var url = urlPart.Trim('<', '>');
            return url;
        }
    }

    return null;
}


// read from file

// add header

// call github + pagination & aggregate result