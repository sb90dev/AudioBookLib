using OpenQA.Selenium.Chrome;
using Scraper.Pages;

string goodReadsBookId = "15839976";
string audibleBookId = "B00I2W0H9W";

var options = new ChromeOptions();
options.AddArgument("--headless=new");
options.AddArgument($"--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36");

var driver = new ChromeDriver(options);
driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

var goodReadsPage = new GoodReadsPage(driver, goodReadsBookId);
var bookData = await goodReadsPage.GetData();
Console.WriteLine(bookData);

var audiblePage = new AudiblePage(driver, audibleBookId);
var audibleData = await audiblePage.GetData();
Console.WriteLine(audibleData);

Console.ReadLine();