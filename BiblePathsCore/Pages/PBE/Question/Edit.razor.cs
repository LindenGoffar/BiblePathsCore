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
using System.Runtime.CompilerServices;

namespace BiblePathsCore.Pages.PBE.Question
{
    [Authorize]
    public partial class Edit : ComponentBase
    {
        [Parameter]
        [SupplyParameterFromQuery]
        public int? QuestionId { get; set; }
        [Parameter]
        [SupplyParameterFromQuery(Name = "Caller")]
        public string Caller { get; set; }

        [Inject] private NavigationManager NavManager { get; set; }
        [Inject] private BiblePathsCoreDbContext DbContext { get; set; }
        [Inject] private IOpenAIResponder OpenAIResponder { get; set; }
        [Inject] private IOptions<OpenAISettings> OpenAISettings { get; set; }
        [Inject] private UserManager<IdentityUser> UserManager { get; set; }
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        //[Inject] private IJSRuntime JSRuntime { get; set; }
        ////[CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; }

        //private BibleVerse SelectedVerse { get; set; }
        private QuizQuestion ModelQuestion { get; set; } = new();
        private BibleBook PBEBook { get; set; }
        private string AnswerText { get; set; } = string.Empty;
        private List<BibleVerse> DisplayVerses { get; set; } = new();

        private IdentityUser currentUser;
        private QuizUser PBEUser;
        private string ReturnPath;
        private bool ShowModal { get; set; } = false;
        private bool ShowPreview { get; set; } = false;
        //private bool IsFITBGenerationEnabled { get; set; } = false;
        private bool IsGeneratingAI { get; set; } = false;
        //private bool IsGeneratingFITB { get; set; } = false;
        //private bool HasExclusion { get; set; } = false;
        private bool IsCommentary { get; set; } = false;
        private bool Loading { get; set; } = true;
        private bool IsOpenAIEnabled { get; set; }
        // private int StartVerse { get; set; } = 1;
        // private int EndVerse { get; set; } = 1;
        //private int ChapterQuestionCount { get; set; } = 0;
        //private int CommentaryQuestionCount { get; set; } = 0;
        //private int ChapterFITBPct { get; set; } = 0;

        // Optional error message shown in the UI when set
        private string ErrorMessage { get; set; } = string.Empty;
        
        // Page specific propoerties
        private bool isInitialized = false;
        private bool isAuthenticated = false;

        // track whether we've already scrolled to the verse
        //private bool _scrolledToVerse = false;

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

            ReturnPath = Caller;

            // Ensure Authorization and initial load when parameters are available
            if (!isAuthenticated)
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                isAuthenticated = authState.User.Identity?.IsAuthenticated == true;
                currentUser = isAuthenticated ? await UserManager.GetUserAsync(authState.User) : null;

                if (isAuthenticated && QuestionId.HasValue)
                {
                    // Go grab the question in question. 
                    ModelQuestion = await DbContext.QuizQuestions.FindAsync(QuestionId);

                    // Initialize ModelQuestion and verse data after parameters are set
                    if (ModelQuestion == null)
                    {
                        ErrorMessage = "That's Odd! We weren't able to find this Question.";
                    }
                    else
                    {
                        isInitialized = true;
                    }
                    // let's check our logged on users permisions
                    PBEUser = await QuizUser.GetOrAddPBEUserAsync(DbContext, currentUser.Email);
                    if (PBEUser == null || !PBEUser.IsValidPBEQuestionBuilder())
                    {
                        // simply close modal; in a real app show error
                        ErrorMessage = "Sorry... Logged on user is not authorized to edit this PBE Questions.";
                        //CloseModal();
                        return;
                    }

                    Loading = false;
                    
                }
            }

            await base.OnParametersSetAsync();
        }

        //protected override async Task OnAfterRenderAsync(bool firstRender)
        //{
        //    // If a Verse was provided in the URL, scroll to it once the verses are rendered
        //    if (!_scrolledToVerse && Verse.HasValue && ModelQuestion?.Verses?.Any() == true)
        //    {
        //        if (Verse.Value < 5) // We don't want to scroll if it's within the first 5 verses
        //        {
        //            // No need to scroll within first few verses
        //            return;
        //        }
        //        try
        //        {
        //            var elementId = $"verse-{Verse.Value}";
        //            await JSRuntime.InvokeVoidAsync("bpScrollToVerse", elementId);
        //            _scrolledToVerse = true;
        //        }
        //        catch
        //        {
        //            // ignore JS errors
        //        }
        //    }

        //    await base.OnAfterRenderAsync(firstRender);
        //}

        private async Task InitializeQuestionAsync()
        {

            PBEBook = await BibleBook.GetPBEBookAndChapterAsync(DbContext, ModelQuestion.BibleId, ModelQuestion.BookNumber, ModelQuestion.Chapter);
            if (PBEBook == null) { ErrorMessage = "That's Odd! We weren't able to find the PBE Book."; }

            // the commentary scenario requires Verse info so doing this before we Populate PBE Question info.
            ModelQuestion.Verses = await ModelQuestion.GetBibleVersesAsync(DbContext, false);
            ModelQuestion.PopulatePBEQuestionInfo(PBEBook);

            // In the Commentary Scenario we have no real "Chapter" so will need to fake some properties like isCommentary
            IsCommentary = (ModelQuestion.Chapter == Bible.CommentaryChapter);
        }
        //private async Task OpenModal(BibleVerse verse)
        //{
        //    //SelectedVerse = verse;
        //    //ModelQuestion.StartVerse = SelectedVerse.Verse;
        //    //ModelQuestion.EndVerse = SelectedVerse.Verse;
        //    UpdatePBEQuestionPreview();
        //    await UpdateDisplayVersesAsync();
        //    ShowModal = true;
        //}

        //private void CloseModal()
        //{
        //    //// Clear any Question/Answer Text entered
        //    //ModelQuestion.Question = null;
        //    //AnswerText = null;
        //    //ModelQuestion.Points = 1;
        //    ShowModal = false;

        //}

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
                AnswerText = string.Empty;
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

        //private async Task GenerateFITBAsync()
        //{
        //    if (SelectedVerse == null) return;

        //    IsGeneratingFITB = true;
        //    await InvokeAsync(StateHasChanged);
        //    try
        //    {
        //        ModelQuestion.StartVerse = SelectedVerse.Verse;
        //        ModelQuestion.EndVerse = SelectedVerse.Verse;

        //        var verse = await BibleVerse.GetVerseAsync(DbContext, ModelQuestion.BibleId, ModelQuestion.BookNumber, ModelQuestion.Chapter, ModelQuestion.StartVerse);
        //        var built = await new QuizQuestion().BuildQuestionForVerseAsync(DbContext, verse, 10, ModelQuestion.BibleId);
        //        if (built != null)
        //        {
        //            ModelQuestion.Question = built.Question;
        //            ModelQuestion.Points = built.Points;
        //            ModelQuestion.EndVerse = ModelQuestion.StartVerse; // We only generate on StartVerse so let's change this. 
        //            // ModelQuestion.Type = built.Type; // Type is detected later.
        //            foreach (QuizAnswer Answer in built.QuizAnswers)
        //            {
        //                AnswerText += Answer.Answer;
        //            }
        //            ModelQuestion.Source = built.Source;

        //            // update preview
        //            UpdatePBEQuestionPreview();
        //            await InvokeAsync(StateHasChanged);
        //        }
        //    }
        //    finally
        //    {
        //        IsGeneratingFITB = false;
        //        await InvokeAsync(StateHasChanged);
        //    }
        //}

        private async Task SaveAsync()
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(ModelQuestion.BibleId)) return;
            if (ModelQuestion.BookNumber <= 0 || ModelQuestion.Chapter <= 0) return;

            // Get user
            if (currentUser != null)
            {
                var email = currentUser.Email;
                PBEUser = await QuizUser.GetOrAddPBEUserAsync(DbContext, currentUser.Email);
                if (PBEUser == null || ( (PBEUser.Email != ModelQuestion.Owner) && !PBEUser.IsQuizModerator() ) )
                {
                    // 11/19/2023 We are having edit problems so only letting owners or moderators do question edits.
                    ErrorMessage = "Sorry! You do not have sufficient rights to edit this PBE question";
                    return;
                }
            }
            else
            {
                ErrorMessage = "That's odd we do not have a logged in user.";
                return;
            }
            ModelQuestion.Modified = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(AnswerText))
            {
                // We need the Original Answer and while techincally we support multiple Answers
                // we are only going to allow operating on the first one in this basic edit experience.
                await DbContext.Entry(ModelQuestion).Collection(Q => Q.QuizAnswers).LoadAsync();
                if (ModelQuestion.QuizAnswers.Count > 0)
                {
                    QuizAnswer OriginalAnswer = ModelQuestion.QuizAnswers.OrderBy(A => A.Id).First();
                    if (OriginalAnswer.Answer != AnswerText)
                    {
                        // It's not clear whether we need to track history of answers so for now we will just update the existing one.
                        OriginalAnswer.Modified = DateTime.Now;
                        OriginalAnswer.Answer = AnswerText;
                        OriginalAnswer.IsPrimary = true;
                        ModelQuestion.IsAnswered = true;
                    }
                }
            }

            await DbContext.SaveChangesAsync();

            // Assuming success... we now navigate away
            switch (ReturnPath)
            {
                case "Questions":
                    NavManager.NavigateTo($"/PBE/Questions?BibleId={ModelQuestion.BibleId}&BookNumber={ModelQuestion.BookNumber}&Chapter={ModelQuestion.Chapter}", forceLoad: true);
                    //return RedirectToPage("Questions", new { BibleId = QuestionToUpdate.BibleId, BookNumber = QuestionToUpdate.BookNumber, Chapter = QuestionToUpdate.Chapter });
                    break; 

                //case "ChallengedQuestions":
                //    return RedirectToPage("ChallengedQuestions", new { BibleId = QuestionToUpdate.BibleId, BookNumber = QuestionToUpdate.BookNumber, Chapter = QuestionToUpdate.Chapter });
                //// break; not needed unreachable

                default:
                    NavManager.NavigateTo($"/PBE/Questions?BibleId={ModelQuestion.BibleId}&BookNumber={ModelQuestion.BookNumber}&Chapter={ModelQuestion.Chapter}", forceLoad: true);
                    break; 
            }

            // Optionally refresh verse list or navigate to same chapter
            //CloseModal();
            //await InitializeQuestionAsync();
            //await InvokeAsync(StateHasChanged);
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