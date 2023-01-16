﻿using System.Reflection;
using System.Xml.XPath;
using I18Next.Net.Plugins;
using Sepia.Globalization;
using Sepia.Globalization.Plurals;

namespace BnbnavNetClient.i18n;

public class CldrPluralResolver : IPluralResolver
{
    public string GetPluralSuffix(string language, int count)
    {
        var order = new List<string>
        {
            "zero",
            "one",
            "two",
            "few",
            "many",
            "other"
        };

        //HACK: For some reason German doesn't seem to be working correctly so hardcode the English rules
        var locale = Locale.Create(language.Split("-")[0]);

        var pluralsFile = Assembly.GetExecutingAssembly().GetManifestResourceStream("BnbnavNetClient.I18n.Resources.plurals.xml")!;
        var xp = new XPathDocument(pluralsFile);
        
        var rules = new List<XPathDocument>{xp}
            .Elements("supplementalData/plurals[@type='cardinal']/pluralRules[contains(@locales, '" +
                      locale.Id.Language.ToLowerInvariant() + "')]/pluralRule").Select<XPathNavigator, Rule>(Rule.Parse)
            .OrderBy(x => order.IndexOf(x.Category)).ToList();

        return $"_{rules.FirstOrDefault(rule => rule.Matches(RuleContext.Create(count)))?.Category ?? "other"}";
    }

    public bool NeedsPlural(string language)
    {
        return true;
    }

}