using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BiblePathsCore.Models.DB;
using System.Text;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using OpenAI.Chat;
using Azure.AI.OpenAI; // Maybe able to deprecate
using Azure;
using Newtonsoft.Json.Schema.Generation;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Emit;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using BiblePathsCore.Models;

namespace BiblePathsCore.Services
{
    public class OpenAISettings
    {
        public string OpenAIEnabled { get; set; }
        public string OpenAIAPIKey { get; set; }
    }

    public class QandAObj
    {
        [JsonProperty("question", Required = Required.Always)]
        public string question { get; set; }
        [JsonProperty("answer", Required = Required.Always)]
        public string answer { get; set; }
        [JsonProperty("points", Required = Required.Always)]
        public int points { get; set; }
    }
    public class WordTongueObj
    {
        [JsonProperty("order", Required = Required.Always)]
        public int order { get; set; }
        [JsonProperty("word", Required = Required.Always)]
        public string word { get; set; }
        [JsonProperty("translation", Required = Required.Always)]
        public string translation { get; set; }
        [JsonProperty("pronunciation", Required = Required.Always)]
        public string pronunciation { get; set; }
    }
    public class VerseTongueObj
    {
        [JsonProperty("usermessage", Required = Required.Default)]
        public string usermessage { get; set; }
        [JsonProperty("words", Required = Required.Always)]
        public List<WordTongueObj> words { get; set; }
    }

    public interface IOpenAIResponder
    {
        // public Task<QandAObj> GetAIQuestionAsync(string text, string key);
        public Task<QandAObj> GetAIQuestionAsync(string text);
        public Task<VerseTongueObj> GetAIVerseTongueAsync(BibleVerse verse, string FromLanguage, string ToLanguage);
    }

    public class OpenAIResponder : IOpenAIResponder
    {
        public const string OpenAIAPI = "o4-mini";
        private readonly OpenAISettings _openAIsettings;
        //private readonly HttpClient _httpClient;

        public OpenAIResponder(IOptions<OpenAISettings> openAISettings)
        {
            _openAIsettings = openAISettings.Value;
        }

        public async Task<QandAObj> GetAIQuestionAsync(string text)
        {

            string QnASystemRequest = AIPromptBuilder.GetRandomSystemChatMessage();
            string QnAAssistantRequest = AIPromptBuilder.GetRandomAssistantChatMessage();

            string QnAUserRequest = "<Verse>"
                                + text
                                + "</Verse>";

            string key = _openAIsettings.OpenAIAPIKey;

            ChatClient client = new(OpenAIAPI, key);

            ChatCompletionOptions options = new()
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "QandAObj",
                jsonSchema: BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "question": { "type": "string" },
                            "answer": { "type": "string" },
                            "points": { "type": "integer"}
                        },
                        "required": ["question", "answer", "points"],
                        "additionalProperties": false
                    }
                    """),
                    jsonSchemaIsStrict: true),
                Temperature = (float)1,
            };

            QandAObj qandAObj = new();

            ChatCompletion chatCompletion = await client.CompleteChatAsync(
                [new SystemChatMessage(QnASystemRequest),
                new UserChatMessage(QnAUserRequest),
                new AssistantChatMessage(QnAAssistantRequest)],
                options);

            //// Handling some errors
            if (chatCompletion == null)
            {
                qandAObj.question = "Uh Oh... We got no response object from our friends at OpenAI. ";
                return qandAObj;
            }

            if (chatCompletion.Content == null)
            {
                qandAObj.question = "Uh Oh... our response object from our friends at OpenAI contained no Value";
                return qandAObj;
            }

            if (chatCompletion.Content.Count >= 1)
            {
                // Very oddly the response may show up on one of two properties. 
                string JSONResponseString = chatCompletion.Content[0].Text;
                // OK sometimes we may not get back a well formed JSON String... let's handle that. 
                try
                {
                    qandAObj = JsonConvert.DeserializeObject<QandAObj>(JSONResponseString);
                }
                catch
                {
                    qandAObj.question = "Uh Oh... we had a problem parsing the following response: ";
                    qandAObj.question += JSONResponseString;
                }
            }
            else
            {
                qandAObj.question = "Hmm... We didn't get a response back that we could use, please try again.";
            }

            return qandAObj;
        }

        public async Task<VerseTongueObj> GetAIVerseTongueAsync(BibleVerse verse, string FromLanguage, string ToLanguage)
        {          
            string TongueSystemRequest = "You are a Bible Scholar with a background in linguistics. " +
                "You will be provided a Bible verse in the User Message delimited by an xml <Verse> tag." +
                "Please review the entire User Message before formulating a response." +
                "Your task is to provide an ordered list of words from the verse, their order in the verse," + 
                "their translation in " +
                ToLanguage +
                "and their pronunciation in " +
                FromLanguage +

                "The output should be in the schema specified in the Function request.";

            string TongueUserRequest = "<Verse>"
                                + verse.Text
                                + "</Verse>";

            string key = _openAIsettings.OpenAIAPIKey;

            ChatClient client = new(OpenAIAPI, key);

            ChatCompletionOptions options = new()
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "VerseTongueObj",
                jsonSchema: BinaryData.FromString("""
                    {
                      "type": "object",
                      "properties": {
                        "usermessage": { "type": "string" },
                        "words": {
                          "type": "array",
                          "items": {
                            "type": "object",
                            "properties": {
                              "order": { "type": "integer" },
                              "word": { "type": "string" },
                              "translation": { "type": "string" },
                              "pronunciation": { "type": "string" }
                            },
                            "required": ["order", "word", "translation", "pronunciation"],
                            "additionalProperties": false
                          }
                        }
                      },
                      "required": ["words"],
                      "additionalProperties": false
                    }
                    
                    """),
                    jsonSchemaIsStrict: true),
                Temperature = (float)1,
            };

            VerseTongueObj verseTongueObj = new();

            ChatCompletion chatCompletion = await client.CompleteChatAsync(
                [new SystemChatMessage(TongueSystemRequest),
                new UserChatMessage(TongueUserRequest)],
                options);

            //// Handling some errors
            if (chatCompletion == null)
            {
                verseTongueObj.usermessage = "Uh Oh... We got no response object from our friends at OpenAI. ";
                return verseTongueObj;
            }

            if (chatCompletion.Content == null)
            {
                verseTongueObj.usermessage = "Uh Oh... our response object from our friends at OpenAI contained no Value";
                return verseTongueObj;
            }

            if (chatCompletion.Content.Count >= 1)
            {
                // Very oddly the response may show up on one of two properties. 
                string JSONResponseString = chatCompletion.Content[0].Text;
                // OK sometimes we may not get back a well formed JSON String... let's handle that. 
                try
                {
                    verseTongueObj = JsonConvert.DeserializeObject<VerseTongueObj>(JSONResponseString);
                }
                catch
                {
                    verseTongueObj.usermessage = "Uh Oh... we had a problem parsing the following response: ";
                    verseTongueObj.usermessage += JSONResponseString;
                }
            }
            else
            {
                verseTongueObj.usermessage = "Hmm... We didn't get a response back that we could use, please try again.";
            }

            return verseTongueObj;
        }
    }
}

