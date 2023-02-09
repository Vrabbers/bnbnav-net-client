﻿using System.Reflection;
using System.Xml.XPath;
using I18Next.Net.Plugins;
using Sepia.Globalization;
using Sepia.Globalization.Plurals;

namespace BnbnavNetClient.I18Next;

public sealed class CldrPluralResolver : IPluralResolver
{
    static readonly string[] SuffixOrder = new[]
    {
        "zero",
        "one",
        "two",
        "few",
        "many",
        "other"
    };

    public string GetPluralSuffix(string language, int count)
    {
        if (language == "pt") language = "en";
        
        var pluralsFile = Assembly.GetAssembly(GetType())!.GetManifestResourceStream("BnbnavNetClient.I18Next.Resources.plurals.xml")!;
        var xp = new XPathDocument(pluralsFile);
        
        var rules = new List<XPathDocument> { xp }
            .Elements("supplementalData/plurals[@type='cardinal']/pluralRules[contains(@locales, '" +
                      language.Split("-")[0].ToLowerInvariant() + "')]/pluralRule").Select<XPathNavigator, Rule>(Rule.Parse)
            .OrderBy(x => Array.IndexOf(SuffixOrder, x.Category)).ToList();

        return $"_{rules.FirstOrDefault(rule => rule.Matches(RuleContext.Create(count)))?.Category ?? "other"}";
    }

    public bool NeedsPlural(string language)
    {
        return true;
    }
}