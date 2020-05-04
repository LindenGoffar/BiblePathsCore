using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace BiblePathsCore.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public string CatchPhrase { get; set; }

        public void OnGet()
        {
            Random rnd = new Random();
            string[] Phrases = new string[]
            {
                "Bible only, Bible studies, for you by you...",
                "Even fishers of men need a .net...",
                "Best served with prayer...",
                "Caution: Approach with prayer...",
                "If it's not in the Bible, it's not here...",
                "Lost? Find your Path here...",
                "Welcome to the family! Now that you're here..."
            };
            int r = rnd.Next(Phrases.Count());

            CatchPhrase = Phrases[r];
        }
    }
}
