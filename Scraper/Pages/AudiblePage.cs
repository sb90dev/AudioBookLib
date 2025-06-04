using OpenQA.Selenium;
using System.Text;

namespace Scraper.Pages
{
    internal class AudiblePage
    {
        private readonly IWebDriver _driver;
        private readonly Uri _bookUrl;

        public AudiblePage(IWebDriver driver, string bookId)
        {
            _driver = driver;
            _bookUrl = new Uri($"https://www.audible.co.uk/pd/{bookId}");
        }

        public async Task<AudibleData> GetData()
        {
            await _driver.Navigate().GoToUrlAsync(_bookUrl);

            var productSections = _driver.FindElements(By.XPath("//adbl-product-metadata"));

            var narratorSection = productSections[0].GetShadowRoot().FindElements(By.ClassName("line"))[1];
            var narrators = narratorSection.FindElements(By.ClassName("value")).Select(x => x.Text).ToList();

            var lengthSection = productSections[1].GetShadowRoot().FindElements(By.ClassName("line"))[4];
            var length = lengthSection.FindElement(By.ClassName("value")).Text;

            var ratingSection = _driver.FindElement(By.XPath(".//adbl-star-rating")).GetShadowRoot();
            var rating = ratingSection.FindElement(By.ClassName("value-label")).Text;
            var ratingsCount = ratingSection.FindElement(By.ClassName("count-label")).Text;

            return new(narrators, length, rating, ratingsCount);
        }
    }

    internal record class AudibleData(
        List<string> Narrators, 
        string Length, 
        string Rating, 
        string RatingsCount)
    {
        public override string ToString()
        {
            var builder = new StringBuilder();

            if (Narrators.Count > 1)
            {
                builder.AppendLine($"Narrators: {string.Join(", ", Narrators)}");
            }
            else
            {
                builder.AppendLine($"Narrators: {Narrators[0]}");
            }

            builder.AppendLine($"Length: {Length}");

            builder.AppendLine($"Rating: {Rating} ({RatingsCount} ratings)");

            return builder.ToString();
        }
    };
}
