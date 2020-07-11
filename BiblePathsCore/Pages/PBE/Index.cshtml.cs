using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiblePathsCore.Models.DB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace BiblePathsCore.Pages.PBE
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }
        public string BibleId { get; set; }

        public void OnGet()
        {
            BibleId = Bibles.DefaultPBEBibleId;
        }
    }
}
