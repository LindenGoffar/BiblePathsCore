using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class QuizUser
    {
        public static async Task<QuizUser> GetOrAddPBEUserAsync(BiblePathsCoreDbContext context, string LoggedOnUserName)
        {
            // This might be a bit inefficient as it could result in two similar queries, but the goal is to protect
            // from having to rely on Exceptions to identify whether the user exists or not. 

            QuizUser ReturnUser = new QuizUser();
            // We've gotten bit by this so first let's check whether this user exists, if so we'll grab the user.
            // if not we'll go create them. 
            if (await context.QuizUsers.Where(u => u.Email == LoggedOnUserName).AnyAsync())
            {
                try
                {
                    ReturnUser = await context.QuizUsers.Where(u => u.Email == LoggedOnUserName)
                                    .OrderBy(u => u.Id)
                                    .FirstAsync();
                }
                catch { } // not much to do here other than handle and return null. 

            }
            else
            {
                //Let's add a user and return the newly added user. 
                ReturnUser.Email = LoggedOnUserName;
                ReturnUser.Added = DateTime.Now;
                ReturnUser.Modified = DateTime.Now;
                context.QuizUsers.Add(ReturnUser);
                await context.SaveChangesAsync();
            }
            return ReturnUser;
        }

        public static async Task<QuizUser> GetPBEUserAsync(BiblePathsCoreDbContext context, string LoggedOnUserName)
        {
            // Try to find the user in the DB... we should not have dupes but a bug has allowed dupes we need to clean up. 
            QuizUser ReturnUser = new QuizUser();

            try
            {
                ReturnUser = await context.QuizUsers.Where(u => u.Email == LoggedOnUserName)
                                .OrderBy(u => u.Id)
                                .FirstAsync();
            }
            catch { } // not much to do here other than handle and return null. 
            return ReturnUser;
        }

        public static async Task<bool> IsValidPBEQuestionOwnerAsync(BiblePathsCoreDbContext context, string UserName)
        {
            return await context.QuizUsers.Where(U => U.Email.ToLower() == UserName.ToLower() && U.IsQuestionBuilderLocked == false).AnyAsync();
        }

        public bool IsValidPBEQuestionBuilder()
        {
            return !IsQuestionBuilderLocked;
        }

        public bool IsValidPBEQuizHost()
        {
            return !IsQuizTakerLocked;
        }

        public bool IsQuizModerator()
        {
            return IsModerator;
        }

        public async Task<bool> CheckAPITokenAsync(BiblePathsCoreDbContext context, string Token)
        {
            // Find the most recent token for this user
            QuizQuestionStat TokenStat = new QuizQuestionStat();
            try
            {
                TokenStat = await context.QuizQuestionStats.Where(T => T.QuizUserId == this.Id
                                                                    && T.EventType == (int)QuizQuestion.QuestionEventType.QuestionAPIToken)
                                                                 .OrderByDescending(T => T.EventWritten).Take(1).SingleAsync();
            }
            catch
            {
                // We'll take any exceptions to indicate we couldn't find a Token 
                return false;
            }
            TimeSpan TimeSinceTokenCreated = (TimeSpan)(DateTime.Now - TokenStat.EventWritten);
            if (TimeSinceTokenCreated.TotalHours < 24)
            {
                string TokenString = TokenStat.EventData;
                if (TokenString.Trim() == Token.Trim())
                {
                    // this is the only success case. 
                    return true;
                }
            }
            return false;
        }
        public async Task<string> GetQuestionAPITokenAsync(BiblePathsCoreDbContext context)
        {
            string TokenString = "No API Token Found";
            // Find the most recent token for this user
            QuizQuestionStat TokenStat = new QuizQuestionStat();
            try
            {
                TokenStat = await context.QuizQuestionStats.Where(T => T.QuizUserId == this.Id
                                                                    && T.EventType == (int)QuizQuestion.QuestionEventType.QuestionAPIToken)
                                                                 .OrderByDescending(T => T.EventWritten).Take(1).SingleAsync();
            }
            catch 
            {
                // We'll take any exceptions to indicate we couldn't find a Token 
                return TokenString;
            }
            TokenString = "API Token Expired";
            TimeSpan TimeSinceTokenCreated = (TimeSpan)(DateTime.Now - TokenStat.EventWritten);
            if (TimeSinceTokenCreated.TotalHours < 24)
            {
                TokenString = TokenStat.EventData;
            }
            return TokenString;
        }
        public async Task<string> CreateQuestionAPITokenAsync(BiblePathsCoreDbContext context)
        {
            // Build a Token 
            string returnString = "InvalidToken";
            Random rnd = new Random();
            int Rnd5Digits = rnd.Next(10000, 99999);
            // Token = Base64Encoded( User.Email+RandomNumber )
            string EncodeString = this.Email + Rnd5Digits.ToString();
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(EncodeString);
            string TokenString = System.Convert.ToBase64String(plainTextBytes);

            try
            {
                // Tokens are stored on Question Stat objects so we need to grab an dummy question, any will work.
                QuizQuestion DummyQuestion = await context.QuizQuestions.Take(1).FirstAsync();
                if (await DummyQuestion.RegisterEventAsync(context, QuizQuestion.QuestionEventType.QuestionAPIToken, this.Id, TokenString, null, null))
                {
                    returnString = TokenString;
                }
            }
            catch { }
            return returnString;
        }

    }
}
