using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace BiblePathsCore.Pages.PBE
{
    public class IndexModel : PageModel
    {

        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context, ILogger<IndexModel> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }
        public string BibleId { get; set; }
        public QuizUsers PBEUser { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            PBEUser = new QuizUsers();
            if (User.Identity.IsAuthenticated)
            {
                IdentityUser user = await _userManager.GetUserAsync(User);
                PBEUser = await QuizUsers.GetPBEUserAsync(_context, user.Email);
            }
            BibleId = Bibles.DefaultPBEBibleId;
            return Page();
        }
    }
}
