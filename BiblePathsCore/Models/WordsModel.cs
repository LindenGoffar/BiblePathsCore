using BiblePathsCore.Controllers;
using Humanizer.Localisation;
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

    public class WordCount
    {
        public string Word { get; set; }
        public int Count { get; set; }
        public WordCount() { }
        public static async Task<List<WordCount>> GetWordCountListFromBookOrBookListAsync(BiblePathsCoreDbContext context, string BibleId, int BookNumber)
        {
            List<WordCount> WordCounts = new List<WordCount>();
            List<BibleWordIndex> Words = new List<BibleWordIndex>();
            List<BibleBook> Books = new List<BibleBook>();
            // We'll end up looking for exclusions per book. 
            List<QuizQuestion> Exclusions = new List<QuizQuestion>();

            // BookList Scenario
            if (BookNumber >= Bible.MinBookListID)
            {
                QuizBookList BookList = await context.QuizBookLists.Include(L => L.QuizBookListBookMaps).Where(L => L.Id == BookNumber).FirstAsync();
                foreach (QuizBookListBookMap Map in BookList.QuizBookListBookMaps)
                {
                    BibleBook Book = await context.BibleBooks.Where(B => B.BibleId == BibleId && B.BookNumber == Map.BookNumber).FirstAsync();
                    Books.Add(Book);
                }
            }
            else
            {
                BibleBook Book = await context.BibleBooks.Where(B => B.BibleId == BibleId && B.BookNumber == BookNumber).FirstAsync();
                Books.Add(Book);
            }

            // Now we need to iterate through each book and get the associated Word counts
            foreach (BibleBook Book in Books)
            {
                // Go Get the per-book Word Index. 
                Words = await context.BibleWordIndices.Where(W => W.BibleId == BibleId && W.BookNumber == Book.BookNumber).ToListAsync();

                // Let's grab the exclusions for this book. 
                Exclusions = await context.QuizQuestions
                                            .Where(Q => (Q.BibleId == BibleId || Q.BibleId == null)
                                                    && Q.BookNumber == BookNumber
                                                    && Q.Type == (int)QuestionType.Exclusion
                                                    && Q.IsDeleted == false)
                                            .ToListAsync();

                foreach (BibleWordIndex Word in Words)
                {
                    // We need to see if the verse is excluded. 
                    if (BibleVerse.IsBookChapterVerseInExclusionList(Exclusions, Word.BookNumber, Word.Chapter, Word.Verse) == false)
                    {
                        // Has this word been added to our list yet? 
                        WordCount WordCount = WordCounts.Where(W => W.Word == Word.Word).FirstOrDefault();
                        if (WordCount == null)
                        {
                            WordCount = new WordCount() { Word = Word.Word, Count = 1 };
                            WordCounts.Add(WordCount);
                        }
                        else
                        {
                            WordCount.Count++;
                        }
                    }
                }
            }
            return WordCounts;
        }
        public static async Task<List<BibleVerse>> GetVerseListForWordFromBookOrBookListAsync(BiblePathsCoreDbContext context, string TheWord, string BibleId, int BookNumber)
        {
            List<BibleVerse> Verses = new List<BibleVerse>();
            List<BibleWordIndex> Words = new List<BibleWordIndex>();
            List<BibleBook> Books = new List<BibleBook>();

            // We'll end up looking for exclusions per book. 
            List<QuizQuestion> Exclusions = new List<QuizQuestion>();

            // BookList Scenario
            if (BookNumber >= Bible.MinBookListID)
            {
                QuizBookList BookList = await context.QuizBookLists.Include(L => L.QuizBookListBookMaps).Where(L => L.Id == BookNumber).FirstAsync();
                foreach (QuizBookListBookMap Map in BookList.QuizBookListBookMaps)
                {
                    BibleBook Book = await context.BibleBooks.Where(B => B.BibleId == BibleId && B.BookNumber == Map.BookNumber).FirstAsync();
                    Books.Add(Book);
                }
            }
            else
            {
                BibleBook Book = await context.BibleBooks.Where(B => B.BibleId == BibleId && B.BookNumber == BookNumber).FirstAsync();
                Books.Add(Book);
            }

            // Now we need to iterate through each book and get the associated Word Indices
            foreach (BibleBook Book in Books)
            {
                // Go Get the per-book Word Index. 
                Words = await context.BibleWordIndices.Where(W => W.Word == TheWord 
                                                            && W.BibleId == BibleId 
                                                            && W.BookNumber == Book.BookNumber)
                                                        .ToListAsync();


                // Let's grab the exclusions for this book. 
                Exclusions = await context.QuizQuestions
                                            .Where(Q => (Q.BibleId == BibleId || Q.BibleId == null)
                                                    && Q.BookNumber == BookNumber
                                                    && Q.Type == (int)QuestionType.Exclusion
                                                    && Q.IsDeleted == false)
                                            .ToListAsync();

                foreach (BibleWordIndex Word in Words)
                {
                    // We need to see if the verse is excluded. 
                    if (BibleVerse.IsBookChapterVerseInExclusionList(Exclusions, Word.BookNumber, Word.Chapter, Word.Verse) == false)
                    {
                        // If by off chance we've already got this verse in our list we skip it.
                        if (Verses.Where(v => v.Id == Word.VerseId).Any() == false) {
                            // Now this is a bit expensive we need to grab the Verse
                            BibleVerse WordVerse = await context.BibleVerses.FindAsync(Word.VerseId);
                            Verses.Add(WordVerse);
                        }
                    }
                }
            }
            return Verses;
        }
    }
}
