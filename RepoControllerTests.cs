using AccreditAssessment.Controllers;
using AccreditAssessment.Exceptions;
using AccreditAssessment.Models;
using AccreditAssessment.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace AccreditAssessment.Tests
{
    [TestClass]
    public class RepoControllerTests
    {
        private Mock<IHttpService> httpServiceMock = new Mock<IHttpService>();
        private ReposController controller = null;

        [TestInitialize]
        public void SetUp()
        {
            httpServiceMock = new Mock<IHttpService>();
            controller = new ReposController(httpServiceMock.Object);
        }


        [TestMethod]
        public void ReposController_Home_WhenModelStateNotPresent_DoesntTransferModelState()
        {
            ActionResult result = controller.Home();
            Assert.IsTrue(!controller.TempData.ContainsKey("ModelState"));
            Assert.IsTrue(controller.ModelState.IsValid);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void ReposController_Home_WhenModelStatePresent_TransfersModelState()
        {
            const string Key = "modelStateKey";

            controller.ModelState.AddModelError(Key, "errorMessage");
            ActionResult result = controller.Home();

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsFalse(controller.ModelState.IsValid);
            Assert.IsTrue(controller.ModelState.ContainsKey(Key));
        }

        [TestMethod]
        public async Task ReposController_Search_UsernameEmptyOrNotPresent_RedirectsToHome_WithErrorMessage()
        {
            const string Key = "username";
            controller.ModelState.AddModelError(Key, "Username is required");

            ActionResult result = await controller.Search(new User());

            Assert.IsInstanceOfType(result, typeof(RedirectToRouteResult));

            RedirectToRouteResult redirectResult = (RedirectToRouteResult)result;

            Assert.IsTrue(redirectResult.RouteValues.ContainsKey("action"));

            Assert.IsTrue(
                redirectResult.RouteValues["action"].ToString() == nameof(ReposController.Home));

            Assert.IsTrue(controller.TempData.ContainsKey("ModelState"));
            Assert.IsInstanceOfType(controller.TempData["ModelState"], typeof(ModelStateDictionary));

            ModelStateDictionary modelState = (ModelStateDictionary)controller.TempData["ModelState"];
            Assert.IsTrue(modelState.ContainsKey(Key));
            Assert.IsTrue(modelState.Values.First().Errors.Select(x => x.ErrorMessage).Contains("Username is required"));
        }


        // Test app handles exception well when JSON Convert throws an exception
        // Test userResult values are populated
        [TestMethod]
        public async Task ReposController_Search_RedirectsWithCorrectValues()
        {
            SetupHttpService_GetUser();
            List<RepoResult> repoResults = SetupHttpService_GetRepoResults();

            ActionResult result = await controller.Search(new User { Username = "tester" });

            Assert.IsInstanceOfType(result, typeof(RedirectToRouteResult));

            RedirectToRouteResult redirectToRouteResult = (RedirectToRouteResult)result;

            Assert.AreEqual("Result", redirectToRouteResult.RouteValues["action"]);
            Assert.AreEqual("name", redirectToRouteResult.RouteValues["Username"]);
            Assert.AreEqual("location", redirectToRouteResult.RouteValues["Location"]);
            Assert.AreEqual("avatarUrl", redirectToRouteResult.RouteValues["AvatarUrl"]);
            Assert.IsTrue(controller.TempData.ContainsKey("Repos"));
            Assert.IsInstanceOfType(controller.TempData["Repos"], typeof(List<RepoResult>));
            Assert.AreEqual(repoResults, (List<RepoResult>)controller.TempData["Repos"]);
        }

        public async Task ReposController_Search_VerifyServiceIsCalled()
        {
            SetupHttpService_GetUser();
            SetupHttpService_GetRepoResults();

            ActionResult result = await controller.Search(new User { Username = "tester" });

            Assert.IsInstanceOfType(result, typeof(RedirectToRouteResult));

            httpServiceMock.Verify(x => x.GetUserAsync("tester"), Times.Once());
            httpServiceMock.Verify(x => x.GetReposAsync("reposUrl"), Times.Once());
        }

        // Test app handles exception well when JSON Convert throws an exception
        public async Task ReposController_Search_HandlesException_WhenJsonConvert_ThrowsException()
        {
            httpServiceMock.Setup(x => x.GetUserAsync("tester")).Throws(new Exception("Test error message"));

            ActionResult result = await controller.Search(new User { Username = "tester" });

            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));

            HttpStatusCodeResult codeResult = (HttpStatusCodeResult)result;

            Assert.AreEqual(500, codeResult.StatusCode);
            Assert.AreEqual("Test error message", codeResult.StatusDescription);
        }

        [TestMethod]
        public async Task ReposController_Search_Returns404WhenUserNotFound()
        {
            httpServiceMock.Setup(x => x.GetUserAsync("tester")).Throws(new UserNotFoundException("tester"));

            ActionResult result = await controller.Search(new User { Username = "tester" });

            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));

            HttpStatusCodeResult codeResult = (HttpStatusCodeResult)result;

            Assert.AreEqual(404, codeResult.StatusCode);
        }

        [TestMethod]
        public void ReposController_Result_RedirectsHome_WhenUsernameIsEmpty()
        {
            GitHubUserResult userResult = new GitHubUserResult()
            {
                AvatarUrl = "avatarUrl",
                Location = "location",
            };

            ActionResult result = controller.Result(userResult);

            Assert.IsInstanceOfType(result, typeof(RedirectToRouteResult));

            RedirectToRouteResult redirectResult = (RedirectToRouteResult)result;
            Assert.AreEqual("Home", redirectResult.RouteValues["action"]);
        }

        public void ReposController_Result_AllDataPopulated()
        {
            List<RepoResult> repoResults = Enumerable.Range(1, 4).Select(x => new RepoResult
            {
                Description = $"Description{x}",
                Name = $"Name{x}",
                StargazerCount = x
            }).ToList();

            controller.TempData["Repos"] = repoResults;

            GitHubUserResult userResult = new GitHubUserResult()
            {
                AvatarUrl = "avatarUrl",
                Location = "location",
                Username = "username"
            };

            ActionResult result = controller.Result(userResult);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            Assert.IsTrue(((ViewResult)result).Model == userResult);
            Assert.IsTrue(userResult.Repos != null && userResult.Repos == repoResults);
        }

        private UserResult SetupHttpService_GetUser(string username = "tester", string location = "location", string name = "name", string avatarUrl = "avatarUrl", string reposUrl = "reposUrl")
        {
            UserResult userResult = new UserResult
            {
                AvatarUrl = avatarUrl,
                Location = location,
                Name = name,
                ReposUrl = reposUrl
            };

            httpServiceMock.Setup(x => x.GetUserAsync(username))
                .Returns(Task.FromResult(userResult));

            return userResult;
        }

        private List<RepoResult> SetupHttpService_GetRepoResults(string reposUrl = "reposUrl", int count = 3)
        {
            List<RepoResult> repoResults = Enumerable.Range(1, count).Select(x => new RepoResult
            {
                Description = $"Description{x}",
                Name = $"Name{x}",
                StargazerCount = x
            }).ToList();

            httpServiceMock.Setup(x => x.GetReposAsync(reposUrl)).Returns(Task.FromResult(repoResults));

            return repoResults;
        }
    }
}
