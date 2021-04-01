/**
 * Authors: Daniel Kopta ,Sephora Bateman and corbin Gurnee
 * Homework seven - scaffolding
 * */


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LibraryWebServer;
using LibraryWebServer.Models;
using Remotion.Linq.Clauses;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.Runtime.Serialization;
using Microsoft.IdentityModel.Xml;

namespace LibraryWebServer.Controllers
{
  public class HomeController : Controller
  {
    // WARNING:
    // This very simple web server is designed to be as tiny and simple as possible
    // This is NOT the way to save user data.
    // This will only allow one user of the web server at a time (aside from major security concerns).
    private static string user = "";
    private static int card = -1;

    /// <summary>
    /// Given a Patron name and CardNum, verify that they exist and match in the database.
    /// If the login is successful, sets the global variables "user" and "card"
    /// </summary>
    /// <param name="name">The Patron's name</param>
    /// <param name="cardnum">The Patron's card number</param>
    /// <returns>A JSON object with a single field: "success" with a boolean value:
    /// true if the login is accepted, false otherwise.
    /// </returns>
    [HttpPost]
    public IActionResult CheckLogin(string name, int cardnum)
    {
      // TODO: Fill in. Determine if login is successful or not.
      bool loginSuccessful = false;

            using (Team70LibraryContext db = new Team70LibraryContext())
            {
                var query = from p in db.Patrons
                            select new Tuple<string, uint>(p.Name, p.CardNum);
                foreach (Tuple<string, uint> q in query)
                {
                    if (q.Item1 == name && q.Item2 == cardnum)
                    {
                        loginSuccessful = true;
                    }
                }

            }
            if (!loginSuccessful)
            {
                return Json(new { success = false });
            }
            else
            { 
                user = name;
                card = cardnum;
                return Json(new { success = true });
            }
    }
  

    /// <summary>
    /// Logs a user out. This is implemented for you.
    /// </summary>
    /// <returns>Success</returns>
    [HttpPost]
    public ActionResult LogOut()
    {
      user = "";
      card = -1;
      return Json(new { success = true });
    }

    /// <summary>
    /// Returns a JSON array representing all known books.
    /// Each book should contain the following fields:
    /// {"isbn" (string), "title" (string), "author" (string), "serial" (uint?), "name" (string)}
    /// Every object in the list should have isbn, title, and author.
    /// Books that are not in the Library's inventory (such as Dune) should have a null serial.
    /// The "name" field is the name of the Patron who currently has the book checked out (if any)
    /// Books that are not checked out should have an empty string "" for name.
    /// </summary>
    /// <returns>The JSON representation of the books</returns>
    [HttpPost]
    public ActionResult AllTitles()
    {
            // Titles(isbn,Title,Author), CheckOut, Patron(name) ,Inventory(serial)  


            using (Team70LibraryContext db = new Team70LibraryContext())
            {
                /*
                 * select Titles.ISBN, Title, Author, Inventory.Serial, Name from Titles Left Join Inventory on Titles.ISBN = Inventory.ISBN Left JOIN CheckedOut ON Inventory.Serial = CheckedOut.Serial Left Join Patrons on CheckedOut.CardNum = Patrons.CardNum;
                */

                // not showing up on web
                var query = from t in db.Titles
                            join i in db.Inventory on t.Isbn equals i.Isbn
                            into titleInventory // left join Title and Inventory   

                            from tI in titleInventory.DefaultIfEmpty()
                            join ChOu in db.CheckedOut on tI.Serial equals ChOu.Serial
                            into tICheckedout // Left join temp table(Title and Inventory) with checkedOut  

                            from tIC in tICheckedout.DefaultIfEmpty()
                            join p in db.Patrons on tIC.CardNum equals p.CardNum
                            into tICPatrons // Left Join temp table(title ,inventory,checkout) with patrons

                        from tICP in tICPatrons.DefaultIfEmpty()
                        select new Tuple<string, string, string, string, string>(
                        t.Isbn ?? String.Empty, 
                        t.Title ?? String.Empty,
                        t.Author ?? String.Empty,
                        tI.Serial.ToString() ?? String.Empty,
                        tICP.Name ?? "");
                return Json(query.ToArray());
            }

            //return Json(query.ToArray());

        }

        /// <summary>
        /// Returns a JSON array representing all books checked out by the logged in user 
        /// The logged in user is tracked by the global variable "card".
        /// Every object in the array should contain the following fields:
        /// {"title" (string), "author" (string), "serial" (uint) (note this is not a nullable uint) }
        /// Every object in the list should have a valid (non-null) value for each field.
        /// </summary>
        /// <returns>The JSON representation of the books</returns>
        [HttpPost]
    public ActionResult ListMyBooks()
    {
            /*
                 * select Title, Author, Inventory.Serial from Titles Left Join Inventory on
                 * Titles.ISBN = Inventory.ISBN Left JOIN CheckedOut ON
                 * Inventory.Serial = CheckedOut.Serial Left Join Patrons
                 * on CheckedOut.CardNum = Patrons.CardNum WHERE Name = 'Dan';
                 */
            List<Tuple<string, string, string, string, string>> tester = new List<Tuple<string, string, string, string, string>>();
            using (Team70LibraryContext db = new Team70LibraryContext())
            {
                var query = from t in db.Titles
                            join i in db.Inventory on t.Isbn equals i.Isbn
                            into titleInventory // left join Title and Inventory   

                            from tI in titleInventory
                            join ChOu in db.CheckedOut on tI.Serial equals ChOu.Serial
                            into tICheckedout // Left join temp table(Title and Inventory) with checkedOut  

                            from tIC in tICheckedout
                            join p in db.Patrons on tIC.CardNum equals p.CardNum
                            into tICPatrons // Left Join temp table(title ,inventory,checkout) with patrons

                            from tICP in tICPatrons 
                            select new Tuple<string, string, string, string, string>(
                            t.Isbn ?? String.Empty,
                            t.Title ?? String.Empty,
                            t.Author ?? String.Empty,
                            tI.Serial.ToString() ?? String.Empty,
                            tICP.Name ?? "");
                foreach (Tuple<string, string, string, string, string> q in query)
                {
                    if (q.Item5.Equals(user))
                    {
                        tester.Add(q);
                    }
                }
            }

            return Json(tester.ToArray());
    }


    /// <summary>
    /// Updates the database to represent that
    /// the given book is checked out by the logged in user (global variable "card").
    /// In other words, insert a row into the CheckedOut table.
    /// You can assume that the book is not currently checked out by anyone.
    /// </summary>
    /// <param name="serial">The serial number of the book to check out</param>
    /// <returns>success</returns>
    [HttpPost]
    public ActionResult CheckOutBook(int serial)
    {
            // You may have to cast serial to a (uint)
            
            using (Team70LibraryContext db = new Team70LibraryContext())
            {
                CheckedOut checkedOutBook = new CheckedOut();
                var query = from p in db.Patrons
                            select new Tuple <string,uint>(p.Name,p.CardNum);
                foreach (Tuple<string, uint> q in query)
                {
                    if(q.Item1 == user)
                    {
                        checkedOutBook.CardNum = q.Item2;
                        checkedOutBook.Serial = (uint)serial;
                    }
                }
                db.CheckedOut.Add(checkedOutBook);
                return Json(new { success = true });
            }

            
    }


    /// <summary>
    /// Returns a book currently checked out by the logged in user (global variable "card").
    /// In other words, removes a row from the CheckedOut table.
    /// You can assume the book is checked out by the user.
    /// </summary>
    /// <param name="serial">The serial number of the book to return</param>
    /// <returns>Success</returns>
    [HttpPost]
    public ActionResult ReturnBook(int serial)
    {
            // You may have to cast serial to a (uint)

            using (Team70LibraryContext db = new Team70LibraryContext())
            {

            }
      // You may have to cast serial to a (uint)
      //DELETE FROM CheckedOut WHERE Serial = number;
      return Json(new { success = true });
    }
    
    /*******************************************/
                /****** Do not modify below this line ******/
                /*******************************************/

                /// <summary>
                /// Return the home page.
                /// </summary>
                /// <returns></returns>
                public IActionResult Index()
    {
      if(user == "" && card == -1)
        return View("Login");

      return View();
    }

    /// <summary>
    /// Return the MyBooks page.
    /// </summary>
    /// <returns></returns>
    public IActionResult MyBooks()
    {
      if (user == "" && card == -1)
        return View("Login");

      return View();
    }

    /// <summary>
    /// Return the About page.
    /// </summary>
    /// <returns></returns>
    public IActionResult About()
    {
      ViewData["Message"] = "Your application description page.";

      return View();
    }

    /// <summary>
    /// Return the Login page.
    /// </summary>
    /// <returns></returns>
    public IActionResult Login()
    {
      user = "";
      card = -1;
      
      ViewData["Message"] = "Please login.";

      return View();
    }

    
    /// <summary>
    /// Return the Contact page.
    /// </summary>
    /// <returns></returns>
    public IActionResult Contact()
    {
      ViewData["Message"] = "Your contact page.";

      return View();
    }

    /// <summary>
    /// Return the Error page.
    /// </summary>
    /// <returns></returns>
    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
  }
}

