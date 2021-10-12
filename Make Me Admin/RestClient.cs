using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;

namespace Make_Me_Admin
{

    public class PrerequisitesReply
    {
        public bool success { get; set; }
        public string preferredService { get; set; }
        public string message { get; set; }
    }

    public class ValidateReply
    {
        public bool success { get; set; }
        public string message { get; set; }
        public bool validated { get; set; }
    }

    public class RestClient
    {
        private HttpClient client;

        public RestClient()
        {
            client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:6666");
            // client.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<PrerequisitesReply> GetPrerequisites(string user)
        {
            try
            {
                var content = await client.GetStringAsync("/prerequisite/" + WebUtility.UrlEncode(user));
                var reply = JsonConvert.DeserializeObject<PrerequisitesReply>(content);
                reply.success = true;
                return reply;
            }
            catch(HttpRequestException ex)
            {
                return new PrerequisitesReply() { preferredService = null,  message = ex.Message };
            }
            catch (ArgumentNullException ex)
            {
                return new PrerequisitesReply() { preferredService = null, message = ex.Message };
            }
        }

        public async Task<ValidateReply> ValidateTotp(string user, string code)
        {
            try
            {
                var content = await client.GetStringAsync(String.Format("/validate/totp/{0}/{1}", WebUtility.UrlEncode(user), code));
                var reply = JsonConvert.DeserializeObject<ValidateReply>(content);
                reply.success = true;
                return reply;
            }
            catch (HttpRequestException ex)
            {
                return new ValidateReply() { message = ex.Message };

            }
            catch (ArgumentNullException ex)
            {
                return new ValidateReply() { message = ex.Message };
            }
        }
        public async Task<ValidateReply> ValidateFreja(string user)
        {
            try { 
                var content = await client.GetStringAsync("/validate/freja/" + WebUtility.UrlEncode(user));
                var reply = JsonConvert.DeserializeObject<ValidateReply>(content);
                reply.success = true;
                return reply;
            }
            catch (HttpRequestException ex)
            {
                return new ValidateReply() { message = ex.Message };
            }
            catch (ArgumentNullException ex)
            {
                return new ValidateReply() { message = ex.Message };
            }
        }
    }
}
