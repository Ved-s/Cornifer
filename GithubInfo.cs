using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Cornifer
{
    public static class GithubInfo
    {
        public static string? Commit;
        public static string? Branch;

        public static string Desc = "";
        public static string Status = "";

        public static void Load()
        {
            if (File.Exists("gitinfo"))
            {
                string[] gitinfo = File.ReadAllLines("gitinfo");
                if (gitinfo.Length >= 2)
                {
                    Commit = gitinfo[0];
                    Branch = gitinfo[1];
                }

                Desc = $"gitinfo: {Branch} {Commit}";

                Status = "Getting commit info...";

                ThreadPool.QueueUserWorkItem(GetGithubInfo);
            }
            else
            {
                Status = "gitinfo not found";
            }
        }

        static async void GetGithubInfo(object? _)
        {
            HttpClient client = new();

            HttpRequestMessage request = new(HttpMethod.Get, $"https://api.github.com/repos/Ved-s/Cornifer/compare/master...{Commit}");

            request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
            request.Headers.TryAddWithoutValidation("User-Agent", "Cornifer HttpClient (https://github.com/Ved-s/Cornifer)");

            HttpResponseMessage response = await client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                GithubResponse ghResp = JsonSerializer.Deserialize<GithubResponse>(await response.Content.ReadAsStreamAsync())!;

                if (ghResp.Status == "identical")
                    Status = "This is the latest version";
                else if (ghResp.AheadBy == 0 && ghResp.BehindBy == 0)
                    Status = "Unknown version";
                else if (ghResp.AheadBy == 0)
                    Status = $"This version is behind by {ghResp.BehindBy} commit{(ghResp.BehindBy == 1 ? "" : "s")}";
                else
                    Status = $"This version is ahead by {ghResp.AheadBy} commit{(ghResp.AheadBy == 1 ? "" : "s")}";
            }
            else 
            {
                Status = $"Error {(int)response.StatusCode} {response.StatusCode}";
            }
        }

        class GithubResponse
        {
            [JsonPropertyName("status")]
            public string Status { get; set; } = "";

            [JsonPropertyName("ahead_by")]
            public int AheadBy { get; set; }

            [JsonPropertyName("behind_by")]
            public int BehindBy { get; set; }
        }
    }
}
