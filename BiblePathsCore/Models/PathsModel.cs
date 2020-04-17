using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models
{
    // Enums generally used in the Paths class
    public enum EventType { PathStarted, PathCompleted, NonOwnerEdit, DirectReference, PathDeleted }
    public enum SortBy { HighestRated, Newest, Shortest, Reads }

}

namespace BiblePathsCore.Models.DB
{
    public partial class Paths
    {
        [NotMapped]
        public int FirstStepId { get; set; }
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
            string RetVal = Bibles.DefaultBibleId;
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
            List<PathNodes> pathNodes = await context.PathNodes.Where(N => N.PathId == Id).ToListAsync();
            foreach (PathNodes node in pathNodes)
            {
                retVal += (node.EndVerse - node.StartVerse + 1);
            }
            return retVal;
        }

        public async Task<List<BibleVerses>> GetPathVersesAsync(BiblePathsCoreDbContext context, String BibleId)
        {
            List<BibleVerses> returnVerses = new List<BibleVerses>();
            List<PathNodes> pathNodes = await context.PathNodes.Where(N => N.PathId == Id).OrderBy(P => P.Position).ToListAsync();
            foreach (PathNodes node in pathNodes)
            {
                returnVerses.AddRange(await context.BibleVerses
                   .Where(v => v.BibleId == BibleId && v.BookNumber == node.BookNumber && v.Chapter == node.Chapter && v.Verse >= node.StartVerse && v.Verse <= node.EndVerse)
                   .ToListAsync());
            }
            return returnVerses;
        }

        public async Task<bool> RedistributeStepsAsync(BiblePathsCoreDbContext context,  int FromPosition)
        {
            int DefaultInterval = 10;
            int NextPosition = FromPosition + DefaultInterval;
            // Build a list of all steps in the path after "FromPosition", then iterate 
            // Through this list and re-position the remaining nodes if necessary. 
            try
            {
                List<PathNodes> pathNodes = await context.PathNodes.Where(N => N.PathId == Id && N.Position >= FromPosition).OrderBy(L => L.Position).ToListAsync();
                foreach (PathNodes node in pathNodes)
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
        public async Task<bool> RegisterEventAsync(BiblePathsCoreDbContext context, EventType eventType, string EventData)
        {
            PathStats stat = new PathStats
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
    }
}
