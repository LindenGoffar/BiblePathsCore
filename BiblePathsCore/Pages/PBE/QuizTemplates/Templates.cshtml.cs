﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace BiblePathsCore.Pages.PBE
{
    [Authorize]
    public class TemplatesModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public TemplatesModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<PredefinedQuiz> Templates { get;set; }
        public List<PredefinedQuiz> SharedTemplates { get; set; }
        public QuizUser PBEUser { get; set; }
        public string BibleId { get; set; }
        public string UserMessage { get; set;  }

        public async Task<IActionResult> OnGetAsync(string BibleId, string Message)
        {
            IdentityUser user = await _userManager.GetUserAsync(User);
            PBEUser = await QuizUser.GetOrAddPBEUserAsync(_context, user.Email); // Static method not requiring an instance
            this.BibleId = await Bible.GetValidPBEBibleIdAsync(_context, BibleId);

            Templates = await _context.PredefinedQuizzes.Include(T => T.PredefinedQuizQuestions)
                                                    .Where(T => T.IsDeleted == false && T.QuizUser == PBEUser)
                                                    .OrderByDescending(T => T.Created)
                                                    .ToListAsync();

            SharedTemplates = await _context.PredefinedQuizzes.Where(T => T.IsDeleted == false
                                                        && T.Type == (int)QuizTemplateType.Shared)
                                                    .OrderByDescending(T => T.Created)
                                                    .ToListAsync();

            UserMessage = GetUserMessage(Message);
            return Page();
        }

        public string GetUserMessage(string Message)
        {
            if (Message != null)
            {
                // Arbitrarily limiting User Message length. 
                if (Message.Length > 0 && Message.Length < 128)
                {
                    return Message;
                }
            }
            return null;
        }
    }
}
