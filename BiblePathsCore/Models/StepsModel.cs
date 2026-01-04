using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiblePathsCore.Models
{
    // Enums generally used in the Steps classes
    public enum StepType { Standard, Commented }
}

namespace BiblePathsCore.Models.DB
{
    public partial class PathNode
    {
        [NotMapped]
        public int FWStepId { get; set; }
        [NotMapped]
        public int BWStepId { get; set; }
        [NotMapped]
        public int StepNumber { get; set; }
        [NotMapped]
        public List<BibleVerse> Verses { get; set; }
        [NotMapped]
        public string VerseText { get; set; }
        [NotMapped]
        public string BookName { get; set; }
        [NotMapped]
        public string PathName { get; set; }
        [NotMapped]
        public int PathStepCount { get; set; }
        [NotMapped]
        public string LegalNote { get; set;  }
        [NotMapped]
        public int PrevChapter { get; set; }
        [NotMapped]
        public int NextChapter { get; set; }
        [NotMapped]
        public string SummaryText { get; set; }
        [NotMapped]
        public int PathType { get; set; }

        public async Task<bool> AddPathStepPropertiesAsync(BiblePathsCoreDbContext context)
        {
            // Calculate Step Number
            StepNumber = (int)Position / 10;
            // Add Fwd and Back Steps
            _ = await AddFwdBackStepAsync(context);
            // Add PathName and StepCount
            _ = await AddPathPropertiesAsync(context);

            // bodge together an 18 character summary text string, mostly for delete dialogs.
            string originalText = Type == (int)StepType.Commented ? Text : BookName + " " + Chapter + "...";
            if (originalText == null)
            {
                SummaryText = "Empty Step";
            }
            else { SummaryText = originalText.Length >= 15 ? originalText.Substring(0, 15) + "..." : originalText; }

            return true;
        }

        public async Task<bool> AddGenericStepPropertiesAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            _ = await AddBookNameAsync(context, BibleId);
            _ = await AddLegalNoteAsync(context, BibleId);
            _ = await AddPrevNextChapters(context, BibleId);
            return true;
        }

        public async Task<bool> AddFwdBackStepAsync(BiblePathsCoreDbContext context)
        {
            // Get BWStepID
            try
            {
                PathNode Node = await context.PathNodes.Where(N => N.PathId == PathId && N.Position < Position).OrderByDescending(L => L.Position).FirstAsync();
                BWStepId = Node.Id;
            }
            catch (InvalidOperationException) { BWStepId = 0;  }

            // Get FWStepID
            try
            {
                PathNode Node = await context.PathNodes.Where(N => N.PathId == PathId && N.Position > Position).OrderBy(L => L.Position).FirstAsync();
                FWStepId = Node.Id;
            }
            catch (InvalidOperationException) { FWStepId = 0; }

            return true;
        }

        public async Task<bool> AddBookNameAsync(BiblePathsCoreDbContext context, string bibleId)
        {
            // Get BookName 
            BibleBook Book = await context.BibleBooks.Where(B => B.BibleId == bibleId && B.BookNumber == BookNumber).FirstAsync();
            BookName = Book.Name;

            return true;
        }

        public async Task<bool> AddPathPropertiesAsync(BiblePathsCoreDbContext context)
        {
            try
            {
                Path path = await context.Paths.FindAsync(PathId);
                PathName = path.Name;
                PathStepCount = Path.StepCount;
                PathType = Path.Type;
            }
            catch {
                return false; 
            }
            return true;
        }

        public async Task<bool> ValidateBookChapterAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            if (await context.BibleVerses.Where(v => v.BibleId == BibleId && v.BookNumber == BookNumber && v.Chapter == Chapter).AnyAsync())
            {
                return true;
            }
            return false;
        }

        public async Task<int> GetChapterLengthAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            int retVal = 0;
            try
            {
                retVal = (int)await context.BibleChapters.Where(C => C.BibleId == BibleId
                                                           && C.BookNumber == BookNumber
                                                           && C.ChapterNumber == Chapter)
                                                    .Select(C => C.Verses)
                                                    .SingleAsync();
            }
            catch
            {
                // Don't need to do anything here. 
            }
            return retVal;
        }

        public async Task<string> GetValidBibleIdAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            string RetVal = Bible.DefaultBibleId;
            if (BibleId != null)
            {
                if (await context.Bibles.Where(B => B.Id == BibleId).AnyAsync())
                {
                    RetVal = BibleId;
                }
            }
            return RetVal;
        }
        public async Task<bool> AddLegalNoteAsync(BiblePathsCoreDbContext context, string BibleId)
        {
            Bible Bible = await context.Bibles.FindAsync(BibleId);
            Bible.HydrateBible();
            LegalNote = Bible.LegalNote;
            return true;
        }
        private async Task<bool> AddPrevNextChapters(BiblePathsCoreDbContext context, string BibleId)
        {
            PrevChapter = Chapter - 1;
            NextChapter = Chapter + 1;
            NextChapter = (await context.BibleChapters.Where(c => c.BibleId == BibleId && c.BookNumber == BookNumber && c.ChapterNumber == NextChapter).AnyAsync()) ? NextChapter : 0;
            return true;
        }

        public async Task<List<BibleVerse>> GetBibleVersesAsync(BiblePathsCoreDbContext context, string bibleId, bool inPathOnly, bool includeProximity)
        {
            List<BibleVerse> bibleVerses = new List<BibleVerse>();
            // First retrieve all of the verses, 
            if (inPathOnly)
            {
                bibleVerses = await context.BibleVerses.Where(v => v.BibleId == bibleId && v.BookNumber == BookNumber && v.Chapter == Chapter && v.Verse >= StartVerse && v.Verse <= EndVerse).OrderBy(v => v.Verse).ToListAsync();
            }
            else
            {
                bibleVerses = await context.BibleVerses.Where(v => v.BibleId == bibleId && v.BookNumber == BookNumber && v.Chapter == Chapter).OrderBy(v => v.Verse).ToListAsync();
            }
            if (includeProximity)
            {
                foreach (BibleVerse verse in bibleVerses)
                {
                    verse.InPath = false; //default all verses to false
                    if (verse.Verse < StartVerse) { verse.Proximity = StartVerse - verse.Verse; }
                    if ((verse.Verse >= StartVerse) && (verse.Verse <= EndVerse))
                    {   //These two are essentially the same thing. 
                        verse.InPath = true;
                        verse.Proximity = 0;
                    }
                    if (verse.Verse > EndVerse) { verse.Proximity = verse.Verse - EndVerse; }

                    if (inPathOnly && verse.InPath == false)
                    {
                        bibleVerses.Remove(verse);
                    }
                }
            }
            return bibleVerses;
        }
        public bool AddVerseText()
        {
            bool RetVal = true;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} {1}: ", BookName, Chapter.ToString());
            if (EndVerse > StartVerse) { sb.AppendFormat("{0}-{1}", StartVerse.ToString(), EndVerse.ToString()); }
            if (EndVerse > StartVerse) { sb.AppendFormat("{0}", StartVerse.ToString()); }
            foreach (BibleVerse Verse in Verses)
            {
                sb.Append(Verse.Text);
                sb.Append(' ');
            }
            VerseText = sb.ToString();
            return RetVal;
        }
        public async Task<bool> RegisterReadEventsAsync(BiblePathsCoreDbContext context, bool FullPathRead = false)
        {
            // there are two scenarios we should register events for 
            // 1. If this is the second step in a path we register a Read, this avoids false reads for accidental clicks.
            // 2. If this is the final step in the path we register a Finished
            if ((Position > 14 && Position < 21) || FullPathRead) // by some fluke second step may be between 15 and 20...
            {
                if (Path == null ) { context.Entry(this).Reference(s => s.Path).Load(); }
                _ = await Path.RegisterEventAsync(context, EventType.PathStarted, null);
            }
            if (this.FWStepId == 0 || FullPathRead) // this is the last step
            {
                if (Path == null) { context.Entry(this).Reference(s => s.Path).Load(); }
                _ = await Path.RegisterEventAsync(context, EventType.PathCompleted, null);


                // Conditionally Apply a new Rating to this path... also adds a summary if not present.
                if (Path.Reads % 10 == 0)
                {
                    _ = await Path.ApplyPathRatingAsync(context);
                }

                // Now we need to increment Read Count... 
                context.Attach(Path).State = EntityState.Modified;
                Path.Reads++;
                await context.SaveChangesAsync();
            }

            return true;
        }
    }
}
