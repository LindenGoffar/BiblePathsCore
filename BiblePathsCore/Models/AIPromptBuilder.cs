using NuGet.DependencyResolver;
using System;

namespace BiblePathsCore.Models
{
    public class AIPromptBuilder
    {
        // Array of system chat messages we will randomize through. 
        private static readonly string[] SystemChatMessages = new string[]
        {
            "You are a teacher preparing a quiz. " +
                "you will be provided a snippet of Bible text, delimited by an xml <Verse> tag, " +
                "you will write a question from this text that can be answered from the same text." +
                "The question should be brief, and avoid using the phrase 'according to'." +
                "You will also provide the Answer and determine how many points a correct answer will be worth, " +
                "The Answer should be brief and should not include the contents of the question, or restate the question." +
                "points will be an integer between 1 and 6 each independent clause or statement in the answer is worth 1 point, not each word." +
                "Use a default of 1 if points cannot be determined. " +
                "The output, including question, answer, and points should be in the schema specified ",

            "You are a Bible teacher preparing a quiz for high school students. " +
                "you will be provided a snippet of Bible text, delimited by an xml <Verse> tag, " +
                "write a question from this text that can be answered from only the <Verse> text." +
                "The question should be brief, and avoid using the phrase 'according to'." +
                "You will also provide the Answer and determine how many points a correct answer will be worth, " +
                "The Answer should be a few words and should not include the contents of the question, or restate the question." +
                "Points will be an integer between 1 and 6 each independent clause or statement in the answer is worth 1 point, not each word." +
                "Use a default of 1 if points cannot be determined. " +
                "The output, including question, answer, and points should be in the schema specified ",

            "You are a Bible teacher preparing a quiz for elementary students. " + 
                "You will be provided with a snippet of Bible text, delimited by an XML <Verse> tag. " +
                "From this text, write a question that can be answered solely using the <Verse> text. " + 
                "The question should be brief and should not use the phrase 'according to.' " + 
                "Additionally, provide the answer and determine how many points a correct answer will be worth. " + 
                "The answer should be a few words and should not include the contents of the question or restate the question. " + 
                "Points will be an integer between 1 and 6; each independent clause or statement in the answer is worth 1 point. " + 
                "If the points cannot be determined, use a default of 1. " + 
                "The output, including the question, answer, and points, should be in the specified schema."

        };

        // Array of system chat messages we will randomize through. 
        private static readonly string[] AssistantChatMessages = new string[]
        {
            "Here is an example of a verse, question, answer, and points, you might use as reference: \r\n" +
                "verse: Now the LORD said to Joshua: \"Do not be afraid, nor be dismayed; take all the people of war with you, and arise, go up to Ai. See, I have given into your hand the king of Ai, his people, his city, and his land. \r\n" +
                "question: Who did God tell Joshua to take with him to go up to Ai? \r\n" +
                "answer: All the people of war \r\n" +
                "points: 1 \r\n",

            "Here is an example of a verse, question, answer, and points, you might use as reference: \r\n" +
                "verse: Yet if your brother is grieved because of your food, you are no longer walking in love. Do not destroy with your food the one for whom Christ died \r\n" +
                "question: What should you not do if your brother is grieved because of your food? \r\n" +
                "answer: Do not destroy with your food the one for whom Christ died.\r\n" +
                "points: 2 \r\n",

            "Here is an example of a verse, question, answer, and points, you might use as reference: \r\n" +
                "verse: For they being ignorant of God's righteousness, and seeking to establish their own righteousness, have not submitted to the righteousness of God. \r\n" +
                "question: For what two reasons have the children of Israel not submitted to the righteousness of God? \r\n" +
                "answer: being ignorant of God’s righteousness, and seeking to establish their own righteousness \r\n" +
                "points: 2 \r\n"
        };

        // Method to get a random message from the array
        public static string GetRandomSystemChatMessage()
        {
            Random random = new Random();
            int index = random.Next(SystemChatMessages.Length);
            return SystemChatMessages[index];
        }
        public static string GetRandomAssistantChatMessage()
        {
            Random random = new Random();
            int index = random.Next(AssistantChatMessages.Length);
            return AssistantChatMessages[index];
        }
    }
}
