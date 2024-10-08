﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;

namespace BiblePathsCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizQuestionsController : ControllerBase
    {
        private readonly BiblePathsCoreDbContext _context;

        public QuizQuestionsController(BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        // GET: api/QuizQuestions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MinQuestion>>> GetQuizQuestions(string BibleId, string BookName, int Chapter)
        {
            BibleId = await QuizQuestion.GetValidBibleIdAsync(_context, BibleId);

            BibleBook Book = await BibleBook.GetBookAndChapterByNameAsync(_context, BibleId, BookName, Chapter);
            if (Book == null) { return NotFound(); }

            List<MinQuestion> minQuestions = new List<MinQuestion>();

            // Switch to static method
            List<QuizQuestion> Questions = await QuizQuestion.GetQuestionListAsync(_context, BibleId, Book.BookNumber, Chapter, false);
            //List<QuizQuestion> Questions = await _context.QuizQuestions
            //                                            .Include(Q => Q.QuizAnswers)
            //                                            .Where(Q => (Q.BibleId == BibleId || Q.BibleId == null)
            //                                                    && Q.BookNumber == Book.BookNumber
            //                                                    && Q.Chapter == Chapter
            //                                                    && Q.IsDeleted == false
            //                                                    && Q.Challenged == false
            //                                                    && Q.Type == (int)QuestionType.Standard
            //                                                    && Q.IsAnswered == true).ToListAsync();
            foreach (QuizQuestion Question in Questions)
            {
                // Commentary scenario requires Verses be populated before calling PopulatePBEQuestionInfo.
                if (Question.Chapter == Bible.CommentaryChapter)
                {
                    Question.Verses = await Question.GetCommentaryMetadataAsVersesAsync(_context, true);
                }
                Question.PopulatePBEQuestionInfo(Book);
                MinQuestion minQuestion = new MinQuestion(Question);
                minQuestions.Add(minQuestion);
            }
            return minQuestions;
        }

        // GET: api/QuizQuestion/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MinQuestion>> GetQuizQuestion(int id)
        {
            var quizQuestion = await _context.QuizQuestions.FindAsync(id);

            if (quizQuestion == null)
            {
                return NotFound();
            }
            // Explicit load our answers.
            await _context.Entry(quizQuestion).Collection(q => q.QuizAnswers).LoadAsync();

            BibleBook PBEBook = await BibleBook.GetPBEBookAndChapterAsync(_context, quizQuestion.BibleId, quizQuestion.BookNumber, quizQuestion.Chapter);
            // Commentary scenario requires Verses be populated before calling PopulatePBEQuestionInfo.
            if (quizQuestion.Chapter == Bible.CommentaryChapter)
            {
                quizQuestion.Verses = await quizQuestion.GetCommentaryMetadataAsVersesAsync(_context, true);
            }
            quizQuestion.PopulatePBEQuestionInfo(PBEBook);
            MinQuestion minQuestion = new MinQuestion(quizQuestion);


            return minQuestion;
        }

        //// PUT: api/QuizQuestions/5
        //// To protect from overposting attacks, enable the specific properties you want to bind to, for
        //// more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutQuizQuestions(int id, QuizQuestions quizQuestions)
        //{
        //    if (id != quizQuestions.Id)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(quizQuestions).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!QuizQuestionsExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return NoContent();
        //}

        // POST: api/QuizQuestions
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<MinQuestion>> PostQuizQuestions([FromBody]MinQuestion Question)
        {
           
            // Confirm we have a valid user and token
            if (await Question.APIUserTokenCheckAsync(_context) == false)
            {
                return Unauthorized();
            }

            // Now let's create an empty question and put only our valid properties onto it. 
            var emptyQuestion = new QuizQuestion
            {
                Created = DateTime.Now,
                Modified = DateTime.Now,
                BibleId = Question.BibleId,
                Points = Question.Points,
                BookNumber = Question.BookNumber,
                Chapter = Question.Chapter,
                StartVerse = Question.StartVerse,
                EndVerse = Question.EndVerse,
                Question = Question.Question,
                Owner = Question.Owner,
                Source = Question.Source,
                Type = Question.DetectQuestionType()
                //Type = (int)QuestionType.Standard
            };

            // If the Question is in an Exclusion range we will show an Error
            if (await emptyQuestion.IsQuestionInExclusionAsync(_context)) { return Unauthorized(); }

            _context.QuizQuestions.Add(emptyQuestion);
            // now we need to add the Answer if there are any 
            foreach (string AnswerString in Question.Answers)
            {
                if (AnswerString.Length > 0)
                {
                    QuizAnswer Answer = new QuizAnswer
                    {
                        Created = DateTime.Now,
                        Modified = DateTime.Now,
                        Question = emptyQuestion,
                        Answer = AnswerString,
                        IsPrimary = true
                    };
                    _context.QuizAnswers.Add(Answer);
                    // Register that this question has an answer. 
                    emptyQuestion.IsAnswered = true;
                }
            }
            await _context.SaveChangesAsync();
            MinQuestion returnQuestion = new MinQuestion(emptyQuestion);
            return CreatedAtAction("GetQuizQuestion", new { id = returnQuestion.Id }, returnQuestion);
        }

        //// DELETE: api/QuizQuestions/5
        //[HttpDelete("{id}")]
        //public async Task<ActionResult<QuizQuestions>> DeleteQuizQuestions(int id)
        //{
        //    var quizQuestions = await _context.QuizQuestions.FindAsync(id);
        //    if (quizQuestions == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.QuizQuestions.Remove(quizQuestions);
        //    await _context.SaveChangesAsync();

        //    return quizQuestions;
        //}

        //private bool QuizQuestionsExists(int id)
        //{
        //    return _context.QuizQuestions.Any(e => e.Id == id);
        //}
    }
}
