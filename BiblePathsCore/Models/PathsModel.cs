using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Collections;
using Microsoft.AspNetCore.Mvc;

namespace BiblePathsCore.Models
{
    // Enums generally used in the Paths class
    public enum EventType { PathStarted, PathCompleted, NonOwnerEdit, DirectReference, PathDeleted, UserRating }
    public enum SortBy { HighestRated, Newest, Shortest, Reads }

}

namespace BiblePathsCore.Models.DB
{
    public partial class Path
    {
        [NotMapped]
        public int FirstStepId { get; set; }
        [NotMapped]
        public string LengthInMinutes { get; set; }
        [NotMapped]
        public string PathLink { get; set; }
        public void SetInitialProperties(string OwnerEmail)
        {
            // Set key properties that will not be supplied by the user on creation
            Owner = OwnerEmail;
            Length = 0;
            ComputedRating = (decimal)4.5; // this is just a default it will change.
            Created = DateTime.Now;
            Modified = DateTime.Now;
            Topics = "";
            IsPublished = false;
            IsDeleted = false;
            StepCount = 0;
            Reads = 0;
        }

        public async Task<bool> AddCalculatedPropertiesAsync(BiblePathsCoreDbContext context)
        {
            _ = await AddFirstStepIdAsync(context);
            LengthInMinutes = GetLengthInMinutesString();
            PathLink = "https://www.BiblePaths.Net/Paths/" + Name; 
            return true;
        }
        public async Task<bool> AddFirstStepIdAsync(BiblePathsCoreDbContext context)
        {
            int TempID = 0;
            try
            {
                TempID = (await context.PathNodes.Where(s => s.PathId == Id).OrderBy(s => s.Position).FirstAsync()).Id;
            }
            catch 
            {
                // If there is no FirstStep we acccept that and move on setting FirstStep ID = 0
            }
            FirstStepId = TempID;
            return true;
        }
        public bool IsValidPathEditor(string UserEmail)
        {
            if (string.IsNullOrEmpty(UserEmail))
            {
                return false;
            }
            if (IsPublicEditable || Owner.ToLower() == UserEmail.ToLower()) 
            { 
                return true; 
            }
            // worst case we'll return false 
            return false; 
        }
        public bool IsPathOwner(string UserEmail)
        {
            if (Owner.ToLower() == UserEmail.ToLower())
            {
                return true; 
            }
            else
            {
                return false;
            }
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
                else
                {
                    // Let's try the Paths OwnerBibleId
                    if (await context.Bibles.Where(B => B.Id == OwnerBibleId).AnyAsync())
                    {
                        RetVal = OwnerBibleId;
                    }
                }
            }
            return RetVal;             
        }
        public async Task<int> GetPathVerseCountAsync(BiblePathsCoreDbContext context)
        {
            int retVal = 0;
            List<PathNode> pathNodes = await context.PathNodes.Where(N => N.PathId == Id).ToListAsync();
            foreach (PathNode node in pathNodes)
            {
                retVal += (node.EndVerse - node.StartVerse + 1);
            }
            return retVal;
        }

        private string GetLengthInMinutesString()
        {
            // Verses: 31,102 Words: 783,137 roughly 25 words per verse. 
            // Length is a count of verses in the path. 
            // an average adult reads ~200 Words per minute we'll decrement by 10% to account for switching between steps.
            // so we have an average read time of 180 Words Per minute or 7.2 verses per minute. 
            double AverageVersesPerMinute = 7.2;
            double TimeToRead = Length / AverageVersesPerMinute;
            int MinutesToRead = (int)Math.Ceiling(TimeToRead);
            string LengthInMinutes = MinutesToRead.ToString() + " min";
            return LengthInMinutes;
        }

        public async Task<List<BibleVerse>> GetPathVersesAsync(BiblePathsCoreDbContext context, String BibleId)
        {
            List<BibleVerse> returnVerses = new List<BibleVerse>();
            List<PathNode> pathNodes = await context.PathNodes.Where(N => N.PathId == Id).OrderBy(P => P.Position).ToListAsync();
            foreach (PathNode node in pathNodes)
            {
                returnVerses.AddRange(await context.BibleVerses
                   .Where(v => v.BibleId == BibleId && v.BookNumber == node.BookNumber && v.Chapter == node.Chapter && v.Verse >= node.StartVerse && v.Verse <= node.EndVerse)
                   .ToListAsync());
            }
            return returnVerses;
        }
        // Note this is a static class it is not called with an instance of a path object. 
        public static async Task<bool> PathNameAlreadyExistsStaticAsync(BiblePathsCoreDbContext context, string CheckName)
        {
            var knownTerms = new[] { "create", "delete", "edit", "index", "mypaths", "path", "pathcomplete", "publish", "steps", "unpublish" };
            if (knownTerms.Contains(CheckName.ToLower()))
            {
                return true;
            }
            if (await context.Paths.Where(p => p.Name.ToLower() == CheckName.ToLower()).AnyAsync())
            {
                return true; 
            }
            return false;
        }

        public async Task<bool> RedistributeStepsAsync(BiblePathsCoreDbContext context)
        {
            int DefaultInterval = 10;
            int NextPosition = 10;
            // Build a list of all steps in the path after "FromPosition", then iterate 
            // Through this list and re-position the remaining nodes if necessary. 
            try
            {
                List<PathNode> pathNodes = await context.PathNodes.Where(N => N.PathId == Id).OrderBy(L => L.Position).ToListAsync();
                foreach (PathNode node in pathNodes)
                {
                    if (node.Position != NextPosition)
                    {
                        context.Attach(node);
                        node.Position = NextPosition;
                        await context.SaveChangesAsync();
                    }
                    NextPosition += DefaultInterval;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        // This method returns a list of Path objects related to the current path
        // These can be referenced in the PathCompleted view or 
        // elsewhere as determined helpful. 
        public async Task<List<Path>> GetRelatedPathsAsync(BiblePathsCoreDbContext context)
        {
            Hashtable UniqueRelatedPaths = new Hashtable();

            List<Path> RelatedPaths = new List<Path>();

            // We need to load the collection of Steps assocaited with this Path. 
            context.Entry(this)
                .Collection(p => p.PathNodes)
                .Load();

            foreach (PathNode Step in PathNodes)
            {
                // Let's go fetch any other PathNodes that match the Book/Chapter/Verse combo
                // this means any steps that start between the curent Steps range of verses... 
                // NOTE: The above described logic may not be intuitive, what if the step ends with in the curent steps range? 

                // TODO Perf: this likely becomes expensive over time. 
                List<PathNode> dbNodes = await context.PathNodes.Include(N => N.Path).Where(N => N.BookNumber == Step.BookNumber && N.Chapter == Step.Chapter && N.StartVerse >= Step.StartVerse && N.StartVerse <= Step.EndVerse && N.PathId != Id).ToListAsync();

                if (dbNodes.Count > 0)
                {
                    // Iterate through the related step nodes to see if it is part of a unique published path
                    foreach (PathNode entry in dbNodes)
                    {
                        // At this point we only know there is another Step associated with this Step, but is the Path published? 
                        if (entry.Path != null)
                        {
                            if (entry.Path.IsPublished && !UniqueRelatedPaths.ContainsKey(entry.Path.Id))
                            {
                                UniqueRelatedPaths.Add(entry.Path.Id, entry.Path.Name); // used only to ensure uniqueness.
                                // looks to be a published and unique path, 
                                if (!RelatedPaths.Contains(entry.Path))
                                {
                                    RelatedPaths.Add(entry.Path);
                                }
                            }
                        }
                    }
                }
            }
            return RelatedPaths;
        }
        public async Task<bool> RegisterEventAsync(BiblePathsCoreDbContext context, EventType eventType, string EventData)
        {
            PathStat stat = new PathStat
            {
                PathId = Id,
                EventType = (int)eventType,
                EventData = EventData,
                EventWritten = DateTime.Now
            };
            context.PathStats.Add(stat);
            if (await context.SaveChangesAsync() == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> ApplyPathRatingAsyc(BiblePathsCoreDbContext context)
        {
            // This Rating System is likely to change over time but for now we've got the following rules. 
            // Rating is the average of the following Scores ranging from 0 - 5 (there is a little arbitrary uplift)
            // 1. Initial Rating on entry to this method counts as one Rating (all paths start at 4.5)
            // 2. A Rating is calculated from the % of Reads (FinishCount / StartCount * 100) this is a % of 5
            // 3. A "Book Diversity Rating" where a path gets 1 point for each unique Book and a point for spanning testaments up to 5
            // 4. Average of all UserRatings (uplift creates a 1.1 - 5.5 range)

            int firstNTBook = 40; // the first book in the New Testemant is book 40 in the protestant Bible.
            int ScoreCount = 0; // this becomes the number of total Scores that we will average together. 
            double TotalScore = 0;
            // load all of the PathStats for this Path... we'll need these 
            // We need to load the collection of Steps assocaited with this Path, as well as the Nodes. 
            context.Entry(this)
                .Collection(p => p.PathStats)
                .Load();
            context.Entry(this)
                .Collection(p => p.PathNodes)
                .Load();

            // 1. Initial Rating on entry to this method counts as one Rating (all paths start at 4.5)
            if (ComputedRating.HasValue)
            {
                TotalScore += (double)ComputedRating;
                ScoreCount++;
            }

            // 2. A Rating is calculated from the % of Reads (FinishCount / StartCount * 100) this is a % of 5
            int NumStarts = PathStats.Where(s => s.EventType == (int)EventType.PathStarted).ToList().Count;
            if (NumStarts > 0)
            { 
                int NumCompletes = PathStats.Where(s => s.EventType == (int)EventType.PathStarted).ToList().Count;
                double ReadPercent = NumCompletes / NumStarts;
                TotalScore += ReadPercent * 5;
                ScoreCount++;
            }

            // 3. A "Book Diversity Rating" where a path gets 1 point for each unique Book up to 5
            if (PathNodes.Count > 0)
            {
                int BookDiversityScore = 0;
                var BookHash = new HashSet<int>();
                foreach (PathNode node in PathNodes)
                {
                    BookHash.Add(node.BookNumber);
                }
                BookDiversityScore += BookHash.Count();
                // Does the path span testaments? 
                if ((BookHash.Max() >= firstNTBook) && (BookHash.Min() < firstNTBook))
                {
                    BookDiversityScore += 1; // Add a free point for spanning testaments.  
                }

                TotalScore += BookDiversityScore > 5 ? 5 : BookDiversityScore;
                ScoreCount++;                
            }

            // 4. Average of all UserRatings
            List<PathStat> UserRatings = PathStats.Where(s => s.EventType == (int)EventType.UserRating).ToList();
            int SumRatings = 0;
            int NumRatings = 0; 
            foreach (PathStat UserRating in UserRatings)
            {
                int Rating = 0;
                try
                {
                    Rating = int.Parse(UserRating.EventData);
                }
                catch
                {
                    continue;
                }
                if (Rating > 0 && Rating <= 5)
                {
                    SumRatings += Rating;
                    NumRatings++; 
                }
            }
            double AvgUserRating = SumRatings / NumRatings; 
            if (AvgUserRating > 0 && AvgUserRating <= 5)
            {
                TotalScore += (AvgUserRating * 1.1); // Make this count by applying a small uplift. 
                ScoreCount++;
            }

            // Ok now it's time to calculate a our new rating. 
            double TempRating = TotalScore / ScoreCount;
            TempRating = TempRating > 5 ? 5 : TempRating;
            TempRating = TempRating < 0 ? 0.5 : TempRating;

            // Save our Rating Now. 
            context.Attach(this).State = EntityState.Modified;
            ComputedRating = (decimal)TempRating;
            await context.SaveChangesAsync();

            return true; 
        }
    }
}
