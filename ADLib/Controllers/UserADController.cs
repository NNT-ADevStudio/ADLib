using ADLib.Classes;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ADLib.Controllers
{
    internal class UserADController
    {
        private const string GraphResourceUri = "https://graph.microsoft.com/v1.0/";

        public Auth Auth { get; }

        public UserADController(Auth auth)
        {
            Auth = auth;
        }

        public async Task<UserAD> GetCurrectUser()
        {
            await Auth.GetAccessTokenAsync();

            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, GraphResourceUri + "me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Auth.AccessToken);

            HttpResponseMessage response = await httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                UserAD user = JsonConvert.DeserializeObject<UserAD>(json);
                return user;
            }
            else
            {
                Console.WriteLine($"Ошибка запроса: {response.StatusCode}");
                return null;
            }
        }

        public async Task<UserAD> GetByID(int ID)
        {
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, GraphResourceUri + $"users/{ID}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Auth.AccessToken);

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                UserAD user = JsonConvert.DeserializeObject<UserAD>(json);
                return user;
            }
            else
            {
                Console.WriteLine($"Ошибка запроса: {response.StatusCode}");
                return null;
            }

        }

        public async Task<List<UserAD>> GetAllUsers()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Auth.AccessToken);

            List<UserAD> allUsers = new List<UserAD>();

            string nextLink = $"{GraphResourceUri}/users";

            do
            {
                var response = await client.GetAsync(nextLink);
                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject users = JObject.Parse(jsonResponse);

                nextLink = null;

                if (users == null) continue;

                var usersValue = users["value"];
                if (usersValue == null) continue;

                foreach (JToken user in usersValue)
                {
                    if (user == null) continue;
                    UserAD tempUser = user.ToObject<UserAD>();

                    if (tempUser == null) continue;
                    allUsers.Add(tempUser);
                }

                if (users["@odata.nextLink"] == null) continue;
                if (users["@odata.nextLink"]?.Type == JTokenType.Null) continue;

                nextLink = Convert.ToString(users["@odata.nextLink"]);
            }
            while (nextLink != null);

            return allUsers;
        }
    }
}
