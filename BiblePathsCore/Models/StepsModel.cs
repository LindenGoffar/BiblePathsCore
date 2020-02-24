using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class PathNodes
    {
        [NotMapped]
        public int FWStepId { get; set; }
        [NotMapped]
        public int BWStepId { get; set; }
        [NotMapped]
        public List<BibleVerses> Verses { get; set; }
        [NotMapped]
        public string BookName { get; set; }

        public async Task<bool> AddFwdBackStepAsync(BiblePathsCoreDbContext context)
        {
            // Get BWStepID
            try
            {
                PathNodes Node = await context.PathNodes.Where(N => N.PathId == PathId && N.Position < Position).OrderByDescending(L => L.Position).FirstAsync();
                BWStepId = Node.Id;
            }
            catch (InvalidOperationException) { BWStepId = 0;  }

            // Get FWStepID
            try
            {
                PathNodes Node = await context.PathNodes.Where(N => N.PathId == PathId && N.Position > Position).OrderBy(L => L.Position).FirstAsync();
                FWStepId = Node.Id;
            }
            catch (InvalidOperationException) { FWStepId = 0; }

            return true;
        }

        public async Task<bool> AddBookNameAsync(BiblePathsCoreDbContext context, string bibleId)
        {
            // Get BookName 
            BibleBooks Book = await context.BibleBooks.Where(B => B.BibleId == bibleId && B.BookNumber == BookNumber).FirstAsync();
            BookName = Book.Name;

            return true;
        }

        public async Task<List<BibleVerses>> GetBibleVersesAsync(BiblePathsCoreDbContext context, string bibleId, bool inPathOnly, bool includeProximity)
        {
            List<BibleVerses> bibleVerses = new List<BibleVerses>();
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
                foreach (BibleVerses verse in bibleVerses)
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
    }
}
