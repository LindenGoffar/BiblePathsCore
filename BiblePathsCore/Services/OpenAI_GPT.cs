using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BiblePathsCore.Services
{
    public interface IOpenAIResponder
    {
        string GetOpenAIResponseAsync(string key);
    }

    public class OpenAIResponder : IOpenAIResponder

    {
        private readonly IConfiguration _configuration;
        public OpenAIResponder(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetOpenAIResponseAsync(string key)
        {
            return _configuration[key];
        }
    }
}

