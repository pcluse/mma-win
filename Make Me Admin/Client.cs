using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;


namespace Make_Me_Admin
{

    public class AdminRequest
    {   
        public string user { get; set; }
        public string twofactor { get; set; }
        public int expire { get; set; }
    }

    public class AdminResult
    {
        public bool success { get; set; }
        public string message { get; set; }
    }

    public class Client
    {
        private HttpClient client;

        public Client()
        {
            client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:6666");
            client.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<AdminResult> CheckAdmin(string user)
        {
            // return new AdminResult() { success = true, message = "Yada" };
            var content = await client.GetStringAsync("/check-admin?user=" + user);
            return JsonConvert.DeserializeObject<AdminResult>(content);
        }

        public async Task<AdminResult> AddAdmin(string user, string twofactor, int expire)
        {
            var adminRequest = new AdminRequest()
            {
                user = user,
                twofactor = twofactor,
                expire = expire
            };

            var json = JsonConvert.SerializeObject(adminRequest);
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            var httpResponse = await client.PostAsync("/add-admin", requestContent);
            if (httpResponse.Content != null)
            {
                // Error Here
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<AdminResult>(responseContent);
            }
            return new AdminResult() { success = false, message = "No reply from server" };        
        }

    }
}
