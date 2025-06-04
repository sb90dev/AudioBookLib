using OpenQA.Selenium;
using System.Text;

namespace Scraper.Pages
{
    internal class GoodReadsPage
    {
        private readonly IWebDriver _driver;
        private readonly Uri _bookUrl;

        public GoodReadsPage(IWebDriver driver, string bookId)
        {
            _driver = driver;
            _bookUrl = new Uri($"https://www.goodreads.com/book/show/{bookId}");
        }

        public async Task<GoodReadsData> GetData()
        {
            await _driver.Navigate().GoToUrlAsync(_bookUrl);

            var titleSection = _driver.FindElement(By.ClassName("BookPageTitleSection__title"));            
            var title = titleSection.FindElement(By.ClassName("Text__title1")).Text;
            var seriesEntry = titleSection.FindElements(By.ClassName("Text__title3")).FirstOrDefault()?.Text;

            var authorSection = _driver.FindElement(By.ClassName("BookPageMetadataSection__contributor"));
            var authors = authorSection.FindElements(By.ClassName("ContributorLink__name")).Select(x => x.Text).ToList();

            var ratingSection = _driver.FindElement(By.ClassName("RatingStatistics"));
            var rating = ratingSection.FindElement(By.ClassName("RatingStatistics__rating")).Text;
            var ratingsCount = ratingSection.FindElement(By.ClassName("RatingStatistics__meta")).FindElement(By.XPath(".//span[@data-testid='ratingsCount']")).Text;

            return new(title, authors, seriesEntry, rating, ratingsCount);
        }
    }

    internal record class GoodReadsData(
        string BookTitle, 
        List<string> Authors,
        string? SeriesEntry, 
        string Rating, 
        string RatingsCount)
    {
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Title: {BookTitle}");

            if (Authors.Count > 1)
            {
                builder.AppendLine($"Authors: {string.Join(", ", Authors)}");
            }
            else
            {
                builder.AppendLine($"Author: {Authors[0]}");
            }

            if (SeriesEntry is not null)
            {
                builder.AppendLine($"Series Entry: {SeriesEntry}");
            }

            builder.AppendLine($"Rating: {Rating} ({RatingsCount} ratings)");

            return builder.ToString();
        }
    };
}
