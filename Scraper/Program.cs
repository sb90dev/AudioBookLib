// Goodreads -> Series Entry, Book name, Author, Rating
// Audible -> Narrator, Duration, Rating

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

string goodReadsUrl = "https://www.goodreads.com/book/show/15839976";
string audibleUrl = "https://www.audible.co.uk/pd/Red-Rising-Audiobook/B00I2W0H9W";

var options = new ChromeOptions();
//options.AddArgument("--headless=new");
var driver = new ChromeDriver(options);
driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

await driver.Navigate().GoToUrlAsync(goodReadsUrl);

/*
<div class="BookPageTitleSection__title">
  <h3 class="Text Text__title3 Text__italic Text__regular Text__subdued"><a>Red Rising Saga #1</a></h3> 
  <h1 class="Text Text__title1" data-testid="bookTitle">Red Rising</h1>
</div> 
 */
var titleSection = driver.FindElement(By.ClassName("BookPageTitleSection__title"));
var seriesEntry = titleSection.FindElements(By.ClassName("Text__title3")).FirstOrDefault()?.Text;
var title = titleSection.FindElement(By.ClassName("Text__title1")).Text;
Console.WriteLine("Series Entry: " + seriesEntry);
Console.WriteLine("Title: " + title);

/*
 <div class="BookPageMetadataSection__contributor">
  <h3 class="Text Text__title3 Text__regular" aria-label="By: Pierce Brown">
    <div class="ContributorLinksList">
      <span tabindex="-1">
        <a class="ContributorLink" href="https://www.goodreads.com/author/show/6474348.Pierce_Brown">
          <span class="ContributorLink__name" data-testid="name">Pierce Brown</span>
        </a>
      </span>
      <span tabindex="-1"></span>
    </div>
  </h3>
</div> 
 */
var authorSection = driver.FindElement(By.ClassName("BookPageMetadataSection__contributor"));
var authors = authorSection.FindElements(By.ClassName("ContributorLink__name")).Select(x => x.Text).ToList();
Console.WriteLine(authors.Count > 1 ? "Authors: " : "Author: " + string.Join(", ", authors));

/*
 <a class="RatingStatistics RatingStatistics__interactive RatingStatistics__centerAlign" href="#CommunityReviews">
  <div class="RatingStatistics__column" aria-label="Average rating of 4.27 stars." role="figure">
    <div class="RatingStatistics__rating" aria-hidden="true">4.27</div>
  </div>
  <div class="RatingStatistics__column">
    <div class="RatingStatistics__meta" aria-label="624,599 ratings and 66,395 reviews" role="figure">
      <span data-testid="ratingsCount" aria-hidden="true">624,599&nbsp;ratings</span>
      <span data-testid="reviewsCount" class="u-dot-before" aria-hidden="true">66,395&nbsp;reviews</span>
    </div>
  </div>
</a>
 */
var ratingSection = driver.FindElement(By.ClassName("RatingStatistics"));
var rating = ratingSection.FindElement(By.ClassName("RatingStatistics__rating")).Text;
var ratingsCount = ratingSection.FindElement(By.ClassName("RatingStatistics__meta")).FindElement(By.XPath(".//span[contains(@data-testid, 'ratingsCount')]")).Text;
Console.WriteLine("Rating: " + rating);
Console.WriteLine("Ratings Count: " + ratingsCount);

await driver.Navigate().GoToUrlAsync(audibleUrl);

var productSections = driver.FindElements(By.XPath("//adbl-product-metadata"));

var narratorSection = productSections[0].GetShadowRoot().FindElements(By.ClassName("line"))[1];
var narrators = narratorSection.FindElements(By.ClassName("value")).Select(x => x.Text).ToList();
Console.WriteLine(narrators.Count > 1 ? "Narrators: " : "Narrator: " + string.Join(", ", narrators));

var lengthSection = productSections[1].GetShadowRoot().FindElements(By.ClassName("line"))[4];
var length = lengthSection.FindElement(By.ClassName("value")).Text;
Console.WriteLine("Length: " + length);

var adblRatingSection = driver.FindElement(By.XPath(".//adbl-star-rating")).GetShadowRoot();
var adblRating = adblRatingSection.FindElement(By.ClassName("value-label")).Text;
var adblRatingsCount = adblRatingSection.FindElement(By.ClassName("count-label")).Text;
Console.WriteLine("Rating: " + adblRating);
Console.WriteLine("Ratings Count: " + adblRatingsCount);

Console.ReadLine();