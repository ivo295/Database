
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Mail;
using System.IO;
using System.Drawing;
using System.Web.Security;
using System.ComponentModel.Design;
using Microsoft.AspNet.Identity;
using KSUCorner.Models;

/*
 * Course: ACST 3540
 * Section: 01
 * Name: Ivo Simeonov
 * Professor: Prof. Shaw
 * Assignment #: Mod 8 Assignment 2
 */

namespace KSUCorner.Controllers
{
    public class HomeController : Controller
    {
        // View method name for each Tab
        string[] tabViews = { "Index", "Profile", "Friends", "Messaging",
                            "MediaGalleries", "Blogging", "Forums", "Groups",
                            "Admin", "About" };

        // View label displayed on each Tab
        string[] tabLabels = { "Home", "Profile", "Friends", "Messaging",
                            "Media Galleries", "Blogging", "Forums", "Groups",
                            "Admin", "About" };

        public KSUCornerDBEntities db = new KSUCornerDBEntities();

        public ActionResult Index()
        {
            Session["PageHeading"] = "KSU Corner";

            if (!Request.IsAuthenticated)
            {
                return RedirectToAction("LoginOrRegister");
            }

            try
            {
                if (User.Identity.GetUserName() == null)
                {
                    return RedirectToAction("BadAccount");
                }
            }
            catch (Exception ex)
            {
                Session["ErrorMessage"] = "Ex: " + ex.Message;
                Session["InnerException"] = ex.InnerException;
                return RedirectToAction("ErrorMessage");
            }


            try
            {
                string username = User.Identity.GetUserName();
                string email = GetEmail(username);

                EmailConfirmation ev = db.EmailConfirmations.First(x => x.UserName == username && x.Email == email);
                if (ev.IsConfirmed == false)  // Email confirmation record found but no confirmation yet
                    return RedirectToAction("StillNeedConfirmation");
            }
            catch
            {
                // No email confirmation found so authentication message is emailed to the user
                return RedirectToAction("SendAuthenticationEmail");
            }

            // If this line is reached, the email confirmation record was found and confirmation was completed
            // So the user may now proceed to the Index view and the rest of the KSU Corner application
            ViewBag.Label1 = "<h2>Welcome to the KSU Corner!</h2>";

            return View();
        }

        public ActionResult About()
        {
            // The About Us message and image
            ViewBag.Message = "About Us at KSU Corner!<p><i>Webmaster: Ivo Simeono</i>";
            ViewBag.Message2 = "<img src=\"/Images/aboutus.jpg\" />";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your Contact Page";

            return View();
        }

        public ActionResult LoginOrRegister()
        {
            // The user needs to login or register
            ViewBag.Message = "You must <a href=\"/Account/Login\">Login</a> or ";
            ViewBag.Message += "<a href=\"/Account/Register\">Register</a> a new ";
            ViewBag.Message += "account<br>in order to access the KSU Corner website.";
            ViewBag.Message2 = "<img src=\"/Images/pleaselogin.jpg\" />";
            return View();
        }

        public ActionResult StillNeedConfirmation()
        {
            // The user's email address has not been confirmed yet
            ViewBag.Message = "Your email account has not been confirmed yet!<br>";
            ViewBag.Message += "Read the email that was sent and follow the instructions...";
            ViewBag.Message += "<ul><li>Or you can <a href=\"/Home/SendAuthenticationEmail\">";
            ViewBag.Message += "Have Another Email Sent to you</a></li>";
            ViewBag.Message += "<li>Or you can <a href=\"/Home/LogOff\">LogOff</a> and ";
            ViewBag.Message += "try to create a new account</li></ul><p></p>";
            ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
            return View();
        }

        public ActionResult BadAccount()
        {
            // The Account is bad because its credentials are not registering properly with the system
            ViewBag.Message = "Your account is not functioning properly!<p>";
            ViewBag.Message += "You will have to <a href=\"/Home/LogOff\">LogOff</a> and ";
            ViewBag.Message += "try again<br>you can create a new account after logging off.";
            ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";

            return View();
        }

        public ActionResult SetConfirmed(string ID)
        {
            EmailConfirmation ev = null;

            if (!string.IsNullOrEmpty(ID))
            {
                try
                {
                    // Locating the email confirmation record using the ID in the URL
                    ev = db.EmailConfirmations.First(x => x.ID == ID);
                    ev.IsConfirmed = true;
                    db.SaveChanges();

                    // Sets Account flags confirming the email and activating the account
                    using (KSUCornerDBEntities1 db2 = new KSUCornerDBEntities1())
                    {
                        var user = db2.Accounts.First(x => x.Email == ev.Email);
                        user.EmailConfirmed = true;
                        user.IsActivated = true;
                        db2.SaveChanges();
                    }
                }
                catch
                {
                    ev = null;
                }
            }
            if (ev != null)
            {
                // Found the email confirmation record using the ID in the URL
                ViewBag.Message = "Email Confirmation Succeeded - You can now access KSU Corner!";
                ViewBag.Message2 = "<img src=\"/Images/validemail.jpg\" />";
            }
            else
            {
                // Did not find the email confirmation record using the ID in the URL
                ViewBag.Message = "Email Confirmation Failed - Your URL was invalid<p>";
                ViewBag.Message += "so you must go back to your email and try again.";
                ViewBag.Message2 = "<img src=\"/Images/invalidemail.jpg\" />";
            }
            return View();
        }

        // Sends authentication message to the user to verify their email address
        public ActionResult SendAuthenticationEmail()
        {
            if (!Request.IsAuthenticated)
            {
                return RedirectToAction("LoginOrRegister");
            }
            if (User.Identity.GetUserName() == null)
            {
                return RedirectToAction("BadAccount");
            }

            // Creating a globally unique identifier for the email confirmation record Key
            string username = User.Identity.GetUserName();
            string email = GetEmail(username);

            string guidID = Guid.NewGuid().ToString() + "-" + URLFriendly(username) + "-" + URLFriendly(email);

            EmailConfirmation ev = null;
            try
            {
                // Testing to see if the email confirmation record is already in the DB
                ev = db.EmailConfirmations.First(x => x.UserName == username && x.Email == email);
                guidID = ev.ID;
                ev.IsConfirmed = false;
            }
            catch
            {
                ev = null;
            }
            if (ev == null)
            {
                // Adding a new email confirmation record to the DB
                ev = new EmailConfirmation();
                ev.ID = guidID;
                ev.UserName = username;
                ev.Email = email;
                ev.IsConfirmed = false;
                db.EmailConfirmations.Add(ev);
            }
            db.SaveChanges();

            try
            {
                // Getting the Url's domain string
                string domain = Request.Url.ToString();
                int off = Math.Max(domain.ToLower().IndexOf("/account"), domain.ToLower().IndexOf("/home"));
                domain = (off < 1) ? domain : domain.Substring(0, off);
                if (domain.Length > 0 && domain[domain.Length - 1] != '/')
                    domain += '/';

                // Preparing authentication message to send to user
                MailMessage message = new MailMessage();
                message.From = new MailAddress("DoNotReply@KSUCorner.edu");
                message.To.Add(new MailAddress(email));
                message.Subject = "KSU Corner Email Confirmation";
                message.Body = "Hello New KSU Corner Member (" + username + ")!";
                message.Body += Environment.NewLine + Environment.NewLine;
                message.Body += "In order to gain access to the KSU Corner website, ";
                message.Body += "just click on the link below, or copy and paste it ";
                message.Body += "into your browser's address field:";
                message.Body += Environment.NewLine + Environment.NewLine;
                message.Body += domain + "Home/SetConfirmed/" + guidID;
                message.Body += Environment.NewLine + Environment.NewLine;
                message.Body += "We look forward to your participation in KSU's ";
                message.Body += "newest Social Networking venture.";
                message.Body += Environment.NewLine + Environment.NewLine;
                message.Body += "-The Staff at KSU Corner";

                // Sending the message to the SMTP server
                SmtpClient client = new SmtpClient();
                client.Host = "208.73.222.114";
                client.Port = 7301;
                client.Send(message);

                // Explains to the user the message was sent
                ViewBag.Message = "Authentication instructions were sent to your email address!<p>";
                ViewBag.Message += "Read the email and follow the instructions, and then<br>";
                ViewBag.Message += "you'll be able to access the KSU Corner Homepage.";
                ViewBag.Message2 = "<img src=\"/Images/invalidemail.jpg\" />";
            }
            catch (Exception err)
            {
                // The email message failed to send for some reason
                ViewBag.Message = "Error Sending Email: " + err.Message;
            }
            return View();
        }

        // Lets the user log off
        public ActionResult LogOff()
        {
            Request.Cookies.Remove("UserId");
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Account");
        }

        // Gets the list of tabs and the Site heading label
        public ActionResult GetTabs(string id)
        {
            string headStr = "";
            if (Session["PageHeading"] != null)
            {
                headStr += "<ul id=\"headmenu\"><li>";
                if (tabViews.Length > 0)
                    headStr += "<a href=\"/Home/" + tabViews[0] + "\">";
                headStr += Session["PageHeading"].ToString();
                headStr = headStr.Replace(":", "");
                if (tabViews.Length > 0)
                    headStr += "</a>";
                headStr += "</li></ul>:";
            }
            int tabNum = -1;
            for (int i = 0; i < tabViews.Length && tabNum < 0; ++i)
                if (tabViews[i].ToLower() == id.ToLower())
                    tabNum = i;
            string tabStr = "<ul id=\"tabmenu\">" + Environment.NewLine;
            for (int i = 0; i < tabViews.Length; ++i)
            {
                tabStr += "<li>";
                if (i != tabNum)
                    tabStr += "<a href=\"/Home/" + tabViews[i] + "\">";
                tabStr += tabLabels[i];
                if (i != tabNum)
                    tabStr += "</a>";
                tabStr += "</li>" + Environment.NewLine;
            }
            tabStr += "</ul>";
            return Content(headStr + tabStr);
        }

        // Converts a string to something that won't break a URL
        public string URLFriendly(string sentence)
        {
            if (sentence == null) return "";
            string result = "";
            for (int i = 0; i < sentence.Length; i++)
            {
                char c = sentence[i];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') ||
                                c == '-' || c == '_' || c == '+' || c == '@')
                    result += c;
                else if (c == ' ' || c == ',' || c == '=')
                    result += '-';
            }
            return result;
        }

        // Gets the email string from the Accounts table
        public string GetEmail(string username)
        {
            using (KSUCornerDBEntities1 db1 = new KSUCornerDBEntities1())
            {
                var result = from u in db1.Accounts where (u.UserName == username) select u;
                if (result.Count() > 0)
                    return result.FirstOrDefault().Email;
            }
            return "";
        }

        [Authorize]
        public ActionResult Profile(string ID)
        {
            string username = (String.IsNullOrEmpty(ID)) ? User.Identity.GetUserName() : ID;
            string fullname = GetFullname(username, false);
            using (KSUCornerDBEntities1 db1 = new KSUCornerDBEntities1())
            {
                var result = from u in db1.Accounts where (u.UserName == username) select u;
                if (result.Count() == 0)
                {
                    if (username.ToUpper() != User.Identity.GetUserName().ToUpper())
                        Session["NotOwnerAccount"] = true;
                    return RedirectToAction("BadAccount");
                }

                Account account = result.FirstOrDefault();
                Profile profile = GetProfile(username, account);

                if (username.ToUpper() == User.Identity.GetUserName().ToUpper())
                {
                    ViewBag.Message = "My Public Profile:";
                    ViewBag.IsOwner = true;
                    if (Session["EmailStatus"] != null && Session["EmailStatus"].ToString() != "")
                    {
                        if (Session["EmailStatus"].ToString() == "ConfirmRequestSent")
                        {
                            ViewBag.Message2 = "<h3><font color=\"purple\">";
                            ViewBag.Message2 += "A request for confirmation has been sent to your ";
                            ViewBag.Message2 += "new email address.<br />Your email address will not change ";
                            ViewBag.Message2 += "until you confirm it by following<br />the instructions ";
                            ViewBag.Message2 += "in the email you have been sent.";
                            ViewBag.Message2 += "</font></h3><br />";
                        }
                        else
                        {
                            ViewBag.Message2 = Session["EmailStatus"];
                        }
                        Session["EmailStatus"] = "";
                    }
                }
                else
                {
                    fullname += (fullname.ToLower()[fullname.Length - 1] != 's') ? "'s" : "'";
                    ViewBag.Message = fullname + " Public Profile:";
                    ViewBag.IsOwner = false;
                }

                ViewBag.DefaultAvatar =
                    "<img src=\"/Images/DefaultAvatar.jpg\" alt=\"Profile Avatar\" />";
                ViewBag.Email = (profile.EmailConfirmed) ? profile.Email : account.Email;
                ViewBag.Label1 = "Account Info";
                ViewBag.Label2 = "Email";
                ViewBag.Label3 = "Gender";
                ViewBag.Label4 = "Birthday";
                ViewBag.Label5 = "Updated";
                ViewBag.Label6 = "<b>What I Am Interested In</b>";
                ViewBag.Label7 = "<b>Some Details About Me</b>";

                return View(profile);
            }
        }

        [Authorize]
        public ActionResult EditProfile()
        {
            ViewBag.Message = "Edit Profile:";
            string username = User.Identity.GetUserName();
            using (KSUCornerDBEntities1 db1 = new KSUCornerDBEntities1())
            {
                var result = from u in db1.Accounts where (u.UserName == username) select u;
                if (result.Count() == 0)
                    return RedirectToAction("BadAccount");
                Account account = result.FirstOrDefault();
                Profile profile = GetProfile(username, account);

                SetProfileViewData(profile);

                return View(profile);
            }
        }

        [HttpPost, Authorize]
        public ActionResult EditProfile(Profile model, HttpPostedFileBase FileUpload, FormCollection form)
        {
            Session["EmailStatus"] = "";
            SetProfileViewData(null);

            if (!ModelState.IsValid)
            {
                ViewBag.Message = "Error Editing Profile:";
                return View("EditProfile", model);
            }
            string username = User.Identity.GetUserName();
            string avatarFilePath = "";
            int avatarWidth = -1, avatarHeight = -1;
            if (FileUpload != null && FileUpload.ContentLength > 0)
            {
                string fileName = URLFriendly2(username + "@_@" + Path.GetFileName(FileUpload.FileName));
                avatarFilePath = Path.Combine(Server.MapPath("/Content/uploads/"), fileName);
                try
                {
                    FileUpload.SaveAs(avatarFilePath);

                    Bitmap img = new Bitmap(avatarFilePath);
                    avatarWidth = img.Width;
                    avatarHeight = img.Height;

                    avatarFilePath = "/Content/uploads/" + fileName;
                }
                catch (ArgumentException)
                {
                    ViewBag.Message = "Error Editing Profile:";
                    ViewData.ModelState.AddModelError("", "Error: The file uploaded was not an image file.");
                    return View("EditProfile", model);
                }
                catch (Exception err)
                {
                    ViewBag.Message = "Error Editing Profile:";
                    ViewData.ModelState.AddModelError("", "Error: " + err.Message);
                    return View("EditProfile", model);
                }
            }
            else if (FileUpload != null && FileUpload.ContentLength == 0)
            {
                ViewBag.Message = "Error Editing Profile:";
                ViewData.ModelState.AddModelError("", "Error: The image file was an empty file.");
                return View("EditProfile", model);
            }

            using (KSUCornerDBEntities2 db2 = new KSUCornerDBEntities2())
            {
                try
                {
                    Profile profile = db2.Profiles.First(x => x.UserName == username);
                    profile.FirstName = model.FirstName;
                    profile.LastName = model.LastName;

                    if (profile.Email.ToUpper() != model.Email.ToUpper())
                    {
                        profile.Email = model.Email;
                        profile.EmailConfirmed = false;
                        if (SendAuthenticationProfileEmail(username, model.Email))
                            Session["EmailStatus"] = "ConfirmRequestSent";
                    }
                    profile.EmailIsPublic = model.EmailIsPublic;

                    profile.Gender = model.Gender;
                    profile.GenderIsPublic = model.GenderIsPublic;

                    try
                    {
                        profile.BirthDate = new DateTime(Int32.Parse(form["Year"]),
                                                   Int32.Parse(form["Month"]), Int32.Parse(form["Day"]));
                        profile.BirthDateIsPublic = model.BirthDateIsPublic;
                    }
                    catch
                    {
                        ViewBag.Message = "Error Editing Profile:";
                        ViewData.ModelState.AddModelError("", "Error: You entered an invalid date.");
                        return View("EditProfile", model);
                    }

                    if (avatarFilePath != "")
                    {
                        profile.AvatarPath = avatarFilePath;
                        profile.AvatarWidth = avatarWidth;
                        profile.AvatarHeight = avatarHeight;
                    }
                    profile.AvatarIsPublic = model.AvatarIsPublic;

                    profile.Interests = model.Interests;
                    profile.InterestsIsPublic = model.InterestsIsPublic;

                    profile.AboutMe = model.AboutMe;
                    profile.AboutMeIsPublic = model.AboutMeIsPublic;

                    profile.LastUpdateDate = DateTime.Now;

                    db2.SaveChanges();

                    using (KSUCornerDBEntities1 db1 = new KSUCornerDBEntities1())
                    {
                        Account account = db1.Accounts.First(x => x.UserName == username);
                        if (account.FirstName != profile.FirstName || account.LastName != profile.LastName ||
                             account.Gender != profile.Gender || account.BirthDate != profile.BirthDate)
                        {
                            account.FirstName = profile.FirstName;
                            account.LastName = profile.LastName;
                            account.Gender = profile.Gender;
                            account.BirthDate = profile.BirthDate;
                            account.LastUpdateDate = DateTime.Now;
                            db1.SaveChanges();
                        }
                    }
                }
                catch (Exception err)
                {
                    ViewBag.Message = "Error Editing Profile:";
                    ViewData.ModelState.AddModelError("", "Error: " + err.Message);
                    return View("EditProfile", model);
                }
            }
            return RedirectToAction("Profile");
        }

        public bool SendAuthenticationProfileEmail(string username, string email)
        {
            // Creating a globally unique identifier for the email confirmation record Key
            string guidID = "_@_" + Guid.NewGuid().ToString() + "-" + URLFriendly(username) + "-" + URLFriendly(email);

            using (KSUCornerDBEntities db = new KSUCornerDBEntities())
            {
                EmailConfirmation ev = null;
                try
                {
                    // Testing to see if the email confirmation record is already in the DB
                    ev = db.EmailConfirmations.First(x => x.UserName == username && x.Email == email);
                    guidID = ev.ID;
                    if (guidID.Substring(0, 3) != "_@_")
                    {
                        guidID = "_@_" + guidID;
                        ev.ID = guidID;
                    }
                    ev.IsConfirmed = false;
                }
                catch
                {
                    ev = null;
                }
                if (ev == null)
                {
                    // Adding a new email confirmation record to the DB
                    ev = new EmailConfirmation();
                    ev.ID = guidID;
                    ev.UserName = username;
                    ev.Email = email;
                    ev.IsConfirmed = false;
                    db.EmailConfirmations.Add(ev);
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception err)
                {
                    // The email message failed to send for some reason
                    Session["EmailStatus"] = "Error Sending Confirm Email Request: " + err.Message;
                }
            }

            try
            {
                // Getting the Url's domain string
                string domain = Request.Url.ToString();
                int off = Math.Max(domain.ToLower().IndexOf("/account"), domain.ToLower().IndexOf("/home"));
                domain = (off < 1) ? domain : domain.Substring(0, off);
                if (domain.Length > 0 && domain[domain.Length - 1] != '/')
                    domain += '/';

                // Preparing authentication message to send to user
                MailMessage message = new MailMessage();
                message.From = new MailAddress("DoNotReply@KSUCorner.edu");
                message.To.Add(new MailAddress(email));
                message.Subject = "KSU Corner Email Confirmation";
                message.Body = "Hello " + GetFullname(username, false) + " (KSU Corner Member)!";
                message.Body += Environment.NewLine + Environment.NewLine;
                message.Body += "In order to change your email address you need to confirm it by ";
                message.Body += "just clicking on the link below, or pasting it into your browser's ";
                message.Body += "address field:";
                message.Body += Environment.NewLine + Environment.NewLine;
                message.Body += domain + "Home/SetConfirmed/" + guidID;
                message.Body += Environment.NewLine + Environment.NewLine;
                message.Body += "-The Staff at KSU Corner";

                // Sending the message to the SMTP server
                SmtpClient client = new SmtpClient();
                client.Host = "208.73.222.114";
                client.Port = 7301;
                client.Send(message);
            }
            catch (Exception err)
            {
                // The email message failed to send for some reason
                Session["EmailStatus"] = "Error Sending Confirm Email Request: " + err.Message;
            }
            return true;
        }

        public void SetProfileViewData(Profile profile)
        {
            if (profile == null)
            {
                string username = User.Identity.GetUserName();
                using (KSUCornerDBEntities1 db1 = new KSUCornerDBEntities1())
                {
                    var result = from u in db1.Accounts where (u.UserName == username) select u;
                    if (result.Count() > 0)
                    {
                        Account account = result.FirstOrDefault();
                        profile = GetProfile(username, account);
                    }
                }
            }
            ViewBag.Content1 = profile.UserName;
            string filename = "";
            if (!String.IsNullOrEmpty(profile.AvatarPath))
            {
                string[] names = profile.AvatarPath.Split('/');
                filename = names[names.Length - 1];
                int x = filename.IndexOf("@_@");
                if (x >= 0)
                    filename = filename.Substring(x + 3);
            }
            ViewBag.Content2 = (filename == "") ? "" :
                        "<br /><font size=\"2\" color=\"purple\">(Current Avatar: " + filename + ")</font>";
            ViewBag.Label1 = "Username";
            ViewBag.Label2 = "First Name";
            ViewBag.Label3 = "Last Name";
            ViewBag.Label4 = "Email";
            ViewBag.Label5 = "Gender";
            ViewBag.Label6 = "Birth Date";
            ViewBag.Label7 = "Avatar Image";
            ViewBag.Label8 = "My Interests";
            ViewBag.Label9 = "About Me";

            int theMonth = profile.BirthDate.Month;
            int theDay = profile.BirthDate.Day;
            int theYear = profile.BirthDate.Year;

            SelectListItem month01 = new SelectListItem() { Text = "January", Value = "01" };
            SelectListItem month02 = new SelectListItem() { Text = "February", Value = "02" };
            SelectListItem month03 = new SelectListItem() { Text = "March", Value = "03" };
            SelectListItem month04 = new SelectListItem() { Text = "April", Value = "04" };
            SelectListItem month05 = new SelectListItem() { Text = "May", Value = "05" };
            SelectListItem month06 = new SelectListItem() { Text = "June", Value = "06" };
            SelectListItem month07 = new SelectListItem() { Text = "July", Value = "07" };
            SelectListItem month08 = new SelectListItem() { Text = "August", Value = "08" };
            SelectListItem month09 = new SelectListItem() { Text = "September", Value = "09" };
            SelectListItem month10 = new SelectListItem() { Text = "October", Value = "10" };
            SelectListItem month11 = new SelectListItem() { Text = "November", Value = "11" };
            SelectListItem month12 = new SelectListItem() { Text = "December", Value = "12" };
            switch (theMonth)
            {
                case 1:
                    month01 = new SelectListItem() { Selected = true, Text = "January", Value = "01" };
                    break;
                case 2:
                    month02 = new SelectListItem() { Selected = true, Text = "February", Value = "02" };
                    break;
                case 3:
                    month03 = new SelectListItem() { Selected = true, Text = "March", Value = "03" };
                    break;
                case 4:
                    month04 = new SelectListItem() { Selected = true, Text = "April", Value = "04" };
                    break;
                case 5:
                    month05 = new SelectListItem() { Selected = true, Text = "May", Value = "05" };
                    break;
                case 6:
                    month06 = new SelectListItem() { Selected = true, Text = "June", Value = "06" };
                    break;
                case 7:
                    month07 = new SelectListItem() { Selected = true, Text = "July", Value = "07" };
                    break;
                case 8:
                    month08 = new SelectListItem() { Selected = true, Text = "August", Value = "08" };
                    break;
                case 9:
                    month09 = new SelectListItem() { Selected = true, Text = "September", Value = "09" };
                    break;
                case 10:
                    month10 = new SelectListItem() { Selected = true, Text = "October", Value = "10" };
                    break;
                case 11:
                    month11 = new SelectListItem() { Selected = true, Text = "November", Value = "11" };
                    break;
                default:
                    month12 = new SelectListItem() { Selected = true, Text = "December", Value = "12" };
                    break;
            }
            SelectListItem[] months = { month01, month02, month03, month04, month05, month06,
                                             month07, month08, month09, month10, month11, month12 };
            ViewBag.Month = months;

            String[] days = { "1", "2", "3", "4", "5", "6", "7", "8",
                              "9", "10", "11", "12", "13", "14", "15", "16",
                              "17", "18", "19", "20", "21", "22", "23", "24",
                              "25", "26", "27", "28", "29", "30", "31" };
            SelectList dayItems = new SelectList(days, theDay.ToString());
            ViewBag.Day = dayItems;

            String[] years = { "1910", "1911", "1912", "1913", "1914",
                               "1915", "1916", "1917", "1918", "1919",
                               "1920", "1921", "1922", "1923", "1924",
                               "1925", "1926", "1927", "1928", "1929",
                               "1930", "1931", "1932", "1933", "1934",
                               "1935", "1936", "1937", "1938", "1939",
                               "1940", "1941", "1942", "1943", "1944",
                               "1945", "1946", "1947", "1948", "1949",
                               "1950", "1951", "1952", "1953", "1954",
                               "1955", "1956", "1957", "1958", "1959",
                               "1960", "1961", "1962", "1963", "1964",
                               "1965", "1966", "1967", "1968", "1969",
                               "1970", "1971", "1972", "1973", "1974",
                               "1975", "1976", "1977", "1978", "1979",
                               "1980", "1981", "1982", "1983", "1984",
                               "1985", "1986", "1987", "1988", "1989",
                               "1990", "1991", "1992", "1993", "1994",
                               "1995", "1996", "1997", "1998", "1999",
                               "2000", "2001", "2002", "2003", "2004",
                               "2005", "2006", "2007", "2008", "2009", "2010" };
            SelectList yearItems = new SelectList(years, theYear.ToString());
            ViewBag.Year = yearItems;
        }

        public Profile GetProfile(string username, Account account)
        {
            using (KSUCornerDBEntities1 db1 = new KSUCornerDBEntities1())
            {
                using (KSUCornerDBEntities2 db2 = new KSUCornerDBEntities2())
                {
                    if (account == null)
                    {
                        var result = from u in db1.Accounts where (u.UserName == username) select u;
                        if (result.Count() == 0)
                            return null;
                        else
                            account = result.FirstOrDefault();
                    }
                    var result2 = from p in db2.Profiles where (p.UserName == username) select p;
                    if (result2.Count() == 0)
                    {
                        Profile profile = new Profile();
                        profile.AccountID = account.AccountID;
                        profile.UserName = account.UserName;
                        profile.FirstName = account.FirstName;
                        profile.LastName = account.LastName;
                        profile.Email = account.Email;
                        profile.EmailConfirmed = true;
                        profile.EmailIsPublic = false;
                        profile.Gender = account.Gender;
                        profile.GenderIsPublic = true;
                        profile.BirthDate = account.BirthDate;
                        profile.BirthDateIsPublic = true;
                        profile.AvatarPath = "";
                        profile.AvatarWidth = 0;
                        profile.AvatarHeight = 0;
                        profile.AvatarIsPublic = true;
                        profile.Interests = "";
                        profile.InterestsIsPublic = true;
                        profile.AboutMe = "";
                        profile.AboutMeIsPublic = true;
                        profile.LastUpdateDate = DateTime.Now;
                        db2.Profiles.Add(profile);
                        db2.SaveChanges();
                        return profile;
                    }
                    else
                    {
                        Profile profile = result2.FirstOrDefault();
                        return profile;
                    }
                }
            }
        }

        public string GetFullname(string username, bool addUsername)
        {
            string fullname = "";
            using (KSUCornerDBEntities1 db1 = new KSUCornerDBEntities1())
            {
                var result = from u in db1.Accounts where (u.UserName == username) select u;
                if (result.Count() > 0)
                {
                    Account account = result.FirstOrDefault();
                    fullname = (account.FirstName + " " + account.LastName).Trim();
                }
            }

            if (String.IsNullOrEmpty(fullname))
                fullname = username;
            else if (addUsername)
                fullname += " (" + username + ")";

            return fullname;
        }

        public string GetFirstName(string username)
        {
            using (KSUCornerDBEntities1 db1 = new KSUCornerDBEntities1())
            {
                var result = from u in db1.Accounts where (u.UserName == username) select u;
                if (result.Count() > 0)
                    return result.FirstOrDefault().FirstName;
            }
            return "";
        }

        public string GetLastName(string username)
        {
            using (KSUCornerDBEntities1 db1 = new KSUCornerDBEntities1())
            {
                var result = from u in db1.Accounts where (u.UserName == username) select u;
                if (result.Count() > 0)
                    return result.FirstOrDefault().LastName;
            }
            return "";
        }

        public string GetAvatarLink(string username, Boolean modifySize)
        {
            return GetAvatarLink(username, 0, 0, modifySize, "");
        }

        public string GetAvatarLink(string username, int width, int height)
        {
            return GetAvatarLink(username, width, height, false, "");
        }

        public string GetAvatarLink(string username, int width, int height, Boolean modifySize)
        {
            return GetAvatarLink(username, width, height, modifySize, "");
        }

        public string GetAvatarLink(string username, int width, int height, Boolean modifySize, string style)
        {
            string imagepath = "<img src=\"/Images/DefaultAvatar.jpg\" alt=\"Profile Avatar\"";
            if (!modifySize)
            {
                if (width > 0)
                    imagepath += " width=\"" + width + "\"";
                if (height > 0)
                    imagepath += " height=\"" + height + "\"";
            }
            if (!String.IsNullOrEmpty(style))
                imagepath += " style=\"" + style + "\"";
            imagepath += " />";
            using (KSUCornerDBEntities2 db2 = new KSUCornerDBEntities2())
            {
                var result = from p in db2.Profiles where (p.UserName == username) select p;
                if (result.Count() == 0)
                    return imagepath;

                Profile profile = result.FirstOrDefault();
                if (!profile.AvatarIsPublic || String.IsNullOrEmpty(profile.AvatarPath))
                    return imagepath;

                imagepath = "<img src=\"" + profile.AvatarPath + "\" alt=\"Profile Avatar\"";
                if (modifySize)
                {
                    if (profile.AvatarWidth < 60 || profile.AvatarWidth > 230)
                        imagepath += " width=\"200\"";
                    if ((profile.AvatarHeight > 2 * profile.AvatarWidth && profile.AvatarWidth > 230) ||
                                      (profile.AvatarHeight > 400 && profile.AvatarWidth <= 230))
                        imagepath += " height=\"400\"";
                }
                else
                {
                    if (width > 0)
                        imagepath += " width=\"" + width + "\"";
                    if (height > 0)
                        imagepath += " height=\"" + height + "\"";
                }
                if (!String.IsNullOrEmpty(style))
                    imagepath += " style=\"" + style + "\"";
                imagepath += " />";
            }
            return imagepath;
        }

        // Converts a string to something that won't break a URL
        public string URLFriendly2(string sentence)
        {
            if (sentence == null) return "";
            string result = "";
            for (int i = 0; i < sentence.Length; i++)
            {
                char c = sentence[i];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') ||
                              c == '.' || c == '-' || c == '_' || c == '+' || c == '@')
                    result += c;
                else if (c == ' ' || c == ',' || c == '=')
                    result += '-';
            }
            return result;
        }


        // Begins user's security question protocol
        public ActionResult PasswordRecovery()
        {
            ViewBag.Message = "Password Recovery";
            ViewBag.Message2 = "<b>If you previously filled out a Security Question and Answer for ";
            ViewBag.Message2 += "your account, then enter your email address below and submit it. ";
            ViewBag.Message2 += "After that you will be given a chance to answer the Security ";
            ViewBag.Message2 += "Question, and if you do so correctly, your password will be ";
            ViewBag.Message2 += "sent to your email address.</b>";
            ViewBag.Label1 = "Your Email Address:";
            return View();
        }

        // Continues user's security question protocol
        [HttpPost]
        public ActionResult PasswordRecovery(FormCollection collection)
        {
            string email = collection["Answer"];
            using (KSUCornerDBEntities1 db = new KSUCornerDBEntities1())
            {
                try
                {
                    // Testing to see if the email is in the database
                    var user = db.Accounts.First(x => x.Email == email && x.SecurityQuestion != "");
                    if (user.SecurityQuestion != null && user.SecurityQuestion.Length > 0)
                    {
                        Session["SecurityQuestion"] = user.SecurityQuestion;
                        Session["SecurityAnswer"] = user.SecurityAnswer;
                        Session["SessionEmail"] = email;
                        Session["SessionPassword"] = user.Password;
                        Session["SessionFirstname"] = user.FirstName;
                        Session["SessionLastname"] = user.LastName;
                        return RedirectToAction("RecoveryQuestion");
                    }
                }
                catch { }
                Session["Reason"] = "Not Found";
                return RedirectToAction("PasswordRecoveryFailed");
            }
        }

        // Prompts user to answer security question
        public ActionResult RecoveryQuestion()
        {
            if (Session["SecurityQuestion"] == null)
            {
                Session["Reason"] = "Not Found";
                return RedirectToAction("PasswordRecoveryFailed");
            }
            ViewBag.Message = "Security Question";
            ViewBag.Message2 = "The following is your Security Question.  If you answer it ";
            ViewBag.Message2 += "correctly, your password will be emailed to you:";
            ViewBag.Message3 = "<b>" + Session["SecurityQuestion"] + "</b>";
            ViewBag.Label1 = "Your Answer:";
            return View();
        }

        // Processes user's answer to security question
        [HttpPost]
        public ActionResult RecoveryQuestion(FormCollection collection)
        {
            string answer = collection["Answer"];
            if (Session["SecurityAnswer"] == null || Session["SessionEmail"] == null ||
                              Session["SessionPassword"] == null)
            {
                Session["Reason"] = "Not Found";
                return RedirectToAction("PasswordRecoveryFailed");
            }
            if (Session["SecurityAnswer"].ToString().Trim().ToLower() != answer.Trim().ToLower())
            {
                Session["Reason"] = "Incorrect Answer";
                return RedirectToAction("PasswordRecoveryFailed");
            }

            string email = Session["SessionEmail"].ToString();
            string fullname = (Session["SessionFirstname"] == null) ? "" :
                                Session["SessionFirstname"].ToString();
            fullname += " " + ((Session["SessionLastname"] == null) ? "" :
                                Session["SessionLastname"].ToString());
            fullname = ("Hello " + fullname).Trim();
            string password = Session["SessionPassword"].ToString();

            try
            {
                // Preparing authentication message to send to user
                MailMessage message = new MailMessage();
                message.From = new MailAddress("DoNotReply@KSUCorner.edu");
                message.To.Add(new MailAddress(email));
                message.Subject = "KSU Corner Password Recovery";
                message.Body = fullname + "," + Environment.NewLine + Environment.NewLine;
                message.Body += "Your KSU Corner Password is: " + password;
                message.Body += Environment.NewLine + Environment.NewLine;
                message.Body += "-The Staff at KSU Corner";

                // Sending the message to the SMTP server
                SmtpClient client = new SmtpClient();
                client.Host = "208.73.222.114";
                client.Port = 7301;
                client.Send(message);
            }
            catch (Exception err)
            {
                // The email message failed to send for some reason
                Session["Reason"] = err.Message;
                return RedirectToAction("PasswordRecoveryFailed");
            }
            return RedirectToAction("PasswordRecoverySucceeded");
        }

        // Announces Failed Password Recovery
        public ActionResult PasswordRecoveryFailed()
        {
            if (Session["Reason"] == null || Session["Reason"].ToString() == "Not Found")
            {
                ViewBag.Message = "Sorry, but there is no Security Question available for the<br />";
                ViewBag.Message += "Email address you gave.";
                ViewBag.Message2 = "<img src=\"/Images/invalidemail.jpg\" />";
            }
            else if (Session["Reason"].ToString() == "Incorrect Answer")
            {
                ViewBag.Message = "Sorry, but that was not the correct answer.";
                ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
            }
            else
            {
                ViewBag.Message = "There was an error: " + Session["Reason"];
                ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
            }
            return View();
        }

        // Announces Successful Password Recovery
        public ActionResult PasswordRecoverySucceeded()
        {
            ViewBag.Message = "Your answer was correct!<p>";
            ViewBag.Message += "Your password has been sent to your Email address.";
            ViewBag.Message2 = "<img src=\"/Images/validemail.jpg\" />";
            return View();
        }

        // Terms of Use Agreement
        public ActionResult TermsOfUse()
        {
            ViewBag.Message = "KSU Corner";
            ViewBag.Label1 = "Terms of Use Agreement";
            return View();
        }


        [Authorize]
        public ActionResult Search(string id, string button, string searchString)
        {
            if (id == "Friendship")
            {
                Session["SearchLabel"] = "Potential Friends";
            }
            else if (button == "Search For Other Profiles")
            {
                Session["SearchLabel"] = "Profile";
            }
            if (Session["SearchLabel"] != null && Session["SearchLabel"].ToString() == "Message")
            {
                ViewBag.Message = "Search for Message Recipient:";
                ViewBag.SearchType = "Message";
            }
            else if (Session["SearchLabel"] != null)
            {
                ViewBag.Message = Session["SearchLabel"] + " Search:";
                ViewBag.SearchType = (Session["SearchLabel"].ToString() == "Potential Friends") ?
                                                               "Friendship" : Session["SearchLabel"];
            }
            else
            {
                ViewBag.Message = "Search:";
                ViewBag.SearchType = "None";
            }
            ViewBag.Label1 = "Search for";
            ViewBag.DefaultAvatar = "<img src=\"/Images/DefaultAvatar.jpg\" alt=\"Profile Avatar\" />";
            ViewBag.Content1 = "";
            using (KSUCornerDBEntities2 db2 = new KSUCornerDBEntities2())
            {
                if (!String.IsNullOrEmpty(searchString))
                {
                    var profiles = from p in db2.Profiles select p;
                    profiles = profiles.Where(p => p.FirstName.ToUpper().Contains(searchString.ToUpper()) ||
                                                   p.LastName.ToUpper().Contains(searchString.ToUpper()) ||
                                                   p.UserName.ToUpper().Contains(searchString.ToUpper()));
                    int count = profiles.Count();
                    if (count == 0)
                        ViewBag.Label2 = "No accounts found that matched your search.";
                    else
                    {
                        ViewBag.Label2 = "Found the following " + count + " account" + ((count > 1) ? "s" : "") + ":";
                        profiles = profiles.OrderBy(p => p.FirstName + " " + p.LastName);
                    }
                    ViewBag.Content1 = searchString;
                    return View(profiles.ToList());
                }
                else
                {
                    ViewBag.Label2 = "Enter all or part of the name of the account you wish to search for.";
                    return View((new Profile[0]).ToList());
                }
            }
        }

        [Authorize]
        public ActionResult Messaging(string sortOrder)
        {
            ViewBag.Message = "My Messages:";
            ViewBag.SubjectSortParm = (sortOrder == "Subject" ? "Subject desc" : "Subject");
            ViewBag.TypeSortParm = (sortOrder == "Type" ? "Type desc" : "Type");
            ViewBag.StatusSortParm = (sortOrder == "Status" ? "Status desc" : "Status");
            ViewBag.SentBySortParm = (sortOrder == "SentBy" ? "SentBy desc" : "SentBy");
            ViewBag.DateSortParm = String.IsNullOrEmpty(sortOrder) ? "Date desc" : "";
            using (KSUCornerDBEntities3 db3 = new KSUCornerDBEntities3())
            {
                string username = User.Identity.GetUserName();
                var messages = from x in db3.Messages select x;
                messages = messages.Where(x => x.SentTo == username);
                switch (sortOrder)
                {
                    case "Subject desc":
                        messages = messages.OrderByDescending(s => s.Subject);
                        break;
                    case "Subject":
                        messages = messages.OrderBy(s => s.Subject);
                        break;
                    case "Type desc":
                        messages = messages.OrderByDescending(s => s.MessageType);
                        break;
                    case "Type":
                        messages = messages.OrderBy(s => s.MessageType);
                        break;
                    case "Status desc":
                        messages = messages.OrderByDescending(s => s.MessageStatus);
                        break;
                    case "Status":
                        messages = messages.OrderBy(s => s.MessageStatus);
                        break;
                    case "SentBy desc":
                        messages = messages.OrderByDescending(s => s.SentBy);
                        break;
                    case "SentBy":
                        messages = messages.OrderBy(s => s.SentBy);
                        break;
                    case "Date desc":
                        messages = messages.OrderByDescending(s => s.CreateDate);
                        break;
                    default:
                        messages = messages.OrderBy(s => s.CreateDate);
                        break;
                }
                return View(messages.ToList());
            }
        }

        [HttpPost, Authorize]
        public ActionResult Messaging(string button, FormCollection form)
        {
            if (button == "Compose A Message")
            {
                Session["SearchLabel"] = "Message";
                return RedirectToAction("Search");
            }
            return View("Messaging");
        }

        [Authorize]
        public ActionResult NewMessage(string ID)
        {
            using (KSUCornerDBEntities1 db1 = new KSUCornerDBEntities1())
            {
                var result = from u in db1.Accounts where (u.UserName == ID) select u;
                if (result.Count() == 0)
                {
                    Session["NotOwnerAccount"] = true;
                    return RedirectToAction("BadAccount");
                }
            }

            SetMessageViewData(ID);
            Message message = new Message();
            message.SentBy = User.Identity.GetUserName();
            message.SentTo = ID;

            return View(message);
        }

        [HttpPost, Authorize]
        public ActionResult NewMessage(Message model)
        {
            SetMessageViewData(model.SentTo);
            using (KSUCornerDBEntities3 db3 = new KSUCornerDBEntities3())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Creating Message:";
                        return View("NewMessage", model);
                    }

                    model.MessageStatus = "Unread";
                    model.CreateDate = DateTime.Now;
                    model.OpenedDate = null;

                    db3.Messages.Add(model);
                    db3.SaveChanges();

                    return RedirectToAction("Messaging");
                }
                catch
                {
                    ViewBag.Message = "Error Creating Message:";
                    return View("NewMessage", model);
                }
            }
        }

        [Authorize]
        public ActionResult ReplyMessage(string ID)
        {
            using (KSUCornerDBEntities1 db1 = new KSUCornerDBEntities1())
            {
                var result = from u in db1.Accounts where (u.UserName == ID) select u;
                if (result.Count() == 0)
                {
                    Session["NotOwnerAccount"] = true;
                    return RedirectToAction("BadAccount");
                }
            }

            SetMessageViewData(ID);
            Message message = new Message();
            message.SentBy = User.Identity.GetUserName();
            message.SentTo = ID;

            if (Session["ReplyMessage"] != null)
            {
                message.Body = Session["ReplyMessage"].ToString();
                Session["ReplyMessage"] = "";
            }
            if (Session["ReplySubject"] != null)
            {
                message.Subject = Session["ReplySubject"].ToString();
                Session["ReplySubject"] = "";
            }

            return View(message);
        }

        [HttpPost, Authorize]
        public ActionResult ReplyMessage(Message model)
        {
            SetMessageViewData(model.SentTo);
            using (KSUCornerDBEntities3 db3 = new KSUCornerDBEntities3())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Creating Message:";
                        return View("NewMessage", model);
                    }

                    model.MessageStatus = "Unread";
                    model.CreateDate = DateTime.Now;
                    model.OpenedDate = null;

                    db3.Messages.Add(model);
                    db3.SaveChanges();

                    return RedirectToAction("Messaging");
                }
                catch
                {
                    ViewBag.Message = "Error Creating Message:";
                    return View("NewMessage", model);
                }
            }
        }

        [Authorize]
        public ActionResult OpenMessage(int ID)
        {
            using (KSUCornerDBEntities3 db3 = new KSUCornerDBEntities3())
            {
                try
                {
                    var result = from x in db3.Messages where (x.MessageID == ID) select x;
                    if (result.Count() == 0)
                    {
                        return RedirectToAction("Messaging");
                    }
                    Message message = result.FirstOrDefault();
                    if (message.MessageStatus == "Unread")
                    {
                        message.MessageStatus = "Read";
                        message.OpenedDate = DateTime.Now;
                        db3.SaveChanges();
                    }

                    string sender = message.SentBy;
                    string fullname = GetFullname(sender, true);
                    ViewBag.SenderContent =
                        "<img src=\"/Images/DefaultAvatar.jpg\" alt=\"Profile Avatar\" width=\"32\" />";
                    using (KSUCornerDBEntities2 db2 = new KSUCornerDBEntities2())
                    {
                        var result2 = from p in db2.Profiles where (p.UserName == sender) select p;
                        if (result2.Count() == 0)
                        {
                            ViewBag.SenderContent = fullname;
                        }
                        else
                        {
                            Profile profile = result2.FirstOrDefault();
                            if (profile.AvatarIsPublic && !String.IsNullOrEmpty(profile.AvatarPath))
                            {
                                ViewBag.SenderContent = "<img src=\"" + profile.AvatarPath +
                                                                "\" alt=\"Profile Avatar\" width=\"32\" />";
                            }
                            ViewBag.SenderContent += " " + fullname;
                        }
                        ViewBag.Sender = sender;
                    }

                    ViewBag.Message = "Your Message";
                    ViewBag.Label1 = "Message Type";
                    ViewBag.Label2 = "Subject";
                    ViewBag.Label3 = "Date";
                    ViewBag.Label4 = "Sent by";

                    Session["ReplyMessage"] = Environment.NewLine + Environment.NewLine +
                                                "-----Original Message-----" + Environment.NewLine +
                                                "Sent on: " + String.Format("{0:g}", message.CreateDate) +
                                                Environment.NewLine + "Sent by: " + fullname +
                                                Environment.NewLine + "Subject: Re: " + message.Subject +
                                                Environment.NewLine + Environment.NewLine + message.Body;
                    if (message.Subject.Length > 2 && message.Subject.Substring(0, 3).ToUpper() == "RE:")
                        Session["ReplySubject"] = message.Subject;
                    else
                        Session["ReplySubject"] = "Re: " + message.Subject;

                    return View(message);
                }
                catch (Exception err)
                {
                    Session["MessageError"] = err.Message;
                    return RedirectToAction("MessageError");
                }
            }
        }

        [Authorize]
        public ActionResult MessageError()
        {
            ViewBag.Message = "Error Opening Message: " + Session["MessageError"];
            ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Messaging'\">" +
                                "Return To Messaging Page</button></form>";
            return View();
        }

        [Authorize]
        public ActionResult DeleteMessage(int ID)
        {
            using (KSUCornerDBEntities3 db3 = new KSUCornerDBEntities3())
            {
                ViewBag.Message = "Are you sure you want to delete the following message:";

                var result = from x in db3.Messages where (x.MessageID == ID) select x;
                if (result.Count() == 0)
                {
                    return RedirectToAction("Messaging");
                }
                Message message = result.FirstOrDefault();

                ViewBag.Message2 = "<h2>\"" + message.Subject + "\"<br />Sent by: " +
                    GetFullname(message.SentBy, true) + "</h2>";
                ViewBag.Message3 =
                    "<form action=\"/Home/DeleteMessage/" + message.MessageID + "\" method=\"post\">";
                ViewBag.Message3 += Environment.NewLine;
                ViewBag.Message3 +=
                    "<input type=\"image\" src=\"/Images/delete.jpg\" value=\"Submit\" alt=\"Submit\">";
                ViewBag.Message3 += Environment.NewLine + "<p></p>" + Environment.NewLine;
                ViewBag.Message3 +=
                    "<input type=\"submit\" value=\"Delete This Message\" name=\"button\" />";
                ViewBag.Message3 += "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";
                ViewBag.Message3 +=
                    "<button type=\"button\" onclick=\"window.location='/Home/Messaging'\">Cancel</button></form>";
                return View();
            }
        }

        [HttpPost, Authorize]
        public ActionResult DeleteMessage(int ID, FormCollection form)
        {
            using (KSUCornerDBEntities3 db3 = new KSUCornerDBEntities3())
            {
                try
                {
                    var result = from x in db3.Messages where (x.MessageID == ID) select x;
                    if (result.Count() > 0)
                    {
                        Message message = result.FirstOrDefault();

                        db3.Messages.Remove(message);
                        db3.SaveChanges();
                    }
                }
                catch (Exception err)
                {
                    Session["DeleteError"] = err.Message;
                    return RedirectToAction("DeleteError");
                }
            }
            return RedirectToAction("Messaging");
        }

        [HttpPost, Authorize]
        public ActionResult MultiDeleteMessage(string button, FormCollection form)
        {
            if (button == "Compose A Message")
            {
                Session["SearchLabel"] = "Message";
                return RedirectToAction("Search");
            }
            using (KSUCornerDBEntities3 db3 = new KSUCornerDBEntities3())
            {
                for (int i = 0; i < form.Count; ++i)
                {
                    string keyName = form.Keys[i];
                    if (keyName.Length > 7 && keyName.Substring(0, 7) == "Delete-")
                    {
                        int idVal = Int32.Parse(keyName.Substring(7));
                        string[] vals = form[i].ToString().Split(',');
                        if (bool.Parse(vals[0]))
                        {
                            try
                            {
                                var result = from x in db3.Messages where (x.MessageID == idVal) select x;
                                if (result.Count() > 0)
                                {
                                    Message message = result.FirstOrDefault();
                                    db3.Messages.Remove(message);
                                    db3.SaveChanges();
                                }
                            }
                            catch (Exception err)
                            {
                                Session["DeleteError"] = err.Message;
                                return RedirectToAction("DeleteError");
                            }
                        }
                    }
                }
            }
            return RedirectToAction("Messaging");
        }

        [Authorize]
        public ActionResult DeleteError()
        {
            ViewBag.Message = "Error Deleting Message: " + Session["DeleteError"];
            ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Messaging'\">" +
                                "Return To Messaging Page</button></form>";
            return View();
        }

        [Authorize]
        public ActionResult Friends(string searchString)
        {
            ViewBag.Message = "My Friends:";
            ViewBag.Label1 = "<b>Friendships</b>";
            ViewBag.DefaultAvatar = "<img src=\"/Images/DefaultAvatar.jpg\" alt=\"Profile Avatar\" />";
            string username = User.Identity.GetUserName();
            List<Profile> profiles = new List<Profile>();
            using (KSUCornerDBEntities2 db2 = new KSUCornerDBEntities2())
            {
                using (KSUCornerDBEntities4 db4 = new KSUCornerDBEntities4())
                {
                    var friends = db4.Friends.Where(f => f.FriendUserName1 == username ||
                                                        f.FriendUserName2 == username);
                    string otheruser;
                    foreach (var f in friends)
                    {
                        otheruser = (f.FriendUserName1 == username) ? f.FriendUserName2 :
                                                                      f.FriendUserName1;
                        var result = from p in db2.Profiles where (p.UserName == otheruser) select p;
                        if (result.Count() > 0)
                        {
                            Profile otherprofile = result.FirstOrDefault();
                            profiles.Add(otherprofile);
                        }
                    }
                }
            }
            return View(profiles.OrderBy(p => p.FirstName + " " + p.LastName).ToList());
        }

        [Authorize]
        public ActionResult InviteFriend(string ID)
        {
            string username = User.Identity.GetUserName();
            if (ID.ToUpper() == username.ToUpper())
            {
                return RedirectToAction("NoToSelf");
            }
            using (KSUCornerDBEntities1 db1 = new KSUCornerDBEntities1())
            {
                var result = from u in db1.Accounts where (u.UserName == ID) select u;
                if (result.Count() == 0)
                {
                    Session["NotOwnerAccount"] = true;
                    return RedirectToAction("BadAccount");
                }
            }
            using (KSUCornerDBEntities4 db4 = new KSUCornerDBEntities4())
            {
                var result2 = from f in db4.Friends
                              where ((f.FriendUserName1 == ID &&
                                    f.FriendUserName2 == username) ||
                                    (f.FriendUserName2 == ID &&
                                    f.FriendUserName1 == username))
                              select f;
                if (result2.Count() > 0)
                {
                    return RedirectToAction("AlreadyFriends");
                }
            }

            string subject = "";
            using (KSUCornerDBEntities2 db2 = new KSUCornerDBEntities2())
            {
                var result3 = from p in db2.Profiles where (p.UserName == username) select p;
                if (result3.Count() == 0)
                {
                    subject = username;
                }
                else
                {
                    Profile profile = result3.FirstOrDefault();
                    subject = (profile.FirstName + " " + profile.LastName).Trim();
                    if (String.IsNullOrEmpty(subject))
                        subject = username;
                }
            }
            subject += (subject.ToLower()[subject.Length - 1] != 's') ? "'s" : "'";
            subject += " (" + username + ")";
            subject += " Friendship Invitation";

            SetInviteViewData(ID);
            Message message = new Message();
            message.SentBy = username;
            message.SentTo = ID;
            message.Subject = subject;
            message.MessageType = "Friendship Invitation";

            return View(message);
        }

        [HttpPost, Authorize]
        public ActionResult InviteFriend(Message model)
        {
            SetInviteViewData(model.SentTo);
            using (KSUCornerDBEntities3 db3 = new KSUCornerDBEntities3())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Sending Invitation:";
                        return View("InviteFriend", model);
                    }

                    model.Body = "@@@" + model.Body;
                    model.MessageStatus = "Unread";
                    model.CreateDate = DateTime.Now;
                    model.OpenedDate = null;

                    db3.Messages.Add(model);
                    db3.SaveChanges();

                    return RedirectToAction("InvitationSent");
                }
                catch
                {
                    ViewBag.Message = "Error Sending Invitation:";
                    return View("InviteFriend", model);
                }
            }
        }

        [Authorize]
        public ActionResult RemoveFriend(string ID)
        {
            string username = User.Identity.GetUserName();
            if (ID.ToUpper() == username.ToUpper())
            {
                return RedirectToAction("NoToSelf");
            }
            using (KSUCornerDBEntities1 db1 = new KSUCornerDBEntities1())
            {
                var result = from u in db1.Accounts where (u.UserName == ID) select u;
                if (result.Count() == 0)
                {
                    Session["NotOwnerAccount"] = true;
                    return RedirectToAction("BadAccount");
                }
            }
            using (KSUCornerDBEntities4 db4 = new KSUCornerDBEntities4())
            {
                var result2 = from f in db4.Friends
                              where ((f.FriendUserName1 == ID &&
                                    f.FriendUserName2 == username) ||
                                    (f.FriendUserName2 == ID &&
                                    f.FriendUserName1 == username))
                              select f;
                if (result2.Count() == 0)
                {
                    ViewBag.Message = "That person is not one of your friends.";
                    ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
                    ViewBag.Message3 = "<button type=\"button\"" +
                                 "onclick=\"window.location='/Home/Friends'\">" +
                                        "Go To Friends Page</button></form>";
                    return View();
                }
            }
            using (KSUCornerDBEntities3 db3 = new KSUCornerDBEntities3())
            {
                ViewBag.Message = "Are you sure you want to remove the following friend:";

                ViewBag.Message2 = "<h2>" + GetAvatarLink(ID, 0, 0, true, "vertical-align:middle;") +
                                                " " + GetFullname(ID, true) + "</h2>";
                ViewBag.Message3 =
                    "<form action=\"/Home/RemoveFriend/" + ID + "\" method=\"post\">";
                ViewBag.Message3 += Environment.NewLine +
                    "<input type=\"submit\" value=\"Remove This Friend\" name=\"button\" />";
                ViewBag.Message3 += "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";
                ViewBag.Message3 +=
                    "<button type=\"button\" onclick=\"window.location='/Home/Friends'\">Cancel</button></form>";
                return View();
            }
        }

        [HttpPost, Authorize]
        public ActionResult RemoveFriend(string ID, FormCollection form)
        {
            string username = User.Identity.GetUserName();
            using (KSUCornerDBEntities4 db4 = new KSUCornerDBEntities4())
            {
                try
                {
                    var result = from f in db4.Friends
                                 where ((f.FriendUserName1 == ID &&
                                       f.FriendUserName2 == username) ||
                                       (f.FriendUserName2 == ID &&
                                       f.FriendUserName1 == username))
                                 select f;
                    if (result.Count() > 0)
                    {
                        Friend friend = result.FirstOrDefault();
                        db4.Friends.Remove(friend);
                        db4.SaveChanges();
                    }
                }
                catch (Exception err)
                {
                    Session["RmvFriendError"] = err.Message;
                    return RedirectToAction("RmvFriendError");
                }
            }
            return RedirectToAction("Friends");
        }

        [Authorize]
        public ActionResult RmvFriendError()
        {
            ViewBag.Message = "Error Removing Friend: " + Session["RmvFriendError"];
            ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Friends'\">" +
                                "Return To Friends Page</button></form>";
            return View();
        }

        [Authorize]
        public ActionResult InvitationSent()
        {
            ViewBag.Message = "Your Friendship Invitation Was Sent!";
            ViewBag.Message2 = "<img src=\"/Images/validemail.jpg\" />";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Friends'\">" +
                                "Return To Friends Page</button></form>";
            return View();
        }

        [Authorize]
        public ActionResult AcceptInvitation(FormCollection form)
        {
            string answer = (!String.IsNullOrEmpty(form["button"]) &&
                form["button"].ToString() == "Accept Invitation") ? "Accepted" : "Declined";
            string sender = (!String.IsNullOrEmpty(form["requester"])) ?
                form["requester"].ToString() : "";
            int messageID = (!String.IsNullOrEmpty(form["ID"])) ?
                Int32.Parse(form["ID"].ToString()) : -1;
            string username = User.Identity.GetUserName();

            try
            {
                if (sender.Trim().ToUpper() == username.Trim().ToUpper())
                {
                    ViewBag.Message = "You cannot become a friend to yourself!";
                    ViewBag.Message3 = "<button type=\"button\"" +
                                 "onclick=\"window.location='/Home/Friends'\">" +
                                        "Return To Friends Page</button></form>";
                    return View();
                }

                using (KSUCornerDBEntities3 db3 = new KSUCornerDBEntities3())
                {
                    var result = from m in db3.Messages where (m.MessageID == messageID) select m;
                    if (result.Count() == 0)
                    {
                        ViewBag.Message = "That Friendship Invitation Is No Longer Active.";
                        ViewBag.Message3 = "<button type=\"button\"" +
                                     "onclick=\"window.location='/Home/Friends'\">" +
                                            "Return To Friends Page</button></form>";
                        return View();
                    }
                    else
                    {
                        Message invitation = result.FirstOrDefault();
                        db3.Messages.Remove(invitation);
                        db3.SaveChanges();
                    }
                }

                using (KSUCornerDBEntities4 db4 = new KSUCornerDBEntities4())
                {
                    var result2 = from f in db4.Friends
                                  where ((f.FriendUserName1 == sender &&
                                        f.FriendUserName2 == username) ||
                                        (f.FriendUserName2 == sender &&
                                        f.FriendUserName1 == username))
                                  select f;
                    if (result2.Count() > 0)
                        return RedirectToAction("AlreadyFriends");
                }

                using (KSUCornerDBEntities2 db2 = new KSUCornerDBEntities2())
                {
                    var result3 = from p in db2.Profiles where (p.UserName == username) select p;
                    if (result3.Count() == 0)
                    {
                        ViewBag.Message = "The Sender of that Friendship Invitation no longer has a valid account.";
                        ViewBag.Message3 = "<button type=\"button\"" +
                                     "onclick=\"window.location='/Home/Friends'\">" +
                                            "Return To Friends Page</button></form>";
                        return View();
                    }
                }

                using (KSUCornerDBEntities3 db3 = new KSUCornerDBEntities3())
                {
                    Message message = new Message();
                    message.SentBy = "[NoReply]";
                    message.SentTo = sender;
                    message.Subject = "Your Friendship Invitation Was " + answer;
                    message.Body = "This is to inform you that " + GetFullname(username, true) + " " +
                                        answer.ToLower() + " your Friendship Invitation.";
                    message.MessageType = "FYI";
                    message.MessageStatus = "Unread";
                    message.CreateDate = DateTime.Now;
                    message.OpenedDate = null;

                    db3.Messages.Add(message);
                    db3.SaveChanges();
                }

                if (answer == "Accepted")
                {
                    using (KSUCornerDBEntities4 db4 = new KSUCornerDBEntities4())
                    {
                        Friend friend = new Friend();
                        friend.FriendUserName1 = sender;
                        friend.FriendUserName2 = username;
                        friend.CreateDate = DateTime.Now;

                        db4.Friends.Add(friend);
                        db4.SaveChanges();
                        return RedirectToAction("Friends");
                    }
                }
            }
            catch (Exception err)
            {
                ViewBag.Message = "Error With Invitation: " + err.Message;
                ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
                ViewBag.Message3 = "<button type=\"button\"" +
                             "onclick=\"window.location='/Home/Messaging'\">" +
                                    "Return To My Messages</button></form>";
                return View();
            }

            ViewBag.Message = "The Friendship Invitation you have declined has been removed.";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Messaging'\">" +
                                "Return To My Messages</button></form>";
            return View();
        }

        [Authorize]
        public ActionResult NoToSelf()
        {
            ViewBag.Message = "Sorry, but you cannot become a Friend of yourself.";
            ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Friends'\">" +
                                "Go To Friends Page</button></form>";
            return View();
        }

        [Authorize]
        public ActionResult AlreadyFriends()
        {
            ViewBag.Message = "You are already Friends with that person.";
            ViewBag.Message2 = "<img src=\"/Images/friends.jpg\" />";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Friends'\">" +
                                "Go To Friends Page</button></form>";
            return View();
        }

        public void SetMessageViewData(string username)
        {
            ViewBag.Message = "New Message:";
            ViewBag.Content1 = GetFullname(username, true);
            ViewBag.Label1 = "Send Message";
            ViewBag.Label2 = "To";
            ViewBag.Label3 = "Subject";
            ViewBag.Label4 = "Type";
            ViewBag.Label5 = "Message";

            String[] types = { "Normal Message", "Urgent Message", "Kind Of Important", "Chatter / Tweet",
                              "Quick Question", "FYI", "Light Hearted" };
            SelectList theItems = new SelectList(types, "Normal Note");
            ViewBag.MessageType = theItems;
        }

        public void SetInviteViewData(string username)
        {
            ViewBag.Message = "Friendship Invitation:";
            ViewBag.Content1 = GetFullname(username, true);
            ViewBag.Label1 = "Send Invitation";
            ViewBag.Label2 = "To";
            ViewBag.Label3 = "Subject";
            ViewBag.Label4 = "Type";
            ViewBag.Label5 = "Optional Note";
        }


        [Authorize]
        public ActionResult NewGallery()
        {
            SetGalleryViewData(true);
            return View(new FileFolder());
        }

        [HttpPost, Authorize]
        public ActionResult NewGallery(FileFolder model)
        {
            SetGalleryViewData(true);
            using (KSUCornerDBEntities5 db5 = new KSUCornerDBEntities5())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Creating Gallery:";
                        return View("NewGallery", model);
                    }

                    model.AccountID = GetAccountID(User.Identity.GetUserName());
                    model.CreateDate = DateTime.Now;
                    model.LastUpdateDate = model.CreateDate;

                    db5.FileFolders.Add(model);
                    db5.SaveChanges();

                    return RedirectToAction("MediaGalleries");
                }
                catch
                {
                    ViewBag.Message = "Error Creating Gallery:";
                    return View("NewGallery", model);
                }
            }
        }

        [Authorize]
        public ActionResult EditGallery(int ID)
        {
            SetGalleryViewData(false);
            using (KSUCornerDBEntities5 db5 = new KSUCornerDBEntities5())
            {
                var result = from g in db5.FileFolders where (g.FileFolderID == ID) select g;
                if (result.Count() > 0)
                    return View(result.FirstOrDefault());
                else
                    return RedirectToAction("GalleryLoadError");
            }
        }

        [HttpPost, Authorize]
        public ActionResult EditGallery(FileFolder model)
        {
            SetGalleryViewData(false);
            using (KSUCornerDBEntities5 db5 = new KSUCornerDBEntities5())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Editing Gallery:";
                        return View("EditGallery", model);
                    }

                    FileFolder folder = db5.FileFolders.First(f => f.FileFolderID == model.FileFolderID);
                    folder.Name = model.Name;
                    folder.Description = model.Description;
                    folder.LastUpdateDate = DateTime.Now;
                    db5.SaveChanges();

                    return RedirectToAction("OpenGallery", new { id = model.FileFolderID });
                }
                catch
                {
                    ViewBag.Message = "Error Editing Gallery:";
                    return View("EditGallery", model);
                }
            }
        }

        private void SetGalleryViewData(bool isNew)
        {
            ViewBag.Message = (isNew) ? "Add New Gallery:" : "Edit Gallery";
            ViewBag.Label1 = "Gallery Details";
            ViewBag.Label2 = "Title";
            ViewBag.Label3 = "Brief Description";
            ViewBag.Label4 = "File Type";
            ViewBag.Status = (isNew) ? "New" : "Old";

            string[] types = { "Image Files", "Audio Files", "Video Files", "Mixed Files" };
            SelectList theItems = new SelectList(types, "Image Files");
            ViewBag.FolderType = theItems;
        }

        [Authorize]
        public ActionResult NewMediaFile(int ID)
        {
            MediaFile mediaFile = new MediaFile();
            if (ID < 0)
            {
                mediaFile.FileFolderID = -1;
                mediaFile.GroupID = -ID - 1;
            }
            else
            {
                mediaFile.FileFolderID = ID;
                mediaFile.GroupID = -1;
            }
            SetFileViewData(mediaFile, true);
            return View(mediaFile);
        }

        [HttpPost, Authorize]
        public ActionResult NewMediaFile(MediaFile model, HttpPostedFileBase FileUpload)
        {
            SetFileViewData(model, true);
            using (KSUCornerDBEntities6 db6 = new KSUCornerDBEntities6())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Loading File:";
                        return View("NewMediaFile", model);
                    }

                    if (GetMediaFileUpload(model, FileUpload, true) < 0)
                        return View("NewMediaFile", model);

                    int off = model.FileType.ToLower().IndexOf(" file");
                    if (off > -1)
                        model.FileType = model.FileType.Substring(0, off);
                    if (model.FileType == "Any")
                        model.FileType = "Mixed";

                    model.AccountID = GetAccountID(User.Identity.GetUserName());
                    model.CreateDate = DateTime.Now;
                    model.LastUpdateDate = model.CreateDate;

                    db6.MediaFiles.Add(model);
                    db6.SaveChanges();

                    if (model.FileFolderID > -1)
                        return RedirectToAction("OpenGallery", new { id = model.FileFolderID });
                    else
                        return RedirectToAction("GroupGallery", new { id = model.GroupID });
                }
                catch
                {
                    ViewBag.Message = "Error Loading File:";
                    return View("NewMediaFile", model);
                }
            }
        }

        [Authorize]
        public ActionResult EditMediaFile(int ID)
        {
            SetGalleryViewData(false);
            using (KSUCornerDBEntities6 db6 = new KSUCornerDBEntities6())
            {
                var result = from f in db6.MediaFiles where (f.FileID == ID) select f;
                if (result.Count() > 0)
                {
                    MediaFile file = result.FirstOrDefault();
                    SetFileViewData(file, false);
                    return View(file);
                }
                else
                    return RedirectToAction("MediaFileLoadError");
            }
        }

        [HttpPost, Authorize]
        public ActionResult EditMediaFile(MediaFile model, HttpPostedFileBase FileUpload)
        {
            SetFileViewData(model, false);
            using (KSUCornerDBEntities6 db6 = new KSUCornerDBEntities6())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Editing Gallery Item:";
                        return View("EditMediaFile", model);
                    }

                    MediaFile file = db6.MediaFiles.First(f => f.FileID == model.FileID);

                    if (GetMediaFileUpload(model, FileUpload, false) < 0)
                        return View("EditMediaFile", model);
                    else if (FileUpload != null && FileUpload.ContentLength > 0)
                    {
                        file.FilePath = model.FilePath;
                        file.Size = model.Size;
                        file.Width = model.Width;
                        file.Height = model.Height;
                    }

                    file.Name = model.Name;
                    file.Description = model.Description;
                    file.MoreInfo = model.MoreInfo;
                    file.FileType = model.FileType;
                    file.LastUpdateDate = DateTime.Now;
                    db6.SaveChanges();

                    if (model.FileFolderID == -1)
                        return RedirectToAction("GroupGallery", new { id = model.GroupID });
                    else
                        return RedirectToAction("OpenGallery", new { id = model.FileFolderID });
                }
                catch
                {
                    ViewBag.Message = "Error Editing Gallery Item:";
                    return View("EditMediaFile", model);
                }
            }
        }

        [Authorize]
        public ActionResult FileDetails(int ID)
        {
            bool isGroup = IsGroupMediaFile(ID);
            using (KSUCornerDBEntities6 db6 = new KSUCornerDBEntities6())
            {
                var result = from f in db6.MediaFiles where (f.FileID == ID) select f;
                if (result.Count() > 0)
                {
                    MediaFile file = result.FirstOrDefault();
                    string galleryName = "";
                    string galleryType = "";
                    string groupName = "";
                    if (!isGroup)
                    {
                        using (KSUCornerDBEntities5 db5 = new KSUCornerDBEntities5())
                        {
                            var result2 = from g in db5.FileFolders where (g.FileFolderID == file.FileFolderID) select g;
                            if (result2.Count() > 0)
                            {
                                galleryName = result2.FirstOrDefault().Name;
                                galleryType = result2.FirstOrDefault().FolderType;
                            }
                        }
                    }
                                       else
                                       {
                                           using (KSUCornerDBEntities7 db7 = new KSUCornerDBEntities7())
                                           {
                                                var result2 = from g in db7.Groups where (g.GroupID == file.GroupID) select g;
                                               if (result2.Count() > 0)
                                                {
                                                    groupName = result2.FirstOrDefault().Name;
                                               }
                                           }
                                       }

                    if (galleryType == "" || galleryType == "Mixed Files")
                        galleryType = "File";
                    else
                        galleryType = galleryType.Substring(0, galleryType.Length - 1);

                    if (groupName != "")
                    {
                        ViewBag.Message = "Group File Details:";
                        ViewBag.Label1 = "Group Name: " + groupName;
                        ViewBag.Label2 = "File Name: " + file.Name;
                        ViewBag.Label3 = "Description: " + file.Description;
                        if (String.IsNullOrWhiteSpace(file.MoreInfo))
                            ViewBag.Content1 = "[No further information available]";
                        else
                            ViewBag.Content1 = file.MoreInfo;
                    }
                    else
                    {
                        ViewBag.Message = galleryType + " Details:";
                        ViewBag.Label1 = "Gallery Name: " + galleryName;
                        ViewBag.Label2 = "File Name: " + file.Name;
                        ViewBag.Label3 = "Description: " + file.Description;
                        if (String.IsNullOrWhiteSpace(file.MoreInfo))
                            ViewBag.Content1 = "[No further information available]";
                        else
                            ViewBag.Content1 = file.MoreInfo;
                    }
                    return View();
                }
                else
                    return RedirectToAction("MediaFileLoadError");
            }
        }

        private int GetMediaFileUpload(MediaFile model, HttpPostedFileBase FileUpload, bool isNew)
        {
            string filePath = "";
            long size = -1;
            int width = -1, height = -1;
            if (FileUpload != null && FileUpload.ContentLength > 0)
            {
                string fileName = URLFriendly2("Gallery" + Guid.NewGuid().ToString() + "Item-" +
                                  User.Identity.GetUserName() + "@_@" + Path.GetFileName(FileUpload.FileName));
                filePath = Path.Combine(Server.MapPath("/Content/uploads/"), fileName);
                try
                {
                    FileUpload.SaveAs(filePath);
                    size = FileUpload.ContentLength;

                    if (model.FileType == "Image File" || model.FileType == "Image")
                    {
                        Bitmap img = new Bitmap(filePath);
                        width = img.Width;
                        height = img.Height;
                        img.Dispose();
                    }
                    filePath = "/Content/uploads/" + fileName;

                }
                catch (ArgumentException err)
                {
                    ViewBag.Message = "Error Loading File:";
                    if (model.FileType == "Image File")
                        ViewData.ModelState.AddModelError("", "Error: The file uploaded was not an image file.");
                    else
                        ViewData.ModelState.AddModelError("", "Error: " + err.Message);
                    return -1;
                }
                catch (Exception err)
                {
                    ViewBag.Message = "Error Loading File:";
                    ViewData.ModelState.AddModelError("", "Error: " + err.Message);
                    return -1;
                }
            }
            else if (FileUpload == null)
            {
                if (isNew)
                {
                    ViewBag.Message = "Error Loading File:";
                    ViewData.ModelState.AddModelError("", "Error: You must click the Browse button and locate a file to upload.");
                    return -1;
                }
                return 1;
            }
            else if (FileUpload.ContentLength == 0)
            {
                ViewBag.Message = "Error Loading File:";
                ViewData.ModelState.AddModelError("", "Error: The file was empty, unreadable or does not exists anymore.");
                return -1;
            }
            model.FilePath = filePath;
            model.Size = size;
            model.Width = width;
            model.Height = height;
            return 1;
        }

        private void SetFileViewData(MediaFile file, bool isNew)
        {
            String galleryName = "Gallery";
            String galleryType = "";

            if (file.FileFolderID > -1)
            {
                using (KSUCornerDBEntities5 db5 = new KSUCornerDBEntities5())
                {
                    var result = from g in db5.FileFolders where (g.FileFolderID == file.FileFolderID) select g;
                    if (result.Count() > 0)
                    {
                        galleryName = result.FirstOrDefault().Name;
                        galleryType = result.FirstOrDefault().FolderType;
                    }
                }
            }
                       else
                      {
                           using (KSUCornerDBEntities7 db7 = new KSUCornerDBEntities7())
                            {
                                var result = from g in db7.Groups where (g.GroupID == file.GroupID) select g;
                                if (result.Count() > 0)
                                {
                                    galleryName = result.FirstOrDefault().Name;
                                    galleryType = "Mixed Files";
                                }
                            }
                        }

            if (galleryType == "" || galleryType == "Mixed Files")
            {
                galleryType = "Any File Type";
                string[] types = { "Image", "Audio", "Video", "Other" };
                string deftype = (isNew || (file.FileType != "Image" && file.FileType != "Audio" &&
                                            file.FileType != "Video")) ? "Other" : file.FileType;
                SelectList theItems = new SelectList(types, deftype);
                ViewBag.FileType = theItems;
            }
            else
                galleryType = galleryType.Substring(0, galleryType.Length - 1);


            ViewBag.Message = ((isNew) ? "Add" : "Edit") + " Gallery File:";
            ViewBag.Label1 = ((galleryType == "Any File Type") ? "Unspecified file type " : galleryType) +
                                                                     " for \"" + galleryName + "\"";
            ViewBag.Label2 = "Title";
            ViewBag.Label3 = "Brief Description";
            ViewBag.Label4 = "File Type";
            ViewBag.Label5 = "File";
            ViewBag.Label6 = "Detailed Description";
            ViewBag.Status = (isNew) ? "New" : "Old";
            ViewBag.Form = (isNew) ? "NewMediaFile" : "EditMediaFile";
            ViewBag.GalleryType = galleryType;

            if (isNew && String.IsNullOrWhiteSpace(file.FileType))
            {
                int off = galleryType.ToLower().IndexOf(" file");
                if (off > -1)
                    file.FileType = galleryType.Substring(0, off);
                else
                    file.FileType = "Mixed";
            }

            if (file.FileType == "Other")
                file.FileType = "Mixed";

            if (!isNew && galleryType == "Any File Type" && file.FileType != "Mixed")
                ViewBag.Label1 = file.FileType + " file for \"" + galleryName + "\"";

            string filename = "";
            if (!String.IsNullOrWhiteSpace(file.FilePath))
            {
                string[] names = file.FilePath.Split('/');
                filename = names[names.Length - 1];
                int x = filename.IndexOf("@_@");
                if (x >= 0)
                    filename = filename.Substring(x + 3);
            }
            ViewBag.Content1 = (filename == "") ? "" :
                        "<br /><font size=\"2\" color=\"purple\">(Current file: " + filename + ")</font>";

        }

        [Authorize]
        public ActionResult MediaGalleries(string sortOrder)
        {
            SetMediaGalleriesViewData();
            ViewBag.NameSortParm = (sortOrder == "Name" ? "Name desc" : "Name");
            ViewBag.DescriptionSortParm = (sortOrder == "Description" ? "Description desc" : "Description");
            ViewBag.TypeSortParm = (sortOrder == "Type" ? "Type desc" : "Type");
            ViewBag.DateSortParm = String.IsNullOrEmpty(sortOrder) ? "Date desc" : "";

            int ID = GetAccountID(User.Identity.GetUserName());
            using (KSUCornerDBEntities5 db5 = new KSUCornerDBEntities5())
            {
                List<GalleryListItem> galleryList = new List<GalleryListItem>();
                var galleries = from g1 in db5.FileFolders where (g1.AccountID == ID) select g1;
                switch (sortOrder)
                {
                    case "Name desc":
                        galleries = galleries.OrderByDescending(s => s.Name);
                        break;
                    case "Name":
                        galleries = galleries.OrderBy(s => s.Name);
                        break;
                    case "Description desc":
                        galleries = galleries.OrderByDescending(s => s.Description);
                        break;
                    case "Description":
                        galleries = galleries.OrderBy(s => s.Description);
                        break;
                    case "Type desc":
                        galleries = galleries.OrderByDescending(s => s.FolderType);
                        break;
                    case "Type":
                        galleries = galleries.OrderBy(s => s.FolderType);
                        break;
                    case "Date desc":
                        galleries = galleries.OrderByDescending(s => s.CreateDate);
                        break;
                    default:
                        galleries = galleries.OrderBy(s => s.CreateDate);
                        break;
                }

                foreach (var g2 in galleries)
                {
                    GalleryListItem gItem = new GalleryListItem();
                    gItem.id = g2.FileFolderID;
                    gItem.isGallery = true;
                    gItem.title = g2.Name;
                    gItem.description = g2.Description;
                    gItem.type = g2.FolderType;
                    gItem.path = "";
                    gItem.width = 0;
                    gItem.height = 0;
                    gItem.dateString = String.Format("{0:g}", g2.CreateDate);
                    gItem.count = 0;
                    galleryList.Add(gItem);
                    using (KSUCornerDBEntities6 db6 = new KSUCornerDBEntities6())
                    {
                        var files = from f1 in db6.MediaFiles
                                    where (f1.FileFolderID == g2.FileFolderID)
                                    select f1;
                        gItem.count = files.Count();
                        foreach (var f2 in files)
                        {
                            gItem = new GalleryListItem();
                            gItem.id = f2.FileID;
                            gItem.isGallery = false;
                            gItem.title = f2.Name;
                            gItem.description = f2.Description;
                            gItem.type = f2.FileType;
                            gItem.path = f2.FilePath;
                            gItem.width = f2.Width;
                            gItem.height = f2.Height;
                            gItem.dateString = String.Format("{0:g}", f2.CreateDate);
                            gItem.count = -1;
                            galleryList.Add(gItem);
                        }
                    }
                }
                return View(galleryList.ToList());
            }
        }

        [HttpPost, Authorize]
        public ActionResult MultiDeleteGallery(string button, FormCollection form)
        {
            if (button == "Create New Gallery")
            {
                return RedirectToAction("NewGallery");
            }
            for (int i = 0; i < form.Count; ++i)
            {
                string keyName = form.Keys[i];
                if (keyName.Length > 7 && keyName.Substring(0, 7) == "Delete-")
                {
                    int idVal = Int32.Parse(keyName.Substring(7));
                    string[] vals = form[i].ToString().Split(',');
                    if (bool.Parse(vals[0]))
                    {
                        if (idVal >= 0)   // This is a Gallery being deleted
                        {
                            using (KSUCornerDBEntities5 db5 = new KSUCornerDBEntities5())
                            {
                                try
                                {
                                    var result = from x in db5.FileFolders
                                                 where (x.FileFolderID == idVal)
                                                 select x;
                                    if (result.Count() > 0)
                                    {
                                        FileFolder folder = result.FirstOrDefault();
                                        db5.FileFolders.Remove(folder);
                                        db5.SaveChanges();

                                        using (KSUCornerDBEntities6 db6 = new KSUCornerDBEntities6())
                                        {
                                            var files = from f1 in db6.MediaFiles
                                                        where (f1.FileFolderID == idVal)
                                                        select f1;
                                            if (files.Count() > 0)
                                            {
                                                string filePath = "";
                                                foreach (var f2 in files)
                                                {
                                                    filePath = Request.MapPath("~" + f2.FilePath);
                                                    if (System.IO.File.Exists(filePath))
                                                        System.IO.File.Delete(filePath);
                                                    db6.MediaFiles.Remove(f2);
                                                }
                                                db6.SaveChanges();
                                            }
                                        }
                                    }
                                }
                                catch (Exception err)
                                {
                                    Session["DeleteError"] = err.Message;
                                    Session["DeleteType"] = "Folder";
                                    return RedirectToAction("DeleteObjectError");
                                }
                            }
                        }
                        else   // This is a MediaFile being deleted
                        {
                            idVal = -idVal - 1;
                            using (KSUCornerDBEntities6 db6 = new KSUCornerDBEntities6())
                            {
                                try
                                {
                                    var result = from x in db6.MediaFiles where (x.FileID == idVal) select x;
                                    if (result.Count() > 0)
                                    {
                                        MediaFile file = result.FirstOrDefault();
                                        string filePath = Request.MapPath("~" + file.FilePath);
                                        if (System.IO.File.Exists(filePath))
                                            System.IO.File.Delete(filePath);
                                        db6.MediaFiles.Remove(file);
                                        db6.SaveChanges();
                                    }
                                }
                                catch (Exception err)
                                {
                                    Session["DeleteError"] = err.Message;
                                    Session["DeleteType"] = "File";
                                    return RedirectToAction("DeleteObjectError");
                                }
                            }
                        }
                    }
                }
            }
            return RedirectToAction("MediaGalleries");
        }

        private void SetMediaGalleriesViewData()
        {
            ViewBag.Message = "My Media Galleries:";
            ViewBag.Label1 = "Name";
            ViewBag.Label2 = "Description";
            ViewBag.Label3 = "Gallery Type";
            ViewBag.Label4 = "Created On";
            ViewBag.Label5 = "Remove";
        }

        [Authorize]
        public ActionResult OpenGallery(int ID)
        {
            SetMediaGalleriesViewData();
            ViewBag.Message = "View Gallery:";
            int userID = GetAccountID(User.Identity.GetUserName());
            using (KSUCornerDBEntities5 db5 = new KSUCornerDBEntities5())
            {
                List<GalleryListItem> galleryList = new List<GalleryListItem>();
                var result = from g in db5.FileFolders
                             where (g.FileFolderID == ID &&
                                    g.AccountID == userID)
                             select g;
                if (result.Count() < 1)
                {
                    return RedirectToAction("GalleryLoadError");
                }
                FileFolder folder = result.FirstOrDefault();
                GalleryListItem gItem = new GalleryListItem();
                gItem.id = folder.FileFolderID;
                gItem.isGallery = true;
                gItem.title = folder.Name;
                gItem.description = folder.Description;
                gItem.type = folder.FolderType;
                gItem.path = "";
                gItem.width = 0;
                gItem.height = 0;
                gItem.dateString = String.Format("{0:g}", folder.CreateDate);
                gItem.count = 0;
                galleryList.Add(gItem);

                using (KSUCornerDBEntities6 db6 = new KSUCornerDBEntities6())
                {
                    var files = from f1 in db6.MediaFiles
                                where (f1.FileFolderID == gItem.id)
                                select f1;
                    foreach (var f2 in files)
                    {
                        gItem = new GalleryListItem();
                        gItem.id = f2.FileID;
                        gItem.isGallery = false;
                        gItem.title = f2.Name;
                        gItem.description = f2.Description;
                        gItem.type = f2.FileType;
                        gItem.path = f2.FilePath;
                        gItem.width = f2.Width;
                        gItem.height = f2.Height;
                        gItem.dateString = String.Format("{0:g}", f2.CreateDate);
                        gItem.count = -1;
                        galleryList.Add(gItem);
                    }
                }
                return View(galleryList.ToList());
            }
        }

        [Authorize]
        public ActionResult DeleteMediaFile(int ID)
        {
            bool isGroup = IsGroupMediaFile(ID);
       
            using (KSUCornerDBEntities6 db6 = new KSUCornerDBEntities6())
            {
                ViewBag.Message = "Are you sure you want to delete the following item:";

                var result = from f in db6.MediaFiles where (f.FileID == ID) select f;
                if (result.Count() == 0)
                {
                    Session["DeleteError"] = "File Not Found";
                    Session["DeleteType"] = "Media File";
                    return RedirectToAction("DeleteObjectError");
                }

                MediaFile file = result.FirstOrDefault();

                ViewBag.Message2 = "<h2>\"" + file.Name + "\"";
                if (file.FileType != "Mixed")
                    ViewBag.Message2 += " - " + file.FileType + " File";
                ViewBag.Message2 += "</h2>";
                ViewBag.Message3 =
                        "<form action=\"/Home/DeleteMediaFile/" + file.FileID + "\" method=\"post\">";

                ViewBag.Message3 += Environment.NewLine;
                ViewBag.Message3 +=
                    "<input type=\"image\" src=\"/Images/delete.jpg\" value=\"Submit\" alt=\"Submit\">";
                ViewBag.Message3 += Environment.NewLine + "<p></p>" + Environment.NewLine;
                ViewBag.Message3 +=
                    "<input type=\"submit\" value=\"Delete This File\" name=\"button\" />";
                ViewBag.Message3 += "        ";
                if (isGroup)
                    ViewBag.Message3 +=
                        "<button type=\"button\" onclick=\"window.location='/Home/GroupGallery/" +
                                                     file.GroupID + "'\">Cancel</button></form>";
                else
                    ViewBag.Message3 +=
                        "<button type=\"button\" onclick=\"window.location='/Home/OpenGallery/" +
                                                     file.FileFolderID + "'\">Cancel</button></form>";
                return View();
            }
        }

        [HttpPost, Authorize]
        public ActionResult DeleteMediaFile(int ID, FormCollection form)
        {
            bool isGroup = IsGroupMediaFile(ID);

            using (KSUCornerDBEntities6 db6 = new KSUCornerDBEntities6())
            {
                try
                {
                    var result = from f in db6.MediaFiles where (f.FileID == ID) select f;
                    if (result.Count() == 0)
                    {
                        Session["DeleteError"] = "File Not Found";
                        Session["DeleteType"] = "Media File";
                        return RedirectToAction("DeleteObjectError");
                    }

                    MediaFile file = result.FirstOrDefault();
                    int parentID = (isGroup) ? file.GroupID : file.FileFolderID;
                    string filePath = Request.MapPath("~" + file.FilePath);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                    db6.MediaFiles.Remove(file);
                    db6.SaveChanges();
                    if (isGroup)
                        return RedirectToAction("GroupGallery", new { id = parentID });
                    else
                        return RedirectToAction("OpenGallery", new { id = parentID });
                }
                catch (Exception err)
                {
                    Session["DeleteError"] = err.Message;
                    Session["DeleteType"] = "Media File";
                    return RedirectToAction("DeleteObjectError");
                }
            }
        }

        [Authorize]
        public ActionResult DeleteObjectError()
        {
            ViewBag.Message = "Error Deleting " + Session["DeleteType"] + ": " + Session["DeleteError"];
            ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";

            string typeStr = Session["DeleteType"].ToString();
            if (typeStr.ToLower().IndexOf("group") > -1)
                ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Groups'\">" +
                                "Return To Groups Page</button></form>";
            else
                ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/MediaGalleries'\">" +
                                "Return To Media Galleries Page</button></form>";
            return View();
        }

        [Authorize]
        public ActionResult GalleryLoadError()
        {
            ViewBag.Message = "Error Loading Gallery - Gallery Not Found.";
            ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Index'\">" +
                                "Return To Home Page</button></form>";
            return View();
        }

        [Authorize]
        public ActionResult MediaFileLoadError()
        {
            ViewBag.Message = "Error Loading Gallery File - File Not Found.";
            ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Index'\">" +
                                "Return To Home Page</button></form>";
            return View();
        }

        private int GetAccountID(string username)
        {
            using (KSUCornerDBEntities1 db1 = new KSUCornerDBEntities1())
            {
                var result = from u in db1.Accounts where (u.UserName == username) select u;
                if (result.Count() > 0)
                {
                    Account account = result.FirstOrDefault();
                    return account.AccountID;
                }
            }
            return -1;
        }




        private string GetAccountUserName(int ID)
        {
            using (KSUCornerDBEntities1 db1 = new KSUCornerDBEntities1())
            {
                var result = from u in db1.Accounts where (u.AccountID == ID) select u;
                if (result.Count() > 0)
                {
                    Account account = result.FirstOrDefault();
                    return account.UserName;
                }
            }
            return null;
        }

        [Authorize]
        public ActionResult Groups(string sortOrder)
        {
            ViewBag.Message = "Groups:";
            ViewBag.Label1 = (String.IsNullOrEmpty(sortOrder) ? "A-Z" :
                                 (sortOrder == "Name desc" ? "Z-A" :
                                 (sortOrder == "Date desc" ? "Reverse Date" : sortOrder))) + " Sort:";
            ViewBag.Label2 = (String.IsNullOrEmpty(sortOrder) ? "Z-A" : "A-Z");
            ViewBag.Label3 = (sortOrder == "Date" ? "Reverse Date" : "By Date");
            ViewBag.Label4 = "New Group";

            ViewBag.NameSortParm = (String.IsNullOrEmpty(sortOrder) ? "Name desc" : "");
            ViewBag.DateSortParm = (sortOrder == "Date" ? "Date desc" : "Date");

            int ID = GetAccountID(User.Identity.GetUserName());
            using (KSUCornerDBEntities7 db7 = new KSUCornerDBEntities7())
            {
                var groupList = from g1 in db7.Groups select g1;
                switch (sortOrder)
                {
                    case "Date desc":
                        groupList = groupList.OrderByDescending(s => s.CreateDate);
                        break;
                    case "Date":
                        groupList = groupList.OrderBy(s => s.CreateDate);
                        break;
                    case "Name desc":
                        groupList = groupList.OrderByDescending(s => s.Name);
                        break;
                    default:
                        groupList = groupList.OrderBy(s => s.Name);
                        break;
                }

                foreach (var g2 in groupList)
                {
                    if (!g2.IsPublic)
                    {
                        using (KSUCornerDBEntities8 db8 = new KSUCornerDBEntities8())
                        {
                            var members = from m in db8.GroupMembers
                                          where (m.GroupID == g2.GroupID && m.AccountID == ID)
                                          select m;
                            g2.IsPublic = (members.Count() > 0);
                        }
                    }
                }
                return View(groupList.ToList());
            }
        }

        [Authorize]
        public ActionResult NewGroup()
        {
            Group group = new Group();
            SetGroupViewData(group, true);
            return View(group);
        }

        [HttpPost, Authorize]
        public ActionResult NewGroup(Group model, HttpPostedFileBase FileUpload)
        {
            SetGroupViewData(model, true);
            using (KSUCornerDBEntities7 db7 = new KSUCornerDBEntities7())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Creating Group:";
                        return View("NewGroup", model);
                    }

                    if (GetGroupLogoUpload(model, FileUpload, true) < 0)
                        return View("NewGroup", model);

                    model.AccountID = GetAccountID(User.Identity.GetUserName());
                    model.CreateDate = DateTime.Now;
                    model.LastUpdateDate = model.CreateDate;

                    db7.Groups.Add(model);
                    db7.SaveChanges();

                    AddGroupMember(model.GroupID, model.AccountID, true);
                    return RedirectToAction("Groups");
                }
                catch
                {
                    ViewBag.Message = "Error Creating Group:";
                    return View("NewGroup", model);
                }
            }
        }

        [Authorize]
        public ActionResult EditGroup(int ID)
        {
            if (GroupMemberStatus(ID, GetAccountID(User.Identity.GetUserName())) != "IsAdmin")
                return RedirectToAction("GroupLoadPermissionError");

            using (KSUCornerDBEntities7 db7 = new KSUCornerDBEntities7())
            {
                var result = from g in db7.Groups where (g.GroupID == ID) select g;
                if (result.Count() > 0)
                {
                    Group group = result.FirstOrDefault();
                    SetGroupViewData(group, false);
                    return View(group);
                }
                else
                    return RedirectToAction("GroupLoadError");
            }
        }

        [HttpPost, Authorize]
        public ActionResult EditGroup(Group model, HttpPostedFileBase FileUpload)
        {
            if (GroupMemberStatus(model.GroupID, GetAccountID(User.Identity.GetUserName())) != "IsAdmin")
                return RedirectToAction("GroupLoadPermissionError");

            SetGroupViewData(model, false);
            using (KSUCornerDBEntities7 db7 = new KSUCornerDBEntities7())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Editing Group Information:";
                        return View("EditGroup", model);
                    }

                    Group group = db7.Groups.First(g => g.GroupID == model.GroupID);

                    if (GetGroupLogoUpload(model, FileUpload, false) < 0)
                        return View("EditGroup", model);

                    else if (FileUpload != null && FileUpload.ContentLength > 0)
                    {
                        group.ImagePath = model.ImagePath;
                        group.Size = model.Size;
                        group.Width = model.Width;
                        group.Height = model.Height;
                    }

                    group.Name = model.Name;
                    group.Description = model.Description;
                    group.Mission = model.Mission;
                    group.ImageLinkType = model.ImageLinkType;
                    group.IsPublic = model.IsPublic;
                    group.LastUpdateDate = DateTime.Now;
                    db7.SaveChanges();

                    return RedirectToAction("Groups");
                }
                catch
                {
                    ViewBag.Message = "Error Editing Group Information:";
                    return View("EditGroup", model);
                }
            }
        }

        [Authorize]
        public ActionResult DeleteGroup(int ID)
        {
            if (GroupMemberStatus(ID, GetAccountID(User.Identity.GetUserName())) != "IsAdmin")
                return RedirectToAction("GroupLoadPermissionError");

            using (KSUCornerDBEntities7 db7 = new KSUCornerDBEntities7())
            {
                ViewBag.Message = "Are you sure you want to delete the following Group:";

                var result = from g in db7.Groups where (g.GroupID == ID) select g;
                if (result.Count() == 0)
                {
                    Session["DeleteError"] = "Group Not Found";
                    Session["DeleteType"] = "Group";
                    return RedirectToAction("DeleteObjectError");
                }

                Group group = result.FirstOrDefault();

                ViewBag.Message2 = "<h2>\"" + group.Name + "\" </h2>";
                ViewBag.Message3 =
                    "<form action=\"/Home/DeleteGroup/" + group.GroupID + "\" method=\"post\">";
                ViewBag.Message3 += Environment.NewLine;
                ViewBag.Message3 +=
                    "<input type=\"image\" src=\"/Images/delete.jpg\" value=\"Submit\" alt=\"Submit\">";
                ViewBag.Message3 += Environment.NewLine + "<p></p>" + Environment.NewLine;
                ViewBag.Message3 +=
                    "<input type=\"submit\" value=\"Delete This Group\" name=\"button\" />";
                ViewBag.Message3 += "        ";
                ViewBag.Message3 +=
                    "<button type=\"button\" onclick=\"window.location='/Home/Groups'\">Cancel</button></form>";
                return View();
            }
        }

        [HttpPost, Authorize]
        public ActionResult DeleteGroup(int ID, FormCollection form)
        {
            if (GroupMemberStatus(ID, GetAccountID(User.Identity.GetUserName())) != "IsAdmin")
                return RedirectToAction("GroupLoadPermissionError");

            using (KSUCornerDBEntities7 db7 = new KSUCornerDBEntities7())
            {
                try
                {
                    var result = from g in db7.Groups where (g.GroupID == ID) select g;
                    if (result.Count() == 0)
                    {
                        Session["DeleteError"] = "Group Not Found";
                        Session["DeleteType"] = "Group";
                        return RedirectToAction("DeleteObjectError");
                    }

                    Group group = result.FirstOrDefault();
                    string filePath = Request.MapPath("~" + group.ImagePath);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                    db7.Groups.Remove(group);
                    db7.SaveChanges();

                    using (KSUCornerDBEntities6 db6 = new KSUCornerDBEntities6())
                    {
                        var files = from f1 in db6.MediaFiles
                                    where (f1.GroupID == ID)
                                    select f1;
                        if (files.Count() > 0)
                        {
                            string filePath2 = "";
                            foreach (var f2 in files)
                            {
                                filePath2 = Request.MapPath("~" + f2.FilePath);
                                if (System.IO.File.Exists(filePath2))
                                    System.IO.File.Delete(filePath2);
                                db6.MediaFiles.Remove(f2);
                            }
                            db6.SaveChanges();
                        }
                    }
                    return RedirectToAction("Groups");
                }
                catch (Exception err)
                {
                    Session["DeleteError"] = err.Message;
                    Session["DeleteType"] = "Group";
                    return RedirectToAction("DeleteObjectError");
                }
            }
        }

        [Authorize]
        public ActionResult GroupGallery(int ID)
        {
            if (GroupMemberStatus(ID, GetAccountID(User.Identity.GetUserName())) == "")
                return RedirectToAction("GroupLoadError");

            using (KSUCornerDBEntities7 db7 = new KSUCornerDBEntities7())
            {
                var result = from g in db7.Groups where (g.GroupID == ID) select g;
                if (result.Count() == 0)
                    return RedirectToAction("GroupLoadError");

                SetMediaGalleriesViewData();
                Group group = result.FirstOrDefault();
                string groupName = group.Name;
                ViewBag.Message = "Group Gallery:";

                List<GalleryListItem> galleryList = new List<GalleryListItem>();

                GalleryListItem gItem = new GalleryListItem();
                gItem.id = ID;
                gItem.isGallery = false;
                gItem.title = groupName;
                gItem.description = group.Description;
                gItem.type = "Mixed Files";
                gItem.path = "";
                gItem.width = 0;
                gItem.height = 0;
                gItem.dateString = String.Format("{0:g}", group.CreateDate);
                gItem.count = 0;
                galleryList.Add(gItem);

                using (KSUCornerDBEntities6 db6 = new KSUCornerDBEntities6())
                {
                    var files = from f1 in db6.MediaFiles where (f1.GroupID == ID) select f1;
                    foreach (var f2 in files)
                    {
                        gItem = new GalleryListItem();
                        gItem.id = f2.FileID;
                        gItem.isGallery = false;
                        gItem.title = f2.Name;
                        gItem.description = f2.Description;
                        gItem.type = f2.FileType;
                        gItem.path = f2.FilePath;
                        gItem.width = f2.Width;
                        gItem.height = f2.Height;
                        gItem.dateString = String.Format("{0:g}", f2.CreateDate);
                        gItem.count = -1;
                        galleryList.Add(gItem);
                    }
                }
                return View(galleryList.ToList());
            }
        }

        [Authorize]
        public ActionResult GroupMission(int ID)
        {
            using (KSUCornerDBEntities7 db7 = new KSUCornerDBEntities7())
            {
                var result = from g in db7.Groups where (g.GroupID == ID) select g;
                if (result.Count() > 0)
                {
                    ViewBag.Message = "Group Mission Statement:";
                    ViewBag.Status = GroupMemberStatus(ID, GetAccountID(User.Identity.GetUserName()));
                    return View(result.FirstOrDefault());
                }
                else
                    return RedirectToAction("GroupLoadError");
            }
        }

        private void SetGroupViewData(Group group, bool isNew)
        {
            ViewBag.Message = ((isNew) ? "New" : "Edit") + " Group:";
            ViewBag.Label1 = "Name";
            ViewBag.Label2 = "Description";
            ViewBag.Label3 = "Logo Image";
            ViewBag.Label4 = "Access";
            ViewBag.Label5 = "Public Group";
            ViewBag.Label6 = "Logo Links To";
            ViewBag.Label7 = "Mission Statement";
            ViewBag.Status = (isNew) ? "New" : "Old";
            ViewBag.Form = (isNew) ? "NewGroup" : "EditGroup";

            SelectListItem choice01 = new SelectListItem() { Text = "Group Forum", Value = "Forum" };
            SelectListItem choice02 = new SelectListItem() { Text = "Group Gallery", Value = "Gallery" };
            SelectListItem choice03 = new SelectListItem() { Text = "Group Mission", Value = "Mission" };
            if (isNew || group.ImageLinkType == "Forum")
                choice01 = new SelectListItem() { Selected = true, Text = "Group Forum", Value = "Forum" };
            else if (group.ImageLinkType == "Gallery")
                choice02 = new SelectListItem() { Selected = true, Text = "Group Gallery", Value = "Gallery" };
            else
                choice03 = new SelectListItem() { Selected = true, Text = "Group Mission", Value = "Mission" };
            SelectListItem[] choices = { choice01, choice02, choice03 };
            ViewBag.ImageLinkType = choices;

            string filename = "";
            if (!String.IsNullOrWhiteSpace(group.ImagePath))
            {
                string[] names = group.ImagePath.Split('/');
                filename = names[names.Length - 1];
                int x = filename.IndexOf("@_@");
                if (x >= 0)
                    filename = filename.Substring(x + 3);
            }
            ViewBag.Content1 = (filename == "") ? "" :
                        "<br /><font size=\"2\" color=\"purple\">(Current file: " + filename + ")</font>";
        }

        private int GetGroupLogoUpload(Group model, HttpPostedFileBase FileUpload, bool isNew)
        {
            string filePath = "";
            long size = -1;
            int width = -1, height = -1;
            if (FileUpload != null && FileUpload.ContentLength > 0)
            {
                string fileName = URLFriendly2("Group" + Guid.NewGuid().ToString() + "Item-" +
                                  User.Identity.GetUserName() + "@_@" + Path.GetFileName(FileUpload.FileName));
                filePath = Path.Combine(Server.MapPath("/Content/uploads/"), fileName);
                try
                {
                    FileUpload.SaveAs(filePath);
                    size = FileUpload.ContentLength;
                    Bitmap img = new Bitmap(filePath);
                    width = img.Width;
                    height = img.Height;
                    img.Dispose();
                    filePath = "/Content/uploads/" + fileName;

                }
                catch (ArgumentException)
                {
                    ViewBag.Message = "Error Loading File:";
                    ViewData.ModelState.AddModelError("", "Error: The file uploaded was not an image file.");
                    return -1;
                }
                catch (Exception err)
                {
                    ViewBag.Message = "Error Loading File:";
                    ViewData.ModelState.AddModelError("", "Error: " + err.Message);
                    return -1;
                }
            }
            else if (FileUpload == null)
            {
                if (isNew)
                {
                    ViewBag.Message = "Error Loading File:";
                    ViewData.ModelState.AddModelError("", "Error: You must click the Browse button and locate an image file (Logo) to upload.");
                    return -1;
                }
                return 1;
            }
            else if (FileUpload.ContentLength == 0)
            {
                ViewBag.Message = "Error Loading File:";
                ViewData.ModelState.AddModelError("", "Error: The file was empty, unreadable or does not exists anymore.");
                return -1;
            }
            model.ImagePath = filePath;
            model.Size = size;
            model.Width = width;
            model.Height = height;
            return 1;
        }

        [Authorize]
        public ActionResult MembershipRequest(int ID)
        {
            string username = User.Identity.GetUserName();
            int accountID = GetAccountID(username);
            string status = GroupMemberStatus(ID, accountID);
            if (status == "IsPublic")
            {
                Session["ErrorMessage"] = "Error: There is No Membership in a Public Group";
                return RedirectToAction("GroupErrorMessage");
            }
            if (status != "")
            {
                Session["ErrorMessage"] = "Error: You Are Already A Member Of That Group";
                return RedirectToAction("GroupErrorMessage");
            }

            string groupName = "", groupAdmin = "";
            using (KSUCornerDBEntities7 db7 = new KSUCornerDBEntities7())
            {
                var groups = from g in db7.Groups where (g.GroupID == ID) select g;
                if (groups.Count() == 0)
                {
                    Session["ErrorMessage"] = "Error: Group Not Found";
                    return RedirectToAction("GroupErrorMessage");
                }
                else
                {
                    Group group = groups.FirstOrDefault();
                    groupName = group.Name;
                    groupAdmin = GetAccountUserName(group.AccountID);
                    if (String.IsNullOrWhiteSpace(groupAdmin))
                    {
                        Session["ErrorMessage"] = "Error: Group Administrator Not Found";
                        return RedirectToAction("GroupErrorMessage");
                    }
                }
            }

            string subject = GetFullname(username, true) + " requests Membership to the " + groupName;
            SetRequestViewData(username, groupName, ID);
            Message message = new Message();
            message.SentBy = username;
            message.SentTo = groupAdmin;
            message.Subject = subject;
            message.MessageType = "Membership Request";

            return View(message);
        }

        [HttpPost, Authorize]
        public ActionResult MembershipRequest(int ID, Message model)
        {
            SetInviteViewData(model.SentTo);
            using (KSUCornerDBEntities3 db3 = new KSUCornerDBEntities3())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Sending Membership Request:";
                        return View("MembershipRequest", model);
                    }

                    model.Body = "@@@" + ID + "@@@" + model.Body;
                    model.MessageStatus = "Unread";
                    model.CreateDate = DateTime.Now;
                    model.OpenedDate = null;

                    db3.Messages.Add(model);
                    db3.SaveChanges();

                    return RedirectToAction("MembershipRequestSent");
                }
                catch
                {
                    ViewBag.Message = "Error Sending Membership Request:";
                    return View("MembershipRequest", model);
                }
            }
        }

        [Authorize]
        public ActionResult AcceptMembershipRequest(int ID, FormCollection form)
        {
            string answer = (!String.IsNullOrEmpty(form["button"]) &&
                form["button"].ToString() == "Accept Request") ? "Accepted" : "Declined";
            string sender = (!String.IsNullOrEmpty(form["requester"])) ?
                form["requester"].ToString() : "";
            int messageID = (!String.IsNullOrEmpty(form["MessageID"])) ?
                Int32.Parse(form["MessageID"].ToString()) : -1;
            string username = User.Identity.GetUserName();
            int requesterID = GetAccountID(sender);

            try
            {
                using (KSUCornerDBEntities3 db3 = new KSUCornerDBEntities3())
                {
                    var result = from m in db3.Messages where (m.MessageID == messageID) select m;
                    if (result.Count() == 0)
                    {
                        ViewBag.Message = "That Membership Request Is No Longer Active.";
                        ViewBag.Message3 = "<button type=\"button\"" +
                                     "onclick=\"window.location='/Home/Groups'\">" +
                                            "Return To Groups Page</button></form>";
                        return View();
                    }
                    else
                    {
                        Message invitation = result.FirstOrDefault();
                        db3.Messages.Remove(invitation);
                        db3.SaveChanges();
                    }
                }

                using (KSUCornerDBEntities2 db2 = new KSUCornerDBEntities2())
                {
                    var result3 = from p in db2.Profiles where (p.UserName == sender) select p;
                    if (result3.Count() == 0)
                    {
                        ViewBag.Message = "The Sender of that Membership Request no longer has a valid account.";
                        ViewBag.Message3 = "<button type=\"button\"" +
                                     "onclick=\"window.location='/Home/Groups'\">" +
                                            "Return To Groups Page</button></form>";
                        return View();
                    }
                }

                string groupName = "";
                using (KSUCornerDBEntities7 db7 = new KSUCornerDBEntities7())
                {
                    var groups = from g in db7.Groups where (g.GroupID == ID) select g;
                    if (groups.Count() == 0)
                    {
                        ViewBag.Message = "That Group No Longer Exists.";
                        ViewBag.Message3 = "<button type=\"button\"" +
                                     "onclick=\"window.location='/Home/Groups'\">" +
                                            "Return To Groups Page</button></form>";
                        return View();
                    }
                    else
                    {
                        Group group = groups.FirstOrDefault();
                        groupName = group.Name;
                    }
                }

                using (KSUCornerDBEntities3 db3 = new KSUCornerDBEntities3())
                {
                    Message message = new Message();
                    message.SentBy = "[NoReply]";
                    message.SentTo = sender;
                    message.Subject = "Your Membership Request Was " + answer;
                    message.Body = "This is to inform you that your Membership Request to join \"" +
                                        groupName + "\" was " + answer.ToLower() + ".";
                    message.MessageType = "FYI";
                    message.MessageStatus = "Unread";
                    message.CreateDate = DateTime.Now;
                    message.OpenedDate = null;

                    db3.Messages.Add(message);
                    db3.SaveChanges();
                }

                if (answer == "Accepted")
                {
                    using (KSUCornerDBEntities8 db8 = new KSUCornerDBEntities8())
                    {
                        var members = from m in db8.GroupMembers
                                      where
                                          (m.GroupID == ID && m.AccountID == requesterID)
                                      select m;
                        if (members.Count() == 0)
                        {
                            GroupMember member = new GroupMember();
                            member.GroupID = ID;
                            member.AccountID = requesterID;
                            member.IsAdmin = false;
                            member.CreateDate = DateTime.Now;

                            db8.GroupMembers.Add(member);
                            db8.SaveChanges();

                            ViewBag.Message = "The new Member has been successfully added to your group.";
                            ViewBag.Message3 = "<button type=\"button\"" +
                                         "onclick=\"window.location='/Home/Messaging'\">" +
                                                "Return To My Messages</button></form>";
                            return View();
                        }
                    }
                }
            }
            catch (Exception err)
            {
                ViewBag.Message = "Error With Membership Request: " + err.Message;
                ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
                ViewBag.Message3 = "<button type=\"button\"" +
                             "onclick=\"window.location='/Home/Messaging'\">" +
                                    "Return To My Messages</button></form>";
                return View();
            }

            ViewBag.Message = "The Membership Request you have declined has been removed.";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Messaging'\">" +
                                "Return To My Messages</button></form>";
            return View();
        }

        public void SetRequestViewData(string userName, string groupName, int groupID)
        {
            ViewBag.Message = "Membership Request:";
            ViewBag.Content1 = "\"" + groupName + "\" group";
            ViewBag.Content2 = GetFullname(userName, false) + " would like to join your group";
            ViewBag.Label1 = "Send Request";
            ViewBag.Label2 = "To";
            ViewBag.Label3 = "Subject";
            ViewBag.Label4 = "Type";
            ViewBag.Label5 = "Optional Note";
            ViewBag.GroupID = groupID;
        }

        private void AddGroupMember(int GroupID, int AccountID, bool IsAdmin)
        {
            using (KSUCornerDBEntities8 db8 = new KSUCornerDBEntities8())
            {
                var members = from m in db8.GroupMembers
                              where (m.GroupID == GroupID && m.AccountID == AccountID)
                              select m;
                if (members.Count() == 0)
                {
                    try
                    {
                        GroupMember member = new GroupMember();
                        member.GroupID = GroupID;
                        member.AccountID = AccountID;
                        member.IsAdmin = IsAdmin;
                        member.CreateDate = DateTime.Now;
                        db8.GroupMembers.Add(member);
                        db8.SaveChanges();
                    }
                    catch { }
                }
            }
        }

        private string GroupMemberStatus(int GroupID, int AccountID)
        {
            using (KSUCornerDBEntities8 db8 = new KSUCornerDBEntities8())
            {
                var members = from m in db8.GroupMembers
                              where (m.GroupID == GroupID && m.AccountID == AccountID)
                              select m;
                if (members.Count() > 0)
                    return (members.FirstOrDefault().IsAdmin) ? "IsAdmin" : "IsMember";
                else
                {
                    using (KSUCornerDBEntities7 db7 = new KSUCornerDBEntities7())
                    {
                        var result = from g in db7.Groups where (g.GroupID == GroupID) select g;
                        if (result.Count() > 0 && result.FirstOrDefault().IsPublic)
                            return "IsPublic";
                    }
                }
                return "";
            }
        }

        private bool IsGroupMediaFile(int FileID)
        {
            using (KSUCornerDBEntities6 db6 = new KSUCornerDBEntities6())
            {
                var files = from f in db6.MediaFiles where (f.FileID == FileID) select f;
                if (files.Count() > 0)
                    return (files.FirstOrDefault().FileFolderID == -1);
                return false;
            }
        }

        [Authorize]
        public ActionResult MembershipRequestSent()
        {
            ViewBag.Message = "Your Membership Request Was Sent!";
            ViewBag.Message2 = "<img src=\"/Images/validemail.jpg\" />";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Groups'\">" +
                                "Return To Groups Page</button></form>";
            return View();
        }

        [Authorize]
        public ActionResult GroupLoadError()
        {
            ViewBag.Message = "Error Loading Group Data - Not Found.";
            ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Groups'\">" +
                                "Return To Groups Page</button></form>";
            return View();
        }

        [Authorize]
        public ActionResult GroupLoadPermissionError()
        {
            ViewBag.Message = "Error - Illegal Operation.";
            ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Groups'\">" +
                                "Return To Groups Page</button></form>";
            return View();
        }

        public ActionResult ErrorMessage()
        {
            string message = Session["ErrorMessage"].ToString();
            if (message.Length > 3 && message.Substring(0, 3) == "Ex:")
            {
                message = message.Substring(3).Trim();
                int n = -1;
                if (message.ToLower().IndexOf("inner exception") > -1)
                {
                    message = Session["InnerException"].ToString();
                    var match = System.Text.RegularExpressions.Regex.Match(
                                                         message, "\\.\\s");
                    if (match.Success)
                    {
                        n = match.Index;
                        match = System.Text.RegularExpressions.Regex.Match(
                                        message.Substring(n + 1), "\\.\\s");
                        if (match.Success)
                            n += match.Index + 1;
                        if (message.Substring(0, n + 1).ToLower().
                                            IndexOf("inner exception") > -1)
                        {
                            match = System.Text.RegularExpressions.Regex.Match(
                                          message.Substring(n + 1), "\\.\\s");
                            if (match.Success)
                                n += match.Index + 1;
                        }

                    }
                }
                message = "Error: " + ((n < 1) ? message :
                                                 message.Substring(0, n + 1));
            }
            ViewBag.Message = message;
            ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Index'\">" +
                                "Return To Home Page</button></form>";
            return View();
        }

        [Authorize]
        public ActionResult GroupErrorMessage()
        {
            ViewBag.Message = Session["ErrorMessage"];
            ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Groups'\">" +
                                "Return To Groups Page</button></form>";
            return View();
        }



        [Authorize]
        public ActionResult GroupForum(int ID)
        {
            if (GroupMemberStatus(ID, GetAccountID(User.Identity.GetUserName())) == "")
                return RedirectToAction("GroupLoadError");

            string label = "";
            using (KSUCornerDBEntities7 db7 = new KSUCornerDBEntities7())
            {
                var result = from f in db7.Groups where (f.GroupID == ID) select f;
                if (result.Count() > 0)
                {
                    Group groupForum = result.FirstOrDefault();
                    label = groupForum.Name;
                    ViewBag.Status = "";
                }
                else
                {
                    Session["ErrorMessage"] = "Error: Group Forum Not Found.";
                    return RedirectToAction("GroupErrorMessage");
                }
            }
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                List<ForumListItem> forumList = new List<ForumListItem>();
                var forums = from f in db10.Forums where (f.GroupID == ID) select f;
                label += " - " + forums.Count() + " Topic";
                if (forums.Count() != 1)
                    label += "s";

                ViewBag.Message = "View Group Forum:";
                ViewBag.Label1 = label;
                ViewBag.ID = -(ID + 1);

                forums = forums.OrderBy(f2 => f2.CreateDate);
                foreach (var f3 in forums)
                {
                    ForumListItem fItem = new ForumListItem();
                    fItem.id = f3.ForumID;
                    fItem.isMain = false;
                    fItem.title = f3.Title;
                    fItem.description = f3.Description;
                    string username = GetAccountUserName(f3.AccountID);
                    fItem.createdBy = GetAvatarLink(username, 40, 40) + " " + username;
                    fItem.createdOn = f3.CreateDate;
                    fItem.count = 0;
                    using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
                    {
                        var replys = from r in db11.ForumReplies where (r.ForumID == f3.ForumID) select r;
                        fItem.count = replys.Count();
                    }
                    fItem.count2 = f3.ViewCount;
                    forumList.Add(fItem);
                }
                return View(forumList.ToList());
            }
        }

        [Authorize]
        public ActionResult Forums(string sortOrder)
        {
            SetForumsViewData();
            ViewBag.NameSortParm = (sortOrder == "Name" ? "Name desc" : "Name");
            ViewBag.DescriptionSortParm = (sortOrder == "Description" ? "Description desc" : "Description");
            ViewBag.ReplySortParm = (sortOrder == "Reply" ? "Reply desc" : "Reply");
            ViewBag.CreatedBySortParm = (sortOrder == "CreatedBy" ? "CreatedBy desc" : "CreatedBy");
            ViewBag.LastPostSortParm = (sortOrder == "LastPost" ? "LastPost desc" : "LastPost");
            ViewBag.DateSortParm = String.IsNullOrEmpty(sortOrder) ? "Date desc" : "";

            using (KSUCornerDBEntities9 db9 = new KSUCornerDBEntities9())
            {
                List<ForumListItem> forumListTemp1 = new List<ForumListItem>();
                var categories = from f1 in db9.ForumCategories select f1;
                foreach (var c1 in categories)
                {
                    ForumListItem fItem = new ForumListItem();
                    fItem.id = c1.CategoryID;
                    fItem.isMain = true;
                    fItem.title = c1.Name;
                    fItem.description = c1.Description;
                    fItem.createdBy = GetAccountUserName(c1.AccountID);
                    fItem.createdOn = c1.CreateDate;
                    fItem.count = 0;
                    fItem.count2 = 0;
                    fItem.lastPost = new DateTime();
                    using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
                    {
                        var forums = from f2 in db10.Forums where (f2.CategoryID == c1.CategoryID) select f2;
                        fItem.count = forums.Count();
                        if (forums.Count() > 0)
                        {
                            forums = forums.OrderByDescending(f3 => f3.CreateDate);
                            bool first = true;
                            foreach (var f4 in forums)
                            {
                                if (first)
                                    fItem.lastPost = f4.CreateDate;
                                first = false;
                                using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
                                {
                                    var replys = from r in db11.ForumReplies
                                                 where (r.ForumID == f4.ForumID)
                                                 select r;
                                    fItem.count2 += replys.Count();
                                }
                            }
                        }
                    }
                    forumListTemp1.Add(fItem);
                }
                IEnumerable<ForumListItem> forumListTemp2 = null;
                switch (sortOrder)
                {
                    case "Name desc":
                        forumListTemp2 = forumListTemp1.OrderByDescending(s => s.title);
                        break;
                    case "Name":
                        forumListTemp2 = forumListTemp1.OrderBy(s => s.title);
                        break;
                    case "Description desc":
                        forumListTemp2 = forumListTemp1.OrderByDescending(s => s.description);
                        break;
                    case "Description":
                        forumListTemp2 = forumListTemp1.OrderBy(s => s.description);
                        break;
                    case "CreatedBy desc":
                        forumListTemp2 = forumListTemp1.OrderByDescending(s => s.createdBy);
                        break;
                    case "CreatedBy":
                        forumListTemp2 = forumListTemp1.OrderBy(s => s.createdBy);
                        break;
                    case "Reply desc":
                        forumListTemp2 = forumListTemp1.OrderByDescending(s => s.count2);
                        break;
                    case "Reply":
                        forumListTemp2 = forumListTemp1.OrderBy(s => s.count2);
                        break;
                    case "LastPost desc":
                        forumListTemp2 = forumListTemp1.OrderByDescending(s => s.lastPost);
                        break;
                    case "LastPost":
                        forumListTemp2 = forumListTemp1.OrderBy(s => s.lastPost);
                        break;
                    case "Date desc":
                        forumListTemp2 = forumListTemp1.OrderByDescending(s => s.createdOn);
                        break;
                    default:
                        forumListTemp2 = forumListTemp1.OrderBy(s => s.createdOn);
                        break;
                }

                List<ForumListItem> forumList = new List<ForumListItem>();
                foreach (var f5 in forumListTemp2)
                {
                    ForumListItem fItem = new ForumListItem();
                    fItem.id = f5.id;
                    fItem.isMain = true;
                    fItem.title = f5.title;
                    fItem.description = f5.description;
                    fItem.createdBy = f5.createdBy;
                    fItem.createdOn = f5.createdOn;
                    fItem.lastPost = f5.lastPost;
                    fItem.count = f5.count;
                    fItem.count2 = f5.count2;
                    forumList.Add(fItem);

                    using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
                    {
                        var forums = from f6 in db10.Forums where (f6.CategoryID == f5.id) select f6;
                        if (sortOrder == "LastPost desc")
                            forums = forums.OrderByDescending(f7 => f7.CreateDate);
                        else
                            forums = forums.OrderBy(f7 => f7.CreateDate);
                        foreach (var f8 in forums)
                        {
                            fItem = new ForumListItem();
                            fItem.id = f8.ForumID;
                            fItem.isMain = false;
                            fItem.title = f8.Title;
                            fItem.description = f8.Description;
                            fItem.createdBy = GetAccountUserName(f8.AccountID);
                            fItem.createdOn = f8.CreateDate;
                            fItem.lastPost = f8.CreateDate;
                            fItem.count = 0;
                            fItem.count2 = 0;
                            using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
                            {
                                var replys = from r in db11.ForumReplies where (r.ForumID == f8.ForumID) select r;
                                fItem.count2 = replys.Count();
                            }
                            forumList.Add(fItem);
                        }
                    }
                }
                return View(forumList.ToList());
            }
        }

        [Authorize]
        public ActionResult NewForumCategory()
        {
            SetForumCategoryViewData(true);
            return View(new ForumCategory());
        }

        [HttpPost, Authorize]
        public ActionResult NewForumCategory(string button, ForumCategory model)
        {
            if (button == "Cancel")
            {
                return RedirectToAction("Forums");
            }
            SetForumCategoryViewData(true);
            using (KSUCornerDBEntities9 db9 = new KSUCornerDBEntities9())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Creating Forum Category:";
                        return View("NewForumCategory", model);
                    }

                    model.AccountID = GetAccountID(User.Identity.GetUserName());
                    model.CreateDate = DateTime.Now;
                    model.LastUpdateDate = model.CreateDate;

                    db9.ForumCategories.Add(model);
                    db9.SaveChanges();

                    return RedirectToAction("Forums");
                }
                catch
                {
                    ViewBag.Message = "Error Creating Forum Category:";
                    return View("NewForumCategory", model);
                }
            }
        }

        [Authorize]
        public ActionResult EditForumCategory(int ID)
        {
            SetForumCategoryViewData(false);
            using (KSUCornerDBEntities9 db9 = new KSUCornerDBEntities9())
            {
                var result = from f in db9.ForumCategories where (f.CategoryID == ID) select f;
                if (result.Count() > 0)
                {
                    ForumCategory category = result.FirstOrDefault();
                    if (category.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        return RedirectToAction("ForumErrorMessage");
                    }
                    return View(category);
                }
                else
                {
                    Session["ErrorMessage"] = "Error: Forum Category Not Found.";
                    return RedirectToAction("ForumErrorMessage");
                }
            }
        }

        [HttpPost, Authorize]
        public ActionResult EditForumCategory(ForumCategory model)
        {
            SetForumCategoryViewData(false);
            using (KSUCornerDBEntities9 db9 = new KSUCornerDBEntities9())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Editing Forum Category:";
                        return View("EditForumCategory", model);
                    }

                    ForumCategory category = db9.ForumCategories.First(f => f.CategoryID == model.CategoryID);
                    if (category.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        return RedirectToAction("ForumErrorMessage");
                    }
                    category.Name = model.Name;
                    category.Description = model.Description;
                    category.LastUpdateDate = DateTime.Now;
                    db9.SaveChanges();

                    return RedirectToAction("OpenForumCategory", new { id = model.CategoryID });
                }
                catch
                {
                    ViewBag.Message = "Error Editing Forum Category:";
                    return View("EditForumCategory", model);
                }
            }
        }

        [Authorize]
        public ActionResult OpenForumCategory(int ID)
        {
            string label = "";
            using (KSUCornerDBEntities9 db9 = new KSUCornerDBEntities9())
            {
                var result = from f in db9.ForumCategories where (f.CategoryID == ID) select f;
                if (result.Count() > 0)
                {
                    ForumCategory category = result.FirstOrDefault();
                    label = category.Name;
                    ViewBag.Status =
                        (category.AccountID == GetAccountID(User.Identity.GetUserName())) ? "Owner" : "";
                }
                else
                {
                    Session["ErrorMessage"] = "Error: Forum Category Not Found.";
                    return RedirectToAction("ForumErrorMessage");
                }
            }
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                List<ForumListItem> forumList = new List<ForumListItem>();
                var forums = from f in db10.Forums where (f.CategoryID == ID) select f;
                label += " - " + forums.Count() + " Topic";
                if (forums.Count() != 1)
                    label += "s";

                ViewBag.Message = "View Forum:";
                ViewBag.Label1 = label;
                ViewBag.ID = ID;

                forums = forums.OrderBy(f2 => f2.CreateDate);
                foreach (var f3 in forums)
                {
                    ForumListItem fItem = new ForumListItem();
                    fItem.id = f3.ForumID;
                    fItem.isMain = false;
                    fItem.title = f3.Title;
                    fItem.description = f3.Description;
                    string username = GetAccountUserName(f3.AccountID);
                    fItem.createdBy = GetAvatarLink(username, 40, 40) + " " + username;
                    fItem.createdOn = f3.CreateDate;
                    fItem.count = 0;
                    using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
                    {
                        var replys = from r in db11.ForumReplies where (r.ForumID == f3.ForumID) select r;
                        fItem.count = replys.Count();
                    }
                    fItem.count2 = f3.ViewCount;
                    forumList.Add(fItem);
                }
                return View(forumList.ToList());
            }
        }

        [Authorize]
        public ActionResult DeleteForumCategory(int ID)
        {
            using (KSUCornerDBEntities9 db9 = new KSUCornerDBEntities9())
            {
                var result = from f in db9.ForumCategories where (f.CategoryID == ID) select f;
                if (result.Count() > 0)
                {
                    ForumCategory category = result.FirstOrDefault();
                    if (category.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        return RedirectToAction("ForumErrorMessage");
                    }

                    ViewBag.Message = "Are you sure you want to delete the following Forum:";
                    ViewBag.Message2 = "<h2>\"" + category.Name + "\" </h2>";
                    ViewBag.Message3 =
                      "<form action=\"/Home/DeleteForumCategory/" + category.CategoryID + "\" method=\"post\">";
                    ViewBag.Message3 += Environment.NewLine;
                    ViewBag.Message3 +=
                      "<input type=\"image\" src=\"/Images/delete.jpg\" value=\"Submit\" alt=\"Submit\">";
                    ViewBag.Message3 += Environment.NewLine + "<p></p>" + Environment.NewLine;
                    ViewBag.Message3 +=
                      "<input type=\"submit\" value=\"Delete This Forum\" name=\"button\" />";
                    ViewBag.Message3 += "        ";
                    ViewBag.Message3 +=
                      "<button type=\"button\" onclick=\"window.location='/Home/Forums'\">Cancel</button></form>";
                    return View();
                }
                else
                {
                    Session["ErrorMessage"] = "Error: Forum Category Not Found.";
                    return RedirectToAction("ForumErrorMessage");
                }
            }
        }

        [HttpPost, Authorize]
        public ActionResult DeleteForumCategory(int ID, FormCollection form)
        {
            using (KSUCornerDBEntities9 db9 = new KSUCornerDBEntities9())
            {
                try
                {
                    ForumCategory category = db9.ForumCategories.First(f => f.CategoryID == ID);
                    if (category.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        return RedirectToAction("ForumErrorMessage");
                    }
                    db9.ForumCategories.Remove(category);
                    db9.SaveChanges();

                    using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
                    {
                        var forums = from f1 in db10.Forums where (f1.CategoryID == ID) select f1;
                        if (forums.Count() > 0)
                        {
                            foreach (var f2 in forums)
                            {
                                db10.Forums.Remove(f2);
                                using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
                                {
                                    var replys = from r1 in db11.ForumReplies
                                                 where (r1.ForumID == f2.ForumID)
                                                 select r1;
                                    if (replys.Count() > 0)
                                    {
                                        foreach (var r2 in replys)
                                        {
                                            db11.ForumReplies.Remove(r2);
                                        }
                                        db11.SaveChanges();
                                    }
                                }
                            }
                            db10.SaveChanges();
                        }
                    }
                    return RedirectToAction("Forums");
                }
                catch (Exception err)
                {
                    Session["DeleteError"] = err.Message;
                    Session["DeleteType"] = "Forum Category";
                    return RedirectToAction("DeleteObjectError");
                }
            }
        }

        [Authorize]
        public ActionResult NewForum(int ID)
        {
            Forum forum = new Forum();
            if (ID < 0)
            {
                forum.CategoryID = -1;
                forum.GroupID = -ID - 1;
            }
            else
            {
                forum.CategoryID = ID;
                forum.GroupID = -1;
            }
            SetForumTopicViewData(true, forum.CategoryID, forum.GroupID);
            return View(forum);
        }

        [HttpPost, Authorize]
        public ActionResult NewForum(Forum model)
        {
            SetForumTopicViewData(true, model.CategoryID, model.GroupID);
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Creating Forum Topic:";
                        return View("NewForum", model);
                    }

                    model.AccountID = GetAccountID(User.Identity.GetUserName());
                    model.ViewCount = 0;
                    model.PublicBlog = false;
                    model.CreateDate = DateTime.Now;
                    model.LastUpdateDate = model.CreateDate;

                    db10.Forums.Add(model);
                    db10.SaveChanges();

                    if (model.CategoryID > -1)
                        return RedirectToAction("OpenForumCategory", new { id = model.CategoryID });
                    else
                        return RedirectToAction("GroupForum", new { id = model.GroupID });
                }
                catch
                {
                    ViewBag.Message = "Error Creating Forum Topic:";
                    return View("NewForum", model);
                }
            }
        }

        [Authorize]
        public ActionResult EditForum(int ID)
        {
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                var result = from f in db10.Forums where (f.ForumID == ID) select f;
                if (result.Count() > 0)
                {
                    Forum forum = result.FirstOrDefault();
                    if (forum.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        return RedirectToAction("ForumErrorMessage");
                    }
                    SetForumTopicViewData(false, forum.CategoryID, forum.GroupID);
                    return View(forum);
                }
                else
                {
                    Session["ErrorMessage"] = "Error: Forum Topic Not Found.";
                    return RedirectToAction("ForumErrorMessage");
                }
            }
        }

        [HttpPost, Authorize]
        public ActionResult EditForum(Forum model)
        {
            SetForumTopicViewData(false, model.CategoryID, model.GroupID);
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Editing Forum Topic:";
                        return View("EditForum", model);
                    }

                    Forum forum = db10.Forums.First(f => f.ForumID == model.ForumID);
                    if (forum.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        return RedirectToAction("ForumErrorMessage");
                    }
                    forum.Title = model.Title;
                    forum.Description = model.Description;
                    forum.Body = model.Body;
                    forum.LastUpdateDate = DateTime.Now;
                    db10.SaveChanges();

                    return RedirectToAction("OpenForum", new { id = model.ForumID });
                }
                catch
                {
                    ViewBag.Message = "Error Editing Forum Topic:";
                    return View("EditForum", model);
                }
            }
        }

        [Authorize]
        public ActionResult OpenForum(int ID)
        {
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                List<ForumListItem> forumList = new List<ForumListItem>();
                int userID = GetAccountID(User.Identity.GetUserName()), categoryID, groupID;
                string username, title;
                ForumListItem fItem;

                try
                {
                    Forum forum = db10.Forums.First(f => f.ForumID == ID);

                    if (forum.AccountID != userID)
                    {
                        forum.ViewCount++;
                        db10.SaveChanges();
                        ViewBag.Status = "";
                    }
                    else
                    {
                        ViewBag.Status = "Owner";
                    }

                    fItem = new ForumListItem();
                    fItem.id = ID;
                    fItem.isMain = true;
                    fItem.title = forum.Description;
                    fItem.description = forum.Body;
                    username = GetAccountUserName(forum.AccountID);
                    fItem.createdBy = GetAvatarLink(username, 40, 40) + " " + username;
                    fItem.createdOn = forum.CreateDate;
                    fItem.count = (forum.AccountID == userID) ? 1 : 0;
                    forumList.Add(fItem);

                    categoryID = forum.CategoryID;
                    groupID = forum.GroupID;
                    title = forum.Title;
                }
                catch (Exception err)
                {
                    Session["ErrorMessage"] = "Error: " + err.Message;
                    return RedirectToAction("ForumErrorMessage");
                }

                if (categoryID > -1)
                {
                    using (KSUCornerDBEntities9 db9 = new KSUCornerDBEntities9())
                    {
                        var result2 = from f2 in db9.ForumCategories where (f2.CategoryID == categoryID) select f2;
                        if (result2.Count() == 0)
                        {
                            Session["ErrorMessage"] = "Error: Forum Category Removed.";
                            return RedirectToAction("ForumErrorMessage");
                        }
                        ViewBag.Label1 = result2.FirstOrDefault().Name + " - " + title;
                    }
                }
                else
                {
                    using (KSUCornerDBEntities7 db7 = new KSUCornerDBEntities7())
                    {
                        var result2 = from f2 in db7.Groups where (f2.GroupID == groupID) select f2;
                        if (result2.Count() == 0)
                        {
                            Session["ErrorMessage"] = "Error: Forum Group Removed.";
                            return RedirectToAction("GroupErrorMessage");
                        }
                        ViewBag.Label1 = result2.FirstOrDefault().Name + " - " + title;
                    }
                }
                ViewBag.Message = "Forum Article:";
                ViewBag.Label2 = "Description";
                ViewBag.Label3 = "Replies";
                ViewBag.Label4 = "Subject";
                ViewBag.ID = ID;
                ViewBag.CategoryID = categoryID;
                ViewBag.GroupID = groupID;

                using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
                {
                    var replys = from r in db11.ForumReplies where (r.ForumID == ID) select r;
                    replys = replys.OrderByDescending(r2 => r2.CreateDate);

                    foreach (var r3 in replys)
                    {
                        fItem = new ForumListItem();
                        fItem.id = r3.ReplyID;
                        fItem.isMain = false;
                        fItem.title = r3.Subject;
                        fItem.description = r3.Body;
                        username = GetAccountUserName(r3.AccountID);
                        fItem.createdBy = GetAvatarLink(username, 40, 40) + " " + username;
                        fItem.createdOn = r3.CreateDate;
                        fItem.count = (r3.AccountID == userID) ? 1 : 0;
                        forumList.Add(fItem);
                    }
                }
                return View(forumList.ToList());
            }
        }

        [Authorize]
        public ActionResult DeleteForum(int ID)
        {
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                var result = from f in db10.Forums where (f.ForumID == ID) select f;
                if (result.Count() > 0)
                {
                    Forum forum = result.FirstOrDefault();
                    if (forum.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        return RedirectToAction("ForumErrorMessage");
                    }

                    ViewBag.Message = "Are you sure you want to delete the following Article:";
                    ViewBag.Message2 = "<h2>\"" + forum.Title + "\" </h2>";
                    ViewBag.Message3 =
                      "<form action=\"/Home/DeleteForum/" + forum.ForumID + "\" method=\"post\">";
                    ViewBag.Message3 += Environment.NewLine;
                    ViewBag.Message3 +=
                      "<input type=\"image\" src=\"/Images/delete.jpg\" value=\"Submit\" alt=\"Submit\">";
                    ViewBag.Message3 += Environment.NewLine + "<p></p>" + Environment.NewLine;
                    ViewBag.Message3 +=
                      "<input type=\"submit\" value=\"Delete This Article\" name=\"button\" />";
                    ViewBag.Message3 += "        ";

                    if (forum.CategoryID > -1)
                        ViewBag.Message3 +=
                           "<button type=\"button\" onclick=\"window.location='/Home/Forums'\">Cancel</button></form>";
                    else
                        ViewBag.Message3 +=
                           "<button type=\"button\" onclick=\"window.location='/Home/GroupForum/" +
                                     forum.GroupID + "'\">Cancel</button></form>";
                    return View();
                }
                else
                {
                    Session["ErrorMessage"] = "Error: Forum Article Not Found.";
                    return RedirectToAction("ForumErrorMessage");
                }
            }
        }

        [HttpPost, Authorize]
        public ActionResult DeleteForum(int ID, FormCollection form)
        {
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                try
                {
                    Forum forum = db10.Forums.First(f => f.ForumID == ID);
                    if (forum.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        return RedirectToAction("ForumErrorMessage");
                    }
                    int categoryID = forum.CategoryID;
                    int groupID = forum.GroupID;
                    db10.Forums.Remove(forum);
                    db10.SaveChanges();

                    using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
                    {
                        var replys = from r1 in db11.ForumReplies where (r1.ForumID == ID) select r1;
                        if (replys.Count() > 0)
                        {
                            foreach (var r2 in replys)
                            {
                                db11.ForumReplies.Remove(r2);
                            }
                            db11.SaveChanges();
                        }
                    }
                    if (categoryID > -1)
                        return RedirectToAction("Forums");
                    else
                        return RedirectToAction("GroupForum", new { id = groupID });
                }
                catch (Exception err)
                {
                    Session["DeleteError"] = err.Message;
                    Session["DeleteType"] = "Article";
                    return RedirectToAction("DeleteObjectError");
                }
            }
        }

        [Authorize]
        public ActionResult NewReply(int ID)
        {
            ForumReply reply = new ForumReply();
            reply.ForumID = ID;
            SetReplyViewData(true, GetForumTitleType(ID));
            string forumType = GetForumType(ID);
            ViewBag.ForumType = forumType;
            Session["ForumType"] = forumType;
            return View(reply);
        }

        [HttpPost, Authorize]
        public ActionResult NewReply(ForumReply model)
        {
            if (IllegalBlogReply(model.ForumID))
            {
                Session["ErrorMessage"] = "Error: Permission Denied.";
                return RedirectToAction("BlogErrorMessage");
            }

            SetReplyViewData(true, GetForumTitleType(model.ForumID));
            string forumType = (string)Session["ForumType"];
            if (String.IsNullOrEmpty(forumType))
                forumType = GetForumType(model.ForumID);
            ViewBag.ForumType = forumType;

            using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Creating Forum Reply:";
                        return View("NewReply", model);
                    }

                    model.AccountID = GetAccountID(User.Identity.GetUserName());
                    model.CreateDate = DateTime.Now;
                    model.LastUpdateDate = model.CreateDate;

                    db11.ForumReplies.Add(model);
                    db11.SaveChanges();

                    if (forumType == "Blog1")
                        return RedirectToAction("OpenPrivateBlog", new { id = model.ForumID });
                    if (forumType == "Blog2")
                        return RedirectToAction("OpenPublishedBlog", new { id = model.ForumID });
                    return RedirectToAction("OpenForum", new { id = model.ForumID });
                }
                catch
                {
                    ViewBag.Message = "Error Creating Forum Reply:";
                    return View("NewReply", model);
                }
            }
        }

        [Authorize]
        public ActionResult EditReply(int ID)
        {
            using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
            {
                var result = from r in db11.ForumReplies where (r.ReplyID == ID) select r;
                if (result.Count() > 0)
                {
                    ForumReply reply = result.FirstOrDefault();
                    SetReplyViewData(false, GetForumTitleType(reply.ForumID));
                    string forumType = GetForumType(reply.ForumID);
                    ViewBag.ForumType = forumType;
                    Session["ForumType"] = forumType;

                    if (reply.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        if (forumType == "Blog1" || forumType == "Blog2")
                            return RedirectToAction("BlogErrorMessage");
                        return RedirectToAction("ForumErrorMessage");
                    }
                    return View(reply);
                }
                else
                {
                    Session["ErrorMessage"] = "Error: Forum Reply Not Found.";
                    return RedirectToAction("ForumErrorMessage");
                }
            }
        }

        [HttpPost, Authorize]
        public ActionResult EditReply(ForumReply model)
        {
            if (IllegalBlogReply(model.ForumID))
            {
                Session["ErrorMessage"] = "Error: Permission Denied.";
                return RedirectToAction("BlogErrorMessage");
            }

            SetReplyViewData(false, GetForumTitleType(model.ForumID));
            string forumType = (string)Session["ForumType"];
            if (String.IsNullOrEmpty(forumType))
                forumType = GetForumType(model.ForumID);
            ViewBag.ForumType = forumType;

            using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Editing Forum Reply:";
                        return View("EditReply", model);
                    }

                    ForumReply reply = db11.ForumReplies.First(r => r.ReplyID == model.ReplyID);
                    if (reply.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        if (forumType == "Blog1" || forumType == "Blog2")
                            return RedirectToAction("BlogErrorMessage");
                        return RedirectToAction("ForumErrorMessage");
                    }
                    reply.Subject = model.Subject;
                    reply.Body = model.Body;
                    reply.LastUpdateDate = DateTime.Now;
                    db11.SaveChanges();

                    if (forumType == "Blog1")
                        return RedirectToAction("OpenPrivateBlog", new { id = model.ForumID });
                    if (forumType == "Blog2")
                        return RedirectToAction("OpenPublishedBlog", new { id = model.ForumID });
                    return RedirectToAction("OpenForum", new { id = model.ForumID });
                }
                catch
                {
                    ViewBag.Message = "Error Editing Forum Reply:";
                    return View("EditReply", model);
                }
            }
        }

        [Authorize]
        public ActionResult DeleteReply(int ID)
        {
            using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
            {
                var result = from r in db11.ForumReplies where (r.ReplyID == ID) select r;
                if (result.Count() > 0)
                {
                    ForumReply reply = result.FirstOrDefault();
                    string forumType = GetForumType(reply.ForumID);
                    if (reply.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        if (forumType == "Blog1" || forumType == "Blog2")
                            return RedirectToAction("BlogErrorMessage");
                        return RedirectToAction("ForumErrorMessage");
                    }
                    Session["ForumType"] = forumType;

                    ViewBag.Message = "Are you sure you want to delete the following Reply:";
                    ViewBag.Message2 = "<h2>\"" + reply.Subject + "\" </h2>";
                    ViewBag.Message3 =
                      "<form action=\"/Home/DeleteReply/" + reply.ReplyID + "\" method=\"post\">";
                    ViewBag.Message3 += Environment.NewLine;
                    ViewBag.Message3 +=
                      "<input type=\"image\" src=\"/Images/delete.jpg\" value=\"Submit\" alt=\"Submit\">";
                    ViewBag.Message3 += Environment.NewLine + "<p></p>" + Environment.NewLine;
                    ViewBag.Message3 +=
                      "<input type=\"submit\" value=\"Delete This Reply\" name=\"button\" />";
                    ViewBag.Message3 += "        ";
                    if (forumType == "Blog1")
                        ViewBag.Message3 +=
                            "<button type=\"button\" onclick=\"window.location='/Home/OpenPrivateBlog/" + reply.ForumID +
                            "'\">Cancel</button></form>";
                    else if (forumType == "Blog2")
                        ViewBag.Message3 +=
                            "<button type=\"button\" onclick=\"window.location='/Home/OpenPublishedBlog/" + reply.ForumID +
                            "'\">Cancel</button></form>";
                    else
                        ViewBag.Message3 +=
                            "<button type=\"button\" onclick=\"window.location='/Home/OpenForum/" + reply.ForumID +
                            "'\">Cancel</button></form>";
                    return View();
                }
                else
                {
                    Session["ErrorMessage"] = "Error: Forum Reply Not Found.";
                    return RedirectToAction("ForumErrorMessage");
                }
            }
        }

        [HttpPost, Authorize]
        public ActionResult DeleteReply(int ID, FormCollection form)
        {
            using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
            {
                try
                {
                    ForumReply reply = db11.ForumReplies.First(r => r.ReplyID == ID);

                    if (IllegalBlogReply(reply.ForumID))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        return RedirectToAction("BlogErrorMessage");
                    }

                    string forumType = (string)Session["ForumType"];
                    if (String.IsNullOrEmpty(forumType))
                        forumType = GetForumType(reply.ForumID);
                    if (reply.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        if (forumType == "Blog1" || forumType == "Blog2")
                            return RedirectToAction("BlogErrorMessage");
                        return RedirectToAction("ForumErrorMessage");
                    }
                    int forumID = reply.ForumID;

                    db11.ForumReplies.Remove(reply);
                    db11.SaveChanges();

                    if (forumType == "Blog1")
                        return RedirectToAction("OpenPrivateBlog", new { id = forumID });
                    if (forumType == "Blog2")
                        return RedirectToAction("OpenPublishedBlog", new { id = forumID });
                    return RedirectToAction("OpenForum", new { id = forumID });
                }
                catch (Exception err)
                {
                    Session["DeleteError"] = err.Message;
                    Session["DeleteType"] = "Forum Reply";
                    return RedirectToAction("DeleteObjectError");
                }
            }
        }

        [Authorize]
        public ActionResult Blogging()
        {
            ViewBag.Message = "Blogging:";
            ViewBag.Label1 = "<b>Choose Which Blogs To View</b>";
            ViewBag.Label2 = "<img src=\"/Images/graylockclosed.png\" style=\"border:none;\" " +
                                           "alt=\"Private Blogs\"><br>My Private Blogs";
            ViewBag.Label3 = "<img src=\"/Images/graylockopened.png\" style=\"border:none;\" " +
                                           "alt=\"Published Blogs\"><br>All Published Blogs</a>";
            return View();
        }

        [Authorize]
        public ActionResult PrivateBlogs(string sortOrder)
        {
            SetBlogViewData(true);
            ViewBag.TitleSortParm = (sortOrder == "Title" ? "Title desc" : "Title");
            ViewBag.SubjectSortParm = (sortOrder == "Subject" ? "Subject desc" : "Subject");
            ViewBag.ReplySortParm = (sortOrder == "Reply" ? "Reply desc" : "Reply");
            ViewBag.ViewsSortParm = (sortOrder == "Views" ? "Views desc" : "Views");
            ViewBag.PublishedSortParm = (sortOrder == "Published" ? "Published desc" : "Published");
            ViewBag.DateSortParm = String.IsNullOrEmpty(sortOrder) ? "Date desc" : "";

            int userID = GetAccountID(User.Identity.GetUserName());
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                var forums = from f in db10.Forums
                             where (f.CategoryID == -1 &&
                                    f.GroupID == -1 &&
                                    f.AccountID == userID)
                             select f;
                List<ForumListItem> forumList = new List<ForumListItem>();
                ForumListItem fItem;
                foreach (var f2 in forums)
                {
                    fItem = new ForumListItem();
                    fItem.id = f2.ForumID;
                    fItem.isMain = f2.PublicBlog;
                    fItem.title = f2.Title;
                    fItem.description = f2.Description;
                    fItem.createdBy = GetAccountUserName(f2.AccountID);
                    fItem.createdOn = f2.CreateDate;
                    fItem.lastPost = f2.CreateDate;
                    fItem.count = 0;
                    using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
                    {
                        var replys = from r in db11.ForumReplies where (r.ForumID == f2.ForumID) select r;
                        fItem.count = replys.Count();
                    }
                    fItem.count2 = f2.ViewCount;
                    forumList.Add(fItem);
                }

                IEnumerable<ForumListItem> forumList2 = null;
                switch (sortOrder)
                {
                    case "Title desc":
                        forumList2 = forumList.OrderByDescending(s => s.title);
                        break;
                    case "Title":
                        forumList2 = forumList.OrderBy(s => s.title);
                        break;
                    case "Subject desc":
                        forumList2 = forumList.OrderByDescending(s => s.description);
                        break;
                    case "Subject":
                        forumList2 = forumList.OrderBy(s => s.description);
                        break;
                    case "Reply desc":
                        forumList2 = forumList.OrderByDescending(s => s.count);
                        break;
                    case "Reply":
                        forumList2 = forumList.OrderBy(s => s.count);
                        break;
                    case "Views desc":
                        forumList2 = forumList.OrderByDescending(s => s.count2);
                        break;
                    case "Views":
                        forumList2 = forumList.OrderBy(s => s.count2);
                        break;
                    case "Published desc":
                        forumList2 = forumList.OrderByDescending(s => s.isMain);
                        break;
                    case "Published":
                        forumList2 = forumList.OrderBy(s => s.isMain);
                        break;
                    case "Date desc":
                        forumList2 = forumList.OrderByDescending(s => s.createdOn);
                        break;
                    default:
                        forumList2 = forumList.OrderBy(s => s.createdOn);
                        break;
                }
                return View(forumList2.ToList());
            }
        }

        [Authorize]
        public ActionResult PublishedBlogs(string sortOrder)
        {
            SetBlogViewData(false);
            ViewBag.TitleSortParm = (sortOrder == "Title" ? "Title desc" : "Title");
            ViewBag.SubjectSortParm = (sortOrder == "Subject" ? "Subject desc" : "Subject");
            ViewBag.ReplySortParm = (sortOrder == "Reply" ? "Reply desc" : "Reply");
            ViewBag.ViewsSortParm = (sortOrder == "Views" ? "Views desc" : "Views");
            ViewBag.CreatedBySortParm = (sortOrder == "CreatedBy" ? "CreatedBy desc" : "CreatedBy");
            ViewBag.DateSortParm = String.IsNullOrEmpty(sortOrder) ? "Date desc" : "";

            int userID = GetAccountID(User.Identity.GetUserName());
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                var forums = from f in db10.Forums
                             where (f.CategoryID == -1 &&
                                    f.GroupID == -1 &&
                                    f.PublicBlog == true)
                             select f;
                List<ForumListItem> forumList = new List<ForumListItem>();
                ForumListItem fItem;
                foreach (var f2 in forums)
                {
                    fItem = new ForumListItem();
                    fItem.id = f2.ForumID;
                    fItem.isMain = true;
                    fItem.title = f2.Title;
                    fItem.description = f2.Description;
                    fItem.createdBy = GetAccountUserName(f2.AccountID);
                    fItem.createdOn = f2.CreateDate;
                    fItem.lastPost = f2.CreateDate;
                    fItem.count = 0;
                    using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
                    {
                        var replys = from r in db11.ForumReplies where (r.ForumID == f2.ForumID) select r;
                        fItem.count = replys.Count();
                    }
                    fItem.count2 = f2.ViewCount;
                    forumList.Add(fItem);
                }

                IEnumerable<ForumListItem> forumList2 = null;
                switch (sortOrder)
                {
                    case "Title desc":
                        forumList2 = forumList.OrderByDescending(s => s.title);
                        break;
                    case "Title":
                        forumList2 = forumList.OrderBy(s => s.title);
                        break;
                    case "Subject desc":
                        forumList2 = forumList.OrderByDescending(s => s.description);
                        break;
                    case "Subject":
                        forumList2 = forumList.OrderBy(s => s.description);
                        break;
                    case "Reply desc":
                        forumList2 = forumList.OrderByDescending(s => s.count);
                        break;
                    case "Reply":
                        forumList2 = forumList.OrderBy(s => s.count);
                        break;
                    case "Views desc":
                        forumList2 = forumList.OrderByDescending(s => s.count2);
                        break;
                    case "Views":
                        forumList2 = forumList.OrderBy(s => s.count2);
                        break;
                    case "CreatedBy desc":
                        forumList2 = forumList.OrderByDescending(s => s.createdBy);
                        break;
                    case "CreatedBy":
                        forumList2 = forumList.OrderBy(s => s.createdBy);
                        break;
                    case "Date desc":
                        forumList2 = forumList.OrderByDescending(s => s.createdOn);
                        break;
                    default:
                        forumList2 = forumList.OrderBy(s => s.createdOn);
                        break;
                }
                return View(forumList2.ToList());
            }
        }

        [Authorize]
        public ActionResult NewBlog()
        {
            Forum forum = new Forum();
            SetBlogForumData(true);
            return View(forum);
        }

        [HttpPost, Authorize]
        public ActionResult NewBlog(Forum model)
        {
            SetBlogForumData(true);
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Creating Blog:";
                        return View("NewBlog", model);
                    }

                    model.AccountID = GetAccountID(User.Identity.GetUserName());
                    model.CategoryID = -1;
                    model.GroupID = -1;
                    model.ViewCount = 0;
                    model.CreateDate = DateTime.Now;
                    model.LastUpdateDate = model.CreateDate;

                    db10.Forums.Add(model);
                    db10.SaveChanges();

                    return RedirectToAction("PrivateBlogs");
                }
                catch
                {
                    ViewBag.Message = "Error Creating Blog:";
                    return View("NewBlog", model);
                }
            }
        }

        [Authorize]
        public ActionResult EditBlog(int ID)
        {
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                var result = from f in db10.Forums
                             where (f.ForumID == ID &&
                                    f.CategoryID == -1 &&
                                    f.GroupID == -1)
                             select f;
                if (result.Count() > 0)
                {
                    Forum forum = result.FirstOrDefault();
                    if (forum.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        return RedirectToAction("BlogErrorMessage");
                    }
                    SetBlogForumData(false);
                    return View(forum);
                }
                else
                {
                    Session["ErrorMessage"] = "Error: Blog Not Found.";
                    return RedirectToAction("BlogErrorMessage");
                }
            }
        }

        [HttpPost, Authorize]
        public ActionResult EditBlog(Forum model)
        {
            SetBlogForumData(false);
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Message = "Error Editing Blog:";
                        return View("EditBlog", model);
                    }

                    Forum forum = db10.Forums.First(f => f.ForumID == model.ForumID &&
                                                         f.CategoryID == -1 && f.GroupID == -1);
                    if (forum.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        return RedirectToAction("BlogErrorMessage");
                    }
                    forum.Title = model.Title;
                    forum.Description = model.Description;
                    forum.Body = model.Body;
                    forum.PublicBlog = model.PublicBlog;
                    forum.LastUpdateDate = DateTime.Now;
                    db10.SaveChanges();

                    return RedirectToAction("OpenPrivateBlog", new { id = model.ForumID });
                }
                catch
                {
                    ViewBag.Message = "Error Editing Blog:";
                    return View("EditBlog", model);
                }
            }
        }

        [Authorize]
        public ActionResult DeleteBlog(int ID)
        {
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                var result = from f in db10.Forums
                             where (f.ForumID == ID &&
                                    f.CategoryID == -1 &&
                                    f.GroupID == -1)
                             select f;
                if (result.Count() > 0)
                {
                    Forum forum = result.FirstOrDefault();
                    if (forum.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        return RedirectToAction("BlogErrorMessage");
                    }

                    ViewBag.Message = "Are you sure you want to delete the following Blog:";
                    ViewBag.Message2 = "<h2>\"" + forum.Title + "\" </h2>";
                    ViewBag.Message3 =
                      "<form action=\"/Home/DeleteBlog/" + forum.ForumID + "\" method=\"post\">";
                    ViewBag.Message3 += Environment.NewLine;
                    ViewBag.Message3 +=
                      "<input type=\"image\" src=\"/Images/delete.jpg\" value=\"Submit\" alt=\"Submit\">";
                    ViewBag.Message3 += Environment.NewLine + "<p></p>" + Environment.NewLine;
                    ViewBag.Message3 +=
                      "<input type=\"submit\" value=\"Delete This Blog\" name=\"button\" />";
                    ViewBag.Message3 += "        ";
                    ViewBag.Message3 +=
                           "<button type=\"button\" onclick=\"window.location='/Home/PrivateBlogs'\">Cancel</button></form>";
                    return View();
                }
                else
                {
                    Session["ErrorMessage"] = "Error: Blog Not Found.";
                    return RedirectToAction("BlogErrorMessage");
                }
            }
        }

        [HttpPost, Authorize]
        public ActionResult DeleteBlog(int ID, FormCollection form)
        {
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                try
                {
                    Forum forum = db10.Forums.First(f => f.ForumID == ID &&
                                                         f.CategoryID == -1 && f.GroupID == -1);
                    if (forum.AccountID != GetAccountID(User.Identity.GetUserName()))
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        return RedirectToAction("BlogErrorMessage");
                    }
                    db10.Forums.Remove(forum);
                    db10.SaveChanges();

                    using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
                    {
                        var replys = from r1 in db11.ForumReplies where (r1.ForumID == ID) select r1;
                        if (replys.Count() > 0)
                        {
                            foreach (var r2 in replys)
                            {
                                db11.ForumReplies.Remove(r2);
                            }
                            db11.SaveChanges();
                        }
                    }
                    return RedirectToAction("PrivateBlogs");
                }
                catch (Exception err)
                {
                    Session["DeleteError"] = err.Message;
                    Session["DeleteType"] = "Blog";
                    return RedirectToAction("DeleteObjectError");
                }
            }
        }

        [Authorize]
        public ActionResult OpenPrivateBlog(int ID)
        {
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                List<ForumListItem> forumList = new List<ForumListItem>();
                int userID = GetAccountID(User.Identity.GetUserName());
                string username, title;
                ForumListItem fItem;

                try
                {
                    Forum forum = db10.Forums.First(f => f.ForumID == ID);
                    if (forum.AccountID != userID)
                    {
                        Session["ErrorMessage"] = "Error: Permission Denied.";
                        return RedirectToAction("BlogErrorMessage");
                    }
                    else
                    {
                        ViewBag.Status = "Owner";
                    }

                    fItem = new ForumListItem();
                    fItem.id = ID;
                    fItem.isMain = forum.PublicBlog;
                    fItem.title = forum.Description;
                    fItem.description = forum.Body;
                    username = GetAccountUserName(forum.AccountID);
                    fItem.createdBy = GetAvatarLink(username, 40, 40) + " " + username;
                    fItem.createdOn = forum.CreateDate;
                    fItem.count = (forum.AccountID == userID) ? 1 : 0;
                    forumList.Add(fItem);

                    title = forum.Title;
                }
                catch (Exception err)
                {
                    Session["ErrorMessage"] = "Error: " + err.Message;
                    return RedirectToAction("BlogErrorMessage");
                }
                ViewBag.Message = "Blog Article:";
                ViewBag.Label1 = title;
                ViewBag.Label2 = "Description";
                ViewBag.Label3 = "Replies";
                ViewBag.Label4 = "Subject";
                ViewBag.ID = ID;
                ViewBag.CategoryID = -1;
                ViewBag.GroupID = -1;
                ViewBag.Source = "Private";

                using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
                {
                    var replys = from r in db11.ForumReplies where (r.ForumID == ID) select r;
                    replys = replys.OrderByDescending(r2 => r2.CreateDate);

                    foreach (var r3 in replys)
                    {
                        fItem = new ForumListItem();
                        fItem.id = r3.ReplyID;
                        fItem.isMain = false;
                        fItem.title = r3.Subject;
                        fItem.description = r3.Body;
                        username = GetAccountUserName(r3.AccountID);
                        fItem.createdBy = GetAvatarLink(username, 40, 40) + " " + username;
                        fItem.createdOn = r3.CreateDate;
                        fItem.count = (r3.AccountID == userID) ? 1 : 0;
                        forumList.Add(fItem);
                    }
                }
                return View(forumList.ToList());
            }
        }

        [Authorize]
        public ActionResult OpenPublishedBlog(int ID)
        {
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                List<ForumListItem> forumList = new List<ForumListItem>();
                int userID = GetAccountID(User.Identity.GetUserName());
                string username, title;
                ForumListItem fItem;

                try
                {
                    Forum forum = db10.Forums.First(f => f.ForumID == ID);

                    if (forum.AccountID != userID)
                    {
                        forum.ViewCount++;
                        db10.SaveChanges();
                        ViewBag.Status = "";
                    }
                    else
                    {
                        ViewBag.Status = "Owner";
                    }

                    fItem = new ForumListItem();
                    fItem.id = ID;
                    fItem.isMain = forum.PublicBlog;
                    fItem.title = forum.Description;
                    fItem.description = forum.Body;
                    username = GetAccountUserName(forum.AccountID);
                    fItem.createdBy = GetAvatarLink(username, 40, 40) + " " + username;
                    fItem.createdOn = forum.CreateDate;
                    fItem.count = (forum.AccountID == userID) ? 1 : 0;
                    forumList.Add(fItem);

                    title = forum.Title;
                }
                catch (Exception err)
                {
                    Session["ErrorMessage"] = "Error: " + err.Message;
                    return RedirectToAction("BlogErrorMessage");
                }
                ViewBag.Message = "Blog Article:";
                ViewBag.Label1 = title;
                ViewBag.Label2 = "Description";
                ViewBag.Label3 = "Replies";
                ViewBag.Label4 = "Subject";
                ViewBag.ID = ID;
                ViewBag.CategoryID = -1;
                ViewBag.GroupID = -1;
                ViewBag.Source = "Public";

                using (KSUCornerDBEntities11 db11 = new KSUCornerDBEntities11())
                {
                    var replys = from r in db11.ForumReplies where (r.ForumID == ID) select r;
                    replys = replys.OrderByDescending(r2 => r2.CreateDate);

                    foreach (var r3 in replys)
                    {
                        fItem = new ForumListItem();
                        fItem.id = r3.ReplyID;
                        fItem.isMain = false;
                        fItem.title = r3.Subject;
                        fItem.description = r3.Body;
                        username = GetAccountUserName(r3.AccountID);
                        fItem.createdBy = GetAvatarLink(username, 40, 40) + " " + username;
                        fItem.createdOn = r3.CreateDate;
                        fItem.count = (r3.AccountID == userID) ? 1 : 0;
                        forumList.Add(fItem);
                    }
                }
                return View(forumList.ToList());
            }
        }

        [HttpPost, Authorize]
        public ActionResult MultiDeleteBlogs(string button, FormCollection form)
        {
            if (button == "Create New Blog")
            {
                return RedirectToAction("NewBlog");
            }
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                for (int i = 0; i < form.Count; ++i)
                {
                    string keyName = form.Keys[i];
                    if (keyName.Length > 7 && keyName.Substring(0, 7) == "Delete-")
                    {
                        int idVal = Int32.Parse(keyName.Substring(7));
                        string[] vals = form[i].ToString().Split(',');
                        if (bool.Parse(vals[0]))
                        {
                            try
                            {
                                var result = from x in db10.Forums where (x.ForumID == idVal) select x;
                                if (result.Count() > 0)
                                {
                                    Forum forum = result.FirstOrDefault();
                                    db10.Forums.Remove(forum);
                                    db10.SaveChanges();
                                }
                            }
                            catch (Exception err)
                            {
                                Session["DeleteError"] = err.Message;
                                Session["DeleteType"] = "Blog";
                                return RedirectToAction("DeleteObjectError");
                            }
                        }
                    }
                }
            }
            return RedirectToAction("PrivateBlogs");
        }

        private void SetForumsViewData()
        {
            ViewBag.Message = "Forums:";
            ViewBag.Label1 = "Forum Categories and Titles";
            ViewBag.Label2 = "Forum Description";
            ViewBag.Label3 = "Replies";
            ViewBag.Label4 = "Created By";
            ViewBag.Label5 = "Created On";
            ViewBag.Label6 = "Last Post On";
            ViewBag.Label7 = "Remove";
        }

        private void SetForumCategoryViewData(bool isNew)
        {
            ViewBag.Message = (isNew) ? "Add New Forum Category:" : "Edit Forum Category:";
            ViewBag.Label1 = "Category Details";
            ViewBag.Label2 = "Name";
            ViewBag.Label3 = "Brief Description";
            ViewBag.Status = (isNew) ? "New" : "Old";
        }

        private void SetForumTopicViewData(bool isNew, int categoryID, int groupID)
        {
            string articleStr = (categoryID > -1) ? "Forum Article:" : "Group Article:";
            ViewBag.Message = ((isNew) ? "New " : "Edit ") + articleStr;
            ViewBag.Label1 = "Topic Details";
            ViewBag.Label2 = "Title";
            ViewBag.Label3 = "Description";
            ViewBag.Label4 = "Forum Article";
            ViewBag.Status = (isNew) ? "New" : "Old";
        }

        private void SetReplyViewData(bool isNew, string articleTitleType)
        {
            string titleStr, typeStr;
            int off = articleTitleType.IndexOf(':');
            if (off > -1)
            {
                titleStr = articleTitleType.Substring(0, off);
                typeStr = articleTitleType.Substring(off + 1);
            }
            else
            {
                titleStr = articleTitleType;
                typeStr = "";
            }
            string typeStr2 = (typeStr == "Blog1" || typeStr == "Blog2" || typeStr == "Blog") ? "Blog" : "Forum";
            ViewBag.Message = (isNew) ? ("New " + typeStr2 + " Reply:") : ("Edit " + typeStr2 + " Reply:");
            ViewBag.Label1 = "Responding to \"" + titleStr + "\"";
            ViewBag.Label2 = "Subject";
            ViewBag.Label3 = "Remarks";
            ViewBag.Status = (isNew) ? "New" : "Old";
        }

        private void SetBlogViewData(bool isPrivate)
        {
            ViewBag.Message = ((isPrivate) ? "My" : "Published") + " Blogs:";
            ViewBag.Label1 = "Title";
            ViewBag.Label2 = "Subject";
            ViewBag.Label3 = "Replies";
            ViewBag.Label4 = "Views";
            ViewBag.Label5 = "Created On";
            ViewBag.Label6 = (isPrivate) ? "Published" : "Created by";
            ViewBag.Label7 = "Remove";
            ViewBag.Source = (isPrivate) ? "PrivateBlogs" : "PublishedBlogs";
        }

        private void SetBlogForumData(bool isNew)
        {
            ViewBag.Message = (isNew) ? "New Blog:" : "Edit Blog:";
            ViewBag.Label1 = "Blog Article";
            ViewBag.Label2 = "Title";
            ViewBag.Label3 = "Subject";
            ViewBag.Label4 = "Publish";
            ViewBag.Label5 = "Post To 'Published Blogs'";
            ViewBag.Status = (isNew) ? "New" : "Old";
        }

        [Authorize]
        public ActionResult ForumErrorMessage()
        {
            ViewBag.Message = Session["ErrorMessage"];
            ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Forums'\">" +
                                "Return To Forums Page</button></form>";
            return View();
        }

        [Authorize]
        public ActionResult BlogErrorMessage()
        {
            ViewBag.Message = Session["ErrorMessage"];
            ViewBag.Message2 = "<img src=\"/Images/noaccess.jpg\" />";
            ViewBag.Message3 = "<button type=\"button\"" +
                         "onclick=\"window.location='/Home/Blogging'\">" +
                                "Return To Blogging Page</button></form>";
            return View();
        }

        private string GetForumTitleType(int ID)
        {
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                var result = from f in db10.Forums where (f.ForumID == ID) select f;
                if (result.Count() > 0)
                {
                    Forum forum = result.FirstOrDefault();
                    return forum.Title + ":" + GetForumType(ID);
                }
                return "";
            }
        }

        private string GetForumType(int ID)
        {
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                var result = from f in db10.Forums where (f.ForumID == ID) select f;
                if (result.Count() > 0)
                {
                    Forum forum = result.FirstOrDefault();
                    string forumType = forum.CategoryID > -1 ? "Category" : (forum.GroupID > -1 ? "Group" : "Blog");
                    if (forumType == "Blog")
                    {
                        if (forum.AccountID != GetAccountID(User.Identity.GetUserName()))
                            forumType = "Blog2";
                        else
                            forumType = (Request.UrlReferrer.ToString().ToLower().IndexOf("published") > -1) ?
                                 "Blog2" : "Blog1";
                    }
                    return forumType;
                }
                return "";
            }
        }

        private bool IllegalBlogReply(int ID)
        {
            using (KSUCornerDBEntities10 db10 = new KSUCornerDBEntities10())
            {
                var result = from f in db10.Forums
                             where (f.ForumID == ID && f.CategoryID == -1 && f.GroupID == -1)
                             select f;
                if (result.Count() > 0)
                {
                    Forum forum = result.FirstOrDefault();
                    return (forum.AccountID != GetAccountID(User.Identity.GetUserName()) && !forum.PublicBlog);
                }
                return false;
            }
        }


    }
}