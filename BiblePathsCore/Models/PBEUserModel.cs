using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePathsCore.Models.DB
{
    public partial class QuizUsers
    {
        public static async Task<QuizUsers> GetOrAddPBEUserAsync(BiblePathsCoreDbContext context, string LoggedOnUserName)
        {
            // First we'll try to find the user in the DB.
            QuizUsers ReturnUser = new QuizUsers();
            try
            {
                ReturnUser = await context.QuizUsers.Where(U => U.Email == LoggedOnUserName).SingleAsync();
            }
            catch (InvalidOperationException) // Generally means the user was not found. So Let's add one.
            {
                //Let's add our user.
                ReturnUser.Email = LoggedOnUserName;
                ReturnUser.Added = DateTime.Now;
                ReturnUser.Modified = DateTime.Now;
                context.QuizUsers.Add(ReturnUser);
                await context.SaveChangesAsync();
            }
            return ReturnUser;
        }

        public static async Task<bool> IsValidPBEUserAsync(BiblePathsCoreDbContext context, string UserName)
        {
            return await context.QuizUsers.Where(U => U.Email.ToLower() == UserName.ToLower() && U.IsQuestionBuilderLocked == false).AnyAsync();
        }

        public bool IsValidPBEQuestionBuilder()
        {
            return !IsQuestionBuilderLocked;
        }
    }
}
