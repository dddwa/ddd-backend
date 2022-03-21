
using System.Net.Http;
using Microsoft.AspNetCore.Http;

public static class RequestExtensions
    {
        public static string GetIpAddress(this HttpRequestMessage req)
        {
            if (req.Properties.ContainsKey("HttpContext"))
                return ((DefaultHttpContext)req.Properties["HttpContext"])?.Connection?.RemoteIpAddress?.ToString();

            return null;
        }
    }