using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Services;
using Microsoft.Extensions.Options;

namespace BiblePathsCore.Pages.Tongues
{
    [Authorize]
    public class VersesModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;
        private readonly OpenAISettings _openAIsettings;
        private readonly IOpenAIResponder _openAIResponder;

        public VersesModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context, IOptions<OpenAISettings> openAISettings, IOpenAIResponder openAIResponder)
        {
            _userManager = userManager;
            _context = context;
            _openAIsettings = openAISettings.Value;
            _openAIResponder = openAIResponder;
        }

        public List<BibleVerse> Verses { get; set; }
        public List<VerseTongueObj> verseTongues { get; set; }
        public Bible bible { get; set; }
        

        public async Task<IActionResult> OnGetAsync(string BibleId, int BookNumber, int Chapter, int? VerseNum, string toLanguage)
        {
            // User Checks.
            IdentityUser user = await _userManager.GetUserAsync(User);

            // Initialize Verses we'll support one Verse now... add more later.
            Verses = await BibleVerse.GetVersesAsync(_context, BibleId, BookNumber, Chapter, VerseNum ?? 1, VerseNum ?? 1);

            foreach(BibleVerse verse in Verses) {


            //// Generate a question since we were asked to. 
            //// TODO: This makes a dangerous assumption about a VerseNum
            //if (generateQuestion)
            //{
            //    BibleVerse verse = await BibleVerse.GetVerseAsync(_context, Question.BibleId, BookNumber, Chapter, (int)VerseNum);
            //    if (generateAIQuestion)
            //    {
            //        Question = await Question.BuildAIQuestionForVerseAsync(_context, verse, _openAIResponder);
            //        foreach (QuizAnswer Answer in Question.QuizAnswers)
            //        {
            //            AnswerText += Answer.Answer;
            //        }
            //    }
            //    else // the FITB Scenario.
            //    {
            //        Question = await Question.BuildQuestionForVerseAsync(_context, verse, 10, Question.BibleId);
            //        foreach (QuizAnswer Answer in Question.QuizAnswers)
            //        {
            //            AnswerText += Answer.Answer;
            //        }
            //    }
            //    IsGeneratedQuestion = true;
            //}
            //else // This is setting up the non-builder scenario. 
            //{
            //    Question.BookNumber = BookNumber;
            //    Question.Chapter = Chapter;
            //    Question.StartVerse = VerseNum ?? 1; // set to 1 if VersNum is Null.
            //    Question.EndVerse = VerseNum ?? 1; // set to 1 if VersNum is Null.
            //    Question.Points = 0;
            //    IsGeneratedQuestion = false;
            //}

            //BibleBook PBEBook = await BibleBook.GetPBEBookAndChapterAsync(_context, Question.BibleId, Question.BookNumber, Question.Chapter);
            //if (PBEBook == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find the PBE Book." }); }

            //// the commentary scenario requires Verse info so doing this before we  Populate PBE Question info.
            //Question.Verses = await Question.GetBibleVersesAsync(_context, false);
            //Question.PopulatePBEQuestionInfo(PBEBook);
            
            //HasExclusion = Question.Verses.Any(v => v.IsPBEExcluded == true);

            //// In the Commentary Scenario we have no real "Chapter" so will need to fake some properties like isCommentary
            //IsCommentary = (Question.Chapter == Bible.CommentaryChapter);
            //if (IsCommentary == false)
            //{
            //    ChapterQuestionCount = PBEBook.BibleChapters.Where(c => c.ChapterNumber == Question.Chapter).First().QuestionCount;
            //    ChapterFITBPct = PBEBook.BibleChapters.Where(c => c.ChapterNumber == Question.Chapter).First().FITBPct;
            //    if (ChapterFITBPct < 33) { IsFITBGenerationEnabled = true; }
            //    else { IsFITBGenerationEnabled = false; }   
            //}
            //else
            //{
            //    IsFITBGenerationEnabled = false;
            //}
            //CommentaryQuestionCount = PBEBook.CommentaryQuestionCount;

            //// and now we need a Verse Select List, and a Section Select List
            //ViewData["VerseSelectList"] = new SelectList(Question.Verses, "Verse", "Verse");
            //if (IsCommentary) { ViewData["SectionSelectList"] = new SelectList(Question.Verses, "Verse", "SectionTitle"); }

            //ViewData["PointsSelectList"] = Question.GetPointsSelectList();
            
            //IsOpenAIEnabled = false;
            //if(_openAIsettings.OpenAIEnabled == "True") { IsOpenAIEnabled = true; }

            return Page();
        }
    }
}
