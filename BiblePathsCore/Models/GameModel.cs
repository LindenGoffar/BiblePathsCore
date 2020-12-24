using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class GameGroup
    {
        public enum GameGroupState { Open, InPlay, SelectPath, Closed}
        public static async Task<List<SelectListItem>> GetPathSelectListAsync(BiblePathsCoreDbContext context)
        {
            List<SelectListItem> PathSelectList = new List<SelectListItem>();

            List<Path> Paths = await context.Paths
                                                .Where(P => P.IsDeleted == false
                                                        && P.IsPublished == true)
                                                .OrderBy(P => P.StepCount)
                                                .ToListAsync();
            // Add a Default entry 
            PathSelectList.Add(new SelectListItem
            {
                Text = "Select a Bible Path",
                Value = 0.ToString()
            });

            // Add our BookLists first
            foreach (Path path in Paths)
            {
                PathSelectList.Add(new SelectListItem
                {
                    Text = path.Name + " - " + path.StepCount + " Steps",
                    Value = path.Id.ToString()
                });
            }
            return PathSelectList;
        }
    }

    public partial class GameTeam
    {
        public enum GameBoardState { Initialize, WordSelect, WordSelectOffPath, StepSelect, StepSelectOffPath, Completed, Closed }

        [NotMapped]
        public List<PathNode> Steps { get; set; }

        public async Task<List<SelectListItem>> GetKeyWordSelectListAsync(BiblePathsCoreDbContext context, PathNode CurrentStep)
        {
            List<SelectListItem> KeyWordSelectList = new List<SelectListItem>();
            // Find all the unique words in our Curent Step
            Hashtable UniqueWords = new Hashtable();
            StringBuilder StepText = new StringBuilder();
            foreach (BibleVerse Verse in CurrentStep.Verses)
            {
                StepText.Append(Verse.Text + " ");
            }
            String text = StepText.ToString().ToLower();
            var punctuation = text.Where(Char.IsPunctuation).Distinct().ToArray();
            var words = text.Split().Select(x => x.Trim(punctuation));
            foreach (string word in words)
            {
                if (word.Length > 2 && !UniqueWords.ContainsKey(word))
                {
                    int occurs = 0;
                    try
                    {
                        BibleNoiseWord noiseWord = await context.BibleNoiseWords.Where(w => w.NoiseWord == word).FirstAsync();
                        if (noiseWord.IsNoise == true) { occurs = 0; }
                        else { occurs = noiseWord.Occurs; }
                    }
                    catch
                    {
                        // Assume we couldn't find the word in our NoiseWord List
                        occurs = 0;
                    }
                    UniqueWords.Add(word, occurs);
                }
            }

            // Now iterate our HasthTable adding only non-noise words 
            // that occur more than 10 times in the whole bible
            foreach(string UniqueWord in UniqueWords.Keys)
            {
                if ((int)UniqueWords[UniqueWord] > 10)
                {
                    KeyWordSelectList.Add(new SelectListItem
                    {
                        Text = UniqueWord,
                        Value = UniqueWord
                    });
                }
            }
            return KeyWordSelectList;
        }

        public async Task<List<PathNode>> GetTeamStepsAsync(BiblePathsCoreDbContext context, string BibleId)
        {

            List<PathNode> ReturnSteps = new List<PathNode>();
            // The goal is to return a set of random Steps + Current Step, all related to Keyword
            // Get our current step and all details 
            PathNode CurrentStep = await context.PathNodes.FindAsync(CurrentStepId);
            _ = await CurrentStep.AddGenericStepPropertiesAsync(context, BibleId);
            _ = await CurrentStep.AddPathStepPropertiesAsync(context);
            CurrentStep.Verses = await CurrentStep.GetBibleVersesAsync(context, BibleId, true, false);
            ReturnSteps.Add(CurrentStep);

            int VerseCount = (CurrentStep.EndVerse - CurrentStep.StartVerse) + 1;

            // Build A hashtable of our current verses so we can exclude them.
            Hashtable CurrentVerses = new Hashtable();
            foreach (BibleVerse currentVerse in CurrentStep.Verses)
            {
                CurrentVerses.Add(currentVerse.Id, null);
            }

            // Find all verses with associated with KeyWord, not in our Current Step verses
            List<BibleVerse> FoundVerses = await context.BibleVerses.Where(V => V.BibleId == BibleId
                                                                            && V.Text.Contains(KeyWord))
                                                                        .ToListAsync();
            var rand = new Random();

            // now select 6 from this set randomly using a HashTable to ensure Uniqueness
            // We select 6 and throw one or more out to reduce impact from duplicates in randomization.
            Hashtable UniqueVerses = new Hashtable();
            for (int i = 0; i < 6; i++)
            {
                int VerseIndex = rand.Next(FoundVerses.Count);
                if (!UniqueVerses.ContainsKey(FoundVerses[VerseIndex].Id))
                {
                    // We don't want any of our CurrentVerses either. 
                    if (!CurrentVerses.ContainsKey(FoundVerses[VerseIndex].Id))
                    {
                        UniqueVerses.Add(FoundVerses[VerseIndex].Id, null);
                    }
                }
            }
            // We need 4 random Unique "Ids" as well we'll use these to sort the bodgedSteps
            // We use a range around CurrentStep to make it difficult to guess based on Id. 
            Hashtable UniqueIds = new Hashtable();
            int LowRange = CurrentStep.Id - 10;
            if (LowRange < 0) { LowRange = 0; }
            int HighRange = CurrentStep.Id + 10;
            int KnownGood = HighRange + 1;

            // now we've got a selected set of unique verses let's build appropriate sized Steps out of them. 
            foreach (int VerseId in UniqueVerses.Keys)
            {
                BibleVerse FoundVerse = FoundVerses.Where(V => V.Id == VerseId).First();

                // Pick a Random ID 
                int RandomID = rand.Next(LowRange, HighRange);
                if (!UniqueIds.ContainsKey(RandomID) && RandomID != CurrentStep.Id)
                {
                    UniqueIds.Add(RandomID, null);
                }
                else
                {
                    RandomID = KnownGood;
                    UniqueIds.Add(KnownGood, null);
                    KnownGood++;
                }

                // Build our Bodged Step 
                PathNode bodgedStep = new PathNode();
                bodgedStep.Id = RandomID;
                bodgedStep.BookNumber = FoundVerse.BookNumber;
                bodgedStep.Chapter = FoundVerse.Chapter;
                int ChapterLen = await bodgedStep.GetChapterLengthAsync(context, BibleId);
                if (VerseCount > 1)
                {
                    int Increment = (int)VerseCount / 2;
                    bodgedStep.StartVerse = (FoundVerse.Verse - Increment + 1 > 0) ? (FoundVerse.Verse - Increment + 1) : FoundVerse.Verse;
                    bodgedStep.EndVerse = (FoundVerse.Verse + Increment <= ChapterLen) ? (FoundVerse.Verse + Increment) : FoundVerse.Verse;

                }
                else
                {
                    bodgedStep.StartVerse = FoundVerse.Verse;
                    bodgedStep.EndVerse = FoundVerse.Verse;
                }
                _ = await bodgedStep.AddGenericStepPropertiesAsync(context, BibleId);
                bodgedStep.Verses = await bodgedStep.GetBibleVersesAsync(context, BibleId, true, false);
                if (ReturnSteps.Count < 4)
                { ReturnSteps.Add(bodgedStep); }
                else 
                { return ReturnSteps.OrderBy(S => S.Id).ToList(); }
            }
            return ReturnSteps.OrderBy(S => S.Id).ToList();
        }

        public static async Task<string> GetValidBibleIdAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            string RetVal = Bible.DefaultPBEBibleId;
            if (BibleId != null)
            {
                if (await context.Bibles.Where(B => B.Id == BibleId).AnyAsync())
                {
                    RetVal = BibleId;
                }
            }
            return RetVal;
        }

    }
}
