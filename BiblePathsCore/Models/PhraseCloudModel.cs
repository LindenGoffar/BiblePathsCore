using System.Collections.Generic;
using System.Text;

namespace BiblePathsCore.Models
{
    public class PhraseCloudModel
    {
        public int NumWords { get; set; }
        public string Input { get; set; }
        public List<Phrase> FoundPhrases { get; set; }

        public PhraseCloudModel(string input)
        {
            Input = input;
        }


    }

    public class Phrase
    {
        public string Text { get; set; }
        public int Count { get; set; }

        public Phrase(string text, int count)
        {
            Text = text;
            Count = count;
        }
    }
}
