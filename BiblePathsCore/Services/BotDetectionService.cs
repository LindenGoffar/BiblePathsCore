using System;
using System.Collections.Generic;
using System.Linq;

namespace BiblePathsCore.Services
{
    public interface IBotDetectionService
    {
        bool IsBotUserAgent(string userAgent);
    }

    public class BotDetectionService : IBotDetectionService
    {
        /// <summary>
        /// Known bot and search engine user agent patterns.
        /// These are case-insensitive patterns commonly used by crawlers and search engines.
        /// </summary>
        private static readonly HashSet<string> BotPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Search Engines
            "googlebot",
            "bingbot",
            "slurp",            // Yahoo
            "duckduckbot",
            "baiduspider",      // Baidu
            "yandexbot",
            "facebookexternalhit",
            "twitterbot",
            "linkedinbot",
            "whatsapp",
            "telegram",
            "viber",

            // Web Crawlers & Indexers
            "crawler",
            "spider",
            "scraper",
            "bot",              // Generic bot detection
            "curl",             // Common in automated requests
            "wget",             // Common in automated requests
            "python",           // Python requests library
            "java",             // Java-based crawlers (often contains 'java' in user agent)
            "perl",             // Perl scripts
            "ruby",             // Ruby scripts
            "node",             // Node.js

            // Monitoring & Analytics
            "pingdom",
            "uptime",
            "statuspage",
            "newrelic",
            "datadog",
            "sentry",
            "splunk",

            // SEO Tools
            "semrush",
            "ahrefs",
            "majestic",
            "screaming frog",
            "seobility",

            // Browser Automation
            "selenium",
            "phantom",          // PhantomJS
            "headless",
            "puppeteer",
            "playwright",

            // Other Common Bots
            "adsbot",
            "mediapartners",
            "feedfetcher",
            "ad",               // Various ad crawlers
            "archive.org",
            "wayback",
            "nutch",            // Apache Nutch
            "rogerbot",
            "dotbot",
            "whatruns",
            "okhttp",           // Often indicates automated requests
            "httpclient",       // Often indicates automated requests
        };

        /// <summary>
        /// Checks if the user agent string belongs to a known bot or search engine crawler.
        /// </summary>
        /// <param name="userAgent">The User-Agent header value from the HTTP request</param>
        /// <returns>True if the user agent appears to be from a bot/crawler/search engine, false otherwise</returns>
        public bool IsBotUserAgent(string userAgent)
        {
            // If no user agent provided, give benefit of doubt (likely a real user)
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return false;
            }

            // Check if any bot pattern matches the user agent string (case-insensitive)
            return BotPatterns.Any(pattern => userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }
    }
}
