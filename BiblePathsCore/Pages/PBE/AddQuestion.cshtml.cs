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

namespace BiblePathsCore.Pages.PBE
{
    [Authorize]
    public class AddQuestionModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public AddQuestionModel(UserManager<IdentityUser> userManager, BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        [BindProperty]
        public QuizQuestions Question { get; set; }

        [BindProperty]
        public string BibleId { get; set; }

        //public async Task<IActionResult> OnGetAsync(string BibleId, int BookNumber, int Chapter, int? VerseNum)
        //{


        //    Question = new QuizQuestions();

        //    Step.PathId = Path.Id;
        //    Step.BookNumber = BookNumber;
        //    Step.Chapter = Chapter;
        //    Step.StartVerse = VerseNum ?? 1; // set to 1 if VersNum is Null.
        //    Step.EndVerse = VerseNum ?? 1; // set to 1 if VersNum is Null.
        //    Step.Position = Position;

        //    // Populate Step for display
        //    _ = await Step.AddBookNameAsync(_context, BibleId);
        //    Step.Verses = await Step.GetBibleVersesAsync(_context, BibleId, false, false);

        //    // and now we need a Verse Select List
        //    ViewData["VerseSelectList"] = new SelectList(Step.Verses, "Verse", "Verse");
        //    ViewData["TargetPage"] = "AddStep";
        //    return Page();
        //}

        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for
        //// more details see https://aka.ms/RazorPagesCRUD.
        //public async Task<IActionResult> OnPostAsync()
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        // Populate Step for display
        //        _ = await Step.AddBookNameAsync(_context, BibleId);
        //        Step.Verses = await Step.GetBibleVersesAsync(_context, BibleId, false, false);

        //        // and now we need a Verse Select List
        //        ViewData["VerseSelectList"] = new SelectList(Step.Verses, "Verse", "Verse");
        //        ViewData["TargetPage"] = "AddStep";
        //        return Page();
        //    }
        //    // Now let's validate a few things, first lets go grab the path.
        //    Path = await _context.Paths.FindAsync(Step.PathId);
        //    if (Path == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Path" }); }

        //    // confirm our owner is a valid path editor i.e. owner or the path is publicly editable
        //    IdentityUser user = await _userManager.GetUserAsync(User);
        //    if (!Path.IsValidPathEditor(user.Email)) { return RedirectToPage("/error", new { errorMessage = "Sorry! You do not have sufficient rights to add to this Path" }); }

        //    if (!Path.IsPathOwner(user.Email))
        //    {
        //        _ = await Path.RegisterEventAsync(_context, EventType.NonOwnerEdit, user.Email);
        //    }

        //    // Now let's create an empty Step aka. PathNode object so we can put only our validated properties onto it. 
        //    var emptyStep = new PathNodes
        //    {
        //        Created = DateTime.Now,
        //        Modified = DateTime.Now
        //    };

        //    if (await TryUpdateModelAsync<PathNodes>(
        //        emptyStep,
        //        "Step",   // Prefix for form value.
        //        S => S.StartVerse, S => S.EndVerse, S => S.PathId, S => S.BookNumber, S => S.Chapter, S => S.Position))
        //    {
        //        // What we get for Position is actually that of the previous node in the path.
        //        // We want to replace that with a temporary position that we'll update later. 
        //        int PreviousNodePosition = emptyStep.Position;
        //        emptyStep.Position = PreviousNodePosition + 5;

        //        _context.PathNodes.Add(emptyStep);
        //        await _context.SaveChangesAsync();
                
        //        // Now we need to update the Path Object with some calculated properties 
        //        if (!Path.IsPathOwner(user.Email)) { _ = await Path.RegisterEventAsync(_context, EventType.NonOwnerEdit, user.Email); }
                
        //        // Prepare to update some properties on Path
        //        _context.Attach(Path);
        //        Path.Length = await Path.GetPathVerseCountAsync(_context);
        //        Path.StepCount = Path.PathNodes.Count;
        //        Path.Modified = DateTime.Now;
        //        // Save our now updated Path Object. 
        //        await _context.SaveChangesAsync();

        //        // Finally we need to re-position each node in the path to ensure safe ordering
        //        _ = await Path.RedistributeStepsAsync(_context);
                
        //        return RedirectToPage("/Paths/Steps", new { PathId = Path.Id });
        //    }

        //        //_context.PathNodes.Add(PathNodes);
        //        //await _context.SaveChangesAsync();

        //        return RedirectToPage("./Index");
        //}
    }
}
