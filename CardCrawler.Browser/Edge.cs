using Microsoft.Win32;
using PuppeteerSharp;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace CardCrawler.Browser
{
    public class Edge
    {

        private static readonly LaunchOptions launchOptions = new()
        {
            Headless = true,
            ExecutablePath = GetEdgePath(),
            Args =
                [
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-blink-features=AutomationControlled",
                    "--disable-extensions",
                    "--disable-gpu"
                ]
        };

        /// <summary>
        /// Fetches the HTML content of a given URL.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="retries"></param>
        /// <returns></returns>
        public static async Task<string?> GetPageContent(string url, int retries = 0)
        {

            using IBrowser browser = await Puppeteer.LaunchAsync(launchOptions);
            using IPage page = await browser.NewPageAsync();

            _ = await page.EvaluateFunctionOnNewDocumentAsync(@"() => {
                Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            }");

            await page.SetUserAgentAsync(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/116.0.0.0 Safari/537.36 Edg/116.0.0.0"
            );

            IResponse response = await page.GoToAsync(url, timeout: 0, waitUntil: [WaitUntilNavigation.Networkidle0]);

            Debug.WriteLine($"GetPageContent: {response.Status}");

            if ((int)response.Status == 429)
            {
                if (retries > 5)
                {
                    return null;
                }
                await Task.Delay(15000);
                return await GetPageContent(url, retries++);
            }

            return await page.GetContentAsync();
        }

        public static string GetEdgePath()
        {
            const string regKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe";
            if (Registry.GetValue(regKey, "", null) is string pathFromReg
                && File.Exists(pathFromReg))
            {
                Trace.WriteLine(pathFromReg);
                return pathFromReg;
            }

            string[] candidates =
            [
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft\\Edge\\Application\\msedge.exe"
                ),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Microsoft\\Edge\\Application\\msedge.exe"
                )
            ];
            foreach (string p in candidates)
            {
                if (File.Exists(p))
                {
                    return p;
                }
            }

            return "msedge.exe";
        }

    }
}
