using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace UrlSnapshot
{
    public class Job
    {
        public string Title;
        public string Url;
        public bool Pass;
        public string Message;
    }

    public class Chrome : IDisposable
    {
        Browser browser = null;
        Page page = null;
        int width, height;

        public Chrome(string chromePath, int width = 1024, int height = 768)
        {
            browser = Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = false,
                ExecutablePath = chromePath
            }).Result;
            page = browser.PagesAsync().Result.First();
            this.width = width;
            this.height = height;
        }

        public void Navigate(Job job)
        {
            try
            {
                page.SetViewportAsync(new ViewPortOptions
                {
                    Width = width,
                    Height = height
                }).Wait();
                var response = page.GoToAsync(job.Url, 30000,
                    waitUntil: new WaitUntilNavigation[] {
                            WaitUntilNavigation.Load,
                            WaitUntilNavigation.Networkidle2
                    }).GetAwaiter().GetResult();
                job.Pass = response.Status == System.Net.HttpStatusCode.OK;
                if (!job.Pass)
                {
                    job.Message = $"** {response.StatusText} **\n{response.TextAsync().Result}";
                }
            }
            catch (Exception ex)
            {
                job.Pass = false;
                job.Message = ex.Message;
            }
        }

        public void TakeSnapshot(string path)
        {
            var h = Convert.ToInt32((float)page.EvaluateExpressionAsync(
                @"Math.max(document.body.scrollHeight, document.documentElement.scrollHeight)").Result);
            var w = Convert.ToInt32((float)page.EvaluateExpressionAsync(
                @"document.body.getBoundingClientRect().width").Result);
            page.SetViewportAsync(new ViewPortOptions
            {
                Width = w,
                Height = h
            }).Wait();
            page.ScreenshotAsync(path).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            if (page != null) page.Dispose();
            if (browser != null) browser.Dispose();
        }
    }
}