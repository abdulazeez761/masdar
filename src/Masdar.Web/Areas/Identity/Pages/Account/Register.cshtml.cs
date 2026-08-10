using System.ComponentModel.DataAnnotations;
using Masdar.Web.Data;
using Masdar.Web.Domain.Entities;
using Masdar.Web.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Masdar.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class RegisterModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext db,
    ILogger<RegisterModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public sealed class InputModel
    {
        [Required, StringLength(120, MinimumLength = 2)]
        [Display(Name = "Full name")]
        public string DisplayName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        if (!ModelState.IsValid) return Page();

        var now = DateTime.UtcNow;
        var user = new ApplicationUser
        {
            UserName = Input.Email.Trim(),
            Email = Input.Email.Trim(),
            EmailConfirmed = true,
            DisplayName = Input.DisplayName.Trim(),
            IsActive = true,
            ReputationBalance = 100,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var result = await userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            if (!await roleManager.RoleExistsAsync("User"))
                await roleManager.CreateAsync(new IdentityRole("User"));
            var roleResult = await userManager.AddToRoleAsync(user, "User");
            if (!roleResult.Succeeded)
            {
                await userManager.DeleteAsync(user);
                foreach (var error in roleResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }

            db.ReputationTransactions.Add(new ReputationTransaction
            {
                UserId = user.Id,
                Type = ReputationTransactionType.InitialGrant,
                Points = 100,
                BalanceAfter = 100,
                Reason = "Welcome reputation grant",
                IdempotencyKey = $"registration:{user.Id}",
                RelatedEntityType = nameof(ApplicationUser),
                RelatedEntityId = user.Id
            });
            await db.SaveChangesAsync();
            logger.LogInformation("A new MASDAR user account was created.");
            await signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return Page();
    }
}
