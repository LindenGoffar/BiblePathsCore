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

    public interface IOpenAIResponder
    {
        // public Task<QandAObj> GetAIQuestionAsync(string text, string key);
        public Task<QandAObj> GetAIQuestionAsync(string text);
    }

    public class OpenAIResponder : IOpenAIResponder

    {
        public const string OpenAIAPI = "gpt-4o-mini";
        private readonly OpenAISettings _openAIsettings;
        //private readonly HttpClient _httpClient;

        public OpenAIResponder(IOptions<OpenAISettings> openAISettings)
        {
            _openAIsettings = openAISettings.Value;
        }

    // 09-05-2024 This method is rewritten to use the OpenAI .NET Library directly. 
    //    public async Task<QandAObj> GetAIQuestionAsync(string text, string key)
    //    {

    //        string QnASystemRequest001 = "You are a teacher preparing a quiz. " + 
    //            "you will be provided a snippet of Bible text, delimited by an xml <Verse> tag, " +
    //            "you will write a question from this text that can be answered from the same text, and provide the answer. " +
    //            "You will also need to determine how many points the answer will be worth, points will be an integer between 1 and 6, " +
    //            "where each independent clause in the answer is worth 1 point." +
    //            "Use a default of 1 if points cannot be determined. " +
    //            "The output, including question, answer, and points should be in the schema specified " +
    //            "The question should be brief and not include the phrase 'according to'. " +
    //            "The Answer should be short and not include the contents of the question, or restate the question.";

    //        string QnASystemRequest = "You are a Bible teacher preparing a quiz for students. " +
    //"you will be provided a snippet of Bible text, delimited by an xml <Verse> tag, " +
    //"you will write a question from this text that can be answered from the same text, and provide the answer. " +
    //"You will also need to determine how many points the answer will be worth, points will be an integer between 1 and 6, " +
    //"where each independent clause in the answer is worth 1 point." +
    //"Use a default of 1 if points cannot be determined. " +
    //"The output, including question, answer, and points should be in the schema specified " +
    //"The question should not include the phrase 'according to'. " +
    //"The Answer should be short and not restate any portion of the question.";


    //        string QnAUserRequest = "<Verse>"
    //                            + text
    //                            + "</Verse>";

    //        ChatClient client = new(OpenAIAPI, key);

    //        ChatCompletionOptions options = new()
    //        {
    //            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
    //            name: "QandAObj",
    //            jsonSchema: BinaryData.FromString("""
    //                {
    //                    "type": "object",
    //                    "properties": {
    //                        "question": { "type": "string" },
    //                        "answer": { "type": "string" },
    //                        "points": { "type": "integer"}
    //                    },
    //                    "required": ["question", "answer", "points"],
    //                    "additionalProperties": false
    //                }
    //                """),
    //                strictSchemaEnabled: true),
    //            Temperature = (float)1.2,
    //        };

    //        QandAObj qandAObj = new();

    //        ChatCompletion chatCompletion = await client.CompleteChatAsync( 
    //            [new SystemChatMessage(QnASystemRequest),
    //            new UserChatMessage(QnAUserRequest)],
    //            options);

    //        //// Handling some errors
    //        if (chatCompletion == null)
    //        {
    //            qandAObj.question = "Uh Oh... We got no response object from our friends at OpenAI. ";
    //            return qandAObj;
    //        }
    //        //if (chatCompletion.     GetRawResponse().Status != 200)
    //        //{
    //        //    qandAObj.question = "Uh Oh... we got an error in our response from our friends at OpenAI.  ";
    //        //    qandAObj.question += " Status Code: " + response.GetRawResponse().Status;
    //        //    qandAObj.question += " Reaseon: " + response.GetRawResponse().ReasonPhrase;
    //        //    return qandAObj;
    //        //}
    //        if (chatCompletion.Content == null)
    //        {
    //            qandAObj.question = "Uh Oh... our response object from our friends at OpenAI contained no Value";
    //            return qandAObj;
    //        }

    //        if (chatCompletion.Content.Count >= 1)
    //        {
    //            // Very oddly the response may show up on one of two properties. 
    //            string JSONResponseString = chatCompletion.Content[0].ToString();
    //            // OK sometimes we may not get back a well formed JSON String... let's handle that. 
    //            try
    //            {
    //                qandAObj = JsonConvert.DeserializeObject<QandAObj>(JSONResponseString);
    //            }
    //            catch
    //            {
    //                qandAObj.question = "Uh Oh... we had a problem parsing the following response: ";
    //                qandAObj.question += JSONResponseString;
    //            }
    //        }
    //        else
    //        {
    //            qandAObj.question = "Hmm... We didn't get a resonse back that we could use, please try again.";
    //        }

    //        return qandAObj;
    //    }

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
                Temperature = (float)1.2,
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
            //if (chatCompletion.     GetRawResponse().Status != 200)
            //{
            //    qandAObj.question = "Uh Oh... we got an error in our response from our friends at OpenAI.  ";
            //    qandAObj.question += " Status Code: " + response.GetRawResponse().Status;
            //    qandAObj.question += " Reaseon: " + response.GetRawResponse().ReasonPhrase;
            //    return qandAObj;
            //}
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


        //public OpenAIResponder()
        //{
        //    _httpClient = new HttpClient();
        //}
        //public async Task<QandAObj> GetAzureAIQuestionAsync(string text, string key)
        //{

        //    string QnASystemRequest = "You will be provided Bible verse text (delimited by XML tags), " +
        //        "write a question from this text that can be answered from the same text, then provide the answer. " +
        //        "The output, including both question and answer, should be in the schema specified in the Function request. " +
        //        "The Question should be brief and not include the phrase 'according to'. " +
        //        "The Answer should be short and not include the contents of the question, or restate the question.";


        //    string QnAUserRequest = "<Verse>"
        //                        + text
        //                        + "</Verse>";
                                
        //    ChatCompletionsOptions CCOptions = new ChatCompletionsOptions();

        //    QandAObj qandAObj = new QandAObj();

        //    Azure.AI.OpenAI.ChatMessage QnASystemMessage = new Azure.AI.OpenAI.ChatMessage();
        //    QnASystemMessage.Role = "system";
        //    QnASystemMessage.Content = QnASystemRequest;

        //    Azure.AI.OpenAI.ChatMessage QnAUserMessage = new Azure.AI.OpenAI.ChatMessage();
        //    QnAUserMessage.Role = "user";
        //    QnAUserMessage.Content = QnAUserRequest;

        //    Azure.AI.OpenAI.ChatMessage QnAFunctionMessage = new Azure.AI.OpenAI.ChatMessage();
        //    QnAFunctionMessage.Role = "function";
        //    QnAFunctionMessage.Name = "QnAFunction";
        //    QnAFunctionMessage.Content = "Use QnAFunction Schema Provided";

        //    //List<FunctionDefinition> QnAFuntions = new List<FunctionDefinition>();
        //    FunctionDefinition QnAFunction = new FunctionDefinition();

        //    //JSchemaGenerator generator = new JSchemaGenerator();
        //    //JSchema qnASchema = generator.Generate(typeof(QandAObj));
        //    //string qnASchemaString = qnASchema.ToString();
        //    string qnASchemaString = "{  \"type\": \"object\",  \"properties\": { \"question\": {\"type\": \"string\" }, \"answer\": { \"type\": \"string\" }  },  \"required\": [ \"question\",  \"answer\" ]}";

        //    QnAFunction.Name = "QnAFunction";
        //    QnAFunction.Parameters = BinaryData.FromString(qnASchemaString);

        //    CCOptions.Functions.Add(QnAFunction);
        //    CCOptions.Messages.Add(QnASystemMessage);
        //    CCOptions.Messages.Add(QnAUserMessage);
        //    CCOptions.Messages.Add(QnAFunctionMessage);
        //    CCOptions.ChoiceCount = 1; // we only want one question generated
        //    CCOptions.Temperature = (float)1.2;

        //    OpenAIClient client = new OpenAIClient(key);
        //    Response<ChatCompletions> response = await client.GetChatCompletionsAsync(
        //        OpenAIAPI, // assumes a matching model deployment or model name
        //        CCOptions);
   
        //    // Handling some errors
        //    if (response == null)
        //    {
        //        qandAObj.question = "Uh Oh... We got no response object from our friends at OpenAI. ";
        //        return qandAObj;
        //    }
        //    if (response.GetRawResponse().Status != 200)
        //    {
        //        qandAObj.question = "Uh Oh... we got an error in our response from our friends at OpenAI.  ";
        //        qandAObj.question += " Status Code: " + response.GetRawResponse().Status;
        //        qandAObj.question += " Reaseon: " + response.GetRawResponse().ReasonPhrase;
        //        return qandAObj;
        //    }
        //    if (response.Value == null)
        //    {
        //        qandAObj.question = "Uh Oh... our response object from our friends at OpenAI contained no Value";
        //        return qandAObj;
        //    }

        //    if (response.Value.Choices.Count >= 1)
        //    {
        //        // Very oddly the response may show up on one of two properties. 
        //        string JSONResponseString = response.Value.Choices[0].Message.Content;
        //        // OK This is wierd but sometimes the result shows up on a deprecated property.
        //        if (JSONResponseString == null)
        //        {
        //            JSONResponseString = response.Value.Choices[0].Message.FunctionCall.Arguments;
        //        }
        //        // OK sometimes we may not get back a well formed JSON String... let's handle that. 
        //        try
        //        {
        //            qandAObj = JsonConvert.DeserializeObject<QandAObj>(JSONResponseString);
        //        }
        //        catch 
        //        {
        //            qandAObj.question = "Uh Oh... we had a problem parsing the following response: ";
        //            qandAObj.question += JSONResponseString;
        //        }               
        //    }
        //    else
        //    {
        //        qandAObj.question = "Hmm... We didn't get a resonse back that we could use, please try again.";
        //    }

        //    return qandAObj;
        //}
    }
}

