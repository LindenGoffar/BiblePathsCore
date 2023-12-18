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
using Azure.AI.OpenAI; 
using Azure;
using Newtonsoft.Json.Schema.Generation;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Emit;

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
    }

    public interface IOpenAIResponder
    {
        public Task<QandAObj> GetAIQuestionAsync(string text, string key);
    }

    public class OpenAIResponder : IOpenAIResponder

    {
        public const string OpenAIAPI = "gpt-4-1106-preview";
        //private readonly HttpClient _httpClient;

        //public OpenAIResponder()
        //{
        //    _httpClient = new HttpClient();
        //}
        public async Task<QandAObj> GetAIQuestionAsync(string text, string key)
        {
             
            string QnARequest = "Build a question and answer pair, with precise questions, and brief answers, from the following text:" 
                                + text;
            ChatCompletionsOptions CCOptions = new ChatCompletionsOptions();

            QandAObj qandAObj = new QandAObj();

            ChatMessage QnAMessage = new ChatMessage();
            QnAMessage.Role = "function";
            QnAMessage.Name = "QnAFunction";
            QnAMessage.Content = QnARequest;

            //List<FunctionDefinition> QnAFuntions = new List<FunctionDefinition>();
            FunctionDefinition QnAFunction = new FunctionDefinition();

            //JSchemaGenerator generator = new JSchemaGenerator();
            //JSchema qnASchema = generator.Generate(typeof(QandAObj));
            //string qnASchemaString = qnASchema.ToString();
            string qnASchemaString = "{  \"type\": \"object\",  \"properties\": { \"question\": {\"type\": \"string\" }, \"answer\": { \"type\": \"string\" }  },  \"required\": [ \"question\",  \"answer\" ]}";

            QnAFunction.Name = "QnAFunction";
            QnAFunction.Parameters = BinaryData.FromString(qnASchemaString);

            CCOptions.Functions.Add(QnAFunction);
            CCOptions.Messages.Add(QnAMessage);
            CCOptions.ChoiceCount = 1; // we only want one question generated
            CCOptions.Temperature = (float)1.2;

            OpenAIClient client = new OpenAIClient(key);
            Response<ChatCompletions> response = await client.GetChatCompletionsAsync(
                OpenAIAPI, // assumes a matching model deployment or model name
                CCOptions);
   
            // Handling some errors
            if (response == null)
            {
                qandAObj.question = "Uh Oh... We got no response object from our friends at OpenAI. ";
                return qandAObj;
            }
            if (response.GetRawResponse().Status != 200)
            {
                qandAObj.question = "Uh Oh... we got an error in our response from our friends at OpenAI.  ";
                qandAObj.question += " Status Code: " + response.GetRawResponse().Status;
                qandAObj.question += " Reaseon: " + response.GetRawResponse().ReasonPhrase;
                return qandAObj;
            }
            if (response.Value == null)
            {
                qandAObj.question = "Uh Oh... our response object from our friends at OpenAI contained no Value";
                return qandAObj;
            }

            if (response.Value.Choices.Count >= 1)
            {
                // Very oddly the response may show up on one of two properties. 
                string JSONResponseString = response.Value.Choices[0].Message.Content;
                // OK This is wierd but sometimes the result shows up on a deprecated property.
                if (JSONResponseString == null)
                {
                    JSONResponseString = response.Value.Choices[0].Message.FunctionCall.Arguments;
                }
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
                qandAObj.question = "Hmm... We didn't get a resonse back that we could use, please try again.";
            }

            return qandAObj;
        }
    }
}

