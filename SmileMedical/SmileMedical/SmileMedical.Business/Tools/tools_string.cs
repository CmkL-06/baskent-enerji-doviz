using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Tools
{
    public static class tools_string
    {
        public static string GenerateSlug(string input)
        {
            // Remove special characters, convert spaces to hyphens, and make it lowercase
            char[] arr = input
                .ToLower()
                .Replace(" ", "-")
                .Where(c => Char.IsLetterOrDigit(c) || c == '-')
                .ToArray();
            // Convert the char array to a string
            string slug = new string(arr);
            // Remove consecutive hyphens
            while (slug.Contains("--"))
            {
                slug = slug.Replace("--", "-");
            }
            // Remove leading and trailing hyphens
            slug = slug.Trim('-');
            return slug;
        }
        public static string DeGenerateSlug(string input)
        {
            // Remove special characters, convert spaces to hyphens, and make it lowercase
            char[] arr = input
                .ToUpper()
                .Replace("-", " ")
                .Where(c => Char.IsLetterOrDigit(c) || c == '-')
                .ToArray();
            // Convert the char array to a string
            string slug = new string(arr);
            // Remove consecutive hyphens
           
            // Remove leading and trailing hyphens
            slug = slug.Trim(' ');
            return slug;
        }

        public static string GetIpAddress(this HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress.ToString();
            return ipAddress;
        }
    }
}