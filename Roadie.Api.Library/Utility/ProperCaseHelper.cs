using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Roadie.Library.Utility
{
    public static class ProperCaseHelper
    {
        public static string ToProperCase(this string input)
        {
            if (IsAllUpperOrAllLower(input))
            {
                // fix the ALL UPPERCASE or all lowercase names
                return string.Join(" ", input.Split(' ').Select(word => WordToProperCase(word)));
            }
            else
            {
                // leave the CamelCase or Propercase names alone
                return input;
            }
        }

        public static bool IsAllUpperOrAllLower(this string input)
        {
            return (input.ToLower().Equals(input) || input.ToUpper().Equals(input));
        }

        private static string WordToProperCase(string word)
        {
            if (string.IsNullOrEmpty(word)) return word;

            // Standard case
            string ret = CapitaliseFirstLetter(word);

            // Special cases:
            ret = ProperSuffix(ret, "'");   // D'Artagnon, D'Silva
            ret = ProperSuffix(ret, ".");   // ???
            ret = ProperSuffix(ret, "-");       // Oscar-Meyer-Weiner
            ret = ProperSuffix(ret, "Mc", t => t.Length > 4);      // Scots
            ret = ProperSuffix(ret, "Mac", t => t.Length != 5);     // Scots except Macey

            // Special words:
            ret = SpecialWords(ret, "van");     // Dick van Dyke
            ret = SpecialWords(ret, "von");     // Baron von Bruin-Valt
            ret = SpecialWords(ret, "de");
            ret = SpecialWords(ret, "di");
            ret = SpecialWords(ret, "da");      // Leonardo da Vinci, Eduardo da Silva
            ret = SpecialWords(ret, "of");      // The Grand Old Duke of York
            ret = SpecialWords(ret, "the");     // William the Conqueror
            ret = SpecialWords(ret, "HRH");     // His/Her Royal Highness
            ret = SpecialWords(ret, "HRM");     // His/Her Royal Majesty
            ret = SpecialWords(ret, "H.R.H.");  // His/Her Royal Highness
            ret = SpecialWords(ret, "H.R.M.");  // His/Her Royal Majesty

            ret = DealWithRomanNumerals(ret);   // William Gates, III

            return ret;
        }

        private static string ProperSuffix(string word, string prefix, Func<string, bool> condition = null)
        {
            if (string.IsNullOrEmpty(word)) return word;
            if (condition != null && !condition(word))
            {
                return word;
            }
            string lowerWord = word.ToLower();
            string lowerPrefix = prefix.ToLower();

            if (!lowerWord.Contains(lowerPrefix)) return word;

            int index = lowerWord.IndexOf(lowerPrefix);

            // If the search string is at the end of the word ignore.
            if (index + prefix.Length == word.Length) return word;

            return word.Substring(0, index) + prefix +
                CapitaliseFirstLetter(word.Substring(index + prefix.Length));
        }

        private static string SpecialWords(string word, string specialWord)
        {
            if (word.Equals(specialWord, StringComparison.InvariantCultureIgnoreCase))
            {
                return specialWord;
            }
            else
            {
                return word;
            }
        }

        private static string DealWithRomanNumerals(string word)
        {
            // Roman Numeral parser thanks to [Hannobo](https://stackoverflow.com/users/785111/hannobo)
            // Note that it excludes the Chinese last name Xi
            return new Regex(@"\b(?!Xi\b)(X|XX|XXX|XL|L|LX|LXX|LXXX|XC|C)?(I|II|III|IV|V|VI|VII|VIII|IX)?\b", RegexOptions.IgnoreCase).Replace(word, match => match.Value.ToUpperInvariant());
        }

        private static string CapitaliseFirstLetter(string word)
        {
            return char.ToUpper(word[0]) + word.Substring(1).ToLower();
        }


        static Dictionary<string, string> _exceptions = new Dictionary<string, string>
        {
            {@"\bMacEdo"     ,"Macedo"},
            {@"\bMacEvicius" ,"Macevicius"},
            {@"\bMacHado"    ,"Machado"},
            {@"\bMacHar"     ,"Machar"},
            {@"\bMacHin"     ,"Machin"},
            {@"\bMacHlin"    ,"Machlin"},
            {@"\bMacIas"     ,"Macias"},
            {@"\bMacIulis"   ,"Maciulis"},
            {@"\bMacKie"     ,"Mackie"},
            {@"\bMacKle"     ,"Mackle"},
            {@"\bMacKlin"    ,"Macklin"},
            {@"\bMacKmin"    ,"Mackmin"},
            {@"\bMacQuarie"  ,"Macquarie"},
            {@"\bMacEy "     ,"Macey "}
        };

        static Dictionary<string, string> _replacements = new Dictionary<string, string>
        {
            { "o'reilly", "O'Reilly" }
        };

        static string[] _conjunctions = { "Y", "E", "I" };

        static string _romanRegex = @"\b((?:[Xx]{1,3}|[Xx][Ll]|[Ll][Xx]{0,3})?(?:[Ii]{1,3}|[Ii][VvXx]|[Vv][Ii]{0,3})?)\b";

        /// <summary>
        /// Case a name field into its appropriate case format 
        /// e.g. Smith, de la Cruz, Mary-Jane,  O'Brien, McTaggart
        /// </summary>
        /// <param name="nameString"></param>
        /// <returns></returns>
        public static string NameCase(this string nameString, bool doFixConjuntion = false)
        {
            nameString = Capitalize(nameString);
            nameString = UpdateRoman(nameString);
            nameString = UpdateIrish(nameString);
            if (doFixConjuntion)
            {
                nameString = FixConjunction(nameString);
            }
            nameString = Regex.Replace(nameString, @"('[A-Z])", m => m.ToString().ToLower(), RegexOptions.IgnoreCase);
            foreach (var replacement in _replacements.Keys)
            {
                nameString = nameString.Replace(replacement, _replacements[replacement], StringComparison.OrdinalIgnoreCase);
            }
            return nameString;
        }

        /// <summary>
        /// Capitalize first letters.
        /// </summary>
        /// <param name="nameString"></param>
        /// <returns></returns>
        private static string Capitalize(string nameString)
        {
            nameString = nameString.ToLower();
            nameString = Regex.Replace(nameString, @"\b\w", x => x.ToString().ToUpper());
            nameString = Regex.Replace(nameString, @"'\w\b", x => x.ToString().ToLower()); // Lowercase 's
            return nameString;
        }

        /// <summary>
        /// Update for Irish names.
        /// </summary>
        /// <param name="nameString"></param>
        /// <returns></returns>
        private static string UpdateIrish(string nameString)
        {
            if (Regex.IsMatch(nameString, @".*?\bMac[A-Za-z^aciozj]{2,}\b") || Regex.IsMatch(nameString, @".*?\bMc"))
            {
                nameString = UpdateMac(nameString);
            }
            return nameString;
        }

        /// <summary>
        /// Updates irish Mac & Mc.
        /// </summary>
        /// <param name="nameString"></param>
        /// <returns></returns>
        private static string UpdateMac(string nameString)
        {
            MatchCollection matches = Regex.Matches(nameString, @"\b(Ma?c)([A-Za-z]+)");
            if (matches.Count == 1 && matches[0].Groups.Count == 3)
            {
                string replacement = matches[0].Groups[1].Value;
                replacement += matches[0].Groups[2].Value.Substring(0, 1).ToUpper();
                replacement += matches[0].Groups[2].Value.Substring(1);
                nameString = nameString.Replace(matches[0].Groups[0].Value, replacement);

                // Now fix "Mac" exceptions
                foreach (var exception in _exceptions.Keys)
                {
                    nameString = Regex.Replace(nameString, exception, _exceptions[exception]);
                }
            }
            return nameString;
        }

        /// <summary>
        /// Fix roman numeral names.
        /// </summary>
        /// <param name="nameString"></param>
        /// <returns></returns>
        private static string UpdateRoman(string nameString)
        {
            MatchCollection matches = Regex.Matches(nameString, _romanRegex);
            if (matches.Count > 1)
            {
                foreach (Match match in matches)
                {
                    if (!string.IsNullOrEmpty(match.Value))
                    {
                        nameString = Regex.Replace(nameString, match.Value, x => x.ToString().ToUpper());
                    }
                }
            }
            return nameString;
        }

        /// <summary>
        /// Fix Spanish conjunctions.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        private static string FixConjunction(string nameString)
        {
            foreach (var conjunction in _conjunctions)
            {
                nameString = Regex.Replace(nameString, @"\b" + conjunction + @"\b", x => x.ToString().ToLower());
            }
            return nameString;
        }
    }
}
