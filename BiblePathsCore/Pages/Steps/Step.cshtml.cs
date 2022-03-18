using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BiblePathsCore
{
    public class StepModel : PageModel
    {
        public enum StepScenarios
        {
            Step,
            Context, 
            Study
        }

        private readonly BiblePathsCore.Models.BiblePathsCoreDbContext _context;

        public StepModel(BiblePathsCore.Models.BiblePathsCoreDbContext context)
        {
            _context = context;
        }

        public PathNode Step { get; set; }

        [BindProperty(SupportsGet = true)]
        public string BibleId { get; set; }
        public StepScenarios Scenario { get; set; }
        public string PageTitle { get; set;  }
        public List<SelectListItem> BibleSelectList { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, string BibleId, int? BookNumber, int? Chapter, int? Verse)
        {
            bool hasValidStepId = false;
            Step = new PathNode();
            int StepBookNumber = 0;
            int StepChapter = 0; 

            // TODO: How does this work with Form Post and Get scenarios? 
            if (this.BibleId != null) { BibleId = this.BibleId; }
            BibleId = await Step.GetValidBibleIdAsync(_context, BibleId);
            this.BibleId = BibleId;
            
            // There are three distinct scenarios to account for here: 
            // 1. Step Scenario, we have an id of a step but no BookNumber or Chapter, this is the most basic step scenario.
            //    a) Standad Step, we'll go grab the step details and display the step
            //    b) Commented Step, we'll send the reader back to the path reader experience. 
            // 2. Context Scenario, we have an id of a step and a BookNumber and Chapter, user is navigating for context.
            // 3. Study Scenario, we have no id of a step, but have Booknumber and Chapter, user is reading the Bible.

            if (id != null)
            {
                Step = await _context.PathNodes.FindAsync(id);
                if (Step == null) { return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Step" }); }
                
                // If the requested Step is a Commented Step then we need to redirected to the CommentedPaths reading experience
                if (Step.Type == (int)StepType.Commented) { return RedirectToPage("/CommentedPaths/Read", new { PathId = Step.PathId, StepId = Step.Id }); }
                
                _ = await Step.AddPathStepPropertiesAsync(_context);
                Scenario = StepScenarios.Step;
                hasValidStepId = true;
                StepBookNumber = Step.BookNumber;
                StepChapter = Step.Chapter; 
            }

            if (BookNumber.HasValue && Chapter.HasValue)
            {
                Step.BookNumber = (int)BookNumber;
                Step.Chapter = (int)Chapter;
                if (Verse.HasValue)
                {
                    Step.StartVerse = (int)Verse;
                    Step.EndVerse = (int)Verse;
                }
                if (await Step.ValidateBookChapterAsync(_context, this.BibleId) == false) {
                    return RedirectToPage("/error", new { errorMessage = "That's Odd! We weren't able to find this Step, or Book/Chapter combination" });
                }
                else
                { 
                    if (hasValidStepId) 
                    {
                        if (Step.BookNumber == StepBookNumber && Step.Chapter == StepChapter)
                        {
                            Scenario = StepScenarios.Step; // we're back to our step. 
                        }
                        else
                        {
                            Scenario = StepScenarios.Context;
                        }
                    }
                    else { Scenario = StepScenarios.Study;  }
                }
            }

            _ = await Step.AddGenericStepPropertiesAsync(_context, BibleId);
            Step.Verses = await Step.GetBibleVersesAsync(_context, BibleId, false, true);

            if (Scenario == StepScenarios.Study) 
            { 
                PageTitle = Step.BookName + " " + Step.Chapter; 
                // Add related path info
                foreach(BibleVerse verse in Step.Verses)
                {
                    _ = await verse.GetRelatedPathsAsync(_context);
                }
            } 
            else { PageTitle = Step.PathName; }

            // Let's see if we need to register any events for this step read. 
            if (Scenario == StepScenarios.Step) { _ = await Step.RegisterReadEventsAsync(_context);  }

            BibleSelectList = await GetBibleSelectListAsync(BibleId);

            return Page();
        }

        private async Task<List<SelectListItem>> GetBibleSelectListAsync(string BibleId)
        {
           // NOTE: NKJV-EN is removed for stricter adherence to Thomas Nelson copyright.  
           return await _context.Bibles.Where(b => b.Id != "NKJV-EN").Select(b =>
                              new SelectListItem
                              {
                                  Value = b.Id,
                                  Text = b.Language + "-" + b.Version
                              }).ToListAsync();
        }
    }
}
