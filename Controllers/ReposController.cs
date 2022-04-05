using AccreditAssessment.Extensions;
using AccreditAssessment.Models;
using AccreditAssessment.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Linq;
using AccreditAssessment.Exceptions;

namespace AccreditAssessment.Controllers
{
    public class ReposController : Controller
    {
        private readonly IHttpService _httpService;

        public ReposController(IHttpService usernameService)
        {
            _httpService = usernameService;
        }

        public ActionResult Home()
        {
            if (TempData.ContainsKey("ModelState"))
            {
                TransferModelState((ModelStateDictionary)TempData["ModelState"]);
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Search(User user)
        {
            if (!ModelState.IsValid)
            {
                TempData["ModelState"] = ModelState;
                return RedirectToAction(nameof(Home));
            }

            try
            {
                UserResult userResult = await _httpService.GetUserAsync(user.Username);

                if (userResult.ReposUrl != null)
                {
                    List<RepoResult> repoResults = await _httpService.GetReposAsync(userResult.ReposUrl);
                    TempData["Repos"] = repoResults;
                }

                GitHubUserResult gitHubUserResult = new GitHubUserResult
                {
                    Username = userResult.Name,
                    AvatarUrl = userResult.AvatarUrl,
                    Location = userResult.Location,
                };

                return RedirectToAction(nameof(Result), gitHubUserResult);
            }
            catch (UserNotFoundException ex)
            {
                return new HttpStatusCodeResult(404, ex.Message);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, ex.Message);
            }
        }

        public ActionResult Result(GitHubUserResult userResult)
        {
            if (userResult?.Username == null)
            {
                return RedirectToAction("Home", typeof(ReposController).GetName());
            }

            if (TempData.ContainsKey("Repos"))
            {
                userResult.Repos = (List<RepoResult>)TempData["Repos"];
            }
            else
            {
                userResult.Repos = new List<RepoResult>();
            }

            return View(userResult);
        }

        private void TransferModelState(ModelStateDictionary modelState)
        {
            foreach (string key in modelState.Keys)
            {
                if (modelState[key]?.Errors == null)
                {
                    continue;
                }

                foreach (string message in modelState[key].Errors.Select(x => x.ErrorMessage))
                {
                    ModelState.AddModelError(key, message);
                }
            }
        }
    }
}