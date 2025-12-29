using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BiblePathsCore.Models;
using BiblePathsCore.Models.DB;
using BiblePathsCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;

namespace BiblePathsCore.Pages.PBE.Question
{
    [Authorize]
    public partial class Add : ComponentBase
    {
        [Parameter]
        [SupplyParameterFromQuery(Name = "BibleId")]
        public string BibleId { get; set; }

        [Parameter]
        [SupplyParameterFromQuery]
        public int? BookNumber { get; set; }

        [Parameter]
        [SupplyParameterFromQuery]
        public int? Chapter { get; set; }

        [Parameter]
        [SupplyParameterFromQuery]
        public int? Verse { get; set; }

        [Inject] private NavigationManager NavManager { get; set; }
        [Inject] private BiblePathsCoreDbContext DbContext { get; set; }
        [Inject] private IOpenAIResponder OpenAIResponder { get; set; }
        [Inject] private IOptions<OpenAISettings> OpenAISettings { get; set; }
        [Inject] private UserManager<IdentityUser> UserManager { get; set; }
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        //[CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; }

        private BibleVerse SelectedVerse { get; set; }
        private QuizQuestion ModelQuestion { get; set; } = new();
        private BibleBook PBEBook { get; set; }
        private string AnswerText { get; set; } = string.Empty;
        private List<BibleVerse> DisplayVerses { get; set; } = new();

        private IdentityUser currentUser;
        private bool ShowModal { get; set; } = false;
        private bool ShowPreview { get; set; } = false;
        private bool IsFITBGenerationEnabled { get; set; } = false;
        private bool IsGeneratingAI { get; set; } = false;
        private bool IsGeneratingFITB { get; set; } = false;
        private bool HasExclusion { get; set; } = false;
        private bool IsCommentary { get; set; } = false;
        private bool Loading { get; set; } = true;
        private bool IsOpenAIEnabled { get; set; }
        // private int StartVerse { get; set; } = 1;
        // private int EndVerse { get; set; } = 1;
        private int ChapterQuestionCount { get; set; } = 0;
        private int CommentaryQuestionCount { get; set; } = 0;
        private int ChapterFITBPct { get; set; } = 0;

        // Optional error message shown in the UI when set
        private string ErrorMessage { get; set; } = string.Empty;
        
        // Page specific propoerties
        private bool isInitialized = false;
        private bool isAuthenticated = false;

        // track whether we've already scrolled to the verse
        private bool _scrolledToVerse = false;

        // Debounce helpers for preview updates
        private CancellationTokenSource _previewCts;
        private readonly object _previewLock = new();
        private int _previewDelayMs = 600; // delay in milliseconds


        // Keep OnInitializedAsync lightweight; parameter handling is done in OnParametersSetAsync
        protected override Task OnInitializedAsync()
        {
            // nothing heavy here - OnParametersSetAsync will handle defaults and initialization
            return base.OnInitializedAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            // Read OpenAI settings and ensure defaults for parameters (querystring or route)
            IsOpenAIEnabled = OpenAISettings?.Value?.OpenAIEnabled == "True";

            // Ensure some defaults only when values are null
            BibleId ??= await QuizQuestion.GetValidBibleIdAsync(DbContext, null);
            BookNumber ??= 23;
            Chapter ??= 6;
            Verse ??= 1;

            // Ensure Authorization and initial load when parameters are available
            if (!isAuthenticated)
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                isAuthenticated = authState.User.Identity?.IsAuthenticated == true;
                currentUser = isAuthenticated ? await UserManager.GetUserAsync(authState.User) : null;

                if (isAuthenticated)
                {
                    // Initialize ModelQuestion and verse data after parameters are set
                    if (ModelQuestion == null || ModelQuestion.Verses == null)
                    {
                        await InitializeQuestionVersesAsync();
                        isInitialized = true;
                    }

                    Loading = false;
                }
            }

            await base.OnParametersSetAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // If a Verse was provided in the URL, scroll to it once the verses are rendered
            if (!_scrolledToVerse && Verse.HasValue && ModelQuestion?.Verses?.Any() == true)
            {
                if (Verse.Value < 5) // We don't want to scroll if it's within the first 5 verses
                {
                    // No need to scroll within first few verses
                    return;
                }
                try
                {
                    var elementId = $"verse-{Verse.Value}";
                    await JSRuntime.InvokeVoidAsync("bpScrollToVerse", elementId);
                    _scrolledToVerse = true;
                }
                catch
                {
                    // ignore JS errors
                }
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task InitializeQuestionVersesAsync()
        {
            ModelQuestion = new QuizQuestion
            {
                BibleId = BibleId,
                BookNumber = BookNumber.Value,
                Chapter = Chapter.Value,
                StartVerse = Verse.Value,
                EndVerse = Verse.Value,
                Points = 1
            };
            PBEBook = await BibleBook.GetPBEBookAndChapterAsync(DbContext, ModelQuestion.BibleId, ModelQuestion.BookNumber, ModelQuestion.Chapter);
            if (PBEBook == null) { ErrorMessage = "That's Odd! We weren't able to find the PBE Book."; }

            // the commentary scenario requires Verse info so doing this before we Populate PBE Question info.
            ModelQuestion.Verses = await ModelQuestion.GetBibleVersesAsync(DbContext, false);
            ModelQuestion.PopulatePBEQuestionInfo(PBEBook);

            HasExclusion = ModelQuestion.Verses.Any(v => v.IsPBEExcluded == true);

            // In the Commentary Scenario we have no real "Chapter" so will need to fake some properties like isCommentary
            IsCommentary = (ModelQuestion.Chapter == Bible.CommentaryChapter);
            if (IsCommentary == false)
            {
                ChapterQuestionCount = PBEBook.BibleChapters.Where(c => c.ChapterNumber == ModelQuestion.Chapter).First().QuestionCount;
                ChapterFITBPct = PBEBook.BibleChapters.Where(c => c.ChapterNumber == ModelQuestion.Chapter).First().FITBPct;
                if (ChapterFITBPct < 10) { IsFITBGenerationEnabled = true; }
                else { IsFITBGenerationEnabled = false; }
            }
            else
            {
                IsFITBGenerationEnabled = false;
            }
            CommentaryQuestionCount = PBEBook.CommentaryQuestionCount;
        }
        private async Task OpenModal(BibleVerse verse)
        {
            SelectedVerse = verse;
            ModelQuestion.StartVerse = SelectedVerse.Verse;
            ModelQuestion.EndVerse = SelectedVerse.Verse;
            await UpdateDisplayVersesAsync();
            ShowModal = true;
        }

        private void CloseModal()
        {
            // Clear any Question/Answer Text entered
            ModelQuestion.Question = null;
            AnswerText = null;
            ModelQuestion.Points = 1;
            ShowModal = false;
            SelectedVerse = null;

        }

        private void ClearGenerated()
        {
            ModelQuestion.Question = string.Empty;
            ModelQuestion.QuizAnswers = new List<QuizAnswer>();
            ModelQuestion.Points = 1;
            ModelQuestion.Source = null;
            AnswerText = string.Empty;

            // Immediately update preview to reflect cleared state
            UpdatePBEQuestionPreview();
        }

        private async Task GenerateAIAsync()
        {
 
            if (!IsOpenAIEnabled || ModelQuestion.StartVerse == 0) return;

            IsGeneratingAI = true;
            await InvokeAsync(StateHasChanged);
            try
            {
                var verse = await BibleVerse.GetVerseAsync(DbContext, ModelQuestion.BibleId, ModelQuestion.BookNumber, ModelQuestion.Chapter, ModelQuestion.StartVerse);
                var built = await new QuizQuestion().BuildAIQuestionForVerseAsync(DbContext, verse, OpenAIResponder);
                if (built != null)
                {
                    ModelQuestion.Question = built.Question;
                    ModelQuestion.Points = built.Points;
                    ModelQuestion.EndVerse = ModelQuestion.StartVerse; // We only generate on StartVerse so let's change this. 
                    // ModelQuestion.Type = built.Type; // Type is detected later.
                    foreach (QuizAnswer Answer in built.QuizAnswers)
                    {
                        AnswerText += Answer.Answer;
                    }
                    ModelQuestion.Source = built.Source;

                    // update preview after generation
                    UpdatePBEQuestionPreview();
                    await InvokeAsync(StateHasChanged);
                }
            }
            finally
            {
                IsGeneratingAI = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task GenerateFITBAsync()
        {
            if (SelectedVerse == null) return;

            IsGeneratingFITB = true;
            await InvokeAsync(StateHasChanged);
            try
            {
                ModelQuestion.StartVerse = SelectedVerse.Verse;
                ModelQuestion.EndVerse = SelectedVerse.Verse;

                var verse = await BibleVerse.GetVerseAsync(DbContext, ModelQuestion.BibleId, ModelQuestion.BookNumber, ModelQuestion.Chapter, ModelQuestion.StartVerse);
                var built = await new QuizQuestion().BuildQuestionForVerseAsync(DbContext, verse, 10, ModelQuestion.BibleId);
                if (built != null)
                {
                    ModelQuestion.Question = built.Question;
                    ModelQuestion.Points = built.Points;
                    ModelQuestion.EndVerse = ModelQuestion.StartVerse; // We only generate on StartVerse so let's change this. 
                    // ModelQuestion.Type = built.Type; // Type is detected later.
                    foreach (QuizAnswer Answer in built.QuizAnswers)
                    {
                        AnswerText += Answer.Answer;
                    }
                    ModelQuestion.Source = built.Source;

                    // update preview
                    UpdatePBEQuestionPreview();
                    await InvokeAsync(StateHasChanged);
                }
            }
            finally
            {
                IsGeneratingFITB = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task SaveAsync()
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(ModelQuestion.BibleId)) return;
            if (ModelQuestion.BookNumber <= 0 || ModelQuestion.Chapter <= 0) return;

            // Create a fresh question object mapping only intended properties
            var emptyQuestion = new QuizQuestion
            {
                Created = DateTime.Now,
                Modified = DateTime.Now,
                BibleId = ModelQuestion.BibleId,
                Points = ModelQuestion.Points,
                BookNumber = ModelQuestion.BookNumber,
                Chapter = ModelQuestion.Chapter,
                StartVerse = Math.Min(ModelQuestion.StartVerse, ModelQuestion.EndVerse),
                EndVerse = Math.Max(ModelQuestion.StartVerse, ModelQuestion.EndVerse),
                Question = ModelQuestion.Question,
                Source = string.IsNullOrWhiteSpace(ModelQuestion.Source) ? "BiblePaths.Net" : ModelQuestion.Source
            };

            // Get user
            if (currentUser != null)
            {
                var email = currentUser.Email;
                var pbeUser = await QuizUser.GetOrAddPBEUserAsync(DbContext, email);
                if (pbeUser == null || !pbeUser.IsValidPBEQuestionBuilder())
                {
                    // simply close modal; in a real app show error
                    CloseModal();
                    return;
                }
                emptyQuestion.Owner = pbeUser.Email;
            }
            else
            {
                CloseModal();
                return;
            }

            emptyQuestion.Type = emptyQuestion.DetectQuestionType();
            DbContext.QuizQuestions.Add(emptyQuestion);

            if (!string.IsNullOrWhiteSpace(AnswerText))
            {
                var ans = new QuizAnswer
                {
                    Created = DateTime.Now,
                    Modified = DateTime.Now,
                    Question = emptyQuestion,
                    Answer = AnswerText,
                    IsPrimary = true
                };
                DbContext.QuizAnswers.Add(ans);
                emptyQuestion.IsAnswered = true;
            }

            await DbContext.SaveChangesAsync();

            // Optionally refresh verse list or navigate to same chapter
            CloseModal();
            await InitializeQuestionVersesAsync();
            await InvokeAsync(StateHasChanged);
            // NavManager.NavigateTo($"/pbe/question/add/{emptyQuestion.BibleId}/{emptyQuestion.BookNumber}/{emptyQuestion.Chapter}", forceLoad: false);
        }

        private void UpdatePBEQuestionPreview()
        {
            ModelQuestion.PopulatePBEQuestionInfo(PBEBook);
        }

        private void TogglePreview()
        {
            ShowPreview = !ShowPreview;
        }

        private async Task OnVerseBoundsChanged(ChangeEventArgs _)
        {
            // ensure start/end are sane (swap if necessary)
            if (ModelQuestion.StartVerse > ModelQuestion.EndVerse)
            {
                var tmp = ModelQuestion.EndVerse;
                ModelQuestion.EndVerse = ModelQuestion.StartVerse;
                ModelQuestion.StartVerse = tmp;
            }

            // reload the verses in the display region
            await UpdateDisplayVersesAsync();

            // we also need to update the PBE Question preview
            UpdatePBEQuestionPreview();

            // ensure UI updates on the renderer thread
            await InvokeAsync(StateHasChanged);
        }

        private async Task UpdateDisplayVersesAsync()
        {
            // load the verses matching the current bounds
            DisplayVerses = await BibleVerse.GetVersesAsync(
                DbContext,
                ModelQuestion.BibleId,
                ModelQuestion.BookNumber,
                ModelQuestion.Chapter,
                ModelQuestion.StartVerse,
                ModelQuestion.EndVerse);

            // nothing else to await here; caller will trigger StateHasChanged
        }

        // Manual handlers for Start/End inputs when not using @bind
        private async Task StartVerseChanged(ChangeEventArgs e)
        {
            if (e?.Value != null && int.TryParse(e.Value.ToString(), out var val))
            {
                ModelQuestion.StartVerse = val;
            }
            else
            {
                ModelQuestion.StartVerse = 1;
            }

            await OnVerseBoundsChanged(e);
        }

        private async Task EndVerseChanged(ChangeEventArgs e)
        {
            if (e?.Value != null && int.TryParse(e.Value.ToString(), out var val))
            {
                ModelQuestion.EndVerse = val;
            }
            else
            {
                ModelQuestion.EndVerse = ModelQuestion.StartVerse;
            }

            await OnVerseBoundsChanged(e);
        }

        private Task OnPointsChanged(ChangeEventArgs e)
        {
            if (e?.Value != null && int.TryParse(e.Value.ToString(), out var val))
            {
                ModelQuestion.Points = val;
            }
            else
            {
                ModelQuestion.EndVerse = 1;
            }

            UpdatePBEQuestionPreview();
            return Task.CompletedTask;
        }

        // New input handlers for debounced preview updates
        private Task OnQuestionChanged(ChangeEventArgs e)
        {
            if (e?.Value != null)
            {
                ModelQuestion.Question = e.Value.ToString();
            }
            else { ModelQuestion.Question = null; }

            UpdatePBEQuestionPreview();
            return Task.CompletedTask;
        }
    }
}