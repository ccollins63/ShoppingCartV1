using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ShoppingCartV1.Models;

namespace ShoppingCartV1.Controllers
{
    public partial class HomeController : Controller
    {
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

        // Converts table entries for a particular product type into a list of products for the website
        private List<Product> LoadViewTableData(string pType, int viewIndex)
        {
            using (ShoppingCartDBEntities1 db1 = new ShoppingCartDBEntities1())
            {
                var productItems = from wp in db1.Products where (wp.ProductType == pType) select wp;
                int index = 1;
                foreach (var p in productItems)
                {
                    string pathlabel = "";
                    Session[pType + "ProductName" + index] = "";
                    if (String.IsNullOrWhiteSpace(p.ProductName) && String.IsNullOrWhiteSpace(p.ImageFile))
                        pathlabel = "[No Image]";
                    else
                    {
                        if (!String.IsNullOrWhiteSpace(p.ProductName))
                        {
                            pathlabel = p.ProductName.Trim();
                            Session[pType + "ProductName" + index] = pathlabel;
                            if (!String.IsNullOrWhiteSpace(p.ImageFile))
                                pathlabel += ":<br />";
                        }
                        if (!String.IsNullOrWhiteSpace(p.ImageFile))
                            pathlabel += "<img src=\"/Images/" + p.ImageFile.Trim() + "\" alt=\"Store Product Image\" />";
                    }
                    Session[pType + "Path" + index] = pathlabel;
                    Session[pType + "UnitPrice" + index] = p.UnitPrice;
                    int amount;
                    if (Session[pType + "Amount" + index] == null)
                        amount = (p.DefaultAmount >= 0) ? p.DefaultAmount : 0;
                    else
                        amount = Int32.Parse(Session[pType + "Amount" + index].ToString());
                    ViewData["DefaultChoice" + index] = Convert.ToString(amount);
                    ++index;
                }
                Session[pType + "ItemAmount"] = productItems.Count();
                return productItems.ToList();
            }
        }

        // Loads the submission details into session variables for each tab
        public void LoadSubmission(string name, int amount, FormCollection collection)
        {
            for (int i = 1; i <= amount; i++)
            {
                int value;
                if (!Int32.TryParse(collection[i - 1], out value))
                    continue;
                Session[String.Format("{0}Amount{1}", name, i)] = collection[i - 1];
                Session[String.Format("{0}Price{1}", name, i)] =
                    Double.Parse(Session[String.Format("{0}UnitPrice{1}",
                      name, i)].ToString()) * value;
            }
        }

        public ActionResult GetTabView(int tabNum)
        {
            Session["PageHeading"] = orderHeading;
            Session["ProductType"] = tabViews[tabNum];
            ViewBag.Message = Session["Message"] = tabHeadings[tabNum] + " Orders:";
            return View(LoadViewTableData(Session["ProductType"].ToString(), tabNum));
        }

        public ActionResult ProcessTabView(int tabNum, string button, FormCollection collection)
        {
            string pType = Session["ProductType"].ToString();
            int amount = Int32.Parse(Session[pType + "ItemAmount"].ToString());

            LoadSubmission(pType, amount, collection);

            for (int i = 1; i <= amount; i++)
            {
                int value;
                if (!Int32.TryParse(collection[i - 1], out value) || value < 0)
                {
                    ViewBag.Message = "<div style=\"color:#800\">" +
                                       "Error: Invalid entry in Item #" + i +
                                        "</div><br />" + Session["Message"].ToString();
                    return View(pType, LoadViewTableData(pType, tabNum));
                }
            }

            if (button == "Save And Go To Checkout")
            {
                return RedirectToAction("CheckOut");
            }
            else
            {
                // This is the View for the next product page
                return RedirectToAction(tabViews[tabNum + 1]);
            }
        }
    }
}