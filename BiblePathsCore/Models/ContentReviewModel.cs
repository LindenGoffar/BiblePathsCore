using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BiblePathsCore.Models
{
    public class ContentReview
    {
        const string CensoredText = "[Censored]";
        const string PatternTemplate = @"\b({0})(s?)\b";
        const RegexOptions Options = RegexOptions.IgnoreCase;
        public HashSet<string> FoundWords { get; set; }
        public string Input { get; set; }

        // Please do not associate the following words with this application, this is used as a sanity check
        // on the content of user generated content only. 
        private readonly string[] badWords = new[] { "anal", "anus", "arse", "ass", "ballsack", "balls", "bitch", "biatch",
                                                    "blowjob", "bollock","bollok", "boner", "boob", "butt", "buttplug",
                                                     "clitoris", "cock", "crap", "cunt", "damn", "dick", "dildo", "dyke",
                                                    "fag", "feck", "fellate", "fellatio", "felching", "fuck", "fudgepacker",
                                                    "goddamn", "homo", "jerk", "jizz", "knobend", "labia", "lmao", "lmfao",
                                                    "muff", "nigger", "nigga", "omg", "penis", "piss", "poop", "prick", "pube",
                                                    "pussy", "queer", "scrotum", "shit", "sh1t", "slut", "smegma", "spunk", 
                                                    "tit", "turd", "twat", "vagina", "wank", "whore", "wtf" };

        public ContentReview(string input)
        {
            Input = input;
            FoundWords = new HashSet<string>();
        }

        public string GetCensoredString()
        {
            IEnumerable<Regex> badWordMatchers = badWords.
                Select(x => new Regex(string.Format(PatternTemplate, x), Options));

            string output = badWordMatchers.
               Aggregate(Input, (current, matcher) => matcher.Replace(current, CensoredText));

            return output;
        }
        public int FindBannedWords()
        {
            foreach (string badWord in badWords)
            {
                // let's add a badWordMatcher to our list of matchers
                Regex badWordMatcher = new(string.Format(PatternTemplate, badWord), Options);
                if (badWordMatcher.IsMatch(Input))
                {
                    _ = FoundWords.Add(badWord); // It's a hashset so only unique values are added
                }
            }
            return FoundWords.Count;
        }


    }
}
