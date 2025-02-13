﻿using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Microsoft.Recognizers.Definitions;
using Microsoft.Recognizers.Definitions.Spanish;

namespace Microsoft.Recognizers.Text.Number.Spanish
{
    public class NumberExtractor : BaseNumberExtractor
    {

        private const RegexOptions RegexFlags = RegexOptions.Singleline | RegexOptions.ExplicitCapture;

        private static readonly ConcurrentDictionary<(NumberMode, NumberOptions), NumberExtractor> Instances =
            new ConcurrentDictionary<(NumberMode, NumberOptions), NumberExtractor>();

        private NumberExtractor(NumberMode mode = NumberMode.Default, NumberOptions options = NumberOptions.None)
        {
            NegativeNumberTermsRegex = new Regex(NumbersDefinitions.NegativeNumberTermsRegex + '$', RegexFlags);

            Options = options;

            var builder = ImmutableDictionary.CreateBuilder<Regex, TypeTag>();

            // Add Cardinal
            CardinalExtractor cardExtract = null;
            switch (mode)
            {
                case NumberMode.PureNumber:
                    cardExtract = CardinalExtractor.GetInstance(NumbersDefinitions.PlaceHolderPureNumber);
                    break;
                case NumberMode.Currency:
                    builder.Add(
                        CurrencyRegex,
                        RegexTagGenerator.GenerateRegexTag(Constants.INTEGER_PREFIX, Constants.NUMBER_SUFFIX));
                    break;
                case NumberMode.Default:
                    break;
            }

            if (cardExtract == null)
            {
                cardExtract = CardinalExtractor.GetInstance();
            }

            builder.AddRange(cardExtract.Regexes);

            // Add Fraction
            var fracExtract = FractionExtractor.GetInstance(mode, Options);
            builder.AddRange(fracExtract.Regexes);

            Regexes = builder.ToImmutable();

            var ambiguityBuilder = ImmutableDictionary.CreateBuilder<Regex, Regex>();

            // Do not filter the ambiguous number cases like '$2000' in NumberWithUnit, otherwise they can't be resolved.
            if (mode != NumberMode.Unit)
            {
                foreach (var item in BaseNumbers.AmbiguityFiltersDict)
                {
                    ambiguityBuilder.Add(new Regex(item.Key, RegexFlags), new Regex(item.Value, RegexFlags));
                }

                foreach (var item in NumbersDefinitions.AmbiguityFiltersDict)
                {
                    ambiguityBuilder.Add(new Regex(item.Key, RegexFlags), new Regex(item.Value, RegexFlags));
                }
            }

            AmbiguityFiltersDict = ambiguityBuilder.ToImmutable();
        }

        internal sealed override ImmutableDictionary<Regex, TypeTag> Regexes { get; }

        protected sealed override ImmutableDictionary<Regex, Regex> AmbiguityFiltersDict { get; }

        protected sealed override NumberOptions Options { get; }

        // "Number"
        protected sealed override string ExtractType { get; } = Constants.SYS_NUM;

        protected sealed override Regex NegativeNumberTermsRegex { get; }

        public static NumberExtractor GetInstance(NumberMode mode = NumberMode.Default, NumberOptions options = NumberOptions.None)
        {
            var cacheKey = (mode, options);
            if (!Instances.ContainsKey(cacheKey))
            {
                var instance = new NumberExtractor(mode, options);
                Instances.TryAdd(cacheKey, instance);
            }

            return Instances[cacheKey];
        }
    }
}