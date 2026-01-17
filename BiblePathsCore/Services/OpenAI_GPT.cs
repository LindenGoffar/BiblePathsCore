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
        public Task<QandAObj> GetAIFixedQuestionAsync(string Versetext, string InitialQuestion, string InitialAnswer, string ChallengeComment, int Points);
        public Task<VerseTongueObj> GetAIVerseTongueAsync(BibleVerse verse, string FromLanguage, string ToLanguage);
        public Task<String> GetPathSummaryAsync(string Pathtext);
    }

    public class OpenAIResponder : IOpenAIResponder
    {
        public const string OpenAIAPI = "gpt-5.2";
        private readonly OpenAISettings _openAIsettings;
        //private readonly HttpClient _httpClient;

        public OpenAIResponder(IOptions<OpenAISettings> openAISettings)
        {
            _openAIsettings = openAISettings.Value;
        }

        public async Task<String> GetPathSummaryAsync(string Pathtext)
        {
            string retVal = "";

            string QnASystemRequest = "You are a Christian pastor, your task is to provide a brief summary of the " +
                "provided Bible verses within a Bible Path delimited by an xml <PathVerses> tag. " +
                "The summary should be limited to 1024 characters, and no more than 6 sentences. " +
                "Please capture the main themes across all of the verses focusing on key learnings and" +
                "providing spiritiual guidance as indicated within the verse text. " +
                "Always refer to the collection of verses using the term Path or this Path. " +
                "Please provide the summary in clear and concise language suitable for a general audience.";


            string QnAUserRequest = "<PathVerses>"
                                + Pathtext
                                + "</PathVerses>";

            string key = _openAIsettings.OpenAIAPIKey;

            ChatClient client = new(OpenAIAPI, key);

            ChatCompletionOptions options = new();

            try
            {
                ChatCompletion chatCompletion = await client.CompleteChatAsync(
                    [new SystemChatMessage(QnASystemRequest),
                    new UserChatMessage(QnAUserRequest)],
                    options);

                //// Handling some errors
                if (chatCompletion == null)
                {
                    retVal = "Uh Oh... We got no response object from our friends at OpenAI. ";
                    return retVal;
                }

                if (chatCompletion.Content == null)
                {
                    retVal = "Uh Oh... our response object from our friends at OpenAI contained no Content";
                    return retVal;
                }

                if (chatCompletion.Content.Count >= 1)
                {
                    // Very oddly the response may show up on one of two properties. 
                    retVal = chatCompletion.Content[0].Text;
                }
                else
                {
                    retVal = "Hmm... We didn't get a response back that we could use, please try again.";
                    return retVal;
                }
            }
            catch (HttpRequestException ex)
            {
                // Network or HTTP error
                retVal = "Uh Oh... We experienced a network or HTTP error: " + ex.Message;
            }
            catch (RequestFailedException ex)
            {
                // Azure OpenAI specific error
                retVal = "Uh Oh... We experienced an Azure OpenAI error: " + ex.Message;
            }
            catch (Exception ex)
            {
                // Other errors
                retVal = "Uh Oh...We encountered an unexpected error: " + ex.Message;
            }

            return retVal;
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

            try
            {
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
            }
            catch (HttpRequestException ex)
            {
                // Network or HTTP error
                qandAObj.question = "Uh Oh... We experienced a network or HTTP error: " + ex.Message;
            }
            catch (RequestFailedException ex)
            {
                // Azure OpenAI specific error
                qandAObj.question = "Uh Oh... We experienced an Azure OpenAI error: " + ex.Message;
            }
            catch (Exception ex)
            {
                // Other errors
                qandAObj.question = "Uh Oh...We encountered an unexpected error: " + ex.Message;
            }

            return qandAObj;
        }

        public async Task<QandAObj> GetAIFixedQuestionAsync(string Versetext, string InitialQuestion, string InitialAnswer, string ChallengeComment, int Points)
        {

            string FixQuestionSystemRequest = "You are a teacher reviewing Questions for a Bible quiz" +
                "you are provided one or more Bible verses in the User Message delimited by an xml <Verse> tag, a proposed question in an xml <Question> tag" +
                "a proposed answer in an xml <Answer> tag, a comment challenging the validity of the question and/or answer in an xml <Comment> tag, " +
                "and a proposed point value in an xml <Points> tag. " +
                "Please review the entire User Message before formulating a more appropriate Question, Answer, and Points value using as much as possible of the " +
                "initial question and answer while addressing the concerns raised in the comment. " +

                "make sure the question can be answered entirely and exclusively from the provided Bible verses. " +
                "The question should be brief, plese avoid using the phrase 'according to','accordingto the text', " +
                "or referencing 'the author' or 'the speaker' unless they are named in the verse(s)." +

                "Next you should ensure the Answer is concise and correct per the provided Bible verses. " +
                "The Answer should not include the contents of the question, or restate the question. " +

                "Finally you will determine how many points your Answer is worth. " +
                "Points will be an integer between 1 and 6 each independent clause or statement in the answer is worth 1 point, not each word." +
                "Use a default of 1 if points cannot be determined. " +

                "The output, including question, answer, and points should be in the schema specified ";

            string FixQuestionUserRequest = "<Verse>"
                                + Versetext
                                + "</Verse>"
                                + "<Question>"
                                + InitialQuestion
                                + "</Question>" 
                                + "<Answer>"
                                + InitialAnswer
                                + "</Answer>"
                                + "<Comment>"
                                + ChallengeComment
                                + "</Comment>"
                                + "<Points>"
                                + Points.ToString()
                                + "</Points>";

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

            try
            {
                ChatCompletion chatCompletion = await client.CompleteChatAsync(
                    [new SystemChatMessage(FixQuestionSystemRequest),
                    new UserChatMessage(FixQuestionUserRequest)],
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
            }
            catch (HttpRequestException ex)
            {
                // Network or HTTP error
                qandAObj.question = "Uh Oh... We experienced a network or HTTP error: " + ex.Message;
            }
            catch (RequestFailedException ex)
            {
                // Azure OpenAI specific error
                qandAObj.question = "Uh Oh... We experienced an Azure OpenAI error: " + ex.Message;
            }
            catch (Exception ex)
            {
                // Other errors
                qandAObj.question = "Uh Oh...We encountered an unexpected error: " + ex.Message;
            }

            return qandAObj;
        }

        public async Task<VerseTongueObj> GetAIVerseTongueAsync(BibleVerse verse, string FromLanguage, string ToLanguage)
        {          
            string TongueSystemRequest = "You are a Bible Scholar with a background in linguistics. " +
                "You will be provided a Bible verse in the User Message delimited by an xml <Verse> tag." +
                "Please review the entire User Message before formulating a response." +
                "Your task is to provide an ordered list of words from the verse, their order in the verse," +
                "and persist any punctuation with the word it is associated with." +
                "You will also return the words translation in " +
                ToLanguage +
                "and the words pronunciation in " +
                FromLanguage +

                "The output will include the the word with punctuation, it's position (order), a translation, and pronunciation. " +
                "These should be returned in the schema specified in the Function request.";
               

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
                      "required": ["usermessage", "words"],
                      "additionalProperties": false
                    }
                    
                    """),
                    jsonSchemaIsStrict: true),
                Temperature = (float)1,
            };

            VerseTongueObj verseTongueObj = new();

            try
            {
                ChatCompletion chatCompletion = await client.CompleteChatAsync(
                    [new SystemChatMessage(TongueSystemRequest),
                    new UserChatMessage(TongueUserRequest)],
                    options);

                //// Handling some specific errors
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
            }
            catch (HttpRequestException ex)
            {
                // Network or HTTP error
                verseTongueObj.usermessage = "Uh Oh... We experienced a network or HTTP error when chatting with OpenAI: " + ex.Message;
            }
            catch (RequestFailedException ex)
            {
                // Azure OpenAI specific error
                verseTongueObj.usermessage = "Uh Oh... We experienced an OpenAI specific error: " + ex.Message;
            }
            catch (Exception ex)
            {
                // Other errors
                verseTongueObj.usermessage = "Uh Oh...We encountered an unexpected error: " + ex.Message;
            }

            return verseTongueObj;
        }
    }
}

