using BlazorBlogs.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorBlogs.Data.Models;
using Microsoft.Extensions.Logging;

namespace BlazorBlogs.Account.Pages
{
    public class InstallWizardAuthModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        public InstallWizardAuthModel(
                   SignInManager<ApplicationUser> signInManager,
                   UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> OnGetAsync(string paramUsername, string paramPassword, string ReturnUrl)
        {
            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, set lockoutOnFailure: true
            await _signInManager.PasswordSignInAsync(paramUsername, paramPassword, false, lockoutOnFailure: false);
            return LocalRedirect(ReturnUrl);
        }
    }
}