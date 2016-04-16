﻿using CampusNabber.Helpers.SchoolClasses;
using CampusNabber.Models;
using CampusNabber.Utility;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CampusNabber.Controllers
{
    public class ProfileController : Controller
    {
        private ApplicationUserManager _userManager;

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        // GET: Profile
        public ActionResult Index()
        {
            return View();
        }

        public ProfileController()
        {

        }

        public ProfileController(ApplicationUserManager manager)
        {
            UserManager = manager;
        }

        public ActionResult ProfileView(string failedPost)
        {

            if (_userManager == null)
                _userManager = UserManager;
            ViewBag.userName = User.Identity.GetUserName();

            //Creates profile model and assigns current users posts to the model
            var profile = new ProfileModel();
            profile.getProfilePosts(_userManager.FindById(User.Identity.GetUserId()));
            profile.user = (_userManager.FindById(User.Identity.GetUserId()));

            //Creates school drop down menu
            SelectList selectCategory = PostItemService.generateSchoolsList();
            ViewBag.selectCategory = selectCategory;
            TempData["FailedPost"] = failedPost;
            return View(profile);
        }

        //Deactivates user and deletes references in all tables.
        public async Task<ActionResult> Deactivate()
        {

            if (_userManager == null)
                _userManager = UserManager;
            ViewBag.userName = User.Identity.GetUserName();
            var id = User.Identity.GetUserId();
            ApplicationUser user = (_userManager.FindById(User.Identity.GetUserId()));
            var logins = user.Logins;

            foreach (var login in logins.ToList())
            {
                await _userManager.RemoveLoginAsync(login.UserId, new UserLoginInfo(login.LoginProvider, login.ProviderKey));
            }

            var rolesForUser = await _userManager.GetRolesAsync(id);

            if (rolesForUser.Count() > 0)
            {
                foreach (var item in rolesForUser.ToList())
                {
                    // item should be the name of the role
                    var result = await _userManager.RemoveFromRoleAsync(user.Id, item);
                }
            }

            //Delete all postings from user.
            PostItemService.deleteALlPostsByUsername(user.UserName);

            await _userManager.DeleteAsync(user);
            return RedirectToAction("LogOffWithoutPost", "Account");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateUser(ProfileModel profileModel)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser Model = UserManager.FindById(User.Identity.GetUserId());
                string oldUserName = Model.UserName;
                string oldSchool = Model.school_name;
                Model.Email = profileModel.user.Email;
                Model.UserName = profileModel.user.UserName;
                Model.school_name = profileModel.user.school_name;
                IdentityResult result = await UserManager.UpdateAsync(Model);
                if(result.Succeeded)
                {
                    if (!oldUserName.Equals(profileModel.user.UserName))
                    {
                        PostItemService.updateAllPostItemsInfo(Model, oldUserName);
                        //Are we sure we want to log the user off when they change their username?
                        return RedirectToAction("LogOffWithoutPost", "Account");
                    }
                    else if (!oldSchool.Equals(profileModel.user.school_name))
                    {
                        PostItemService.updateAllPostItemsInfo(Model, oldUserName);

                    }
                    return RedirectToAction("ProfileView", "Profile", new { failedPost = "false" });
                }
            } 
            return RedirectToAction("ProfileView", "Profile", new { failedPost = "true" });
        }

    }
}